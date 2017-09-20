using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.IO;


namespace MyCompany.LanguageServices.MyScript
{
	class MSXmlVariableDocumentation
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string Description { get; set; }
	}
	class MSXmlFunctionDocumentation
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string Description { get; set; }
		public List<MSXmlVariableDocumentation> Arguments { get; set; }
	}
	class MSXmlDocumentationFile
	{
		void Load(XmlDocument document)
		{
			XmlNode intellisenseNode = document.DocumentElement;

			foreach(XmlNode functionNode in intellisenseNode.ChildNodes)
			{
				MSXmlFunctionDocumentation functionDoc = new MSXmlFunctionDocumentation();
				functionDoc.Name = functionNode.ChildNodes[0].InnerText;
				functionDoc.Type = functionNode.ChildNodes[1].InnerText;
				functionDoc.Description = functionNode.ChildNodes[2].InnerText;
				functionDoc.Arguments = new List<MSXmlVariableDocumentation>();

				XmlNode argumentsNode = functionNode.ChildNodes[3];
				
				foreach(XmlNode variableNode in argumentsNode.ChildNodes)
				{
					MSXmlVariableDocumentation variableDoc = new MSXmlVariableDocumentation();
					variableDoc.Name = variableNode.ChildNodes[0].InnerText;
					variableDoc.Type = variableNode.ChildNodes[1].InnerText;
					variableDoc.Description = variableNode.ChildNodes[2].InnerText;
					functionDoc.Arguments.Add(variableDoc);
				}

				m_functions.Add(functionDoc);
			}
		}
		public MSXmlDocumentationFile(string filename)
		{
			m_filename = filename;

			XmlReaderSettings settings = new XmlReaderSettings();
			
			XmlSchemaSet xs = new XmlSchemaSet();

			string schemaFilename = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\IntellisenseDoc.xsd";
			XmlSchema schema = xs.Add("http://tempuri.org/IntellisenseDoc.xsd", schemaFilename);
			settings.Schemas.Add(schema);
			settings.ValidationEventHandler += ValidationEventHandler;
			settings.ValidationFlags = settings.ValidationFlags | XmlSchemaValidationFlags.ReportValidationWarnings;
			settings.ValidationType = ValidationType.Schema;
			settings.IgnoreWhitespace = true;
			settings.IgnoreComments = true;

			XmlReader reader = XmlReader.Create(filename, settings);
			XmlDocument document = new XmlDocument();
			document.PreserveWhitespace = true;
			document.Load(reader);
			reader.Close();

			document.Validate(ValidationEventHandler);

			Load(document);
		}

		private void ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e)
		{
			throw new Exception();
		}

		private List<MSXmlFunctionDocumentation> m_functions = new List<MSXmlFunctionDocumentation>();
		public IList<MSXmlFunctionDocumentation> Functions
		{
			get
			{
				return m_functions;
			}
		}

		private string m_filename;
		public string Filename
		{
			get
			{
				return m_filename;
			}
		}
	}
}
