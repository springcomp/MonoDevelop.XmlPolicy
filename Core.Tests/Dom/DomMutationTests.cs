// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Linq;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Dom
{
	/// <summary>
	/// Comprehensive mutation tests covering T-004 through T-009:
	/// text/value mutation, span contract, structural mutation, re-parenting,
	/// PreviousSibling, cycle guards, and regression checks for XmlDomExtensions.
	/// </summary>
	[TestFixture]
	public class DomMutationTests
	{
		static XDocument ParseXml (string xml)
		{
			var parser = new XmlTreeParser (new XmlRootState ());
			var (doc, _) = parser.Parse (new System.IO.StringReader (xml));
			return doc;
		}

		// ── T-004: Text / value mutation ─────────────────────────────────────

		[Test]
		public void SetValueChangesAttributeContentAndInvalidatesSpan ()
		{
			var doc = ParseXml ("<root attr=\"old\"/>");
			var root = doc.RootElement!;
			var attr = root.Attributes.First!;

			Assert.That (attr.Span.IsValid, Is.True, "parsed attribute must start with a valid span");

			attr.SetValue ("new");

			Assert.That (attr.Value, Is.EqualTo ("new"));
			Assert.That (attr.Span.IsValid, Is.False, "attribute span must be invalidated after SetValue");
			Assert.That (root.Span.IsValid, Is.False, "parent element span must be invalidated");
			Assert.That (doc.IsDirty, Is.True);
		}

		[Test]
		public void SetValueAppearsInSerialization ()
		{
			var doc = ParseXml ("<root attr=\"old\"/>");
			doc.RootElement!.Attributes.First!.SetValue ("new");

			Assert.That (doc.RootElement.ToXmlString (), Is.EqualTo ("<root attr=\"new\"/>"));
		}

		[Test]
		public void SetTextChangesXTextContentAndInvalidatesSpan ()
		{
			var doc = ParseXml ("<root>original</root>");
			var root = doc.RootElement!;
			var textNode = (XText) root.FirstChild!;

			Assert.That (textNode.Span.IsValid, Is.True);

			textNode.SetText ("updated");

			Assert.That (textNode.Text, Is.EqualTo ("updated"));
			Assert.That (textNode.Span.IsValid, Is.False);
			Assert.That (root.Span.IsValid, Is.False);
			Assert.That (doc.IsDirty, Is.True);
			Assert.That (root.ToXmlString (), Is.EqualTo ("<root>updated</root>"));
		}

		[Test]
		public void SetTextChangesXCommentContentAndInvalidatesSpan ()
		{
			var doc = ParseXml ("<!-- old -->");
			var comment = (XComment) doc.Nodes.First ()!;

			Assert.That (comment.Span.IsValid, Is.True);

			comment.SetText (" new ");

			Assert.That (comment.InnerText, Is.EqualTo (" new "));
			Assert.That (comment.Span.IsValid, Is.False);
			Assert.That (doc.IsDirty, Is.True);
			Assert.That (comment.ToXmlString (), Is.EqualTo ("<!-- new -->"));
		}

		[Test]
		public void SetTextChangesXCDataContentAndInvalidatesSpan ()
		{
			var doc = ParseXml ("<root><![CDATA[old]]></root>");
			var root = doc.RootElement!;
			var cdata = (XCData) root.FirstChild!;

			Assert.That (cdata.Span.IsValid, Is.True);

			cdata.SetText ("new");

			Assert.That (cdata.InnerText, Is.EqualTo ("new"));
			Assert.That (cdata.Span.IsValid, Is.False);
			Assert.That (root.Span.IsValid, Is.False);
			Assert.That (doc.IsDirty, Is.True);
			Assert.That (root.ToXmlString (), Is.EqualTo ("<root><![CDATA[new]]></root>"));
		}

		// ── T-005: Span contract on structural mutations ─────────────────────

		[Test]
		public void AddChildNodeInvalidatesAncestorSpansButNotPrecedingSiblings ()
		{
			var doc = ParseXml ("<root><a/><b/></root>");
			var root = doc.RootElement!;
			var a = (XElement) root.FirstChild!;
			var b = (XElement) a.NextSibling!;

			Assert.That (a.Span.IsValid, Is.True);
			Assert.That (b.Span.IsValid, Is.True);

			var newChild = new XElement ((XName) "c");
			root.AddChildNode (newChild);

			Assert.That (newChild.Span.IsValid, Is.False, "appended child has no valid source span");
			Assert.That (root.Span.IsValid, Is.False, "ancestor must be invalidated");
			Assert.That (doc.IsDirty, Is.True);
			// preceding siblings are NOT affected
			Assert.That (a.Span.IsValid, Is.True);
			Assert.That (b.Span.IsValid, Is.True);
		}

		[Test]
		public void RemoveChildInvalidatesFollowingSiblingsAndAncestors ()
		{
			var doc = ParseXml ("<root><a/><b/><c/></root>");
			var root = doc.RootElement!;
			var children = root.Nodes.Cast<XElement> ().ToList ();
			var a = children[0];
			var b = children[1];
			var c = children[2];

			root.RemoveChild (b);

			Assert.That (b.Span.IsValid, Is.False, "removed node must be invalidated");
			Assert.That (c.Span.IsValid, Is.False, "c was a following sibling of b and must be invalidated");
			Assert.That (root.Span.IsValid, Is.False, "ancestor must be invalidated");
			Assert.That (doc.IsDirty, Is.True);
			Assert.That (a.Span.IsValid, Is.True, "a precedes b and must not be invalidated");
		}

		[Test]
		public void InsertChildBeforeInvalidatesNewChildAndFollowers ()
		{
			var doc = ParseXml ("<root><a/><b/></root>");
			var root = doc.RootElement!;
			var a = (XElement) root.FirstChild!;
			var b = (XElement) a.NextSibling!;
			var newNode = new XElement ((XName) "x");

			root.InsertChildBefore (b, newNode);

			Assert.That (newNode.Span.IsValid, Is.False, "inserted node has no valid source span");
			Assert.That (b.Span.IsValid, Is.False, "b now follows the insertion point");
			Assert.That (root.Span.IsValid, Is.False, "ancestor must be invalidated");
			Assert.That (doc.IsDirty, Is.True);
			Assert.That (a.Span.IsValid, Is.True, "a precedes the insertion and must not be invalidated");
		}

		[Test]
		public void InsertChildAfterInvalidatesNewChildAndFollowers ()
		{
			var doc = ParseXml ("<root><a/><b/></root>");
			var root = doc.RootElement!;
			var a = (XElement) root.FirstChild!;
			var b = (XElement) a.NextSibling!;
			var newNode = new XElement ((XName) "x");

			root.InsertChildAfter (a, newNode);

			Assert.That (newNode.Span.IsValid, Is.False, "inserted node has no valid source span");
			Assert.That (b.Span.IsValid, Is.False, "b now follows the new node");
			Assert.That (root.Span.IsValid, Is.False, "ancestor must be invalidated");
			Assert.That (doc.IsDirty, Is.True);
			Assert.That (a.Span.IsValid, Is.True, "a is the anchor and must not be invalidated");
		}

		[Test]
		public void AddAttributeInvalidatesAttributeAndAncestors ()
		{
			var doc = ParseXml ("<root a=\"1\"/>");
			var root = doc.RootElement!;
			var existingAttr = root.Attributes.First!;

			Assert.That (existingAttr.Span.IsValid, Is.True);
			Assert.That (root.Span.IsValid, Is.True);

			var newAttr = new XAttribute ((XName) "b", "2");
			root.Attributes.AddAttribute (newAttr);

			Assert.That (newAttr.Span.IsValid, Is.False, "new attribute has no valid source span");
			Assert.That (root.Span.IsValid, Is.False, "parent element must be invalidated");
			Assert.That (doc.IsDirty, Is.True);
		}

		[Test]
		public void RemoveAttributeInvalidatesFollowingAttributesAndAncestors ()
		{
			var doc = ParseXml ("<root a=\"1\" b=\"2\" c=\"3\"/>");
			var root = doc.RootElement!;
			var attrs = root.Attributes.ToList ();
			var attrA = attrs[0];
			var attrB = attrs[1];
			var attrC = attrs[2];

			root.Attributes.RemoveAttribute (attrB);

			Assert.That (attrB.Span.IsValid, Is.False, "removed attribute must be invalidated");
			Assert.That (attrC.Span.IsValid, Is.False, "c follows b and must be invalidated");
			Assert.That (root.Span.IsValid, Is.False, "parent element must be invalidated");
			Assert.That (doc.IsDirty, Is.True);
			Assert.That (attrA.Span.IsValid, Is.True, "a precedes b and must not be invalidated");
		}

		[Test]
		public void RecalculateSpansRestoresAllNodeSpansAndClearsDirty ()
		{
			var doc = ParseXml ("<root><a/><b/></root>");
			var root = doc.RootElement!;

			// Remove <a/> → invalidates b and root
			root.RemoveChild ((XElement) root.FirstChild!);
			Assert.That (doc.IsDirty, Is.True);

			doc.RecalculateSpans ();

			Assert.That (doc.IsDirty, Is.False);
			Assert.That (root.Span.IsValid, Is.True);
			Assert.That (root.Nodes.All (n => n.Span.IsValid), Is.True, "all remaining nodes must have valid spans");
			// The recalculated root span should start at 0 (the first '<')
			Assert.That (root.Span.Start, Is.EqualTo (0));
			Assert.That (doc.ToXmlString (), Is.EqualTo ("<root><b/></root>"));
		}

		[Test]
		public void IsDirtySetOnMutationAndClearedByRecalculateSpans ()
		{
			var doc = ParseXml ("<root/>");
			Assert.That (doc.IsDirty, Is.False);

			doc.RootElement!.AddChildNode (new XElement ((XName) "child"));
			Assert.That (doc.IsDirty, Is.True);

			doc.RecalculateSpans ();
			Assert.That (doc.IsDirty, Is.False);
		}

		// ── T-006: Re-parenting, PreviousSibling, cycle guard ────────────────

		[Test]
		public void AddChildNodeAutoDetachesFromPreviousParent ()
		{
			var docA = ParseXml ("<a><child/></a>");
			var docB = ParseXml ("<b/>");
			var treeA = docA.RootElement!;
			var treeB = docB.RootElement!;
			var child = (XElement) treeA.FirstChild!;

			treeB.AddChildNode (child);

			Assert.That (child.Parent, Is.SameAs (treeB));
			Assert.That (treeA.Nodes, Is.Empty, "child must have been detached from treeA");
			Assert.That (treeB.FirstChild, Is.SameAs (child));
		}

		[Test]
		public void PreviousSiblingIsCorrectOnParsedTree ()
		{
			var doc = ParseXml ("<root><a/><b/><c/></root>");
			var root = doc.RootElement!;
			var children = root.Nodes.Cast<XElement> ().ToList ();
			var a = children[0];
			var b = children[1];
			var c = children[2];

			Assert.That (a.PreviousSibling, Is.Null, "first child has no previous sibling");
			Assert.That (b.PreviousSibling, Is.SameAs (a));
			Assert.That (c.PreviousSibling, Is.SameAs (b));
		}

		[Test]
		public void PreviousSiblingUpdatesAfterInsertion ()
		{
			var doc = ParseXml ("<root><a/><b/></root>");
			var root = doc.RootElement!;
			var a = (XElement) root.FirstChild!;
			var b = (XElement) a.NextSibling!;
			var newNode = new XElement ((XName) "x");

			root.InsertChildAfter (a, newNode);

			// order is now a → newNode → b
			Assert.That (newNode.PreviousSibling, Is.SameAs (a));
			Assert.That (b.PreviousSibling, Is.SameAs (newNode));
		}

		[Test]
		public void AttributePreviousSiblingIsCorrectOnParsedTree ()
		{
			var doc = ParseXml ("<root a=\"1\" b=\"2\" c=\"3\"/>");
			var root = doc.RootElement!;
			var attrs = root.Attributes.ToList ();
			var attrA = attrs[0];
			var attrB = attrs[1];
			var attrC = attrs[2];

			Assert.That (attrA.PreviousSibling, Is.Null, "first attribute has no previous sibling");
			Assert.That (attrB.PreviousSibling, Is.SameAs (attrA));
			Assert.That (attrC.PreviousSibling, Is.SameAs (attrB));
		}

		[Test]
		public void InsertIntoOwnSubtreeThrowsInvalidOperation ()
		{
			var doc = ParseXml ("<root><child/></root>");
			var root = doc.RootElement!;
			var child = (XElement) root.FirstChild!;

			Assert.That (
				() => child.AddChildNode (root),
				Throws.TypeOf<InvalidOperationException> ());
		}

		// ── Regression: XmlDomExtensions on unedited trees ───────────────────

		[Test]
		public void FindAtOffsetReturnsCorrectNodeOnUneditedTree ()
		{
			// "<root><child/></root>"
			//  offset 0: '<' of <root> — Span(0,6) covers 0..5
			//  offset 6: '<' of <child/> — Span(6,7) covers 6..12
			var doc = ParseXml ("<root><child/></root>");

			var atRoot = doc.FindAtOffset (1);
			var atChild = doc.FindAtOffset (7);

			Assert.That (atRoot, Is.InstanceOf<XElement> ());
			Assert.That (((XElement) atRoot!).Name.FullName, Is.EqualTo ("root"));

			Assert.That (atChild, Is.InstanceOf<XElement> ());
			Assert.That (((XElement) atChild!).Name.FullName, Is.EqualTo ("child"));
		}

		[Test]
		public void FindAtOffsetReturnsAttributeForOffsetInsideAttribute ()
		{
			// "<root id=\"1\"/>" — attr "id" starts after the space following "root"
			var doc = ParseXml ("<root id=\"1\"/>");
			var root = doc.RootElement!;

			// Attribute span: after '<root ' = offset 6, length of 'id="1"' = 6 → Span(6,6)
			var found = doc.FindAtOffset (7);

			Assert.That (found, Is.InstanceOf<XAttribute> ());
			Assert.That (((XAttribute) found!).Name.FullName, Is.EqualTo ("id"));
		}

		[Test]
		public void GetSquiggleSpanReturnsValidSpanForParsedElement ()
		{
			var doc = ParseXml ("<root/>");
			var squiggle = doc.RootElement!.GetSquiggleSpan ();

			Assert.That (squiggle.IsValid, Is.True);
		}

		[Test]
		public void GetSquiggleSpanReturnsInvalidAfterMutation ()
		{
			var doc = ParseXml ("<root/>");
			var root = doc.RootElement!;

			root.AddChildNode (new XElement ((XName) "child"));

			Assert.That (root.GetSquiggleSpan ().IsValid, Is.False);
		}

		[Test]
		public void GetAttributesSpanReturnsValidSpanForParsedElementWithAttributes ()
		{
			var doc = ParseXml ("<root a=\"1\" b=\"2\"/>");
			var root = doc.RootElement!;

			var span = root.GetAttributesSpan ();

			Assert.That (span, Is.Not.Null);
			Assert.That (span!.Value.IsValid, Is.True);
		}

		[Test]
		public void GetAttributesSpanReturnsNullForElementWithNoAttributes ()
		{
			var doc = ParseXml ("<root/>");
			var span = doc.RootElement!.GetAttributesSpan ();

			Assert.That (span, Is.Null);
		}

		[Test]
		public void FindAtOffsetDoesNotThrowOnTreeWithInvalidSpans ()
		{
			var doc = ParseXml ("<root><child/></root>");
			var root = doc.RootElement!;

			// Mutation invalidates root and doc spans
			root.AddChildNode (new XElement ((XName) "added"));

			// Must not throw despite invalid spans on root and doc
			Assert.That (() => doc.FindAtOffset (3), Throws.Nothing);
		}
	}
}
