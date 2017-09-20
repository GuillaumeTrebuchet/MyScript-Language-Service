﻿using System;
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
	[Export(typeof(ISignatureHelpSourceProvider))]
	[Name("Signature Help source")]
	[Order(Before = "default")]
	[ContentType("MyScript")]
	internal class TestSignatureHelpSourceProvider : ISignatureHelpSourceProvider
	{
		[Import]
		internal ITextDocumentFactoryService DocumentService { get; set; }

		public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer)
		{
			return new MSSignatureHelpSource(textBuffer, DocumentService);
		}
	}
}
