using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainLanguageServer
{
    /// <summary>
    /// Represents a location in source code.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("({Line}, {Column})")]
    public struct SourceLocation : IComparable<SourceLocation>, IEquatable<SourceLocation>
    {
        private readonly int _index;

        /// <summary>
        /// Creates a new source location.
        /// </summary>
        /// <param name="index">The index in the source stream the location represents (0-based).</param>
        /// <param name="line">The line in the source stream the location represents (1-based).</param>
        /// <param name="column">The column in the source stream the location represents (1-based).</param>
        [DebuggerStepThrough]
        public SourceLocation(int index, int line, int column)
        {
            ValidateLocation(index, line, column);

            _index = index;
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Creates a new source location.
        /// </summary>
        /// <param name="line">The line in the source stream the location represents (1-based).</param>
        /// <param name="column">The column in the source stream the location represents (1-based).</param>
        [DebuggerStepThrough]
        public SourceLocation(int line, int column)
        {
            ValidateLocation(0, line, column);

            _index = -1;
            Line = line;
            Column = column;
        }

        [DebuggerStepThrough]
        private static void ValidateLocation(int index, int line, int column)
        {
            if (index < 0)
            {
                throw ErrorOutOfRange("index", 0);
            }
            if (line < 1)
            {
                throw ErrorOutOfRange("line", 1);
            }
            if (column < 1)
            {
                throw ErrorOutOfRange("column", 1);
            }
        }

        [DebuggerStepThrough]
        private static Exception ErrorOutOfRange(object p0, object p1)
            => new ArgumentOutOfRangeException(string.Format("{0} must be greater than or equal to {1}", p0, p1));

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        private SourceLocation(int index, int line, int column, bool noChecks)
        {
            _index = index;
            Line = line;
            Column = column;
        }

        /// <summary>
        /// The index in the source stream the location represents (0-based).
        /// </summary>
        public int Index => _index >= 0 ? _index : throw new InvalidOperationException("Index is not valid");

        /// <summary>
        /// The line in the source stream the location represents (1-based).
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// The column in the source stream the location represents (1-based).
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Compares two specified location values to see if they are equal.
        /// </summary>
        /// <param name="left">One location to compare.</param>
        /// <param name="right">The other location to compare.</param>
        /// <returns>True if the locations are the same, False otherwise.</returns>
        public static bool operator ==(SourceLocation left, SourceLocation right)
        {
            return left.Line == right.Line && left.Column == right.Column;
        }

        /// <summary>
        /// Compares two specified location values to see if they are not equal.
        /// </summary>
        /// <param name="left">One location to compare.</param>
        /// <param name="right">The other location to compare.</param>
        /// <returns>True if the locations are not the same, False otherwise.</returns>
        public static bool operator !=(SourceLocation left, SourceLocation right)
        {
            return left.Line != right.Line || left.Column != right.Column;
        }

        /// <summary>
        /// Compares two specified location values to see if one is before the other.
        /// </summary>
        /// <param name="left">One location to compare.</param>
        /// <param name="right">The other location to compare.</param>
        /// <returns>True if the first location is before the other location, False otherwise.</returns>
        public static bool operator <(SourceLocation left, SourceLocation right)
        {
            return left.Line < right.Line || (left.Line == right.Line && left.Column < right.Column);
        }

        /// <summary>
        /// Compares two specified location values to see if one is after the other.
        /// </summary>
        /// <param name="left">One location to compare.</param>
        /// <param name="right">The other location to compare.</param>
        /// <returns>True if the first location is after the other location, False otherwise.</returns>
        public static bool operator >(SourceLocation left, SourceLocation right)
        {
            return left.Line > right.Line || (left.Line == right.Line && left.Column > right.Column);
        }

        /// <summary>
        /// Compares two specified location values to see if one is before or the same as the other.
        /// </summary>
        /// <param name="left">One location to compare.</param>
        /// <param name="right">The other location to compare.</param>
        /// <returns>True if the first location is before or the same as the other location, False otherwise.</returns>
        public static bool operator <=(SourceLocation left, SourceLocation right)
        {
            return left.Line < right.Line || (left.Line == right.Line && left.Column <= right.Column);
        }

        /// <summary>
        /// Compares two specified location values to see if one is after or the same as the other.
        /// </summary>
        /// <param name="left">One location to compare.</param>
        /// <param name="right">The other location to compare.</param>
        /// <returns>True if the first location is after or the same as the other location, False otherwise.</returns>
        public static bool operator >=(SourceLocation left, SourceLocation right)
        {
            return left.Line > right.Line || (left.Line == right.Line && left.Column >= right.Column);
        }

        /// <summary>
        /// Compares two specified location values.
        /// </summary>
        /// <param name="left">One location to compare.</param>
        /// <param name="right">The other location to compare.</param>
        /// <returns>0 if the locations are equal, -1 if the left one is less than the right one, 1 otherwise.</returns>
        public static int Compare(SourceLocation left, SourceLocation right)
        {
            if (left < right)
            {
                return -1;
            }

            if (left > right)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// A location that is valid but represents no location at all.
        /// </summary>
        public static readonly SourceLocation None = new SourceLocation(0, 0xfeefee, 0, true);

        /// <summary>
        /// An invalid location.
        /// </summary>
        public static readonly SourceLocation Invalid = new SourceLocation(0, 0, 0, true);

        /// <summary>
        /// A minimal valid location.
        /// </summary>
        public static readonly SourceLocation MinValue = new SourceLocation(0, 1, 1);

        /// <summary>
        /// Whether the location is a valid location.
        /// </summary>
        /// <returns>True if the location is valid, False otherwise.</returns>
        public bool IsValid => Line > 0 && Column > 0;

        /// <summary>
        /// Returns a new SourceLocation with modified column. This will never
        /// result in a column less than 1 (unless the original location is invalid),
        /// and will not modify the line number.
        /// </summary>
        public SourceLocation AddColumns(int columns)
        {
            if (!IsValid)
            {
                return Invalid;
            }

            int newIndex = _index, newCol = Column;

            // These comparisons have been arranged to allow columns to
            // be int.MaxValue without the arithmetic overflowing.
            // The naive version is shown as a comment.

            // if (this.Column + columns > int.MaxValue)
            if (columns > int.MaxValue - Column)
            {
                newCol = int.MaxValue;
                if (newIndex >= 0)
                {
                    newIndex = int.MaxValue;
                }
                // if (this.Column + columns <= 0)
            }
            else if (columns == int.MinValue || (columns < 0 && Column <= -columns))
            {
                newCol = 1;
                if (newIndex >= 0)
                {
                    newIndex += 1 - Column;
                }
            }
            else
            {
                newCol += columns;
                if (newIndex >= 0)
                {
                    newIndex += columns;
                }
            }
            return newIndex >= 0 ? new SourceLocation(newIndex, Line, newCol) : new SourceLocation(Line, newCol);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SourceLocation))
            {
                return false;
            }

            var other = (SourceLocation)obj;
            return other.Line == Line && other.Column == Column;
        }

        public override int GetHashCode() => (Line << 16) ^ Column;

        public override string ToString() => $"({Line}, {Column})";

        public bool Equals(SourceLocation other) => other.Line == Line && other.Column == Column;

        public int CompareTo(SourceLocation other)
        {
            var c = Line.CompareTo(other.Line);
            return c == 0 ? Column.CompareTo(other.Column) : c;
        }
    }
}
