using System;
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
using Microsoft.VisualStudio.Text.Adornments;
using System.Windows;

//	Not used
namespace MyCompany.LanguageServices.MyScript
{
	/*[Export(typeof(IIntellisensePresenterProvider))]
	[ContentType("MyScript")]
	[Name("MyScript Intellisense provider")]
	[Order(Before = "Default Completion Presenter")]
	class MyPresenterProvider
		: IIntellisensePresenterProvider
	{
		[ImportMany]
		internal List<IIntellisensePresenterProvider> Providers;

		public IIntellisensePresenter TryCreateIntellisensePresenter(IIntellisenseSession session)
		{
			return new MyPresenter(session);
		}
	}*/
}
