//
// XDocument.cs
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

namespace MonoDevelop.Xml.Dom
{
	public class XDocument : XContainer
	{
		public XElement? RootElement { get; private set; }

		public XDocument () : base (0) { }
		protected override XObject NewInstance () { return new XDocument (); }

		/// <summary>
		/// Indicates that one or more structural or content mutations have been applied to this
		/// document since it was last parsed or since <see cref="MonoDevelop.Xml.Dom.XmlDomWriter.RecalculateSpans"/>
		/// was last called. When <see langword="true"/>, node spans may be <see cref="TextSpan.Invalid"/>.
		/// </summary>
		public bool IsDirty { get; internal set; }

		public override string FriendlyPathRepresentation {
			get { throw new InvalidOperationException ("Should not display document in path bar."); }
		}

		public override void AddChildNodeFromParser (XNode newChild)
		{
			if (RootElement == null && newChild is XElement el)
				RootElement = el;
			base.AddChildNodeFromParser (newChild);
		}

		// XDocument.AddChildNode is no longer overridden: base.AddChildNode calls
		// AddChildNodeFromParser (which tracks RootElement) then InvalidateSpanChain.

		// normally IsEnded checks whether the span is non-zero
		// but XDocument is the only node type that can have a zero-length span
		public new void End (int endPos)
		{
			base.End (endPos);
			isEnded = true;
		}

		bool isEnded;

		public override bool IsEnded => isEnded;
	}
}
