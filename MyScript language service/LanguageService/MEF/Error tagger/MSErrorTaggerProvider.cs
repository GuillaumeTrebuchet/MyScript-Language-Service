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
	[Export(typeof(IViewTaggerProvider))]
	[ContentType("MyScript")]
	[TagType(typeof(ErrorTag))]
	class MLErrorTaggerProvider
		 : IViewTaggerProvider
	{
		[Import]
		internal ITextDocumentFactoryService DocumentService { get; set; }

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
		{
			//create a single tagger for each buffer.
			Func<ITagger<T>> sc = delegate () { return new MSErrorTagger(buffer, DocumentService) as ITagger<T>; };
			return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(sc);
		}
	}
}
