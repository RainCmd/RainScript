﻿using System;
using System.Diagnostics;

namespace RainLanguageServer
{
    /// <summary>
    /// Stores the location of a span of text in a source file.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("({Start.Line}, {Start.Column})-({End.Line}, {End.Column})")]
    public struct SourceSpan : IComparable<SourceSpan>
    {
        /// <summary>
        /// Constructs a new span with a specific start and end location.
        /// </summary>
        /// <param name="start">The beginning of the span.</param>
        /// <param name="end">The end of the span.</param>
        [DebuggerStepThrough]
        public SourceSpan(SourceLocation start, SourceLocation end)
        {
            ValidateLocations(start, end);
            Start = start;
            End = end;
        }

        [DebuggerStepThrough]
        public SourceSpan(int startLine, int startColumn, int endLine, int endColumn)
            : this(new SourceLocation(startLine, startColumn), new SourceLocation(endLine, endColumn)) { }

        [DebuggerStepThrough]
        private static void ValidateLocations(SourceLocation start, SourceLocation end)
        {
            if (start.IsValid && end.IsValid)
            {
                if (start > end)
                {
                    throw new ArgumentException("Start and End must be well ordered");
                }
            }
            else
            {
                if (start.IsValid || end.IsValid)
                {
                    throw new ArgumentException("Start and End must both be valid or both invalid");
                }
            }
        }

        /// <summary>
        /// The start location of the span.
        /// </summary>
        public SourceLocation Start { get; }

        /// <summary>
        /// The end location of the span. Location of the first character behind the span.
        /// </summary>
        public SourceLocation End { get; }

        /// <summary>
        /// A valid span that represents no location.
        /// </summary>
        public static readonly SourceSpan None = new SourceSpan(SourceLocation.None, SourceLocation.None);

        /// <summary>
        /// An invalid span.
        /// </summary>
        public static readonly SourceSpan Invalid = new SourceSpan(SourceLocation.Invalid, SourceLocation.Invalid);

        /// <summary>
        /// Whether the locations in the span are valid.
        /// </summary>
        public bool IsValid => Start.IsValid && End.IsValid;

        public SourceSpan Union(SourceSpan other)
        {
            var startLine = Math.Min(other.Start.Line, Start.Line);
            var startColumn = Math.Min(other.Start.Column, Start.Column);

            var endLine = Math.Max(other.End.Line, End.Line);
            var endColumn = Math.Max(other.End.Column, End.Column);

            return new SourceSpan(new SourceLocation(startLine, startColumn), new SourceLocation(endLine, endColumn));
        }

        /// <summary>
        /// Compares two specified Span values to see if they are equal.
        /// </summary>
        /// <param name="left">One span to compare.</param>
        /// <param name="right">The other span to compare.</param>
        /// <returns>True if the spans are the same, False otherwise.</returns>
        public static bool operator ==(SourceSpan left, SourceSpan right)
            => left.Start == right.Start && left.End == right.End;

        /// <summary>
        /// Compares two specified Span values to see if they are not equal.
        /// </summary>
        /// <param name="left">One span to compare.</param>
        /// <param name="right">The other span to compare.</param>
        /// <returns>True if the spans are not the same, False otherwise.</returns>
        public static bool operator !=(SourceSpan left, SourceSpan right)
            => left.Start != right.Start || left.End != right.End;

        public int CompareTo(SourceSpan other)
        {
            if (Start.Line < other.Start.Line)
            {
                return -1;
            }
            if (Start.Line == other.Start.Line)
            {
                if (Start.Column < other.Start.Column)
                {
                    return -1;
                }
                return Start.Column == other.Start.Column ? 0 : 1;
            }
            return 1;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SourceSpan))
            {
                return false;
            }

            var other = (SourceSpan)obj;
            return Start == other.Start && End == other.End;
        }

        public override string ToString() => string.Format("{0} - {1}", Start, End);

        public override int GetHashCode()
            // 7 bits for each column (0-128), 9 bits for each row (0-512), xor helps if
            // we have a bigger file.
            => (Start.Column) ^ (End.Column << 7) ^ (Start.Line << 14) ^ (End.Line << 23);
    }
}
