using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Reflection;
using Microsoft.VisualStudio.Text.Formatting;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Text.Classification;
using System.Threading;
using Microsoft.VisualStudio.Language.StandardClassification;

namespace MyCompany.LanguageServices.MyScript
{
	internal static class DependencyObjectExtensions
	{
		public static void SetTextProperties(this DependencyObject dependencyObject, TextFormattingRunProperties textProperties)
		{
			dependencyObject.SetValue(TextElement.FontFamilyProperty, (object)textProperties.Typeface.FontFamily);
			dependencyObject.SetValue(TextElement.FontSizeProperty, (object)textProperties.FontRenderingEmSize);
			dependencyObject.SetValue(TextElement.FontStyleProperty, (object)(textProperties.Italic ? FontStyles.Italic : FontStyles.Normal));
			dependencyObject.SetValue(TextElement.FontWeightProperty, (object)(textProperties.Bold ? FontWeights.Bold : FontWeights.Normal));
			dependencyObject.SetValue(TextElement.BackgroundProperty, (object)textProperties.BackgroundBrush);
			dependencyObject.SetValue(TextElement.ForegroundProperty, (object)textProperties.ForegroundBrush);
		}

		public static void SetDefaultTextProperties(this DependencyObject dependencyObject, IClassificationFormatMap formatMap)
		{
			dependencyObject.SetTextProperties(formatMap.DefaultTextProperties);
		}
	}

	internal class MSQuickInfoSource : IQuickInfoSource
	{
		private MSQuickInfoSourceProvider m_provider;
		private ITextBuffer m_buffer;
		private IGlyphService m_glyphService;
		private ITextDocumentFactoryService m_documentService;
		private MSBackgroundParser m_backgroundParser = null;
		private IClassificationTypeRegistryService m_typeRegistryService;
		private IClassificationFormatMapService m_classificationFormatMapService;
		private IStandardClassificationService m_standardClassificationService;

		private TextFormattingRunProperties m_keywordFormatProperties = null;
		private TextFormattingRunProperties m_userTypeFormatProperties = null;
		private TextFormattingRunProperties m_textFormatProperties = null;

		public MSQuickInfoSource(
			MSQuickInfoSourceProvider provider,
			ITextBuffer buffer,
			IGlyphService glyphService,
			ITextDocumentFactoryService documentService,
			IClassificationTypeRegistryService typeRegistryService,
			IClassificationFormatMapService classificationFormatMapService,
			IStandardClassificationService standardClassificationService)
		{
			m_provider = provider;
			m_buffer = buffer;
			m_glyphService = glyphService;
			m_documentService = documentService;
			m_typeRegistryService = typeRegistryService;
			m_classificationFormatMapService = classificationFormatMapService;
			m_standardClassificationService = standardClassificationService;

			IClassificationFormatMap classificationFormatMap = m_classificationFormatMapService.GetClassificationFormatMap("tooltip");

			m_keywordFormatProperties = classificationFormatMap.GetTextProperties(m_typeRegistryService.GetClassificationType("keyword"));
			m_userTypeFormatProperties = classificationFormatMap.GetTextProperties(m_typeRegistryService.GetClassificationType("symbol definition"));
			m_textFormatProperties = classificationFormatMap.GetTextProperties(m_typeRegistryService.GetClassificationType("text"));

			m_backgroundParser = MSBackgroundParser.GetOrCreateFromTextBuffer(buffer, documentService);
		}

		private bool AddQIDocumentationContent(string text, IList<MSXmlDocumentationFile> docFiles, IList<object> qiContent)
		{
			foreach (MSXmlDocumentationFile docFile in docFiles)
			{
				foreach (MSXmlFunctionDocumentation function in docFile.Functions)
				{
					if (function.Name == text)
					{
						List<Tuple<string, string>> arguments = new List<Tuple<string, string>>();
						foreach (MSXmlVariableDocumentation arg in function.Arguments)
						{
							arguments.Add(new Tuple<string, string>(arg.Type, arg.Name));
						}

						AddQIFunctionDeclarationContent(function.Type, function.Name, function.Description, arguments, qiContent);
						return true;
					}
				}
			}

			return false;
		}
		//	Add quick info content for a builtin type
		private bool AddQIBuiltinTypeContent(string text, IList<object> qiContent)
		{
			if (!MSLanguage.BuiltinTypes.Contains(text))
				return false;
			
			TextBlock dependencyObject = new TextBlock();
			dependencyObject.TextWrapping = TextWrapping.NoWrap;

			Image icon = new Image();
			icon.Source = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic);
			InlineUIContainer uiContainer = new InlineUIContainer(icon);
			uiContainer.BaselineAlignment = BaselineAlignment.Center;

			dependencyObject.Inlines.Add(uiContainer);

			Run run = null;
			run = new Run(" " + text);
			run.SetTextProperties(m_keywordFormatProperties);
			dependencyObject.Inlines.Add(run);

			if (text == "bool")
			{
				run = new Run(Environment.NewLine + "Represents a Boolean (true or false) value.");
				run.SetTextProperties(m_textFormatProperties);
				dependencyObject.Inlines.Add(run);
			}
			else if (text == "float")
			{
				run = new Run(Environment.NewLine + "Represents a single-precision floating-point number.");
				run.SetTextProperties(m_textFormatProperties);
				dependencyObject.Inlines.Add(run);
			}
			else if (text == "int")
			{
				run = new Run(Environment.NewLine + "Represents a 32-bit signed integer.");
				run.SetTextProperties(m_textFormatProperties);
				dependencyObject.Inlines.Add(run);
			}
			else if (text == "string")
			{
				run = new Run(Environment.NewLine + "Represents text as a series of ascii characters.");
				run.SetTextProperties(m_textFormatProperties);
				dependencyObject.Inlines.Add(run);
			}

			qiContent.Add(dependencyObject);

			return true;
		}

		private void AddQIVariableDeclarationContent(string type, string name, bool isArgument, IList<object> qiContent)
		{
			TextBlock dependencyObject = new TextBlock();
			dependencyObject.TextWrapping = TextWrapping.NoWrap;

			Image icon = new Image();
			icon.Source = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);
			InlineUIContainer uiContainer = new InlineUIContainer(icon);
			uiContainer.BaselineAlignment = BaselineAlignment.Center;

			dependencyObject.Inlines.Add(uiContainer);

			Run run = null;
			if (isArgument)
			{
				run = new Run(" (parameter) ");
				run.SetTextProperties(m_textFormatProperties);
				dependencyObject.Inlines.Add(run);
			}
			else
			{
				run = new Run(" (local variable) ");
				run.SetTextProperties(m_textFormatProperties);
				dependencyObject.Inlines.Add(run);
			}

			if (type == null)
			{
				run = new Run("<error type> ");
				run.SetTextProperties(m_textFormatProperties);
				dependencyObject.Inlines.Add(run);
			}
			else
			{
				run = new Run(type + " ");
				run.SetTextProperties(m_keywordFormatProperties);
				dependencyObject.Inlines.Add(run);
			}

			run = new Run(name);
			run.SetTextProperties(m_textFormatProperties);
			dependencyObject.Inlines.Add(run);

			qiContent.Add(dependencyObject);
		}
		private void AddQIFunctionDeclarationContent(string type, string name, string comment, IList<Tuple<string, string>> arguments, IList<object> qiContent)
		{
			TextBlock dependencyObject = new TextBlock();
			dependencyObject.TextWrapping = TextWrapping.NoWrap;

			Image icon = new Image();
			icon.Source = m_glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
			InlineUIContainer uiContainer = new InlineUIContainer(icon);
			uiContainer.BaselineAlignment = BaselineAlignment.Center;
			dependencyObject.Inlines.Add(uiContainer);

			Run run = new Run("function ");
			run.SetTextProperties(m_keywordFormatProperties);
			dependencyObject.Inlines.Add(run);

			run = new Run(name);
			run.SetTextProperties(m_textFormatProperties);
			dependencyObject.Inlines.Add(run);

			run = new Run("(");
			run.SetTextProperties(m_textFormatProperties);
			dependencyObject.Inlines.Add(run);
			
            if(arguments.Count == 0)
            {
                run = new Run(")");
                run.SetTextProperties(m_textFormatProperties);
                dependencyObject.Inlines.Add(run);
            }

			for (int i = 0; i < arguments.Count(); ++i)
			{
				Tuple<string, string> arg = arguments.ElementAt(i);

				string argType = arg.Item1;
				string argName = arg.Item2;

				if(argType == null)
				{
					run = new Run("<error type> ");
					run.SetTextProperties(m_textFormatProperties);
					dependencyObject.Inlines.Add(run);
				}
				else
				{
					run = new Run(argType + " ");
					run.SetTextProperties(m_keywordFormatProperties);
					dependencyObject.Inlines.Add(run);
				}

				if (argName == null && argType != null)
				{
					run = new Run("<error>");
					run.SetTextProperties(m_textFormatProperties);
					dependencyObject.Inlines.Add(run);
				}
				else
				{
					run = new Run(argName);
					run.SetTextProperties(m_textFormatProperties);
					dependencyObject.Inlines.Add(run);
				}

				if(i < arguments.Count() - 1)
				{
					run = new Run(", ");
					run.SetTextProperties(m_textFormatProperties);
					dependencyObject.Inlines.Add(run);
				}
				else
				{
					run = new Run(")");
					run.SetTextProperties(m_textFormatProperties);
					dependencyObject.Inlines.Add(run);
				}
			}
			
			
			run = new Run(" : ");
			run.SetTextProperties(m_textFormatProperties);
			dependencyObject.Inlines.Add(run);

            if (type != null)
            {
                run = new Run(type);
				run.SetTextProperties(m_keywordFormatProperties);
				dependencyObject.Inlines.Add(run);
			}
            else
            {
                run = new Run("void");
                run.SetTextProperties(m_keywordFormatProperties);
                dependencyObject.Inlines.Add(run);
            }

			if (!string.IsNullOrEmpty(comment))
			{
				run = new Run(Environment.NewLine + comment);
				run.SetTextProperties(m_textFormatProperties);
				dependencyObject.Inlines.Add(run);
			}

			qiContent.Add(dependencyObject);
		}
		bool AddQIDeclarationContent(int index, string text, IList<SyntaxNode> syntaxTree, IList<object> qiContent)
		{
			//	If cursor is in function, search local then/else enumerate statements
			foreach(SyntaxNode node in syntaxTree)
			{
				if (node is FunctionNode)
				{
					FunctionNode functionNode = node as FunctionNode;

					//	Outside of function boundaries
					if (functionNode.separatorTokens.Count == 0 || functionNode.separatorTokens.Count == 0 || index < functionNode.separatorTokens.First().Span.End)
						continue;
					
					if (functionNode.endToken != null && index > functionNode.endToken.Span.Start)
						continue;

					//	Search for local declarations
					if (AddQIDeclarationContent(index, text, functionNode.statements, qiContent))
						return true;

					//	Search for arguments
					foreach(FunctionNode.Argument arg in functionNode.arguments)
					{
						if(arg.nameExpression != null && arg.nameExpression is NameNode && (arg.nameExpression as NameNode).token.Text == text)
						{
							//	Found, lets add content to quick info
							string type = null;
							string name = null;

							if (arg.typeExpression != null && arg.typeExpression is TypeNode)
								type = (arg.typeExpression as TypeNode).token.Text;

							name = (arg.nameExpression as NameNode).token.Text;

							AddQIVariableDeclarationContent(type, name, true, qiContent);

							return true;
						}
					}

					//	No need to check other nodes, since cursor is inside this one, it is not in another one.
					break;
				}
			}

			foreach(SyntaxNode node in syntaxTree)
			{
				if(node is AssignmentNode)
				{
					AssignmentNode assignNode = node as AssignmentNode;

					//	Not has no type, its not a declaration but an assignment. Skip
					if (assignNode.typeExpression == null)
						continue;

					//	Security check
					if (assignNode.varExpression == null || !(assignNode.varExpression is NameNode))
						continue;

					NameNode name = assignNode.varExpression as NameNode;
					if (index >= name.token.Span.Start && name.token.Text == text)
					{
						//	Found, lets add content to quick info
						string typeText = null;
						string nameText = null;

						if (assignNode.typeExpression != null && assignNode.typeExpression is TypeNode)
							typeText = (assignNode.typeExpression as TypeNode).token.Text;

						if (assignNode.varExpression != null && assignNode.varExpression is NameNode)
							nameText = (assignNode.varExpression as NameNode).token.Text;

						AddQIVariableDeclarationContent(typeText, nameText, false, qiContent);

						return true;
					}
				}
				else if(node is FunctionNode)
				{
					FunctionNode functionNode = node as FunctionNode;

					//	Security check
					if (functionNode.nameExpression == null || !(functionNode.nameExpression is NameNode))
						continue;

					NameNode name = functionNode.nameExpression as NameNode;
					if(name.token.Text == text)
					{
						//	Found, lets add content to quick info
						string typeText = null;
						string nameText = null;

						if (functionNode.returnTypeExpression != null && functionNode.returnTypeExpression is TypeNode)
							typeText = (functionNode.returnTypeExpression as TypeNode).token.Text;

						if (functionNode.nameExpression != null && functionNode.nameExpression is NameNode)
							nameText = (functionNode.nameExpression as NameNode).token.Text;

						List<Tuple<string, string>> arguments = new List<Tuple<string, string>>();
						foreach(FunctionNode.Argument arg in functionNode.arguments)
						{
							string argTypeText = null;
							string argNameText = null;

							if (arg.typeExpression != null && arg.typeExpression is TypeNode)
								argTypeText = (arg.typeExpression as TypeNode).token.Text;

							if (arg.nameExpression != null && arg.nameExpression is NameNode)
								argNameText = (arg.nameExpression as NameNode).token.Text;

							arguments.Add(new Tuple<string, string>(argTypeText, argNameText));

						}

						AddQIFunctionDeclarationContent(typeText, nameText, functionNode.comment, arguments, qiContent);

						return true;
					}
				}
			}

			//	Declaration not found
			return false;
		}
		public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
		{
			/*
			 * Get word at carret position.
			 * Check if word is in function :
			 *		- yes :
			 *			1) Search for matching declaration in function's local variables
			 *			2) Search for matching declartion in function's parameters
			 *			3) Search for matching declartion in global functions list
			 *		- no :
			 *			1) Search for matching declartion in global variables
			 *			2) Search for matching declartion in global functions list
			 */


			// Map the trigger point down to our buffer.
			SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(m_buffer.CurrentSnapshot);
			if (!subjectTriggerPoint.HasValue)
			{
				applicableToSpan = null;
				return;
			}

			ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
			SnapshotSpan querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);

			//look for occurrences of our QuickInfo words in the span
			ITextStructureNavigator navigator = m_provider.NavigatorService.GetTextStructureNavigator(m_buffer);
			TextExtent extent = navigator.GetExtentOfWord(subjectTriggerPoint.Value);
			string searchText = extent.Span.GetText();
			
			//	Get parsed snapshot and tree
			ITextSnapshot parseSnapshot = null;
			IList<SyntaxNode> syntaxTree = null;
			IList<MSXmlDocumentationFile> docFiles = null;

			lock (m_backgroundParser.Lock)
			{
				parseSnapshot = m_backgroundParser.Snapshot;
				syntaxTree = m_backgroundParser.SyntaxTree;
				docFiles = m_backgroundParser.XmlDocs;
			}

			if (parseSnapshot == null || syntaxTree == null)
			{
				//	Background parser did not finish 1st parsing yet
				applicableToSpan = currentSnapshot.CreateTrackingSpan
				(
					extent.Span.Start, extent.Span.Length, SpanTrackingMode.EdgeInclusive
				);
				qiContent.Add("Intellisense documentation is under construction");
				return;
			}

			SnapshotPoint translatedSnapshotPoint = subjectTriggerPoint.Value.TranslateTo(parseSnapshot, PointTrackingMode.Positive);

			bool contentAdded = AddQIDeclarationContent(translatedSnapshotPoint, searchText, syntaxTree, qiContent);
			if (!contentAdded)
				contentAdded = AddQIDocumentationContent(searchText, docFiles, qiContent);
			if (!contentAdded)
				contentAdded = AddQIBuiltinTypeContent(searchText, qiContent);

			if (contentAdded)
			{
				applicableToSpan = currentSnapshot.CreateTrackingSpan
				(
					extent.Span.Start, extent.Span.Length, SpanTrackingMode.EdgeInclusive
				);
			}
			else
			{
			//	/!\	Should check if in comment, etc.
#warning dunno about that
			/*if(IsIdentifier(searchText) && !IsKeyword(searchText))
			{
				applicableToSpan = currentSnapshot.CreateTrackingSpan
				(
					extent.Span.Start, extent.Span.Length, SpanTrackingMode.EdgeInclusive
				);
				qiContent.Add("The name '" + searchText + "' does not exist in the current context");
			}
			else
			{*/
				applicableToSpan = null;
			}
		}

		private bool m_isDisposed;
		public void Dispose()
		{
			if (!m_isDisposed)
			{
				GC.SuppressFinalize(this);
				m_isDisposed = true;
			}
		}
	}
}
