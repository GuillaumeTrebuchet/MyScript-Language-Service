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
	class MyPresenter
		: IPopupIntellisensePresenter
	{
		public MyPresenter(IIntellisenseSession session)
		{
			Session = session;
		}
		public UIElement SurfaceElement { get; set; }

		public ITrackingSpan PresentationSpan { get; set; }

		public PopupStyles PopupStyles { get; set; }

		public string SpaceReservationManagerName { get; set; }

		public double Opacity { get; set; }

		public IIntellisenseSession Session { get; set; }

		public event EventHandler SurfaceElementChanged;
		public event EventHandler PresentationSpanChanged;
		public event EventHandler<ValueChangedEventArgs<PopupStyles>> PopupStylesChanged;
	}
}
