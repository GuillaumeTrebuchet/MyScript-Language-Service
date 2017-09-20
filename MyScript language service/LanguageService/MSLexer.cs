using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text;

namespace MyCompany.LanguageServices.MyScript
{
    public enum MSTokenType
    {
        Identifier,
        Keyword,
        Operator,
        String,
        Integer,
        Boolean,
        Decimal,
        Comment,
		Whitespace,
        Unknown,
    }

    /*
     * Simple token class.
     * Not sure about the Text member... in c# strings are immutable, which means every time a
     * token is created, a corresponding immutable string is created... bad for perf.
     * But doesn't seems there is an easy way to create StringRef in c#
     * */
    public class MSToken
    {
        public MSTokenType Type { get; set; }
        public Span Span { get; set; }
        public string Text { get; set; }
    }

    public enum MSLexerState
    {
        None,
        InComment,
        InSingleString,
        InDoubleString,
    }
    
    /*
     * Design is kept simple. We can set and get the state of the lexer.
     * That way we can lex different parts of the source very easily.
     * */
    public class MSLexer
    {
        string m_source = null;
        int m_index = 0;
        MSLexerState m_state = 0;

        public MSLexer()
        {

        }

        bool IsHex(char c)
        {
            return (c >= '0' && c <= '9') ||
                     (c >= 'a' && c <= 'f') ||
                     (c >= 'A' && c <= 'F');
        }

        bool IsKeyword(string text)
        {
            foreach (string s in MSLanguage.Keywords)
                if (text == s)
                    return true;

            return false;
        }
        
        bool IsIdentifier(ref int length)
        {
            int i = m_index;

            //  1st char cannot be digit
            if (char.IsLetter(m_source[i]) ||
                    m_source[i] == '_')
            {
                ++i;
                while (i < m_source.Length &&
                    (char.IsLetterOrDigit(m_source[i]) || m_source[i] == '_'))
                    ++i;

                length = i - m_index;
                return true;
            }
            else
                return false;
        }

        bool IsWhitespace(ref int length)
        {
            int i = m_index;

            //  1st char cannot be digit
            while (i < m_source.Length &&
                (char.IsWhiteSpace(m_source[i])))
                ++i;

            length = i - m_index;
            if (length > 0)
                return true;
            else
                return false;
        }

        bool IsString(ref MSLexerState state, ref int length)
        {
            int i = m_index;
            char quote = '\0';

            if (state == MSLexerState.InSingleString)
            {
                quote = '\'';
            }
            else if (state == MSLexerState.InDoubleString)
            {
                quote = '"';
            }
            else if (state == MSLexerState.None)
            {
                if (m_source[i] == '"')
                {
                    quote = '"';
                    state = MSLexerState.InDoubleString;
                }
                else if (m_source[i] == '\'')
                {
                    quote = '\'';
                    state = MSLexerState.InSingleString;
                }
                else
                    return false;

                ++i;
            }

            bool closed = false;
            bool escaped = false;
            while (i < m_source.Length)
            {
                if (m_source[i] == quote && !escaped)
                {
                    ++i;
                    closed = true;
                    state = MSLexerState.None;
                    break;
                }

                //  Check end of line
                if (i + Environment.NewLine.Length < m_source.Length &&
                    m_source.Substring(i, Environment.NewLine.Length) == Environment.NewLine)
                {
                    i += Environment.NewLine.Length;
                    break;
                }

                if (m_source[i] == '\\')
                    escaped = true;
                else
                    escaped = false;

                ++i;
            }

            //	No multiline strings
            state = MSLexerState.None;

            length = i - m_index;
            return true;
        }

        bool IsComment(ref MSLexerState state, ref int length)
        {
            int i = m_index;

            if (state == MSLexerState.InComment ||
                (i + 1 < m_source.Length && m_source.Substring(i, 2) == "/*"))
            {
                if (state != MSLexerState.InComment)
                {
                    i += 2;
                    state = MSLexerState.InComment;
                }

                while (i < m_source.Length)
                {
                    //	End of comment
                    if (i + 1 < m_source.Length &&
                        m_source.Substring(i, 2) == "*/")
                    {
                        i += 2;

                        state = MSLexerState.None;

                        break;
                    }

                    ++i;
                }

                length = i - m_index;
                return true;
            }
            else if (i + 1 < m_source.Length &&
                m_source.Substring(i, 2) == "//")
            {
                i += 2;
                while (i < m_source.Length)
                {
                    //  Check end of line
                    if (i + Environment.NewLine.Length < m_source.Length &&
                        m_source.Substring(i, Environment.NewLine.Length) == Environment.NewLine)
                    {
                        i += Environment.NewLine.Length;
                        break;
                    }

                    ++i;
                }

                length = i - m_index;
                return true;
            }

            return false;
        }

        bool IsNumber(ref int length, out bool isInteger)
        {
            isInteger = true;

            int i = m_index;

            if (!char.IsDigit(m_source[i]))
                return false;

            //	Binary
            if (i + 1 < m_source.Length &&
                m_source.Substring(i, 2) == "0b")
            {
                i += 2;
                while (i < m_source.Length)
                {
                    if (!char.IsDigit(m_source[i]))
                        break;
                    ++i;
                }

                length = i - m_index;
                return true;
            }
            //	Hexadecimal
            else if (i + 1 < m_source.Length &&
                m_source.Substring(i, 2) == "0x")
            {
                i += 2;
                while (i < m_source.Length)
                {
                    if (!IsHex(m_source[i]))
                        break;
                    ++i;
                }

                length = i - m_index;
                return true;
            }

            //	Decimal
            while (i < m_source.Length)
            {
                if (!char.IsDigit(m_source[i]))
                    break;
                ++i;
            }

            //	Check for decimal part
            if (i < m_source.Length && m_source[i] == '.')
            {
                isInteger = false;

                ++i;
                while (i < m_source.Length)
                {
                    if (!char.IsDigit(m_source[i]))
                        break;
                    ++i;
                }
            }

            length = i - m_index;
            if (length > 0)
                return true;
            else
                return false;
        }

        bool IsOperator(ref int length)
        {
            int i = m_index;

            foreach (MSLanguage.BinaryOperator op in MSLanguage.BinaryOperators)
            {
                if (i + op.Text.Length < m_source.Length &&
                    m_source.Substring(i, op.Text.Length) == op.Text)
                {
                    length = op.Text.Length;
                    return true;
                }
            }

            return false;
        }

        //  false and true are keywords
        /*bool IsBoolean(ref int length)
        {
            int i = m_index;
            
            if (i + "false".Length < m_source.Length &&
                m_source.Substring(i, "false".Length) == "false")
            {
                length = "false".Length;
                return true;
            }
            else if (i + "true".Length < m_source.Length &&
                m_source.Substring(i, "true".Length) == "true")
            {
                length = "true".Length;
                return true;
            }

            return false;
        }
        */

        /// <summary>
        /// Return false if end of text is reached. All white spaces are skipped.
        /// On return false, token content is meaningless
        /// </summary>
        /// <returns></returns>
        public bool GetNextToken(MSToken token)
        {
            //  End of text
            if (m_index >= m_source.Length)
                return false;


            int length = 0;

            bool isInteger;

            if (m_state == MSLexerState.InComment)
            {
                //	We are already in comment.
                IsComment(ref m_state, ref length);
                token.Type = MSTokenType.Comment;
            }
			//	Gotta check operator first cause 'or' and 'and' are both keywords and operators
			else if (IsOperator(ref length))
			{
				token.Type = MSTokenType.Operator;
			}
			else if (IsIdentifier(ref length))
            {
                if (IsKeyword(m_source.Substring(m_index, length)))
                    token.Type = MSTokenType.Keyword;
                else
                    token.Type = MSTokenType.Identifier;
            }
			else if (IsWhitespace(ref length))
			{
				token.Type = MSTokenType.Whitespace;
			}
			else if (IsString(ref m_state, ref length))
            {
                token.Type = MSTokenType.String;
            }
            else if (IsComment(ref m_state, ref length))
            {
                token.Type = MSTokenType.Comment;
            }
            else if (IsNumber(ref length, out isInteger))
            {
                if (isInteger)
                    token.Type = MSTokenType.Integer;
                else
                    token.Type = MSTokenType.Decimal;
            }
            else
            {
                token.Type = MSTokenType.Unknown;
                length = 1;
            }

            token.Span = new Span(m_index, length);

            m_index += length;

            token.Text = m_source.Substring(token.Span.Start, token.Span.Length);

            return true;
        }

        public void SetSource(string source)
        {
            m_source = source;
            m_index = 0;
            m_state = MSLexerState.None;
        }
        public string GetSource()
        {
            return m_source;
        }

        //	The internal state of the lexer can be read / written so we can lex different parts of the text.
        public void SetState(MSLexerState state)
        {
            m_state = state;
        }
        public MSLexerState GetState()
        {
            return m_state;
        }
        
        public int GetIndex()
        {
            return m_index;
        }
        public void SetIndex(int index)
        {
            m_index = index;
        }

        public TextSpan GetTokenSpan(MSToken token)
        {
            TextSpan span = new TextSpan();

            int i = 0;
            int line = 0;
            int column = 0;
            while (i < m_source.Length)
            {
                if (i == token.Span.Start)
                {
                    span.iStartIndex = column;
                    span.iStartLine = line;
                }
                if (i == token.Span.Start + token.Span.Length)
                {
                    span.iEndIndex = column;
                    span.iEndLine = line;
                }

                if (i + Environment.NewLine.Length < m_source.Length &&
                    m_source.Substring(i, Environment.NewLine.Length) == Environment.NewLine)
                {
                    ++line;
                    column = 0;
                    i += Environment.NewLine.Length;
                }
                else
                {
                    ++i;
                    ++column;
                }
            }

            if (token.Span.Start == m_source.Length)
            {
                span.iStartIndex = column;
                span.iStartLine = line;
            }
            if (token.Span.Start + token.Span.Length == m_source.Length)
            {
                span.iEndIndex = column;
                span.iEndLine = line;
            }
            return span;
        }
    }
}
