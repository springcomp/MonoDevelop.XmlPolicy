//
// XmlCSharpStatementState.cs
//
// Author:
//   springcomp <springcomp@users.noreply.github.com>
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mime;
using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlCSharpStatementState : XmlParserState
	{
		const int START_OFFSET = 2; // "@("

		const int FREE = 0;
		const int MATCH_QUOTE = 1;
		const int MATCH_PARENS = 2;

		const int ESCAPE = 3;

		const string ESCAPED_CHARS = "\\\"\t\r\n";

		private Stack<int> unmatchedQuotes_ = new ();
		private Stack<int> unmatchedParens_ = new ();

		private Stack<int> states_ = new ();

		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile)
		{
			var statement = context.Nodes.Peek () as XCSharpStatement;

			//state has just been entered
			if (context.CurrentStateLength == 0 || statement is null) {
				statement = new XCSharpStatement (context.PositionBeforeCurrentChar - START_OFFSET);
				context.Nodes.Push (statement);
				context.StateTag = FREE;
			}

			if (isEndOfFile) {
				//the parent state should report the error
				return End (isEndOfFile);
			}

			switch (context.StateTag) {
			case FREE:
				if (c == '"') {
					unmatchedQuotes_.Push (context.Position);
					context.StateTag = MATCH_QUOTE;
				}
				if (c == '(') {
					unmatchedParens_.Push (context.Position);
					context.StateTag = MATCH_PARENS;
				}
				if (c == ')') {
					// ending the C# statement
					return End ();
				}
				break;

			case MATCH_QUOTE:
				if (c == '\\') {
					states_.Push (context.StateTag);
					context.StateTag = ESCAPE;
				}
				if (c == '"') {
					Debug.Assert (unmatchedQuotes_.Count > 0);
					unmatchedQuotes_.Pop ();
					context.StateTag = FREE;
				}
				break;
			case MATCH_PARENS:
				if (c == ')') {
					Debug.Assert (unmatchedParens_.Count > 0);
					unmatchedParens_.Pop ();
					context.StateTag = FREE;
				}
				break;
			case ESCAPE:
				if (ESCAPED_CHARS.Contains (new string (c, 1))) {
					Debug.Assert (states_.Count > 0);
					context.StateTag = states_.Pop ();
					// TODO: \uXXXX Unicode
					// TODO: \xXXXX
				}
				break;
			}

			context.KeywordBuilder.Append (c);
			return null;

			XmlParserState? End (bool? isEndOfFile = false)
			{
				var statement = (XCSharpStatement)context.Nodes.Peek ();
				statement.RawText = context.KeywordBuilder.ToString ();
				statement.End (context.PositionBeforeCurrentChar);
				if (isEndOfFile == true) {
					context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteCSharpStatementEof, statement.Span);
				}
				if (statement.RawText?.Length == 0) {
					context.Diagnostics?.Add (XmlCoreDiagnostics.EmptyCSharpStatementValue, statement.Span);
				}

				return Parent;
			}
		}

		public override XmlParserContext? TryRecreateState (ref XObject xobject, int position)
			=> throw new InvalidOperationException ("State has no corresponding XObject");
	}
}
