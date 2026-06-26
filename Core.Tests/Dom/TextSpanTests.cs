using MonoDevelop.Xml.Dom;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Dom
{
	[TestFixture]
	public class TextSpanTests
	{
		[Test]
		public void InvalidSpanIsMarkedInvalid ()
		{
			Assert.That (TextSpan.Invalid.IsValid, Is.False);
			Assert.That (new TextSpan (1, 2).IsValid, Is.True);
		}

		[Test]
		public void InvalidSpanDoesNotContainAnyOffset ()
		{
			Assert.That (TextSpan.Invalid.Contains (0), Is.False);
			Assert.That (TextSpan.Invalid.Contains (-1), Is.False);
		}

		[Test]
		public void InvalidSpanDoesNotContainOrIntersectOtherSpans ()
		{
			var validSpan = new TextSpan (5, 3);
			Assert.That (TextSpan.Invalid.Contains (validSpan), Is.False);
			Assert.That (validSpan.Contains (TextSpan.Invalid), Is.False);
			Assert.That (TextSpan.Invalid.Intersects (validSpan), Is.False);
			Assert.That (validSpan.Intersects (TextSpan.Invalid), Is.False);
		}

		[Test]
		public void FromBoundsReturnsInvalidForInvalidBounds ()
		{
			Assert.That (TextSpan.FromBounds (-1, 2).IsValid, Is.False);
			Assert.That (TextSpan.FromBounds (3, 2).IsValid, Is.False);
		}
	}
}
