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
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Language.StandardClassification;

namespace MyCompany.LanguageServices.MyScript
{
	[Export(typeof(ICompletionSourceProvider))]
	[ContentType("MyScript")]
	[Name("token completion")]
	internal class MSCompletionSourceProvider : ICompletionSourceProvider
	{
		[Import]
		internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

		[Import]
		internal IGlyphService GlyphService { get; set; }

		[Import]
		internal ITextDocumentFactoryService DocumentService { get; set; }

		[Import]
		internal SVsServiceProvider ServiceProvider { get; set; }

		[Import]
		internal IViewTagAggregatorFactoryService ViewTagAggregatorFactoryService { get; set; }

		[Import]
		internal IStandardClassificationService StandardClassificationService { get; set; }

		public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
		{
			return new MSCompletionSource(this, textBuffer, GlyphService, DocumentService, ServiceProvider, ViewTagAggregatorFactoryService, StandardClassificationService);
		}
	}
}
