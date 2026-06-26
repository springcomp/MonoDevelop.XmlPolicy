// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Linq;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Dom
{
	[TestFixture]
	public class CloneTests
	{
		static XDocument ParseXml (string xml)
		{
			var parser = new XmlTreeParser (new XmlRootState ());
			var (doc, _) = parser.Parse (new System.IO.StringReader (xml));
			return doc;
		}

		[Test]
		public void DeepCloneCopiesFullSubtreeAndIsIndependent ()
		{
			var root = ParseXml ("<root id=\"1\"><child a=\"2\">text</child></root>").RootElement!;
			var clone = (XElement)root.Clone (deep: true);

			Assert.That (clone, Is.Not.SameAs (root));
			Assert.That (clone.Parent, Is.Null);
			Assert.That (clone.ToXmlString (), Is.EqualTo (root.ToXmlString ()));
			Assert.That (clone.Span.IsValid, Is.False);
			Assert.That (clone.Attributes.All (a => !a.Span.IsValid), Is.True);

			var cloneChild = (XElement)clone.FirstChild!;
			var rootChild = (XElement)root.FirstChild!;
			((XText)cloneChild.FirstChild!).SetText ("mutated");
			clone.Attributes.First!.SetValue ("99");

			Assert.That (clone.ToXmlString (), Is.EqualTo ("<root id=\"99\"><child a=\"2\">mutated</child></root>"));
			Assert.That (root.ToXmlString (), Is.EqualTo ("<root id=\"1\"><child a=\"2\">text</child></root>"));
			Assert.That (cloneChild, Is.Not.SameAs (rootChild));
		}

		[Test]
		public void ShallowCloneCopiesNodeAndAttributesButNotChildren ()
		{
			var root = ParseXml ("<root id=\"1\"><child/></root>").RootElement!;
			var clone = (XElement)root.Clone (deep: false);

			Assert.That (clone.Parent, Is.Null);
			Assert.That (clone.Nodes, Is.Empty);
			Assert.That (clone.Attributes.Count, Is.EqualTo (1));
			Assert.That (clone.Attributes.First!.Name.FullName, Is.EqualTo ("id"));
			Assert.That (clone.Attributes.First.Value, Is.EqualTo ("1"));
			Assert.That (clone.Attributes.First!.ValueOffset, Is.Null);
			Assert.That (clone.Span.IsValid, Is.False);
		}

		[Test]
		public void CloneIsImmediatelyInsertable ()
		{
			var sourceChild = ParseXml ("<source><child/></source>").RootElement!.FirstChild!;
			var clone = sourceChild.Clone (deep: true);
			var target = new XElement ((XName)"target");

			target.AddChildNode (clone);

			Assert.That (clone.Parent, Is.SameAs (target));
			Assert.That (target.ToXmlString (), Is.EqualTo ("<target><child/></target>"));
		}
	}
}
