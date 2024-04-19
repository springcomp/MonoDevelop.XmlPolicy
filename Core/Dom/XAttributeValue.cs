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
	public class XAttributeValue : XNode
	{
		XCSharpStatement? statement_;
		string? text_;

		public XAttributeValue (int startOffset) : base(startOffset) { }

		public XAttributeValue (string text)
		{
			text_ = text;
		}

		public void End (string text)
		{
			text_ = text;
			Span = new TextSpan (Span.Start, text_.Length);
		}

		public void End (XCSharpStatement statement)
		{
			statement_ = statement;
			Span = new TextSpan (Span.Start, statement_.Length + 2);
		}

		protected XAttributeValue () { }
		protected override XObject NewInstance () => new XAttributeValue ();

		public int Length => text_!.Length;

		internal bool IsNull => text_ == null && statement_ == null;

		public static bool IsText (XAttributeValue? value) => value?.text_ != null;
		public static bool IsCSharpStatement(XAttributeValue? value) => value?.statement_ != null;

		public T As<T> ()
		{
			var typeName = typeof (T).FullName;
			if (typeName == "System.String") {
				if (text_ == null) throw new InvalidCastException (typeName);
				return (T)(object)text_!;
			}
			if (typeName == "MonoDevelop.Xml.Dom.XCSharpStatement") {
				if (statement_ == null) throw new InvalidCastException (typeName);
				return (T)(object)statement_!;
			} else
				throw new InvalidCastException (typeName);
		}

		public override string ToString () => $"[XAttributeValue Location='{Span}' Value='{GetValueRepresentation()}']";

		public override string FriendlyPathRepresentation => GetValueRepresentation();

		private string GetValueRepresentation ()
		{
			if (text_ != null)
				return text_!;
			else if (statement_ != null)
				return statement_!.ToString () ?? "";
			else
				return "null";
		}
	}
}
