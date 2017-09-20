//------------------------------------------------------------------------------
// <copyright file="MLClassifierProvider.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Language.StandardClassification;
using System;

namespace MyCompany.LanguageServices.MyScript
{
    /// <summary>
    /// Classifier provider. It adds the classifier to the set of classifiers.
    /// </summary>
	[Name("MyScript classifier provider")]
    [Export(typeof(IClassifierProvider))]
    [ContentType("MyScript")] // This classifier applies to all text files.
	[ContentType("MyScript Signature Help")]
	internal class MSClassifierProvider : IClassifierProvider
    {
        // Disable "Field is never assigned to..." compiler's warning. Justification: the field is assigned by MEF.
#pragma warning disable 649

        /// <summary>
        /// Classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
        private IClassificationTypeRegistryService classificationRegistry;

        [Import]
        private IStandardClassificationService standardClassificationService;

        [Import]
        internal ITextDocumentFactoryService DocumentService { get; set; }
#pragma warning restore 649

		#region IClassifierProvider
		
		/// <summary>
		/// Gets a classifier for the given text buffer.
		/// </summary>
		/// <param name="buffer">The <see cref="ITextBuffer"/> to classify.</param>
		/// <returns>A classifier for the text buffer, or null if the provider cannot do so in its current state.</returns>
		public IClassifier GetClassifier(ITextBuffer buffer)
        {
			Func<MSClassifier> createClassifier = delegate () { return new MSClassifier(buffer, this.classificationRegistry, this.standardClassificationService, this.DocumentService); };
			return buffer.Properties.GetOrCreateSingletonProperty("MSClassifier", createClassifier);
        }

        #endregion
    }
}
