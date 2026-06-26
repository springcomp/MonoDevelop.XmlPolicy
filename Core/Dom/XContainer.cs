//
// XContainer.cs
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

using System;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Xml.Dom
{
	public abstract class XContainer : XNode
	{
		protected XContainer (int startOffset) : base (startOffset) { }

		protected XContainer (TextSpan span) : base (span) { }

		public XNode? FirstChild { get; private set; }
		public XNode? LastChild { get; private set; }

		public IEnumerable<XNode> Nodes {
			get {
				XNode? next = FirstChild;
				while (next != null) {
					yield return next;
					next = next.NextSibling;
				}
			}
		}

		public virtual IEnumerable<XNode> AllDescendentNodes {
			get {
				foreach (XNode n in Nodes) {
					yield return n;
					if (n is XContainer c)
						foreach (XNode n2 in c.AllDescendentNodes)
							yield return n2;
				}
			}
		}

		/// <summary>
		/// Parser-internal entry point: links the child node WITHOUT applying the span contract.
		/// Override in subclasses for additional parser-time setup.
		/// </summary>
		public virtual void AddChildNodeFromParser (XNode newChild)
		{
			newChild.Parent = this;
			if (LastChild != null)
				LastChild.NextSibling = newChild;
			if (FirstChild == null)
				FirstChild = newChild;
			LastChild = newChild;
		}

		/// <summary>
		/// Adds <paramref name="newChild"/> as the last child of this container and applies the
		/// span invalidation contract (self + ancestors). Use this for post-parse mutations.
		/// </summary>
		public virtual void AddChildNode (XNode newChild)
		{
			if (WouldCreateCycle (newChild, this))
				throw new InvalidOperationException ("Cannot insert a node into its own subtree.");

			// Auto-detach from previous parent (single-parent rule).
			if (newChild.Parent is XContainer oldParent)
				oldParent.RemoveChild (newChild);

			AddChildNodeFromParser (newChild);
			// Apply span contract: invalidate the new child (self) and ancestors.
			newChild.InvalidateSpanChain ();
		}

		public virtual bool RemoveChild (XNode child)
		{
			if (child.Parent != this)
				return false;

			// Apply span contract BEFORE unlinking: invalidate child, following siblings, and ancestors.
			child.InvalidateSpanChain ();

			if (FirstChild == child) {
				FirstChild = child.NextSibling;
				if (LastChild == child)
					LastChild = null;
				child.Parent = null;
				child.NextSibling = null;
				return true;
			}

			XNode? previous = FirstChild;
			while (previous != null && previous.NextSibling != child)
				previous = previous.NextSibling;

			if (previous == null)
				return false;

			previous.NextSibling = child.NextSibling;
			if (LastChild == child)
				LastChild = previous;
			child.Parent = null;
			child.NextSibling = null;
			return true;
		}

		/// <summary>
		/// Replaces an existing child with another node at the same position.
		/// </summary>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="oldChild"/> is not a child of this container.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when replacing with <paramref name="newChild"/> would create a cycle.
		/// </exception>
		public virtual void ReplaceChild (XNode oldChild, XNode newChild)
		{
			if (oldChild.Parent != this)
				throw new ArgumentException ("The node to replace is not a child of this container.", nameof (oldChild));
			if (oldChild == newChild)
				return;
			if (WouldCreateCycle (newChild, this))
				throw new InvalidOperationException ("Cannot insert a node into its own subtree.");

			// Auto-detach from previous parent (single-parent rule).
			if (newChild.Parent is XContainer oldParent)
				oldParent.RemoveChild (newChild);

			if (FirstChild == oldChild) {
				FirstChild = newChild;
			} else {
				XNode? previous = FirstChild;
				while (previous != null && previous.NextSibling != oldChild)
					previous = previous.NextSibling;
				if (previous == null)
					throw new InvalidOperationException ("Could not locate the node to replace in this container.");
				previous.NextSibling = newChild;
			}

			newChild.Parent = this;
			newChild.NextSibling = oldChild.NextSibling;
			if (LastChild == oldChild)
				LastChild = newChild;

			oldChild.Parent = null;
			oldChild.NextSibling = null;
			oldChild.InvalidateSpan ();
			newChild.InvalidateSpanChain ();
		}

		/// <summary>
		/// Removes all child nodes from this container.
		/// </summary>
		public virtual void Clear ()
		{
			while (FirstChild is XNode child)
				RemoveChild (child);
		}

		/// <summary>
		/// Removes all child nodes from this container.
		/// </summary>
		public virtual void RemoveAll () => Clear ();

		/// <summary>
		/// Replaces all children with the supplied nodes in sequence.
		/// </summary>
		public virtual void ReplaceAllChildren (IEnumerable<XNode> newChildren)
		{
			if (newChildren is null)
				throw new ArgumentNullException (nameof (newChildren));

			var replacements = new List<XNode> (newChildren);
			RemoveAll ();
			foreach (var child in replacements)
				AddChildNode (child);
		}

		public virtual void InsertChildAfter (XNode afterNode, XNode newChild)
		{
			if (afterNode.Parent != this)
				throw new ArgumentException ("The anchor node is not a child of this container.", nameof (afterNode));
			if (WouldCreateCycle (newChild, this))
				throw new InvalidOperationException ("Cannot insert a node into its own subtree.");

			// Auto-detach from previous parent (single-parent rule).
			if (newChild.Parent is XContainer oldParent)
				oldParent.RemoveChild (newChild);

			newChild.Parent = this;
			newChild.NextSibling = afterNode.NextSibling;
			afterNode.NextSibling = newChild;
			if (LastChild == afterNode)
				LastChild = newChild;
			// Apply span contract: invalidate new child, its former-next siblings, and ancestors.
			newChild.InvalidateSpanChain ();
		}

		public virtual void InsertChildBefore (XNode beforeNode, XNode newChild)
		{
			if (beforeNode.Parent != this)
				throw new ArgumentException ("The anchor node is not a child of this container.", nameof (beforeNode));
			if (WouldCreateCycle (newChild, this))
				throw new InvalidOperationException ("Cannot insert a node into its own subtree.");

			// Auto-detach from previous parent (single-parent rule).
			if (newChild.Parent is XContainer oldParent)
				oldParent.RemoveChild (newChild);

			newChild.Parent = this;
			if (beforeNode == FirstChild) {
				newChild.NextSibling = FirstChild;
				FirstChild = newChild;
				// Apply span contract: invalidate new child (→ beforeNode → siblings) and ancestors.
				newChild.InvalidateSpanChain ();
				return;
			}

			XNode? previous = FirstChild;
			while (previous != null && previous.NextSibling != beforeNode)
				previous = previous.NextSibling;

			if (previous == null)
				throw new InvalidOperationException ("Could not locate the anchor node in this container.");

			newChild.NextSibling = beforeNode;
			previous.NextSibling = newChild;
			// Apply span contract: invalidate new child (→ beforeNode → siblings) and ancestors.
			newChild.InvalidateSpanChain ();
		}

		/// <summary>
		/// Returns <see langword="true"/> if inserting <paramref name="newChild"/> into
		/// <paramref name="insertionTarget"/> would create a cycle (i.e., <paramref name="newChild"/>
		/// is <paramref name="insertionTarget"/> or an ancestor of it).
		/// </summary>
		static bool WouldCreateCycle (XNode newChild, XContainer insertionTarget)
		{
			if (newChild is not XContainer)
				return false;
			XObject? current = insertionTarget;
			while (current != null) {
				if (current == newChild)
					return true;
				current = current.Parent;
			}
			return false;
		}

		protected XContainer () { }

		internal override XNode CloneCore (bool deep)
		{
			var clone = (XContainer)base.CloneCore (deep);
			if (!deep)
				return clone;
			foreach (var child in Nodes)
				clone.AddChildNodeFromParser (child.CloneCore (true));
			return clone;
		}

		public override void BuildTreeString (StringBuilder builder, int indentLevel)
		{
			base.BuildTreeString (builder, indentLevel);
			foreach (XNode child in Nodes)
				child.BuildTreeString (builder, indentLevel + 1);
		}
	}
}
