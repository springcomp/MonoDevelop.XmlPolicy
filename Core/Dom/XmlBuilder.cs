// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;

namespace MonoDevelop.Xml.Dom
{
	/// <summary>
	/// Fluent builder for constructing XML DOM trees without source offsets.
	/// All nodes produced by this builder have <see cref="XObject.Span"/> set to
	/// <see cref="TextSpan.Invalid"/> and are immediately usable with the mutation
	/// and serialization APIs.
	/// </summary>
	/// <example>
	/// <code>
	/// XElement root = XmlBuilder.Element("root")
	///     .Attr("id", "1")
	///     .Child(XmlBuilder.Element("child").Text("hello"))
	///     .Build();
	/// string xml = root.ToXmlString(); // &lt;root id="1"&gt;&lt;child&gt;hello&lt;/child&gt;&lt;/root&gt;
	/// </code>
	/// </example>
	public sealed class XmlBuilder
	{
		readonly XElement _element;

		XmlBuilder (XElement element) => _element = element;

		/// <summary>Creates a builder rooted at a new <see cref="XElement"/> with the given name.</summary>
		public static XmlBuilder Element (XName name) => new XmlBuilder (new XElement (name));

		/// <summary>Adds an attribute to the current element.</summary>
		public XmlBuilder Attr (XName name, string value)
		{
			_element.Attributes.AddAttribute (new XAttribute (name, value));
			return this;
		}

		/// <summary>Appends a text child node to the current element.</summary>
		public XmlBuilder Text (string text)
		{
			_element.AddChildNode (new XText (text));
			return this;
		}

		/// <summary>Appends a comment child node to the current element.</summary>
		public XmlBuilder Comment (string text)
		{
			_element.AddChildNode (new XComment (text));
			return this;
		}

		/// <summary>Appends a CDATA child node to the current element.</summary>
		public XmlBuilder CData (string text)
		{
			_element.AddChildNode (new XCData (text));
			return this;
		}

		/// <summary>Appends a child element built by another <see cref="XmlBuilder"/>.</summary>
		public XmlBuilder Child (XmlBuilder child)
		{
			_element.AddChildNode (child.Build ());
			return this;
		}

		/// <summary>Appends a pre-built child element.</summary>
		public XmlBuilder Child (XElement child)
		{
			_element.AddChildNode (child);
			return this;
		}

		/// <summary>
		/// Marks the element as self-closing (e.g., <c>&lt;foo/&gt;</c>).
		/// Only valid for elements that have no children.
		/// </summary>
		public XmlBuilder SelfClose ()
		{
			_element.Close (_element);
			return this;
		}

		/// <summary>Returns the built <see cref="XElement"/>.</summary>
		public XElement Build () => _element;

		/// <summary>Implicitly converts the builder to the built <see cref="XElement"/>.</summary>
		public static implicit operator XElement (XmlBuilder builder) => builder.Build ();
	}
}
