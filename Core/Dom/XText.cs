// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace MonoDevelop.Xml.Dom
{
	public class XText : XNode
	{
		public XText (int startOffset) : base (startOffset) { }
		public XText (TextSpan span) : base (span) { }

		/// <summary>
		/// Creates a new text node with the given content and no source offset.
		/// <see cref="XObject.Span"/> is set to <see cref="TextSpan.Invalid"/>.
		/// </summary>
		public XText (string text) : base (TextSpan.Invalid)
		{
			Text = text;
		}

		protected XText () { }
		protected override XObject NewInstance () { return new XText (); }

		public string Text { get; private set; } = "";

		public override string FriendlyPathRepresentation {
			get { return Ellipsize (Text ?? "", 20); }
		}

		/// <remarks>This method is intended for parser use only. Use <see cref="SetText"/> to mutate text content.</remarks>
		public void End (string text)
		{
			Text = text;
			Span = new TextSpan (Span.Start, text.Length);
		}

		/// <summary>
		/// Sets the text content and invalidates the spans of this node, its following siblings,
		/// and all ancestor nodes per the span contract.
		/// </summary>
		public void SetText (string text)
		{
			Text = text;
			InvalidateSpanChain ();
		}

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			var other = (XText)copyFrom;
			Text = other.Text;
			base.ShallowCopyFrom (copyFrom);
		}

		static string Ellipsize (string s, int length)
			=> s.Length < length - 3 ? s : s.Substring (0, length - 3) + "...";
	}
}
