using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using System.Threading;
using System.Threading.Tasks;


namespace MyCompany.LanguageServices.MyScript
{

	class MSRegion
	{
		public int StartLine { get; set; }
		public int StartOffset { get; set; }
		public int EndLine { get; set; }
		public int EndOffset { get; set; }
		public string Tip { get; set; }
	}

	internal sealed class MSOutliningTagger
		: ITagger<IOutliningRegionTag>
	{
		ITextBuffer buffer;
		ITextSnapshot snapshot;
		List<MSRegion> regions;

		MSBackgroundParser m_backgroundParser = null;

		ITextDocumentFactoryService m_documentService;

		public MSOutliningTagger(ITextBuffer buffer, ITextDocumentFactoryService documentService)
		{
			m_documentService = documentService;

			this.buffer = buffer;
			this.snapshot = buffer.CurrentSnapshot;
			this.regions = new List<MSRegion>();

			m_backgroundParser = MSBackgroundParser.GetOrCreateFromTextBuffer(buffer, documentService);
			m_backgroundParser.ParseComplete += ParseComplete;

			this.ReParse();
		}

		public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (spans.Count == 0)
				yield break;

			List<MSRegion> currentRegions = this.regions;
			ITextSnapshot currentSnapshot = this.snapshot;

			SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
			int startLineNumber = entire.Start.GetContainingLine().LineNumber;
			int endLineNumber = entire.End.GetContainingLine().LineNumber;
			foreach (var region in currentRegions)
			{
				if (region.StartLine <= endLineNumber &&
					region.EndLine >= startLineNumber)
				{
					ITextSnapshotLine startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
					ITextSnapshotLine endLine = currentSnapshot.GetLineFromLineNumber(region.EndLine);

					//the region starts at the beginning of the "[", and goes until the *end* of the line that contains the "]".
					yield return new TagSpan<IOutliningRegionTag>(
						new SnapshotSpan(startLine.Start + region.StartOffset,
						endLine.Start + region.EndOffset),
						new OutliningRegionTag(false, false, "...", region.Tip));
				}
			}
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		void ParseComplete(object sender, ParseResultEventArgs e)
		{
			// If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
			/*if (e.After != buffer.CurrentSnapshot)
				return;*/
			this.ReParse();
		}

		void AddNodeRegions(SyntaxNode node, ITextSnapshot snapshot, List<MSRegion> regions)
		{
			if (node is FunctionNode)
			{
				FunctionNode functionNode = (FunctionNode)node;

				SnapshotPoint startPos;
				SnapshotPoint endPos;

				//	If there is a type, set start point after the type. Else after the argument list
				if (functionNode.returnTypeExpression != null && functionNode.returnTypeExpression is TypeNode)
					startPos = new SnapshotPoint(snapshot, (functionNode.returnTypeExpression as TypeNode).token.Span.End);
				else if (functionNode.separatorTokens.Count > 0)
					startPos = new SnapshotPoint(snapshot, functionNode.separatorTokens.Last().Span.End);
				else
					return;

				if (functionNode.endToken != null)
					endPos = new SnapshotPoint(snapshot, functionNode.endToken.Span.End);
				else
					return;

				regions.Add(new MSRegion()
				{
					StartLine = snapshot.GetLineNumberFromPosition(startPos),
					StartOffset = startPos - snapshot.GetLineFromPosition(startPos).Start.Position,
					EndLine = snapshot.GetLineNumberFromPosition(endPos),
					EndOffset = endPos - snapshot.GetLineFromPosition(endPos).Start.Position,
				});

				//	Check for statements nodes
				foreach(SyntaxNode childNode in functionNode.statements)
				{
					AddNodeRegions(childNode, snapshot, regions);
				}
			}
			else if(node is IfNode)
			{
				IfNode ifNode = node as IfNode;

				SnapshotPoint startPos;
				SnapshotPoint endPos;

				if (ifNode.thenToken == null)
					return;

				startPos = new SnapshotPoint(snapshot, ifNode.separatorTokens.Last().Span.End);

				if (ifNode.elseToken != null)
				{
					//	There is an else clause. Add first outlining for if, then set start point for else
					endPos = new SnapshotPoint(snapshot, ifNode.elseToken.Span.Start);
					
					regions.Add(new MSRegion()
					{
						StartLine = snapshot.GetLineNumberFromPosition(startPos),
						StartOffset = startPos - snapshot.GetLineFromPosition(startPos).Start.Position,
						EndLine = snapshot.GetLineNumberFromPosition(endPos),
						EndOffset = endPos - snapshot.GetLineFromPosition(endPos).Start.Position,
					});

					startPos = new SnapshotPoint(snapshot, ifNode.elseToken.Span.End);
				}

				if (ifNode.endToken == null)
					return;

				//	Add outlining till end
				endPos = new SnapshotPoint(snapshot, ifNode.endToken.Span.End);

				regions.Add(new MSRegion()
				{
					StartLine = snapshot.GetLineNumberFromPosition(startPos),
					StartOffset = startPos - snapshot.GetLineFromPosition(startPos).Start.Position,
					EndLine = snapshot.GetLineNumberFromPosition(endPos),
					EndOffset = endPos - snapshot.GetLineFromPosition(endPos).Start.Position,
				});

				//	Check for if statements nodes
				foreach (SyntaxNode childNode in ifNode.statements)
				{
					AddNodeRegions(childNode, snapshot, regions);
				}

				//	Check for else statements nodes
				foreach (SyntaxNode childNode in ifNode.elseStatements)
				{
					AddNodeRegions(childNode, snapshot, regions);
				}
			}
			else if(node is WhileNode)
			{
				WhileNode whileNode = node as WhileNode;

				SnapshotPoint startPos;
				SnapshotPoint endPos;

				if (whileNode.doToken == null)
					return;

				startPos = new SnapshotPoint(snapshot, whileNode.separatorTokens.Last().Span.End);
				
				if (whileNode.endToken == null)
					return;
				
				endPos = new SnapshotPoint(snapshot, whileNode.endToken.Span.End);

				regions.Add(new MSRegion()
				{
					StartLine = snapshot.GetLineNumberFromPosition(startPos),
					StartOffset = startPos - snapshot.GetLineFromPosition(startPos).Start.Position,
					EndLine = snapshot.GetLineNumberFromPosition(endPos),
					EndOffset = endPos - snapshot.GetLineFromPosition(endPos).Start.Position,
				});

				//	Check for if statements nodes
				foreach (SyntaxNode childNode in whileNode.statements)
				{
					AddNodeRegions(childNode, snapshot, regions);
				}
			}
		}
		void ReParse()
		{
			if (m_backgroundParser == null)
				return;

			ITextSnapshot newSnapshot = null;
			IList<SyntaxNode> syntaxTree = null;
			List<MSRegion> newRegions = new List<MSRegion>();

			lock (m_backgroundParser.Lock)
			{
				newSnapshot = m_backgroundParser.Snapshot;
				syntaxTree = m_backgroundParser.SyntaxTree;
			}

			if (newSnapshot == null || syntaxTree == null)
				return;


			foreach (SyntaxNode node in syntaxTree)
			{
				AddNodeRegions(node, newSnapshot, newRegions);
			}

			//determine the changed span, and send a changed event with the new spans
			List<Span> oldSpans =
				new List<Span>(this.regions.Select(r => AsSnapshotSpan(r, this.snapshot)
					.TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive)
					.Span));
			List<Span> newSpans =
					new List<Span>(newRegions.Select(r => AsSnapshotSpan(r, newSnapshot).Span));

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

			this.snapshot = newSnapshot;
			this.regions = newRegions;

			if (changeStart <= changeEnd)
			{
				ITextSnapshot snap = this.snapshot;
				if (this.TagsChanged != null)
					this.TagsChanged(this, new SnapshotSpanEventArgs(
						new SnapshotSpan(this.snapshot, Span.FromBounds(changeStart, changeEnd))));
			}
		}

		static SnapshotSpan AsSnapshotSpan(MSRegion region, ITextSnapshot snapshot)
		{
			var startLine = snapshot.GetLineFromLineNumber(region.StartLine);
			var endLine = (region.StartLine == region.EndLine) ? startLine
				 : snapshot.GetLineFromLineNumber(region.EndLine);
			return new SnapshotSpan(startLine.Start + region.StartOffset, endLine.End);
		}
	}
}
