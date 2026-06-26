// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.IO;
using System.Text;

namespace MonoDevelop.Xml.Dom
{
	/// <summary>
	/// Extension methods for serializing an XML DOM tree to text.
	/// Serialization is driven entirely by node state — it never reads <see cref="TextSpan"/> source offsets.
	/// Nodes whose spans are <see cref="TextSpan.Invalid"/> are serialized correctly.
	/// </summary>
	public static class XmlDomWriter
	{
		/// <summary>
		/// Writes the XML representation of <paramref name="node"/> (and its subtree) to <paramref name="writer"/>.
		/// </summary>
		public static void WriteTo (this XNode node, TextWriter writer)
			=> WriteNode (node, writer);

		/// <summary>
		/// Returns the XML representation of <paramref name="node"/> (and its subtree) as a string.
		/// </summary>
		public static string ToXmlString (this XNode node)
		{
			var sb = new StringBuilder ();
			using var sw = new StringWriter (sb);
			node.WriteTo (sw);
			return sb.ToString ();
		}

		/// <summary>
		/// Writes the XML representation of the child nodes of <paramref name="container"/> to <paramref name="writer"/>.
		/// Equivalent to <c>InnerXml</c>-style serialization.
		/// </summary>
		public static void WriteContentTo (this XContainer container, TextWriter writer)
		{
			foreach (var child in container.Nodes)
				WriteNode (child, writer);
		}

		/// <summary>
		/// Recalculates the <see cref="XObject.Span"/> of every node in the subtree rooted at
		/// <paramref name="node"/>, assigning valid offsets that match the output of
		/// <see cref="ToXmlString"/>. Call this after structural mutations to restore position
		/// information. Clears <see cref="XDocument.IsDirty"/> when the root is a document.
		/// </summary>
		/// <param name="node">The root of the subtree to recalculate.</param>
		/// <param name="startOffset">
		/// The character offset in a larger document where <paramref name="node"/> begins.
		/// Ignored for <see cref="XDocument"/> nodes, which always start at offset 0.
		/// </param>
		/// <remarks>
		/// Attribute <see cref="XAttribute.ValueSpan"/> is not recalculated by this method;
		/// only the outer <see cref="XObject.Span"/> is updated for each node.
		/// </remarks>
		public static void RecalculateSpans (this XNode node, int startOffset = 0)
		{
			// XDocument always starts at 0; for all other roots, honour startOffset.
			int effectiveStart = node is XDocument ? 0 : startOffset;
			var tracker = new SpanTracker (effectiveStart);
			RecalcNode (node, tracker);
			if (node is XDocument doc)
				doc.IsDirty = false;
		}

		// ──────────────────────────────────────────────────────────────────────
		// Serialization helpers
		// ──────────────────────────────────────────────────────────────────────

		static void WriteNode (XNode node, TextWriter writer)
		{
			switch (node) {
			case XDocument doc:
				doc.WriteContentTo (writer);
				break;
			case XElement el:
				WriteElement (el, writer);
				break;
			case XText text:
				WriteEscapedText (text.Text, writer);
				break;
			case XCData cdata:
				writer.Write ("<![CDATA[");
				writer.Write (cdata.InnerText ?? "");
				writer.Write ("]]>");
				break;
			case XComment comment:
				writer.Write ("<!--");
				writer.Write (comment.InnerText);
				writer.Write ("-->");
				break;
			case XDocType docType:
				WriteDocType (docType, writer);
				break;
			case XClosingTag:
				// closing tags are emitted by WriteElement; skip standalone occurrences
				break;
			case XProcessingInstruction:
				// XProcessingInstruction does not store target or data; cannot serialize
				break;
			}
		}

		static void WriteElement (XElement element, TextWriter writer)
		{
			writer.Write ('<');
			writer.Write (element.Name.FullName);

			foreach (var attr in element.Attributes)
				WriteAttribute (attr, writer);

			if (element.IsSelfClosing) {
				writer.Write ("/>");
				return;
			}

			writer.Write ('>');
			element.WriteContentTo (writer);

			writer.Write ("</");
			writer.Write (element.Name.FullName);
			writer.Write ('>');
		}

		static void WriteAttribute (XAttribute attr, TextWriter writer)
		{
			writer.Write (' ');
			writer.Write (attr.Name.FullName);
			if (attr.HasValue) {
				writer.Write ("=\"");
				WriteEscapedAttributeValue (attr.Value, writer);
				writer.Write ('"');
			}
		}

		static void WriteEscapedText (string text, TextWriter writer)
		{
			foreach (char c in text) {
				switch (c) {
				case '&': writer.Write ("&amp;"); break;
				case '<': writer.Write ("&lt;"); break;
				case '>': writer.Write ("&gt;"); break;
				default: writer.Write (c); break;
				}
			}
		}

		static void WriteEscapedAttributeValue (string value, TextWriter writer)
		{
			foreach (char c in value) {
				switch (c) {
				case '&': writer.Write ("&amp;"); break;
				case '<': writer.Write ("&lt;"); break;
				case '>': writer.Write ("&gt;"); break;
				case '"': writer.Write ("&quot;"); break;
				case '\r': writer.Write ("&#xD;"); break;
				case '\n': writer.Write ("&#xA;"); break;
				default: writer.Write (c); break;
				}
			}
		}

		static void WriteDocType (XDocType docType, TextWriter writer)
		{
			writer.Write ("<!DOCTYPE ");
			writer.Write (docType.RootElement.FullName);
			if (docType.IsPublic) {
				writer.Write (" PUBLIC \"");
				writer.Write (docType.PublicFpi);
				writer.Write ('"');
				if (docType.Uri is not null) {
					writer.Write (" \"");
					writer.Write (docType.Uri);
					writer.Write ('"');
				}
			} else if (docType.Uri is not null) {
				writer.Write (" SYSTEM \"");
				writer.Write (docType.Uri);
				writer.Write ('"');
			}
			writer.Write ('>');
		}

		// ──────────────────────────────────────────────────────────────────────
		// Span recalculation helpers
		// ──────────────────────────────────────────────────────────────────────

		/// <summary>
		/// Lightweight character position counter used during span recalculation.
		/// Mirrors every write path in the serialization helpers above.
		/// </summary>
		sealed class SpanTracker
		{
			public int Position { get; private set; }

			public SpanTracker (int start) => Position = start;

			public void Advance (int count) => Position += count;
			public void AdvanceChar () => Position++;

			public void AdvanceString (string? s)
			{
				if (s is not null)
					Position += s.Length;
			}

			/// <summary>Counts characters as if <see cref="WriteEscapedText"/> had been called.</summary>
			public void AdvanceEscapedText (string text)
			{
				foreach (char c in text) {
					Position += c switch {
						'&' => 5, // &amp;
						'<' => 4, // &lt;
						'>' => 4, // &gt;
						_ => 1
					};
				}
			}

			/// <summary>Counts characters as if <see cref="WriteEscapedAttributeValue"/> had been called.</summary>
			public void AdvanceEscapedAttributeValue (string value)
			{
				foreach (char c in value) {
					Position += c switch {
						'&' => 5,  // &amp;
						'<' => 4,  // &lt;
						'>' => 4,  // &gt;
						'"' => 6,  // &quot;
						'\r' => 5, // &#xD;
						'\n' => 5, // &#xA;
						_ => 1
					};
				}
			}
		}

		static void RecalcNode (XNode node, SpanTracker tracker)
		{
			switch (node) {
			case XDocument doc:
				// Documents always start at 0; ensure a valid start before walking children.
				doc.SetSpan (new TextSpan (0, 0));
				foreach (var child in doc.Nodes)
					RecalcNode (child, tracker);
				// XDocument.End also sets isEnded = true
				doc.End (tracker.Position);
				break;
			case XElement el:
				RecalcElement (el, tracker);
				break;
			case XText text: {
					int start = tracker.Position;
					tracker.AdvanceEscapedText (text.Text);
					text.SetSpan (TextSpan.FromBounds (start, tracker.Position));
					break;
				}
			case XCData cdata: {
					int start = tracker.Position;
					tracker.Advance (9);  // <![CDATA[
					tracker.AdvanceString (cdata.InnerText);
					tracker.Advance (3);  // ]]>
					cdata.SetSpan (TextSpan.FromBounds (start, tracker.Position));
					break;
				}
			case XComment comment: {
					int start = tracker.Position;
					tracker.Advance (4);  // <!--
					tracker.AdvanceString (comment.InnerText);
					tracker.Advance (3);  // -->
					comment.SetSpan (TextSpan.FromBounds (start, tracker.Position));
					break;
				}
			case XDocType docType: {
					int start = tracker.Position;
					RecalcDocType (docType, tracker);
					docType.SetSpan (TextSpan.FromBounds (start, tracker.Position));
					break;
				}
			case XClosingTag:
				// handled inline by RecalcElement; skip standalone occurrences
				break;
			case XProcessingInstruction:
				// cannot serialize; skip
				break;
			}
		}

		static void RecalcElement (XElement el, SpanTracker tracker)
		{
			int start = tracker.Position;
			tracker.AdvanceChar ();                    // <
			tracker.AdvanceString (el.Name.FullName);

			foreach (var attr in el.Attributes)
				RecalcAttribute (attr, tracker);

			if (el.IsSelfClosing) {
				tracker.Advance (2);                   // />
				el.SetSpan (TextSpan.FromBounds (start, tracker.Position));
				return;
			}

			tracker.AdvanceChar ();                    // >
			el.SetSpan (TextSpan.FromBounds (start, tracker.Position));

			foreach (var child in el.Nodes)
				RecalcNode (child, tracker);

			if (el.ClosingTag is XClosingTag ct) {
				int ctStart = tracker.Position;
				tracker.Advance (2);                   // </
				tracker.AdvanceString (el.Name.FullName);
				tracker.AdvanceChar ();                // >
				ct.SetSpan (TextSpan.FromBounds (ctStart, tracker.Position));
			}
		}

		static void RecalcAttribute (XAttribute attr, SpanTracker tracker)
		{
			tracker.AdvanceChar ();                    // leading space (not in attribute span)
			int start = tracker.Position;
			tracker.AdvanceString (attr.Name.FullName);
			if (attr.HasValue) {
				tracker.Advance (2);                   // ="
				tracker.AdvanceEscapedAttributeValue (attr.Value);
				tracker.AdvanceChar ();                // closing "
			}
			attr.SetSpan (TextSpan.FromBounds (start, tracker.Position));
		}

		static void RecalcDocType (XDocType docType, SpanTracker tracker)
		{
			tracker.Advance (10);                      // "<!DOCTYPE "
			tracker.AdvanceString (docType.RootElement.FullName);
			if (docType.IsPublic) {
				tracker.Advance (9);                   // " PUBLIC \""
				tracker.AdvanceString (docType.PublicFpi);
				tracker.AdvanceChar ();                // closing "
				if (docType.Uri is not null) {
					tracker.Advance (2);               // " \"
					tracker.AdvanceString (docType.Uri);
					tracker.AdvanceChar ();            // closing "
				}
			} else if (docType.Uri is not null) {
				tracker.Advance (9);                   // " SYSTEM \""
				tracker.AdvanceString (docType.Uri);
				tracker.AdvanceChar ();                // closing "
			}
			tracker.AdvanceChar ();                    // >
		}
	}
}
