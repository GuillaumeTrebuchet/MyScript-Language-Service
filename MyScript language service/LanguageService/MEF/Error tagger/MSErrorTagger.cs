using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MyCompany.LanguageServices.MyScript
{
	class MSErrorTagger
		 : ITagger<IErrorTag>
	{
		ITextBuffer m_buffer;
		ITextSnapshot m_snapshot;

		MSBackgroundParser m_backgroundParser = null;

		ITextDocumentFactoryService m_documentService;

		SyntaxError[] m_errors = null;

		public MSErrorTagger(ITextBuffer buffer, ITextDocumentFactoryService documentService)
		{
			m_documentService = documentService;

			this.m_buffer = buffer;
			this.m_snapshot = buffer.CurrentSnapshot;
			this.m_errors = new SyntaxError[0];

			m_backgroundParser = MSBackgroundParser.GetOrCreateFromTextBuffer(buffer, documentService);
			m_backgroundParser.ParseComplete += ParseComplete;

			this.ReParse();
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		void ParseComplete(object sender, ParseResultEventArgs e)
		{
			// If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
			/*if (e.After != buffer.CurrentSnapshot)
				return;*/
			this.ReParse();
		}
		
		void ReParse()
		{
			if (m_backgroundParser == null)
				return;

			ITextSnapshot newSnapshot = null;
			SyntaxError[] newErrors = null;

			lock (m_backgroundParser.Lock)
			{
				newSnapshot = m_backgroundParser.Snapshot;
				newErrors = m_backgroundParser.Errors.ToArray();
			}

			if (newSnapshot == null || newErrors == null)
				return;


			//determine the changed span, and send a changed event with the new spans
			List<Span> oldSpans =
				new List<Span>(this.m_errors.Select(e => new SnapshotSpan(this.m_snapshot, e.StartIndex, e.EndIndex - e.StartIndex)
					.TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive)
					.Span));
			List<Span> newSpans =
					new List<Span>(newErrors.Select(e => new SnapshotSpan(newSnapshot, e.StartIndex, e.EndIndex - e.StartIndex).Span));

			NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
			NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

			//the changed regions are regions that appear in one set or the other, but not both.
			NormalizedSpanCollection removed =
			NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

			int changeStart = int.MaxValue;
			int changeEnd = -1;

			if (removed.Count > 0)
			{
				changeStart = removed[0].Start;
				changeEnd = removed[removed.Count - 1].End;
			}

			if (newSpans.Count > 0)
			{
				changeStart = Math.Min(changeStart, newSpans[0].Start);
				changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
			}

			this.m_snapshot = newSnapshot;
			this.m_errors = newErrors;

			if (changeStart <= changeEnd)
			{
				if (this.TagsChanged != null)
					this.TagsChanged(this, new SnapshotSpanEventArgs(
						new SnapshotSpan(m_snapshot, Span.FromBounds(changeStart, changeEnd))));
			}
		}
		public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (spans.Count == 0)
				yield break;

			SyntaxError[] currentErrors = m_errors;
			ITextSnapshot currentSnapshot = m_snapshot;

			SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
			int startLineNumber = entire.Start.GetContainingLine().LineNumber;
			int endLineNumber = entire.End.GetContainingLine().LineNumber;
			foreach (SyntaxError error in currentErrors)
			{
				yield return new TagSpan<IErrorTag>(
						new SnapshotSpan(m_snapshot, error.StartIndex, error.EndIndex - error.StartIndex),
						new ErrorTag("syntax error", error.Message));
			}
		}
	}
}