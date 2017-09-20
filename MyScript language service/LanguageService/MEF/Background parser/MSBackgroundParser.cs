using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;

namespace MyCompany.LanguageServices.MyScript
{

	class LambdaComparer<T>
		: IEqualityComparer<T>
	{
		public LambdaComparer(Func<T, T, bool> cmp)
		{
			this.cmp = cmp;
		}
		public bool Equals(T x, T y)
		{
			return cmp(x, y);
		}

		public int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}

		public Func<T, T, bool> cmp { get; set; }
	}
	class MSParseResultEventArgs
		: ParseResultEventArgs
	{

	}
	class MSBackgroundParser
		: BackgroundParser
	{
		/*void OnTextChanged(object sender, TextContentChangedEventArgs e)
		{
			this.RequestParse(true);
		}*/

		string m_filename = null;
		string GetFilePath(ITextBuffer textBuffer)
		{
			Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer bufferAdapter;
			textBuffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer), out bufferAdapter);
			if (bufferAdapter != null)
			{
				var persistFileFormat = bufferAdapter as IPersistFileFormat;
				string ppzsFilename = null;
				uint iii;
				if (persistFileFormat != null)
					persistFileFormat.GetCurFile(out ppzsFilename, out iii);
				return ppzsFilename;
			}
			else
				return null;
		}
		public MSBackgroundParser(ITextBuffer textBuffer, TaskScheduler taskScheduler, ITextDocumentFactoryService textDocumentFactoryService) : base(textBuffer, taskScheduler, textDocumentFactoryService)
		{
			this.ReparseDelay = TimeSpan.FromMilliseconds(300);
			//textBuffer.Changed += OnTextChanged;
			this.RequestParse(true);

			m_filename = GetFilePath(textBuffer);
		}

		private string GetAbsoluteFilename(string relativePath)
		{
			return Path.GetFullPath(Path.GetDirectoryName(m_filename) + "\\" + relativePath);
		}
		private bool DocumentationAlreadyLoaded(string filename)
		{
			foreach (MSXmlDocumentationFile docFile in m_xmlDocs)
			{
				if (docFile.Filename == filename)
					return true;
			}

			return false;
		}

		string EvaluateString(StringNode node)
		{
			StringBuilder sb = new StringBuilder();

			bool escaped = false;

			if (!(node.token.Text.StartsWith("\"") && node.token.Text.EndsWith("\"") && !node.token.Text.EndsWith("\\\"")))
				return null;

			for(int i = 1; i<node.token.Text.Length - 1; ++i)
			{
				char c = node.token.Text[i];
				if (c == '\\')
				{
					escaped = true;
				}
				else if(escaped)
				{
					if (c == 'a')
						sb.Append('\a');
					else if (c == 'b')
						sb.Append('\b');
					else if (c == 'f')
						sb.Append('\f');
					else if (c == 'n')
						sb.Append('\n');
					else if (c == 'r')
						sb.Append('\r');
					else if (c == 't')
						sb.Append('\t');
					else if (c == 'v')
						sb.Append('\v');
					else if (c == '\'')
						sb.Append('\'');
					else if (c == '"')
						sb.Append('\"');
					else if (c == '\\')
						sb.Append('\\');
					else if (c == '?')
						sb.Append('?');
					else
						sb.Append(c);
				}
				else
				{
					sb.Append(c);
				}
			}

			return sb.ToString();
		}
		protected override void ReParseImpl()
		{
			ITextSnapshot snapshot = TextBuffer.CurrentSnapshot;
			MSParser parser = new MSParser();
			//m_errors.Clear();

			MSLexer lexer = new MSLexer();
			lexer.SetSource(snapshot.GetText());
			lexer.SetIndex(0);
			lexer.SetState(MSLexerState.None);

			parser.SetLexer(lexer);
			//parser.SetErrorListener(this);

			parser.ParseAll();

			lock (Lock)
			{
				m_syntaxTree = parser.SyntaxTree;
				m_errors = new List<SyntaxError>(parser.SyntaxErrors);
				m_snapshot = snapshot;
			}

			List<SyntaxError> importErrors = new List<SyntaxError>();
			//	Remove documentation files that are no longer imported
			for (int i = m_xmlDocs.Count - 1; i >= 0; --i)
			{
				MSXmlDocumentationFile docFile = m_xmlDocs[i];
				bool found = false;
				foreach(SyntaxNode node in m_syntaxTree)
				{
					if(node is ImportNode)
					{
						ImportNode importNode = node as ImportNode;
						if (importNode.filenameNode == null || importNode.semicolonToken == null)
							continue;

						string filenameString = EvaluateString(importNode.filenameNode);
						try
						{
							string absFilename = GetAbsoluteFilename(filenameString);
							if (absFilename == docFile.Filename)
							{
								found = true;
								break;
							}
						}
						catch(Exception e)
						{
							int iStart = importNode.filenameNode.token.Span.Start;
							int iEnd = importNode.filenameNode.token.Span.End;

							importErrors.Add(new SyntaxError(0, "cannot open source file \"" + filenameString + "\"", iStart, iEnd));
						}
					}
				}
				if(found == false)
					m_xmlDocs.Remove(docFile);
			}

			//	Add new documentation files that are not already loaded
			foreach (SyntaxNode node in m_syntaxTree)
			{
				if (node is ImportNode)
				{
					ImportNode importNode = node as ImportNode;
					if (importNode.filenameNode == null || importNode.semicolonToken == null)
						continue;

					string filenameString = EvaluateString(importNode.filenameNode);
					try
					{
						string absFilename = GetAbsoluteFilename(filenameString);
						MSXmlDocumentationFile docFile = m_xmlDocs.Find(doc => String.Compare(doc.Filename, absFilename, StringComparison.OrdinalIgnoreCase) == 0);
						if (docFile == null)
							m_xmlDocs.Add(new MSXmlDocumentationFile(absFilename));
					}
					catch (Exception)
					{
						int iStart = importNode.filenameNode.token.Span.Start;
						int iEnd = importNode.filenameNode.token.Span.End;

						importErrors.Add(new SyntaxError(0, "cannot open source file \"" + filenameString + "\"", iStart, iEnd));
					}
				}
			}

			lock(Lock)
			{
				m_errors.AddRange(importErrors);
			}
			/*ContextAnalyser analyzer = new ContextAnalyser();
			analyzer.SetSyntaxTree(snapshot.GetText(), parser.SyntaxTree);
			analyzer.SetErrorListener(this);
			analyzer.AnalyseAll();*/
			
			OnParseComplete(new MSParseResultEventArgs());
		}

		/*public void SyntaxError(int errorCode, string message, int iStart, int iEnd)
		{
			m_errors.Add(new MLSyntaxError(errorCode, message, iStart, iEnd));
		}*/

		object m_lock = new object();
		public object Lock
		{
			get
			{
				return m_lock;
			}
		}

		ITextSnapshot m_snapshot = null;
		public ITextSnapshot Snapshot
		{
			get
			{
				return m_snapshot;
			}
		}
		IList<SyntaxNode> m_syntaxTree = null;
		public IList<SyntaxNode> SyntaxTree
		{
			get
			{
				if (m_syntaxTree == null)
					return new SyntaxNode[0];
				else
					return m_syntaxTree;
			}
		}

		List<SyntaxError> m_errors = null;
		public IList<SyntaxError> Errors
		{
			get
			{
				if (m_errors == null)
					return new SyntaxError[0];
				else
					return m_errors;
			}
		}

		private List<MSXmlDocumentationFile> m_xmlDocs = new List<MSXmlDocumentationFile>();
		public IList<MSXmlDocumentationFile> XmlDocs
		{
			get
			{
				return m_xmlDocs;
			}
		}

		/// <summary>
		/// Create or get text buffer from properties of a text buffer.
		/// </summary>
		public static MSBackgroundParser GetOrCreateFromTextBuffer(ITextBuffer buffer, ITextDocumentFactoryService documentFactoryService)
		{
			Func<MSBackgroundParser> createBackgroundParser = delegate () { return new MSBackgroundParser(buffer, TaskScheduler.Default, documentFactoryService); };
			return buffer.Properties.GetOrCreateSingletonProperty("MSBackgroundParser", createBackgroundParser);
		}
	}
}
