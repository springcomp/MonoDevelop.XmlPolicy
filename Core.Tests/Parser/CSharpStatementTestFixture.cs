using System;
using System.Linq;
using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class CSharpStatementTestFixture
	{
		[Test]
		public void XmlAttributeValueState ()
		{
			AssertInsideAttributeValue ("<xml-element attribute=\"@$");
		}

		[Test]
		public void EmptyCSharpStatement ()
		{
			var parser = new XmlTreeParser (new XmlRootState ());
			var result = parser.Parse ("<set-variable value=\"@($)",
				() => {
					parser.AssertStateIs<XmlCSharpStatementState> ();
				});

			parser.AssertDiagnostics (
				(XmlCoreDiagnostics.EmptyCSharpStatementValue, 21, 2),
				(XmlCoreDiagnostics.IncompleteAttributeEof, 24, 0),
				(XmlCoreDiagnostics.IncompleteTagEof, 24, 0)
			);
		}

		[Test]
		public void IncompleteCSharpStatement_Eof ()
		{
			var parser = new XmlTreeParser (new XmlRootState ());
			var result = parser.Parse ("<set-variable value=\"@($",
				() => {
					parser.AssertStateIs<XmlCSharpStatementState> ();
				});

			parser.AssertDiagnostics (
				(XmlCoreDiagnostics.IncompleteCSharpStatementEof, 21, 2),
				(XmlCoreDiagnostics.EmptyCSharpStatementValue, 21, 2),
				(XmlCoreDiagnostics.IncompleteAttributeEof, 23, 0),
				(XmlCoreDiagnostics.IncompleteTagEof, 23, 0)
			);
		}

		[TestCase("<set-variable value=\"@( \"hello, world!\"$ )\" />", " \"hello, world!\" ")] // "hello, world"
		[TestCase("<set-variable value=\"@( (string)\"hello\"$ )\" />", " (string)\"hello\" ")] // (string)"hello"
		[TestCase("<set-variable value=\"@( (string)\"(\"$ )\" />", " (string)\"(\" ")] // (string)"("
		[TestCase("<set-variable value=\"@( (string)\")\"$ )\" />", " (string)\")\" ")] // (string)")"
		[TestCase("<set-variable value=\"@( \"\\\"\"$ )\" />", " \"\\\"\" ")] // "\""
		public void CSharpStatement (string xml, string expected)
		{
			var parser = new XmlTreeParser (new XmlRootState ());
			var result = parser.Parse (xml,
				() => {
					parser.AssertStateIs<XmlCSharpStatementState> ();
				});

			var document = result.doc;
			var root = document.RootElement;
			var attribute = root!.Attributes.Single (att => att.Name == "value");

			Assert.NotNull(attribute);
			Assert.NotNull (attribute.Value);

			Assert.True(XAttributeValue.IsCSharpStatement(attribute.Value));
			Assert.AreEqual (expected, attribute.Value.As<XCSharpStatement> ().RawText ?? "");
		}

		public void AssertInsideAttributeValue (string doc)
		{
			TestXmlParser.Parse (doc, p => p.AssertStateIs<XmlAttributeValueState> ());
		}
	}
}
