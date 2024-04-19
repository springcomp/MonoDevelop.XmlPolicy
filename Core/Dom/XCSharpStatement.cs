//
// XAttributeValue.cs
//
// Author:
//   springcomp <springcomp@users.noreply.github.com>
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
	public class XCSharpStatement : XNode
	{
		private string? text_;
		public XCSharpStatement (int startOffset) : base (startOffset) { }
		protected XCSharpStatement () { }
		protected override XObject NewInstance () => new XCSharpStatement ();

		public string? RawText {
			get => text_;
			internal set => text_ = value;
		}

		public int Length => text_ != null ? text_.Length : 0;

		public override string ToString () => $"[XCSharpStatement Location='{Span}' Value='{GetValueRepresentation ()}']";

		private string GetValueRepresentation () => text_! ?? "";
	}
}
