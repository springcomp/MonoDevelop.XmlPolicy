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
	[TestFixture]
	public class CollectionMutationTests
	{
		static XDocument ParseXml (string xml)
		{
			var parser = new XmlTreeParser (new XmlRootState ());
			var (doc, _) = parser.Parse (new System.IO.StringReader (xml));
			return doc;
		}

		[Test]
		public void ReplaceChildSwapsInPlaceAndDetachesOldNode ()
		{
			var doc = ParseXml ("<root><a/><b/></root>");
			var root = doc.RootElement!;
			var oldChild = (XElement) root.FirstChild!;
			var nextChild = (XElement) oldChild.NextSibling!;
			var replacement = new XElement (-1, new XName ("c"));
			replacement.Close (replacement);

			root.ReplaceChild (oldChild, replacement);

			var children = root.Nodes.Cast<XElement> ().ToList ();
			Assert.That (children.Select (c => c.Name.FullName), Is.EqualTo (new[] { "c", "b" }));
			Assert.That (children[0], Is.SameAs (replacement));
			Assert.That (children[1], Is.SameAs (nextChild));
			Assert.That (replacement.Parent, Is.SameAs (root));
			Assert.That (oldChild.Parent, Is.Null);
			Assert.That (oldChild.NextSibling, Is.Null);
		}

		[Test]
		public void RemoveAllClearsContainerAndDetachesChildren ()
		{
			var doc = ParseXml ("<root><a/><b/><c/></root>");
			var root = doc.RootElement!;
			var oldChildren = root.Nodes.ToList ();

			root.RemoveAll ();

			Assert.That (root.FirstChild, Is.Null);
			Assert.That (root.LastChild, Is.Null);
			Assert.That (root.Nodes, Is.Empty);
			Assert.That (oldChildren.All (c => c.Parent is null && c.NextSibling is null), Is.True);
		}

		[Test]
		public void ReplaceAllChildrenReparentsAndReordersChildren ()
		{
			var leftDoc = ParseXml ("<left><a/><b/></left>");
			var rightDoc = ParseXml ("<right><x/></right>");
			var left = leftDoc.RootElement!;
			var right = rightDoc.RootElement!;
			var moved = (XElement) right.FirstChild!;
			var inserted = new XElement (-1, new XName ("y"));
			inserted.Close (inserted);

			left.ReplaceAllChildren (new[] { moved, inserted });

			Assert.That (left.Nodes.Cast<XElement> ().Select (n => n.Name.FullName), Is.EqualTo (new[] { "x", "y" }));
			Assert.That (right.Nodes, Is.Empty);
			Assert.That (moved.Parent, Is.SameAs (left));
			Assert.That (inserted.Parent, Is.SameAs (left));
		}

		[Test]
		public void InsertChildBeforeThrowsForMissingAnchor ()
		{
			var root = ParseXml ("<root><a/></root>").RootElement!;
			var missingAnchor = new XElement (-1, new XName ("missing"));
			missingAnchor.Close (missingAnchor);
			var newChild = new XElement (-1, new XName ("new"));
			newChild.Close (newChild);

			Assert.That (
				() => root.InsertChildBefore (missingAnchor, newChild),
				Throws.TypeOf<ArgumentException> ().With.Property ("ParamName").EqualTo ("beforeNode"));
		}

		[Test]
		public void InsertChildAfterThrowsForMissingAnchor ()
		{
			var root = ParseXml ("<root><a/></root>").RootElement!;
			var missingAnchor = new XElement (-1, new XName ("missing"));
			missingAnchor.Close (missingAnchor);
			var newChild = new XElement (-1, new XName ("new"));
			newChild.Close (newChild);

			Assert.That (
				() => root.InsertChildAfter (missingAnchor, newChild),
				Throws.TypeOf<ArgumentException> ().With.Property ("ParamName").EqualTo ("afterNode"));
		}

		[Test]
		public void AttributeCollectionClearReplaceAndContainsAreConsistent ()
		{
			var doc = ParseXml ("<root a=\"1\" b=\"2\"/>");
			var root = doc.RootElement!;
			var originalAttributes = root.Attributes.ToList ();
			var attrA = originalAttributes[0];

			Assert.That (root.Attributes.Contains (attrA), Is.True);

			root.Attributes.Clear ();

			Assert.That (root.Attributes.Count, Is.EqualTo (0));
			Assert.That (root.Attributes.First, Is.Null);
			Assert.That (root.Attributes.Last, Is.Null);
			Assert.That (root.Attributes.Contains (attrA), Is.False);
			Assert.That (originalAttributes.All (a => a.Parent is null && a.NextSibling is null), Is.True);

			var otherDoc = ParseXml ("<other c=\"3\" d=\"4\"/>");
			var other = otherDoc.RootElement!;
			var replacements = other.Attributes.ToList ();

			root.Attributes.ReplaceAllAttributes (replacements);

			Assert.That (root.Attributes.Select (a => a.Name.FullName), Is.EqualTo (new[] { "c", "d" }));
			Assert.That (root.Attributes.Count, Is.EqualTo (2));
			Assert.That (other.Attributes.Count, Is.EqualTo (0));
			Assert.That (replacements.All (a => a.Parent == root), Is.True);
		}
	}
}
