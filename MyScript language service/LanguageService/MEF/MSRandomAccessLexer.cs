using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace MyCompany.LanguageServices.MyScript
{
    /*
     * Well thats ugly...
     * Thats basically a lexer that allows reverse parsing.
     * We need all that because MyScript code MUST be lexed left to right, or it will
     * be a mess with comments, string etc...
     * So we are doing it line by line based on the lexer's state stored in the classifier.
     * */
	class MSRandomAccessLexer
	{
		private MSClassifier m_classifier = null;
		private ITextSnapshot m_snapshot = null;

		private int m_iLine = 0;
		private List<MSToken> m_tokens = new List<MSToken>();

		//	Represents the token index we are at the start of.
		private int m_iCurrentToken = 0;

        //  Load the given line and read all tokens into m_tokens.
		private void LoadLine(int iLine, bool loadState)
		{
			m_tokens.Clear();

			MSLexer lexer = new MSLexer();

			ITextSnapshotLine line = m_snapshot.GetLineFromLineNumber(iLine);
			lexer.SetSource(line.GetText());
			lexer.SetIndex(0);

			if (loadState)
			{
				if (iLine > 0)
					lexer.SetState(m_classifier.LineStates[iLine - 1]);
				else
					lexer.SetState(MSLexerState.None);
			}

			MSToken token = new MSToken();
			while (lexer.GetNextToken(token))
			{
				//	span is relative to start of line. Makes it absolute
				token.Span = new Span(token.Span.Start + line.Start, token.Span.Length);
				m_tokens.Add(token);
				token = new MSToken();
			}
		}

		public MSRandomAccessLexer(MSClassifier classifier, ITextSnapshot snapshot)
		{
			if (classifier == null || snapshot == null)
				throw new ArgumentNullException();

			m_classifier = classifier;
			m_snapshot = snapshot;
		}
		
        //  Set the current point of the lexer.
		public void SetPoint(SnapshotPoint point, bool beforeWord = false)
		{
			int iLine = point.GetContainingLine().LineNumber;
			if (iLine != m_iLine)
			{
				LoadLine(iLine, true);
				m_iLine = iLine;
			}

			for(int i = 0; i<m_tokens.Count; ++i)
			{
				if(m_tokens[i].Span.Contains(point))
				{
					if (beforeWord)
						m_iCurrentToken = i;
					else
						m_iCurrentToken = i + 1;
					return;
				}
			}
			m_iCurrentToken = m_tokens.Count;
		}
		public MSToken GetNextToken()
		{
			if (m_iCurrentToken < m_tokens.Count)
			{
				return m_tokens[m_iCurrentToken++];
			}
			else
			{
				if (m_iLine + 1 < m_snapshot.LineCount)
				{
					LoadLine(++m_iLine, false);
					m_iCurrentToken = 0;
					return GetNextToken();
				}
				else
					return null;
			}
		}
		public MSToken GetPreviousToken()
		{
			if(m_iCurrentToken > 0)
			{
				return m_tokens.ElementAt(--m_iCurrentToken);
			}
			else
			{
				if (m_iLine > 0)
				{
					LoadLine(--m_iLine, true);
					m_iCurrentToken = m_tokens.Count;
					return GetPreviousToken();
				}
				else
					return null;
			}
		}

		public MSToken PreviousTokenSkipWhitespace()
		{
			MSToken token = GetPreviousToken();
			while (token != null && (token.Type == MSTokenType.Whitespace || token.Type == MSTokenType.Comment))
				token = GetPreviousToken();

			return token;
		}
		public MSToken NextTokenSkipWhitespace()
		{
			MSToken token = GetNextToken();
			while (token != null && (token.Type == MSTokenType.Whitespace || token.Type == MSTokenType.Comment))
				token = GetNextToken();

			return token;
		}
	}
}
