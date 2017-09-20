using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace MyCompany.LanguageServices.MyScript
{
	internal class MSParameter : IParameter
	{
		public string Documentation { get; private set; }
		public Span Locus { get; private set; }
		public string Name { get; private set; }
		public ISignature Signature { get; private set; }
		public Span PrettyPrintedLocus { get; private set; }

		public MSParameter(string documentation, Span locus, string name, ISignature signature)
		{
			Documentation = documentation;
			Locus = locus;
			Name = name;
			Signature = signature;
		}
	}

	internal class MSSignature : ISignature
	{
		private ITextBuffer m_subjectBuffer;
		private IParameter m_currentParameter;
		private string m_content;
		private string m_documentation;
		private ITrackingSpan m_applicableToSpan;
		private ReadOnlyCollection<IParameter> m_parameters;
		private string m_printContent;

		internal MSSignature(ITextBuffer subjectBuffer, string content, string doc, ReadOnlyCollection<IParameter> parameters)
		{
			m_subjectBuffer = subjectBuffer;
			m_content = content;
			m_documentation = doc;
			m_parameters = parameters;
			m_subjectBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(OnSubjectBufferChanged);
		}

		public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

		public IParameter CurrentParameter
		{
			get { return m_currentParameter; }
			internal set
			{
				if (m_currentParameter != value)
				{
					IParameter prevCurrentParameter = m_currentParameter;
					m_currentParameter = value;
					this.RaiseCurrentParameterChanged(prevCurrentParameter, m_currentParameter);
				}
			}
		}

		private void RaiseCurrentParameterChanged(IParameter prevCurrentParameter, IParameter newCurrentParameter)
		{
			EventHandler<CurrentParameterChangedEventArgs> tempHandler = this.CurrentParameterChanged;
			if (tempHandler != null)
			{
				tempHandler(this, new CurrentParameterChangedEventArgs(prevCurrentParameter, newCurrentParameter));
			}
		}

		internal void ComputeCurrentParameter()
		{
			if (Parameters.Count == 0)
			{
				this.CurrentParameter = null;
				return;
			}

			//the number of commas in the string is the index of the current parameter
			string sigText = ApplicableToSpan.GetText(m_subjectBuffer.CurrentSnapshot);

			MSLexer lexer = new MSLexer();
			lexer.SetSource(sigText);
			lexer.SetIndex(0);
			lexer.SetState(MSLexerState.None);

			int iParam = 0;
			int level = -1;

			MSToken token = new MSToken();
			while(lexer.GetNextToken(token))
			{
				if (token.Text == "(")
					++level;
				else if (token.Text == ")")
					--level;
				else if (token.Text == "," && level == 0)
					++iParam;
			}
			
			if (iParam < Parameters.Count)
			{
				this.CurrentParameter = Parameters[iParam];
			}
			else
			{
				//too many commas, so use the last parameter as the current one.
				this.CurrentParameter = Parameters.Last();
			}
		}

		internal void OnSubjectBufferChanged(object sender, TextContentChangedEventArgs e)
		{
			this.ComputeCurrentParameter();
		}

		public ITrackingSpan ApplicableToSpan
		{
			get { return (m_applicableToSpan); }
			internal set { m_applicableToSpan = value; }
		}

		public string Content
		{
			get { return (m_content); }
			internal set { m_content = value; }
		}

		public string Documentation
		{
			get { return (m_documentation); }
			internal set { m_documentation = value; }
		}

		public ReadOnlyCollection<IParameter> Parameters
		{
			get { return (m_parameters); }
			internal set { m_parameters = value; }
		}

		public string PrettyPrintedContent
		{
			get { return (m_printContent); }
			internal set { m_printContent = value; }
		}
	}


	internal class MSSignatureHelpSource : ISignatureHelpSource
	{
		private ITextBuffer m_textBuffer;
		private MSClassifier m_classifier;
		private MSBackgroundParser m_backgroundParser;

		public MSSignatureHelpSource(ITextBuffer textBuffer, ITextDocumentFactoryService documentFactoryService)
		{
			m_textBuffer = textBuffer;
			m_textBuffer.Properties.TryGetProperty<MSClassifier>("MSClassifier", out m_classifier);

			m_backgroundParser = MSBackgroundParser.GetOrCreateFromTextBuffer(textBuffer, documentFactoryService);
		}

		bool AddDocumentationSignatures(string name, IList<MSXmlDocumentationFile> docFiles, ITrackingSpan applicableToSpan, IList<ISignature> signatures)
		{
			foreach (MSXmlDocumentationFile docFile in docFiles)
			{
				foreach (MSXmlFunctionDocumentation function in docFile.Functions)
				{
					if (function.Name == name)
					{
						StringBuilder sigBuilder = new StringBuilder("function " + function.Name + "(");

						List<string> argDocs = new List<string>();
						foreach (MSXmlVariableDocumentation arg in function.Arguments)
						{
							argDocs.Add(arg.Description);

							sigBuilder.Append(arg.Type);
							sigBuilder.Append(" ");
							sigBuilder.Append(arg.Name);

							if (arg.Equals(function.Arguments.Last()))
								sigBuilder.Append(")");
							else
								sigBuilder.Append(", ");
						}

						sigBuilder.Append(" : ");
						sigBuilder.Append(function.Type);
												
						signatures.Add(CreateSignature(m_textBuffer, sigBuilder.ToString(), function.Description, argDocs, applicableToSpan));
						return true;
					}
				}
			}

			return false;
		}
		bool AddSyntaxTreeSignatures(string name, SnapshotPoint point, IList<SyntaxNode> syntaxTree, ITrackingSpan applicableToSpan, IList<ISignature> signatures)
		{
			foreach(SyntaxNode node in syntaxTree)
			{
				if(node is FunctionNode)
				{
					FunctionNode functionNode = node as FunctionNode;
					
					if(functionNode.nameExpression == null ||
						!(functionNode.nameExpression is NameNode) ||
						(functionNode.nameExpression as NameNode).token.Text != name)
						continue;

					StringBuilder sigBuilder = new StringBuilder("function ");
					sigBuilder.Append(" ");
					sigBuilder.Append((functionNode.nameExpression as NameNode).token.Text);

					sigBuilder.Append("(");

					List<string> argDocs = new List<string>();
					foreach(FunctionNode.Argument arg in functionNode.arguments)
					{
						argDocs.Add(arg.comment);

						if (arg.typeExpression == null || !(arg.typeExpression is TypeNode))
							continue;

						if (arg.nameExpression == null || !(arg.nameExpression is NameNode))
							continue;

						sigBuilder.Append((arg.typeExpression as TypeNode).token.Text);
						sigBuilder.Append(" ");
						sigBuilder.Append((arg.nameExpression as NameNode).token.Text);

						if (arg.Equals(functionNode.arguments[functionNode.arguments.Count - 1]))
							sigBuilder.Append(")");
						else
							sigBuilder.Append(", ");
					}

					if (functionNode.returnTypeExpression == null || !(functionNode.returnTypeExpression is TypeNode))
					{
						sigBuilder.Append(" : void");
					}
					else
						sigBuilder.Append(" : ");
						sigBuilder.Append((functionNode.returnTypeExpression as TypeNode).token.Text);


					signatures.Add(CreateSignature(m_textBuffer, sigBuilder.ToString(), functionNode.comment, argDocs, applicableToSpan));
					return true;
				}
			}

			return false;
		}

		public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
		{
			ITextSnapshot snapshot = m_textBuffer.CurrentSnapshot;
			SnapshotPoint point = session.GetTriggerPoint(m_textBuffer).GetPoint(snapshot);

			MSRandomAccessLexer lexer = new MSRandomAccessLexer(m_classifier, snapshot);
			lexer.SetPoint(point - 1);

			MSToken token = lexer.PreviousTokenSkipWhitespace();
			if (token == null || (token.Text != "(" && token.Text != ","))
			{
				session.Dismiss();
				return;
			}

			int tokenLimit = 50;

			int level = 1;
			while (token != null)
			{
				if (token.Text == "(")
					--level;
				else if (token.Text == ")")
					++level;
				else if(token.Text == "end" || token.Text == ";" || tokenLimit == 0)
				{
					//	Dismiss session cause we didnt found the function name
					session.Dismiss();
					return;
				}
				if (level == 0)
					break;

				token = lexer.PreviousTokenSkipWhitespace();
				--tokenLimit;
			}

			MSToken nameToken = lexer.PreviousTokenSkipWhitespace();
			MSToken functionToken = lexer.PreviousTokenSkipWhitespace();
			
			if (nameToken != null && functionToken != null && functionToken.Text != "function")
			{
				ITrackingSpan applicableToSpan = m_textBuffer.CurrentSnapshot.CreateTrackingSpan(nameToken.Span.Start, point.Position - nameToken.Span.Start, SpanTrackingMode.EdgeInclusive, 0);
				
				ITextSnapshot treeSnapshot = null;
				IList<SyntaxNode> syntaxTree = null;
				IList<MSXmlDocumentationFile> docFiles = null;

				lock (m_backgroundParser.Lock)
				{
					treeSnapshot = m_backgroundParser.Snapshot;
					syntaxTree = m_backgroundParser.SyntaxTree;
					docFiles = m_backgroundParser.XmlDocs;
				}

				if (syntaxTree == null || treeSnapshot == null)
					return;

				if (!AddSyntaxTreeSignatures(nameToken.Text, point.TranslateTo(treeSnapshot, PointTrackingMode.Negative), syntaxTree, applicableToSpan, signatures))
					AddDocumentationSignatures(nameToken.Text, docFiles, applicableToSpan, signatures);
			}
			else
			{
				session.Dismiss();
				return;
			}

		}

		private MSSignature CreateSignature(ITextBuffer textBuffer, string methodSig, string methodDoc, IList<string> paramDocs, ITrackingSpan span)
		{
			MSSignature sig = new MSSignature(textBuffer, methodSig, methodDoc, null);
			textBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(sig.OnSubjectBufferChanged);

			//find the parameters in the method signature (expect methodname(one, two)
			string[] pars = methodSig.Split(new char[] { '(', ',', ')' });
			List<IParameter> paramList = new List<IParameter>();

			int locusSearchStart = 0;
			for (int i = 1; i < pars.Length - 1; i++)
			{
				string param = pars[i].Trim();

				if (string.IsNullOrEmpty(param))
					continue;

				//find where this parameter is located in the method signature
				int locusStart = methodSig.IndexOf(param, locusSearchStart);
				if (locusStart >= 0)
				{
					Span locus = new Span(locusStart, param.Length);
					locusSearchStart = locusStart + param.Length;

					string paramDoc = null;
					if (paramDocs.Count > i - 1)
						paramDoc = paramDocs[i - 1];

					paramList.Add(new MSParameter(paramDoc, locus, param.Split(null)[1], sig));
				}
			}

			sig.Parameters = new ReadOnlyCollection<IParameter>(paramList);
			sig.ApplicableToSpan = span;
			sig.ComputeCurrentParameter();
			return sig;
		}

		public ISignature GetBestMatch(ISignatureHelpSession session)
		{
			if (session.Signatures.Count > 0)
			{
				ITrackingSpan applicableToSpan = session.Signatures[0].ApplicableToSpan;
				string text = applicableToSpan.GetText(applicableToSpan.TextBuffer.CurrentSnapshot);

				if (text.Trim().Equals("add"))  //get only "add" 
					return session.Signatures[0];
			}
			return null;
		}

		private bool m_isDisposed;
		public void Dispose()
		{
			if (!m_isDisposed)
			{
				GC.SuppressFinalize(this);
				m_isDisposed = true;
			}
		}
	}
}
