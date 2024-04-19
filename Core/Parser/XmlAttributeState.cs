//
// XmlAttributeState.cs
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

using System.Diagnostics;

using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlAttributeState : XmlParserState
	{
		readonly XmlNameState XmlNameState;
		readonly XmlAttributeValueState AttributeValueState;

		const int NAMING = 0;
		const int GETTINGEQ = 1;
		const int GETTINGVAL = 2;

		public XmlAttributeState () : this (
			new XmlNameState (),
			new XmlAttributeValueState ())
		{}

		public XmlAttributeState (
			XmlNameState nameState,
			XmlAttributeValueState attributeValueState)
		{
			XmlNameState = Adopt (nameState);
			AttributeValueState = Adopt (attributeValueState);
		}

		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile)
		{
			var att = context.Nodes.Peek () as XAttribute;

			//state has just been entered
			if (context.CurrentStateLength == 0 || att is null)  {
				if (context.PreviousState is XmlNameState) {
					if (att is null) {
						InvalidParserStateException.ThrowExpected<XAttribute> (context);
					}
					// error parsing name, or end of file
					// the name state will have logged an error already so no need to do so here
					if (!att.IsNamed || isEndOfFile) {
						EndAndPop ();
						return Parent;
					}
					context.StateTag = GETTINGEQ;
				} else if (context.PreviousState is XmlAttributeValueState) {
					if (att is null) {
						InvalidParserStateException.ThrowExpected<XAttribute> (context);
					}

					//Got value, so end attribute
					replayCharacter = true;
					EndAndPop (logDuplicate: true);
					return Parent;
				}
				else {
					//starting a new attribute
					Debug.Assert (att == null);
					Debug.Assert (context.StateTag == NAMING);
					att = new XAttribute (context.PositionBeforeCurrentChar);
					context.Nodes.Push (att);
					replayCharacter = true;
					return XmlNameState;
				}
			}

			if (att is null) {
				InvalidParserStateException.ThrowExpected<XAttribute> (context);
			}

			if (isEndOfFile) {
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteAttributeEof, context.PositionBeforeCurrentChar);
				return EndAndPop ();
			}

			if (context.StateTag == GETTINGEQ) {
				if (char.IsWhiteSpace (c)) {
					return null;
				}
				if (c == '=') {
					context.StateTag = GETTINGVAL;
					return null;
				}
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteAttribute, (TextSpan)context.PositionBeforeCurrentChar, c);
			} else if (context.StateTag == GETTINGVAL) {
				if (char.IsWhiteSpace (c)) {
					return null;
				}
				replayCharacter = true;
				return AttributeValueState;
			} else if (c != '<') {
				//parent handles message for '<'
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteAttribute, (TextSpan)context.PositionBeforeCurrentChar, c);
			}

			if (att is not null) {
				replayCharacter = true;
				return EndAndPop ();
			}

			replayCharacter = true;
			return Parent;

			XmlParserState? EndAndPop (bool logDuplicate = false)
			{
				var att = (XAttribute)context.Nodes.Pop ();
				att.End (context.PositionBeforeCurrentChar);

				if (!att.IsNamed) {
					return Parent;
				}

				var element = (IAttributedXObject)context.Nodes.Peek ();

				if (logDuplicate && context.Diagnostics is not null && element.Attributes.Get (att.Name, false) is not null) {
					context.Diagnostics?.Add (XmlCoreDiagnostics.DuplicateAttributeName, att.Span, att.Name);
				}

				element.Attributes.AddAttribute (att);
				return Parent;
			}
		}

		public override XmlParserContext? TryRecreateState (ref XObject xobject, int position)
		{
			// recreating name builder and value builder state is a pain to get right
			// for now, let parent recreate state at start of attribute
			return null;
		}

		public static bool IsExpectingQuote (XmlParserContext context) => context.CurrentState is XmlAttributeState && context.StateTag == GETTINGVAL;
	}
}
