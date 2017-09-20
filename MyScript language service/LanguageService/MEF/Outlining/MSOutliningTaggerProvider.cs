using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;

namespace MyCompany.LanguageServices.MyScript
{
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IOutliningRegionTag))]
	[ContentType("MyScript")]
	internal sealed class MLOutliningTaggerProvider
		: ITaggerProvider
	{
		[Import]
		internal ITextDocumentFactoryService DocumentService { get; set; }

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
		{
			//create a single tagger for each buffer.
			Func<ITagger<T>> sc = delegate () { return new MSOutliningTagger(buffer, DocumentService) as ITagger<T>; };
			return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(sc);
		}
	}
}
