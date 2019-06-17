// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using MonoDevelop.Xml.Editor.IntelliSense;
using MonoDevelop.Xml.Parser;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace MonoDevelop.Xml.Tests.Completion
{
	[TestFixture]
	public class CompletionTriggerTests
	{
		[Test]
		[TestCase("", XmlCompletionTrigger.ElementWithBracket)]
		[TestCase("<", XmlCompletionTrigger.Element)]
		[TestCase ("", '<', XmlCompletionTrigger.ElementWithBracket)]
		[TestCase ("", 'a', XmlCompletionTrigger.None)]
		[TestCase("\"", '"', XmlCompletionTrigger.None)]
		[TestCase("<", XmlCompletionTrigger.Element)]
		[TestCase("<foo", '"', XmlCompletionTrigger.None)]
		[TestCase ("<foo", ' ', XmlCompletionTrigger.Attribute)]
		[TestCase ("<foo bar='1'   ", ' ', XmlCompletionTrigger.Attribute)]
		[TestCase ("<foo ", XmlCompletionTrigger.Attribute)]
		[TestCase ("<foo bar", XmlCompletionTrigger.Attribute, 3)]
		[TestCase ("", '&', XmlCompletionTrigger.Entity)]
		[TestCase ("<foo ", '&', XmlCompletionTrigger.None)]
		[TestCase ("<foo bar='", '&', XmlCompletionTrigger.Entity)]
		[TestCase ("<", '!', XmlCompletionTrigger.DocTypeOrCData, 2)]
		[TestCase ("<!", XmlCompletionTrigger.DocTypeOrCData, 2)]
		[TestCase ("<!DOCTYPE foo", XmlCompletionTrigger.DocType, 13)]
		[TestCase ("<!DOC", XmlCompletionTrigger.DocType, 5)]
		[TestCase ("<foo bar=\"", XmlCompletionTrigger.AttributeValue, 0)]
		[TestCase ("<foo bar='", XmlCompletionTrigger.AttributeValue, 0)]
		[TestCase ("<foo bar=", '"', XmlCompletionTrigger.AttributeValue, 0)]
		[TestCase ("<foo bar=", '\'', XmlCompletionTrigger.AttributeValue, 0)]
		[TestCase ("<foo bar='wxyz", XmlCompletionTrigger.AttributeValue, 4)]
		[TestCase ("<foo bar=wxyz", XmlCompletionTrigger.None)]
		public void TriggerTests (object[] args)
		{
			string doc = (string)args[0];
			char triggerChar = (args[1] as char?) ?? '\0';
			var kind = (XmlCompletionTrigger)(args[1] is char ? args[2] : args[1]);
			int length = args[args.Length - 1] as int? ?? 0;

			if (triggerChar != '\0') {
				doc += triggerChar;
			}

			var spine = new XmlParser (new XmlRootState (), false);
			spine.Parse (new StringReader (doc));

			var result = XmlCompletionTriggering.GetTrigger (spine, triggerChar);
			Assert.AreEqual (kind, result.kind);
			Assert.AreEqual (length, result.length);
		}
	}
}
