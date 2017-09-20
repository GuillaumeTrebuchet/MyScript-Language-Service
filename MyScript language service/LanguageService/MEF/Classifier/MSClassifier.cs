//------------------------------------------------------------------------------
// <copyright file="MLClassifier.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MyCompany.LanguageServices.MyScript
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "MLClassifier" classification type.
    /// </summary>
    internal class MSClassifier : IClassifier
    {
        private IStandardClassificationService m_standardClassificationService = null;
        private IClassificationTypeRegistryService m_classificationService = null;
        private ITextBuffer m_buffer;
        private ITextDocumentFactoryService m_documentService;
		private MSBackgroundParser m_backgroundParser = null;
		
        /// <summary>
        /// Initializes a new instance of the <see cref="MLClassifier"/> class.
        /// </summary>
        /// <param name="registry">Classification registry.</param>
        internal MSClassifier(ITextBuffer buffer, IClassificationTypeRegistryService classificationRegistryService, IStandardClassificationService standardClassificationService, ITextDocumentFactoryService documentService)
        {
            m_buffer = buffer;
            m_buffer.Changed += OnTextChanged;
            m_classificationService = classificationRegistryService;
            m_standardClassificationService = standardClassificationService;
           
            m_lineStates = new List<MSLexerState>(buffer.CurrentSnapshot.LineCount);
            for (int i = 0; i < buffer.CurrentSnapshot.LineCount; ++i)
                m_lineStates.Add(MSLexerState.None);

			m_backgroundParser = MSBackgroundParser.GetOrCreateFromTextBuffer(buffer, documentService);
		}
		
		
		private void OnTextChanged(object sender, TextContentChangedEventArgs e)
        {
            //	Maybe could check for "/*" or "*/" to raise ClassificationChanged.
            //	But thats complicated cause I need to check if this is not a string... etc...

            foreach (ITextChange tc in e.Changes)
            {
                if (tc.LineCountDelta > 0)
                {
                    //	lines were added.
                    //	get the line where text was added, and adds lexer states for the added lines.
                    //	Added states are the same as the original line. This way, the new line will be analysed
                    //	and if a change in state is detected, other lines are re-lexed. Otherwise all good.
                    int linePos = e.After.GetLineNumberFromPosition(tc.OldPosition);
                    MSLexerState oldState = m_lineStates[linePos];

                    for (int i = 0; i < tc.LineCountDelta; ++i)
                        m_lineStates.Insert(linePos + 1, oldState);
                }
            }
        }
        #region IClassifier

#pragma warning disable 67

        /// <summary>
        /// An event that occurs when the classification of a span of text has changed.
        /// </summary>
        /// <remarks>
        /// This event gets raised if a non-text change would affect the classification in some way,
        /// for example typing /* would cause the classification to change in C# without directly
        /// affecting the span.
        /// </remarks>
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

        //	This is the state of the lexer at the end of each line. So we can resume parsing from one line to another.
        List<MSLexerState> m_lineStates = null;

        /// <summary>
        /// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
        /// </summary>
        /// <remarks>
        /// This method scans the given SnapshotSpan for potential matches for this classification.
        /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
        /// </remarks>
        /// <param name="span">The span currently being classified.</param>
        /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {

			ITextSnapshot snapshot = span.Snapshot;
            List<ClassificationSpan> result = new List<ClassificationSpan>();

            int iLineStart = snapshot.GetLineNumberFromPosition(span.Start);
            int iLineEnd = snapshot.GetLineNumberFromPosition(span.End);
			
			// /!\	Warning : Seems like GetClassificationSpans is called line by line but span end is at the start of the next line
			//		Thus we are scanning it too. Maybe should avoid that but whatever.
#warning Should look at this in details

			for (int iLine = iLineStart; iLine <= iLineEnd; ++iLine)
            {
                ITextSnapshotLine line = snapshot.GetLineFromLineNumber(iLine);

                MSLexer lexer = new MSLexer();
                lexer.SetSource(line.GetText());
				
				//	Restore the lexer state at the end of the previous line.
				MSLexerState state = MSLexerState.None;
                if (iLine > 0)
                    state = m_lineStates[iLine - 1];

                lexer.SetState(state);

                while (true)
                {
                    MSToken token = new MSToken();

                    if (lexer.GetNextToken(token))
                    {
                        //	adjust token span with line offset
                        Span adjustedSpan = new Span(token.Span.Start + line.Start.Position, token.Span.Length);
                        token.Span = adjustedSpan;

                        SnapshotSpan? tokenSpan = span.Intersection(token.Span);
                        if (!tokenSpan.HasValue)
                            continue;

                        IClassificationType type = null;
						if (MSLanguage.Keywords.Contains(token.Text))
						{
							//	Some keyword are also operators so gotta check text
							result.Add(new ClassificationSpan(tokenSpan.Value, m_standardClassificationService.Keyword));
						}
						else
						{
							switch (token.Type)
							{
								case MSTokenType.Whitespace:
									type = m_standardClassificationService.WhiteSpace;
									break;
								case MSTokenType.Comment:
									type = m_standardClassificationService.Comment;
									break;
								case MSTokenType.Identifier:
									type = m_standardClassificationService.Identifier;
									break;
								case MSTokenType.Keyword:
									type = m_standardClassificationService.Keyword;
									break;
								case MSTokenType.Integer:
									type = m_standardClassificationService.NumberLiteral;
									break;
								case MSTokenType.Decimal:
									type = m_standardClassificationService.NumberLiteral;
									break;
								case MSTokenType.Boolean:
									type = m_standardClassificationService.Literal;
									break;
								case MSTokenType.Operator:
									type = m_standardClassificationService.Operator;
									break;
								case MSTokenType.String:
									type = m_standardClassificationService.StringLiteral;
									break;
								case MSTokenType.Unknown:
									type = m_standardClassificationService.Other;
									break;
								default:
									throw new Exception();
							}

							result.Add(new ClassificationSpan(tokenSpan.Value, type));
						}
                    }
                    else
                        break;
                }

                //	Get new state. If there is a next line, and the state changed, need to re-lex the next line.
                MSLexerState newState = lexer.GetState();
                if (newState != m_lineStates[iLine])
                {
                    m_lineStates[iLine] = newState;
                    if (snapshot.LineCount > iLine + 1)
                    {
                        ClassificationChangedEventArgs args = new ClassificationChangedEventArgs(snapshot.GetLineFromLineNumber(iLine + 1).Extent);
                        ClassificationChanged(this, args);
                    }
                }

            }



            return result;
        }

        #endregion

		public IList<MSLexerState> LineStates
		{
			get
			{
				return m_lineStates;
			}
		}
    }
}
