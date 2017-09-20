using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;

namespace MyCompany.LanguageServices.MyScript
{
    /*
     * This is a legacy language service. Its only used for the language option page.
     * Other modern MEF language services seems to use it for options too (like c#).
     * */
    [Guid("2A7BFD98-F209-4078-A99D-F7102EABB147")]
    class MSLanguageService
        : LanguageService
    {
        private LanguagePreferences m_preferences = null;

        public MSLanguageService()
        {

        }


        public override IScanner GetScanner(IVsTextLines buffer)
        {
            return null;
        }

        public override AuthoringScope ParseSource(ParseRequest req)
        {
            return null;
        }

        public override string Name
        {
            get { return "MyScript"; }
        }

        public override string GetFormatFilterList()
        {
            return "MyScript files (*.ms)|*.ms";
        }

        public override LanguagePreferences GetLanguagePreferences()
        {
            if (m_preferences == null)
            {
                m_preferences = new LanguagePreferences(this.Site,
                                                        typeof(MSLanguageService).GUID,
                                                        this.Name);
                m_preferences.Init();
            }
            return m_preferences;
        }
    }
}
