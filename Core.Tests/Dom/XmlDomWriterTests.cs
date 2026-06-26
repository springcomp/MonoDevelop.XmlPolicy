// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Dom
{
	[TestFixture]
	public class XmlDomWriterTests
	{
		static XDocument ParseXml (string xml)
		{
			var parser = new XmlTreeParser (new XmlRootState ());
			var (doc, _) = parser.Parse (new System.IO.StringReader (xml));
			return doc;
		}

		[Test]
		public void SimpleElementRoundTrips ()
		{
			const string xml = "<root/>";
			var doc = ParseXml (xml);
			var result = doc.ToXmlString ();
			Assert.That (result, Is.EqualTo (xml));
		}

		[Test]
		public void ElementWithChildrenRoundTrips ()
		{
			const string xml = "<root><child/><child/></root>";
			var doc = ParseXml (xml);
			Assert.That (doc.ToXmlString (), Is.EqualTo (xml));
		}

		[Test]
		public void ElementWithAttributesRoundTrips ()
		{
			const string xml = "<root id=\"1\" name=\"hello\"/>";
			var doc = ParseXml (xml);
			Assert.That (doc.ToXmlString (), Is.EqualTo (xml));
		}

		[Test]
		public void TextContentRoundTrips ()
		{
			const string xml = "<root>hello world</root>";
			var doc = ParseXml (xml);
			Assert.That (doc.ToXmlString (), Is.EqualTo (xml));
		}

		[Test]
		public void CommentRoundTrips ()
		{
			const string xml = "<!-- a comment -->";
			var doc = ParseXml (xml);
			Assert.That (doc.ToXmlString (), Is.EqualTo (xml));
		}

		[Test]
		public void CDataRoundTrips ()
		{
			const string xml = "<root><![CDATA[some <raw> content]]></root>";
			var doc = ParseXml (xml);
			Assert.That (doc.ToXmlString (), Is.EqualTo (xml));
		}

		[Test]
		public void SpecialCharsInTextAreEscaped ()
		{
			// Create a text node with literal special characters (as set by mutation/build APIs)
			var textNode = new XText (0);
			textNode.End ("a & b < c");
			var root = new XElement (-1, new XName ("root"));
			root.AddChildNode (textNode);
			root.Close (new XClosingTag (-1));

			var result = root.ToXmlString ();
			Assert.That (result, Does.Contain ("&amp;"));
			Assert.That (result, Does.Contain ("&lt;"));
		}

		[Test]
		public void SpecialCharsInAttributeValueAreEscaped ()
		{
			var doc = ParseXml ("<root attr=\"a &amp; b\"/>");
			var serialized = doc.ToXmlString ();
			Assert.That (serialized, Does.Contain ("&amp;"));
		}

		[Test]
		public void HandBuiltTreeSerializesWithInvalidSpans ()
		{
			// Use start=-1 so spans are invalid (TextSpan.Invalid has Start=-1)
			var root = new XElement (-1, new XName ("root"));
			var child = new XElement (-1, new XName ("child"));
			child.Close (child);
			root.AddChildNode (child);
			root.Close (new XClosingTag (-1));

			var result = root.ToXmlString ();
			Assert.That (result, Is.EqualTo ("<root><child/></root>"));
		}

		[Test]
		public void HandBuiltTreeHasInvalidSpans ()
		{
			var root = new XElement (-1, new XName ("root"));
			Assert.That (root.HasValidSpan, Is.False);
		}

		[Test]
		public void WriteContentToEmitsOnlyChildren ()
		{
			var doc = ParseXml ("<root><a/><b/></root>");
			var sb = new System.Text.StringBuilder ();
			using var writer = new System.IO.StringWriter (sb);
			doc.RootElement!.WriteContentTo (writer);
			Assert.That (sb.ToString (), Is.EqualTo ("<a/><b/>"));
		}

		[Test]
		public void NamespacePrefixedElementRoundTrips ()
		{
			const string xml = "<ns:root xmlns:ns=\"http://example.com\"/>";
			var doc = ParseXml (xml);
			var result = doc.ToXmlString ();
			Assert.That (result, Does.Contain ("ns:root"));
		}

		[Test]
		public void ParseSerializeReParseIsStructurallyEquivalent ()
		{
			const string xml = "<root id=\"42\"><child>text</child><!-- note --></root>";
			var doc1 = ParseXml (xml);
			var serialized = doc1.ToXmlString ();
			var doc2 = ParseXml (serialized);

			var root1 = doc1.RootElement!;
			var root2 = doc2.RootElement!;

			Assert.That (root2.Name.FullName, Is.EqualTo (root1.Name.FullName));
			Assert.That (root2.Attributes.Count, Is.EqualTo (root1.Attributes.Count));

			var children1 = System.Linq.Enumerable.ToList (root1.Nodes);
			var children2 = System.Linq.Enumerable.ToList (root2.Nodes);
			Assert.That (children2.Count, Is.EqualTo (children1.Count));
		}
	}
}
