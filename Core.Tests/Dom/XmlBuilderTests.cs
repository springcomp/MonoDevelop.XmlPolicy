// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using MonoDevelop.Xml.Dom;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Dom
{
	[TestFixture]
	public class XmlBuilderTests
	{
		// ── Convenience constructor span checks ──────────────────────────────

		[Test]
		public void XElementConvenienceCtorHasInvalidSpan ()
		{
			var el = new XElement ((XName)"foo");
			Assert.That (el.Span.IsValid, Is.False);
		}

		[Test]
		public void XAttributeConvenienceCtorNameOnlyHasInvalidSpan ()
		{
			var attr = new XAttribute ((XName)"id");
			Assert.That (attr.Span.IsValid, Is.False);
			Assert.That (attr.HasValue, Is.False);
		}

		[Test]
		public void XAttributeConvenienceCtorNameValueHasInvalidSpan ()
		{
			var attr = new XAttribute ((XName)"id", "42");
			Assert.That (attr.Span.IsValid, Is.False);
			Assert.That (attr.Value, Is.EqualTo ("42"));
		}

		[Test]
		public void XTextConvenienceCtorHasInvalidSpan ()
		{
			var text = new XText ("hello");
			Assert.That (text.Span.IsValid, Is.False);
			Assert.That (text.Text, Is.EqualTo ("hello"));
		}

		[Test]
		public void XCommentConvenienceCtorHasInvalidSpan ()
		{
			var comment = new XComment ("a comment");
			Assert.That (comment.Span.IsValid, Is.False);
			Assert.That (comment.InnerText, Is.EqualTo ("a comment"));
		}

		[Test]
		public void XCDataConvenienceCtorHasInvalidSpan ()
		{
			var cdata = new XCData ("raw content");
			Assert.That (cdata.Span.IsValid, Is.False);
			Assert.That (cdata.InnerText, Is.EqualTo ("raw content"));
		}

		// ── Hand-built tree serialization ────────────────────────────────────

		[Test]
		public void HandBuiltTreeSerializesToValidXml ()
		{
			var root = new XElement ((XName)"root");
			root.Attributes.AddAttribute (new XAttribute ((XName)"id", "1"));
			var child = new XElement ((XName)"child");
			child.AddChildNode (new XText ("hello"));
			root.AddChildNode (child);

			var xml = root.ToXmlString ();
			Assert.That (xml, Is.EqualTo ("<root id=\"1\"><child>hello</child></root>"));
		}

		[Test]
		public void HandBuiltCommentAndCDataSerialize ()
		{
			var root = new XElement ((XName)"root");
			root.AddChildNode (new XComment ("a comment"));
			root.AddChildNode (new XCData ("raw&data"));

			var xml = root.ToXmlString ();
			Assert.That (xml, Is.EqualTo ("<root><!--a comment--><![CDATA[raw&data]]></root>"));
		}

		[Test]
		public void HandBuiltSelfClosingElementSerializes ()
		{
			var el = new XElement ((XName)"br");
			el.Close (el);

			Assert.That (el.ToXmlString (), Is.EqualTo ("<br/>"));
		}

		// ── Fluent builder ───────────────────────────────────────────────────

		[Test]
		public void BuilderProducesExpectedXml ()
		{
			XElement root = XmlBuilder.Element ("root")
				.Attr ("id", "1")
				.Child (XmlBuilder.Element ("child").Text ("hi"))
				.Build ();

			Assert.That (root.ToXmlString (), Is.EqualTo ("<root id=\"1\"><child>hi</child></root>"));
		}

		[Test]
		public void BuilderSelfCloseProducesSelfClosingElement ()
		{
			XElement el = XmlBuilder.Element ("img").Attr ("src", "logo.png").SelfClose ();
			Assert.That (el.ToXmlString (), Is.EqualTo ("<img src=\"logo.png\"/>"));
		}

		[Test]
		public void BuilderImplicitConversionToXElement ()
		{
			XElement el = XmlBuilder.Element ("p").Text ("text");
			Assert.That (el, Is.InstanceOf<XElement> ());
			Assert.That (el.Name.FullName, Is.EqualTo ("p"));
		}

		[Test]
		public void BuilderCommentAndCData ()
		{
			XElement el = XmlBuilder.Element ("root")
				.Comment ("note")
				.CData ("raw")
				.Build ();

			Assert.That (el.ToXmlString (), Is.EqualTo ("<root><!--note--><![CDATA[raw]]></root>"));
		}

		[Test]
		public void BuilderNodeHasNoFakeOffset ()
		{
			XElement el = XmlBuilder.Element ("x").Attr ("a", "b").Build ();
			Assert.That (el.Span.IsValid, Is.False);
			foreach (var attr in el.Attributes)
				Assert.That (attr.Span.IsValid, Is.False);
		}
	}
}
