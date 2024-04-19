//
// XmlAttributeValueState.cs
//
// Author:
//   Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Net.Mime;
using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlAttributeValueState : XmlParserState
	{
		const int FREE = 0;
		const int UNQUOTED = 1;
		const int SINGLEQUOTE = 2;
		const int DOUBLEQUOTE = 3;
		const int SINGLEQUOTE_VALUE = SINGLEQUOTE + 4;
		const int DOUBLEQUOTE_VALUE = DOUBLEQUOTE + 4;
		const int AT = DOUBLEQUOTE_VALUE + 8;

		private const int tagMask = 3;
		private const int valueMask = 4;

		private int currentStateLength_ = -1;
		private int stateTag_ = -1;

		public XmlAttributeValueState () : this (new XmlCSharpStatementState ()) { }

		public XmlAttributeValueState (XmlCSharpStatementState csharpStatementState)
		{
			CSharpStatementState = Adopt (csharpStatementState);
		}

		protected XmlCSharpStatementState CSharpStatementState { get; }

		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile)
		{
			System.Diagnostics.Debug.Assert (
				context.Nodes.Peek () switch {
					XAttribute att => att.Value == null,
					XAttributeValue attValue => attValue.IsNull || XAttributeValue.IsCSharpStatement (attValue),
					XCSharpStatement attValue => true,
					_ => false,
				});

			PopState (context);

			if (c == '<') {
				//the parent state should report the error
				replayCharacter = true;
				return EndAndPop ();
			}

			if (isEndOfFile) {
				//the parent state should report the error
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteAttributeEof, context.PositionBeforeCurrentChar);
				return EndAndPop ();
			}

			if (context.CurrentStateLength == 0) {
				if (c == '"') {
					context.StateTag = DOUBLEQUOTE;
					return null;
				}
				if (c == '\'') {
					context.StateTag = SINGLEQUOTE;
					return null;
				}
				context.StateTag = UNQUOTED;
			}

			switch (context.StateTag) {
			case SINGLEQUOTE:
			case DOUBLEQUOTE:
				// starting a new attribute value
				var position = (c == '@')
					? context.PositionAfterCurrentChar
					: context.PositionBeforeCurrentChar
					;

				var attributeValue = new XAttributeValue (position);
				context.Nodes.Push (attributeValue);
				context.StateTag += valueMask;
				if (c == '@') {
					context.StateTag = AT;
					return null;
				}
				break;
			case AT:
				if (c == '(') {
					PushState (context);
					return CSharpStatementState;
				}
				break;
			}

			int maskedTag = context.StateTag & tagMask;

			if (maskedTag == UNQUOTED) {
				return BuildUnquotedValue (c, context, ref replayCharacter);
			}

			if ((c == '"' && maskedTag == DOUBLEQUOTE) || c == '\'' && maskedTag == SINGLEQUOTE) {
				//ending the value
				return EndAndPop ();
			}

			context.KeywordBuilder.Append (c);
			return null;

			XmlParserState? EndAndPop ()
			{
				var type = context.Nodes.Peek ().GetType ();
				if (type == typeof (XCSharpStatement))
					EndCSharpStatement (context);
				if (type == typeof (XAttributeValue))
					EndAttributeValue (context);

				return Parent;
			}
		}

		public static bool IsUnquotedValueChar (char c) => char.IsLetterOrDigit (c) || c == '_' || c == '.' || c == '-';

		XmlParserState? BuildUnquotedValue (char c, XmlParserContext context, ref bool replayCharacter)
		{
			// even though unquoted values aren't legal, attempt to build a value anyway
			if (IsUnquotedValueChar (c)) {
				context.KeywordBuilder.Append (c);
				return null;
			}

			// if first char is not something we can handle as an unquoted char, just reject it for parent to deal with
			if (context.KeywordBuilder.Length == 0) {
				if (context.Diagnostics is not null && context.Nodes.Peek () is XAttribute badAtt && badAtt.Name.IsValid) {
					context.Diagnostics.Add (XmlCoreDiagnostics.IncompleteAttributeValue, context.Position, badAtt.Name!.FullName, c);
				}
				replayCharacter = true;
				return Parent;
			}

			var att = (XAttribute)context.Nodes.Peek ();
			att.Value = new XAttributeValue (context.KeywordBuilder.ToString ());

			if (context.Diagnostics is not null && att.Name.IsValid) {
				context.Diagnostics.Add (XmlCoreDiagnostics.UnquotedAttributeValue, new TextSpan (context.Position - att.Value.Length, att.Value.Length), att.Name.FullName);
			}

			replayCharacter = true;
			return Parent;
		}

		public override XmlParserContext TryRecreateState (ref XObject xobject, int position)
			=> throw new InvalidOperationException ("State has no corresponding XObject");

		public static char? GetDelimiterChar (XmlParserContext context)
			=> context.CurrentState is XmlAttributeValueState
			? (context.StateTag & tagMask) switch {
				SINGLEQUOTE => '\'',
				DOUBLEQUOTE => '"',
				_ => (char?)null
			}
			: null;

		private void PushState (XmlParserContext context)
		{
			currentStateLength_ = context.CurrentStateLength;
			stateTag_ = context.StateTag;
		}
		private void PopState (XmlParserContext context)
		{
			if (currentStateLength_ != -1) {
				context.StateTag = stateTag_;
				context.CurrentStateLength = currentStateLength_;

				stateTag_ = -1;
				currentStateLength_ = -1;
			}
		}

		private void EndAttributeValue (XmlParserContext context)
		{
			var attributeValue = (XAttributeValue)context.Nodes.Pop ();
			if (context.PreviousState == Parent) {
				var text = context.StateTag == AT
					? "@"
					: context.KeywordBuilder.ToString ()
					;

				attributeValue.End (text);
			}
			((XAttribute)context.Nodes.Peek ()).Value = attributeValue;
		}
		private void EndCSharpStatement (XmlParserContext context)
		{
			var statement = (XCSharpStatement)context.Nodes.Pop ();
			var attributeValue = (XAttributeValue)context.Nodes.Pop ();
			attributeValue.End (statement);
			((XAttribute)context.Nodes.Peek ()).Value = attributeValue;
		}
	}
}
