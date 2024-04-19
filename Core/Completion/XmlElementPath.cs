//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Text;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Editor.Completion
{
	/// <summary>
	/// Represents the path to an xml element starting from the root of the
	/// document.
	/// </summary>
	class XmlElementPath
	{
		readonly List<QualifiedName> elements;

		public XmlNamespacePrefixMap Namespaces { get; } = new XmlNamespacePrefixMap ();

		public XmlElementPath (params QualifiedName[] elements)
		{
			this.elements = new List<QualifiedName> (elements);
		}

		public static XmlElementPath Resolve (IList<XObject> path, string? defaultNamespace = null, string? defaultPrefix = null)
		{
			var elementPath = new XmlElementPath ();

			if (!string.IsNullOrEmpty (defaultNamespace))
				elementPath.Namespaces.AddPrefix (defaultNamespace, defaultPrefix ?? "");

			foreach (var obj in path) {
				var el = obj as XElement;
				if (el == null)
					continue;
				foreach (var att in el.Attributes) {
					if (XAttributeValue.IsText(att.Value)) {
						var value = att.Value!.As<string> ();
						if (value != "") {
							if (att.Name.HasPrefix) {
								if (att.Name.Prefix == "xmlns" && att.Name.IsValid)
									elementPath.Namespaces.AddPrefix (value, att.Name.Name);
							} else if (att.Name.Name == "xmlns") {
								elementPath.Namespaces.AddPrefix (value, "");
							}
						}
					}
				}
				string prefix = el.Name.HasPrefix ? el.Name.Prefix : "";
				elementPath.Namespaces.TryGetNamespace (prefix, out string? ns);
				var qn = new QualifiedName (el.Name.Name, ns, prefix);
				elementPath.Elements.Add (qn);
			}
			return elementPath;
		}

		/// <summary>
		/// Gets the elements specifying the path.
		/// </summary>
		/// <remarks>The order of the elements determines the path.</remarks>
		public List<QualifiedName> Elements {
			get {
				return elements;
			}
		}

		/// <summary>
		/// Compacts the path so it only contains the elements that are from 
		/// the namespace of the last element in the path. 
		/// </summary>
		/// <remarks>This method is used when we need to know the path for a
		/// particular namespace and do not care about the complete path.
		/// </remarks>
		public void Compact ()
		{
			if (elements.Count > 0) {
				QualifiedName lastName = Elements[Elements.Count - 1];
				int index = FindNonMatchingParentElement (lastName.Namespace);
				if (index != -1) {
					RemoveParentElements (index);
				}
			}
		}

		/// <summary>
		/// An xml element path is considered to be equal if 
		/// each path item has the same name and namespace.
		/// </summary>
		public override bool Equals (object? obj)
		{
			if (this == obj) {
				return true;
			}

			if (obj is not XmlElementPath other) {
				return false;
			}

			if (elements.Count == other.elements.Count) {

				for (int i = 0; i < elements.Count; ++i) {
					if (!elements[i].Equals (other.elements[i])) {
						return false;
					}
				}
				return true;
			}

			return false;
		}

		public override int GetHashCode () => elements.GetHashCode ();

		/// <summary>
		/// Removes elements up to and including the specified index.
		/// </summary>
		void RemoveParentElements (int index)
		{
			while (index >= 0) {
				--index;
				if (elements.Count > 0)
					elements.RemoveAt (0);
			}
		}

		/// <summary>
		/// Finds the first parent that does belong in the specified
		/// namespace.
		/// </summary>
		int FindNonMatchingParentElement (string namespaceUri)
		{
			int index = -1;

			if (elements.Count > 1) {
				// Start the check from the the last but one item.
				for (int i = elements.Count - 2; i >= 0; --i) {
					QualifiedName name = elements[i];
					if (name.Namespace != namespaceUri) {
						index = i;
						break;
					}
				}
			}
			return index;
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();
			foreach (var el in elements) {
				sb.AppendFormat ("/{0}[{1}]", el.Name, el.Namespace);
			}
			return sb.ToString ();
		}
	}
}
