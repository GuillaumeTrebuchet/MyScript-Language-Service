using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MyCompany.LanguageServices.MyScript
{
	[Export(typeof(IViewTaggerProvider))]
	[ContentType("MyScript")]
	[TagType(typeof(TextMarkerTag))]
	internal class MSBraceMatchingTaggerProvider : IViewTaggerProvider
	{
		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
		{
			if (textView == null)
				return null;

			//provide highlighting only on the top-level buffer
			if (textView.TextBuffer != buffer)
				return null;

			return new MSBraceMatchingTagger(textView, buffer) as ITagger<T>;
		}
	}
}
