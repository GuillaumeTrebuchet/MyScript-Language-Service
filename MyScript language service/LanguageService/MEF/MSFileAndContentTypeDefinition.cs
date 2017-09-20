using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

/*
 * Defines content type for MyScript.
 * This is used by other MEF component to identify the content they are used for.
 * */
namespace MyCompany.LanguageServices.MyScript
{
    internal static class MSFileAndContentTypeDefinitions
    {
        [Export]
        [Name("MyScript")]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition MSContentTypeDefinition;

        [Export]
        [FileExtension(".ms")]
        [ContentType("MyScript")]
        internal static FileExtensionToContentTypeDefinition MSFileExtensionDefinition;
    }
}
