//------------------------------------------------------------------------------
// <copyright file="MyScriptLanguagePackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace MyCompany.LanguageServices.MyScript
{
    //  Language service attributes
    [ProvideService(typeof(MSLanguageService),
                             ServiceName = "MyScript Language Service")]
    [ProvideLanguageService(typeof(MSLanguageService),
                             "MyScript",
                             106,             // resource ID of localized language name
                             CodeSense = true,             // Supports IntelliSense
                             RequestStockColors = false,   // Supplies custom colors
                             EnableCommenting = true,      // Supports commenting out code
                             EnableAsyncCompletion = true  // Supports background parsing
                             )]
    [ProvideLanguageExtension(typeof(MSLanguageService),
                                       ".ms")]
    [ProvideLanguageCodeExpansion(
             typeof(MSLanguageService),
             "MyScript", // Name of language used as registry key.
             106,           // Resource ID of localized name of language service.
             "MyScript",  // language key used in snippet templates.
             @"%InstallRoot%\MyScript\SnippetsIndex.xml",  // Path to snippets index
             SearchPaths = @"%InstallRoot%\MyScript\Snippets\%LCID%\Snippets\;" +
                           @"%TestDocs%\Code Snippets\MyScript\Test Code Snippets"
             )]

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(MyScriptLanguagePackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class MyScriptLanguagePackage : Package
    {
        /// <summary>
        /// MyScriptLanguagePackage GUID string.
        /// </summary>
        public const string PackageGuidString = "88d84906-85b8-4ed0-800b-8b31b2a51b5e";

        /// <summary>
        /// Initializes a new instance of the <see cref="MyScriptLanguagePackage"/> class.
        /// </summary>
        public MyScriptLanguagePackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Proffer the service.
            IServiceContainer serviceContainer = this as IServiceContainer;
            MSLanguageService langService = new MSLanguageService();
            langService.SetSite(this);
            serviceContainer.AddService(typeof(MSLanguageService),
                                        langService,
                                        true);
        }

        #endregion
    }
}
