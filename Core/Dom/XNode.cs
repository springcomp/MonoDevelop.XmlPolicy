//
// XNode.cs
//
// Author:
//   Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

#nullable enable

namespace MonoDevelop.Xml.Dom
{
	public abstract class XNode : XObject
	{
		protected XNode (int startOffset) : base (startOffset) { }
		protected XNode (TextSpan span) : base (span) { }
		protected XNode () { }

		public XNode? NextSibling { get; internal protected set; }

		/// <summary>
		/// Gets the previous sibling of this node, or <see langword="null"/> if this is the first child.
		/// Computed by walking the parent's child list.
		/// </summary>
		public XNode? PreviousSibling {
			get {
				if (Parent is XContainer container) {
					var n = container.FirstChild;
					while (n != null) {
						if (n.NextSibling == this)
							return n;
						n = n.NextSibling;
					}
				}
				return null;
			}
		}

		/// <summary>
		/// Applies the span invalidation contract: marks this node, all following siblings,
		/// and all ancestors up to the root as <see cref="TextSpan.Invalid"/>.
		/// Also sets the owning <see cref="XDocument.IsDirty"/> flag when the root is a document.
		/// </summary>
		internal void InvalidateSpanChain ()
		{
			InvalidateSpan ();
			var sib = NextSibling;
			while (sib is not null) {
				sib.InvalidateSpan ();
				sib = sib.NextSibling;
			}
			var ancestor = Parent;
			while (ancestor is not null) {
				ancestor.InvalidateSpan ();
				if (ancestor is XDocument doc)
					doc.IsDirty = true;
				ancestor = ancestor.Parent;
			}
		}

		/// <summary>
		/// Creates a detached clone of this node.
		/// </summary>
		/// <param name="deep">
		/// When <see langword="true"/>, clone the full subtree recursively.
		/// When <see langword="false"/>, clone only this node.
		/// </param>
		public XNode Clone (bool deep) => CloneCore (deep);

		internal virtual XNode CloneCore (bool deep)
		{
			var clone = (XNode)ShallowCopy ();
			clone.Parent = null;
			clone.NextSibling = null;
			clone.SetSpan (TextSpan.Invalid);
			return clone;
		}
	}
}
