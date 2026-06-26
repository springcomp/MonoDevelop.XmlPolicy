// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace MonoDevelop.Xml.Dom
{
	public struct TextSpan : IEquatable<TextSpan>
	{
		/// <summary>
		/// Sentinel span used when no source offset information is available.
		/// </summary>
		public static readonly TextSpan Invalid = new (-1, 0);

		public TextSpan (int start, int length)
		{
			Start = start;
			Length = length;
		}

		public int Start { get; }
		public int Length { get; }
		public int End => Start + Length;

		/// <summary>
		/// Gets whether this span points to a known offset range.
		/// </summary>
		public bool IsValid => Start >= 0;

		public bool Contains (int offset) => IsValid && offset >= Start && offset < End;

		public bool ContainsOuter (int offset) => IsValid && offset >= Start && offset <= End;
		public bool Contains (TextSpan other) => IsValid && other.IsValid && Start <= other.Start && (End > other.End || (End == other.End && other.Length > 0));
		public bool ContainsOuter (TextSpan other) => IsValid && other.IsValid && Start <= other.Start && End >= other.End;
		public bool Intersects (TextSpan other) => IsValid && other.IsValid && other.Start <= End && other.End >= Start;

		public static TextSpan FromBounds (int start, int end) => start < 0 || end < start ? Invalid : new (start, end - start);

		public override string ToString () => $"({Start}-{End})";

		public bool Equals (TextSpan other) => other.Start == Start && other.Length == Length;

		public override bool Equals (object? obj) => obj is TextSpan t && t.Equals (this);

		public override int GetHashCode () => (Start << 16) ^ (Start >> 16) ^ Length; //try to distribute bits over the range a bit better

		public static explicit operator TextSpan (int position) => new (position, 0);
	}
}
