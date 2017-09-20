using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;

namespace MyCompany.LanguageServices.MyScript
{
	class MSCompletionSource
		: ICompletionSource
	{
		private MSCompletionSourceProvider m_sourceProvider;
		private ITextBuffer m_textBuffer;
		private SortedList<string, Completion> m_compList;
		private IGlyphService m_glyphService;
		private ITextDocumentFactoryService m_documentService;
		private MSBackgroundParser m_backgroundParser = null;
		private SVsServiceProvider m_serviceProvider = null;
		private IViewTagAggregatorFactoryService m_viewTagAggregatorFactoryService;
		private IStandardClassificationService m_standardClassificationService;
		private MSClassifier m_classifier;

		public MSCompletionSource(MSCompletionSourceProvider
			sourceProvider,
			ITextBuffer textBuffer,
			IGlyphService glyphService,
			ITextDocumentFactoryService documentService,
			SVsServiceProvider serviceProvider,
			IViewTagAggregatorFactoryService viewTagAggregatorFactoryService,
			IStandardClassificationService standardClassificationService)
		{
			m_sourceProvider = sourceProvider;
			m_textBuffer = textBuffer;
			m_glyphService = glyphService;
			m_documentService = documentService;
			m_serviceProvider = serviceProvider;
			m_viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			m_standardClassificationService = standardClassificationService;

			m_compList = new SortedList<string, Completion>();

			m_backgroundParser = MSBackgroundParser.GetOrCreateFromTextBuffer(textBuffer, documentService);

			m_textBuffer.Properties.TryGetProperty<MSClassifier>("MSClassifier", out m_classifier);
		}


		void AddLanguageDefaultCompletionItems(SortedList<string, Completion> completions)
		{
			ImageSource keywordGlyph = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphKeyword, StandardGlyphItem.GlyphItemPublic);

			string name = "bool";
			ImageSource glyph = keywordGlyph;

			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "do";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "else";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "end";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "false";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "float";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "if";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "int";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "null";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "return";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "then";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "true";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "while";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
			name = "string";
			m_compList.Add(name, new Completion(name, name, name + " Keyword", glyph, null));
		}
		
		void AddDocumentationToCompletionList(IList<MSXmlDocumentationFile> docFiles, SortedList<string, Completion> completions)
		{
			foreach (MSXmlDocumentationFile docFile in docFiles)
			{
				foreach (MSXmlFunctionDocumentation function in docFile.Functions)
				{
					StringBuilder sigBuilder = new StringBuilder("function " + function.Name + "(");
					foreach (MSXmlVariableDocumentation arg in function.Arguments)
					{
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

					if(!string.IsNullOrEmpty(function.Description))
					{
						sigBuilder.AppendLine();
						sigBuilder.Append(function.Description);
					}

					ImageSource glyph = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
					if (!m_compList.ContainsKey(function.Name))
						m_compList.Add(function.Name, new Completion(function.Name, function.Name, sigBuilder.ToString(), glyph, null));
				}
			}
		}
		void AddSyntaxTreeToCompletionList(int index, IList<SyntaxNode> syntaxTree, SortedList<string, Completion> completions)
		{
			foreach(SyntaxNode node in syntaxTree)
			{
				if (node is FunctionNode)
				{
					FunctionNode functionNode = node as FunctionNode;

					//	Outside of function boundaries
					if (functionNode.separatorTokens.Count == 0 || index < functionNode.separatorTokens.First().Span.End)
						continue;

					if (functionNode.endToken != null && index > functionNode.endToken.Span.Start)
						continue;

					//	Search for local declarations
					AddSyntaxTreeToCompletionList(index, functionNode.statements, completions);

					//	Search for arguments
					foreach (FunctionNode.Argument arg in functionNode.arguments)
					{
						if (arg.nameExpression != null && arg.nameExpression is NameNode)
						{
							//	Found, lets add content to quick info
							string argType = null;
							string argName = null;

							if (arg.typeExpression != null && arg.typeExpression is TypeNode)
								argType = (arg.typeExpression as TypeNode).token.Text;

							argName = (arg.nameExpression as NameNode).token.Text;

							ImageSource argGlyph = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);
							if (!m_compList.ContainsKey(argName))
								m_compList.Add(argName, new Completion(argName, argName, "(parameter) " + argType + " " + argName, argGlyph, null));
						}
					}

					StringBuilder sigBuilder = new StringBuilder("function ");

					if (functionNode.nameExpression == null ||
						!(functionNode.nameExpression is NameNode))
						continue;

					sigBuilder.Append(" ");
					sigBuilder.Append((functionNode.nameExpression as NameNode).token.Text);

					sigBuilder.Append("(");

					foreach (FunctionNode.Argument arg in functionNode.arguments)
					{
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
					{ 
						sigBuilder.Append(" : ");
						sigBuilder.Append((functionNode.returnTypeExpression as TypeNode).token.Text);
					}

					if(string.IsNullOrEmpty(functionNode.comment))
					{
						sigBuilder.AppendLine();
						sigBuilder.Append(functionNode.comment);
					}

					string name = (functionNode.nameExpression as NameNode).token.Text;
					ImageSource glyph = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
					m_compList.Add(name, new Completion(name, name, sigBuilder.ToString(), glyph, null));
				}
				else if(node is AssignmentNode)
				{
					AssignmentNode assignNode = node as AssignmentNode;

					//	Not has no type, its not a declaration but an assignment. Skip
					if (assignNode.typeExpression == null)
						continue;

					//	Security check
					if (assignNode.varExpression == null || !(assignNode.varExpression is NameNode))
						continue;
					
					NameNode name = assignNode.varExpression as NameNode;
					if (index >= name.token.Span.Start)
					{
						//	Found, lets add content to quick info
						string typeText = null;
						string nameText = null;
						
						if (assignNode.typeExpression != null && assignNode.typeExpression is TypeNode)
							typeText = (assignNode.typeExpression as TypeNode).token.Text;

						if (assignNode.varExpression != null && assignNode.varExpression is NameNode)
							nameText = (assignNode.varExpression as NameNode).token.Text;

						ImageSource glyph = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);
						if (!m_compList.ContainsKey(nameText))
							m_compList.Add(nameText, new Completion(nameText, nameText, nameText + " Keyword", glyph, null));
						
					}
				}
			}
		}

		string GetTextFromTagSpan(IMappingTagSpan<IClassificationTag> tagSpan, ITextView textView)
		{
			SnapshotPoint? start = tagSpan.Span.Start.GetPoint(textView.TextBuffer, PositionAffinity.Predecessor);
			SnapshotPoint? end = tagSpan.Span.End.GetPoint(textView.TextBuffer, PositionAffinity.Predecessor);

			if (!start.HasValue || !end.HasValue)
				return null;
			
			return textView.TextSnapshot.GetText(start.Value, end.Value - start.Value);
		}
		
		bool IsType(MSToken token)
		{
			string s = token.Text;
			if (s == "int" || s == "float" || s == "bool" || s == "string" || s == "void")
				return true;
			else
				return false;
		}
		
		void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
		{
			//	/!\ THIS IS CALLED BEFORE NEW EDIT HAS BEEN CLASSIFIED

			m_compList.Clear();

			SnapshotPoint point = session.GetTriggerPoint(session.TextView.TextBuffer).GetPoint(session.TextView.TextSnapshot) - 1;

			MSRandomAccessLexer lexer = new MSRandomAccessLexer(m_classifier, point.Snapshot);
			lexer.SetPoint(point);

			lexer.PreviousTokenSkipWhitespace();

			MSToken previousToken = lexer.PreviousTokenSkipWhitespace();
			if(previousToken != null && IsType(previousToken))
			{
				//	previous token is a type, so we are typing a name. Dont trigger completion.
				//	If previous token is ':', this was a function declaration, we can trigger completion
				previousToken = lexer.PreviousTokenSkipWhitespace();
				if(previousToken == null || previousToken.Text != ":")
				{
					return;
				}
			}

			ITextSnapshot snapshot = null;
			IList<SyntaxNode> syntaxTree = null;
			IList<MSXmlDocumentationFile> docFiles = null;

			//	Get syntax tree from the parser
			lock (m_backgroundParser.Lock)
			{
				snapshot = m_backgroundParser.Snapshot;
				syntaxTree = m_backgroundParser.SyntaxTree;
				docFiles = m_backgroundParser.XmlDocs;
			}

			//	Get trigger point in parsing snapshot
			SnapshotPoint triggerPoint = session.GetTriggerPoint(m_textBuffer).GetPoint(snapshot);


			if (syntaxTree != null)
				AddSyntaxTreeToCompletionList(triggerPoint, syntaxTree, m_compList);
			if(docFiles != null)
				AddDocumentationToCompletionList(docFiles, m_compList);

			AddLanguageDefaultCompletionItems(m_compList);

			completionSets.Add(new CompletionSet(
				"Tokens",    //the non-localized title of the tab
				"Tokens",    //the display title of the tab
				FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer),
					session),
				m_compList.Values,
				null));
		}
		
		private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session)
		{
			SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
			ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
			TextExtent extent = navigator.GetExtentOfWord(currentPoint);
			return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
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
