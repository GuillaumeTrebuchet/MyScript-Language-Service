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


namespace MyCompany.LanguageServices.MyScript
{
	[Export(typeof(IVsTextViewCreationListener))]
	[Name("MyScript Signature Help controller")]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	[ContentType("MyScript")]
	internal class MSSignatureHelpCommandProvider : IVsTextViewCreationListener
	{
		[Import]
		internal IVsEditorAdaptersFactoryService AdapterService;

		[Import]
		internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

		[Import]
		internal ISignatureHelpBroker SignatureHelpBroker;

		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
			if (textView == null)
				return;

			textView.Properties.GetOrCreateSingletonProperty(
				 () => new MSSignatureHelpCommandHandler(textViewAdapter,
					textView,
					NavigatorService.GetTextStructureNavigator(textView.TextBuffer),
					SignatureHelpBroker));
		}
	}

	internal sealed class MSSignatureHelpCommandHandler : IOleCommandTarget
	{
		IOleCommandTarget m_nextCommandHandler;
		ITextView m_textView;
		ISignatureHelpBroker m_broker;
		ISignatureHelpSession m_session;
		ITextStructureNavigator m_navigator;

		internal MSSignatureHelpCommandHandler(IVsTextView textViewAdapter, ITextView textView, ITextStructureNavigator nav, ISignatureHelpBroker broker)
		{
			this.m_textView = textView;
			this.m_broker = broker;
			this.m_navigator = nav;

			//add this to the filter chain
			textViewAdapter.AddCommandFilter(this, out m_nextCommandHandler);
		}
		
		/*bool IsInCall(SnapshotPoint point, out string functionName)
		{
			functionName = null;
			
			//	Use the classifier for backward lexing because it keeps a list of line states.
			//	This way we dont need to lex the entire file. Important for very big files.
			//	/!\ Not sure about edits though. CompletionSource is called BEFORE edit has been classified.
			//	Maybe its the same here, what if edits are pending, and they change the states???
			MSClassifier classifier = null;
			if (!m_textView.TextBuffer.Properties.TryGetProperty<MSClassifier>("MSClassifier", out classifier))
				return false;

			MSReverseLexer lexer = new MSReverseLexer(classifier, point.Snapshot);
			lexer.SetPoint(point);

			int parenthesisLevel = 0;
			MSToken token = PreviousTokenSkipWhitespace(lexer);
			while (token != null)
			{
				if (token.Text == ")")
					++parenthesisLevel;
				else if (token.Text == "(")
				{
					if (parenthesisLevel == 0)
					{
						token = PreviousTokenSkipWhitespace(lexer);
						break;
					}
					else
					{
						--parenthesisLevel;
					}
				}
			}

			MSToken nameToken = token;
			MSToken functionToken = PreviousTokenSkipWhitespace(lexer);
			if (nameToken != null && functionToken != null && functionToken.Text != "function")
			{
				functionName = nameToken.Text;
				return true;
			}

			return false;
		}*/
		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			char typedChar = char.MinValue;

			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
			{
				typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
				if (typedChar == '(')
				{
					if (m_session != null)
					{
						m_session.Dismiss();
						m_session = null;
					}

					//	Commit the character to make things easier
					int result = m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

					//move the point back so it's in the preceding word
					SnapshotPoint point = m_textView.Caret.Position.BufferPosition - 1;

					try
					{
						MSRandomAccessLexer lexer = new MSRandomAccessLexer(m_textView.TextBuffer.Properties.GetProperty<MSClassifier>("MSClassifier"), point.Snapshot);
						lexer.SetPoint(point);

						//	Ensure previous token is '(', and is not inside a string, comment, etc.
						MSToken token = lexer.PreviousTokenSkipWhitespace();
						if (token != null && token.Text == "(")
						{

							if (m_textView.Caret.Position.BufferPosition >= m_textView.Caret.Position.BufferPosition.Snapshot.Length ||
								char.IsWhiteSpace((m_textView.Caret.Position.BufferPosition).GetChar()))
							{
								m_textView.TextBuffer.Insert(m_textView.Caret.Position.BufferPosition, ")");
								m_textView.Caret.MoveToPreviousCaretPosition();
							}
							m_session = m_broker.TriggerSignatureHelp(m_textView);
						}
					}
					catch(Exception)
					{

					}
					
					return result;
				}
				else if(typedChar == ',' && (m_session == null || m_session.IsDismissed))
				{
					m_session = null;

					//	Commit the character to make things easier
					int result = m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

					SnapshotPoint point = m_textView.Caret.Position.BufferPosition - 1;

					MSClassifier classifier = null;
					if (m_textView.TextBuffer.Properties.TryGetProperty<MSClassifier>("MSClassifier", out classifier))
					{
						MSRandomAccessLexer lexer = new MSRandomAccessLexer(classifier, point.Snapshot);
						lexer.SetPoint(point);

						MSToken token = lexer.PreviousTokenSkipWhitespace();
						if (token != null && token.Text == ",")
							m_session = m_broker.TriggerSignatureHelp(m_textView);
					}

					return result;
				}
				else if (typedChar == ')' && m_session != null)
				{
					m_session.Dismiss();
					m_session = null;
				}
				
			}
			else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE)
			{
				MSRandomAccessLexer lexer = new MSRandomAccessLexer(m_textView.TextBuffer.Properties.GetProperty<MSClassifier>("MSClassifier"), m_textView.TextBuffer.CurrentSnapshot);
				lexer.SetPoint(m_textView.Caret.Position.BufferPosition, true);

				MSToken previousToken = lexer.GetPreviousToken();
				lexer.NextTokenSkipWhitespace();
				MSToken nextToken = lexer.NextTokenSkipWhitespace();
				if (previousToken != null && previousToken.Text == "(" && nextToken != null && nextToken.Text == ")")
				{
					int iStart = previousToken.Span.End;
					int length = nextToken.Span.End - iStart;
					m_textView.TextBuffer.Delete(new Span(iStart, length));
				}

			}
			else if(nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
			{
				/* Well thats complicated...
				 * Best way would be to use background parser to get the indentation of the closest scope. Then parse from that point using RandomLexer
				 * to check begining of blocks and end, and get indentation this way.
				 *
				 * */

				MSBackgroundParser backgroundParser = null;
				if (m_textView.TextBuffer.Properties.TryGetProperty<MSBackgroundParser>("MSBackgroundParser", out backgroundParser))
				{
					ITextSnapshot snapshot = null;
					IList<SyntaxNode> syntaxTree = null;

					//	Get syntax tree from the parser
					lock (backgroundParser.Lock)
					{
						snapshot = backgroundParser.Snapshot;
						syntaxTree = backgroundParser.SyntaxTree;
					}

					if (snapshot != null && syntaxTree != null)
					{
						int pos = m_textView.Caret.Position.BufferPosition.TranslateTo(snapshot, PointTrackingMode.Positive).Position;

						FunctionNode node = MSSyntaxUtility.GetFunctionAtPos(pos, syntaxTree);
					}
				}
			}

			return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}
	}
}
