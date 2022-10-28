using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainLanguageServer
{
    [Serializable]
    public struct Position : IEquatable<Position>
    {
        /// <summary>
        /// Line position in a document (zero-based).
        /// </summary>
        public int line;

        /// <summary>
        /// Character offset on a line in a document (zero-based). Assuming that the line is
        /// represented as a string, the `character` value represents the gap between the
        /// `character` and `character + 1`.
        /// 
        /// If the character value is greater than the line length it defaults back to the
        /// line length.
        /// </summary>
        public int character;

        public static implicit operator SourceLocation(Position p) => new SourceLocation(p.line + 1, p.character + 1);
        public static implicit operator Position(SourceLocation loc) => new Position { line = loc.Line - 1, character = loc.Column - 1 };

        public static bool operator >(Position p1, Position p2) => p1.line > p2.line || p1.line == p2.line && p1.character > p2.character;
        public static bool operator <(Position p1, Position p2) => p1.line < p2.line || p1.line == p2.line && p1.character < p2.character;
        public static bool operator ==(Position p1, Position p2) => p1.Equals(p2);
        public static bool operator !=(Position p1, Position p2) => !p1.Equals(p2);

        public bool Equals(Position other) => line == other.line && character == other.character;

        public override bool Equals(object obj) => obj is Position other ? Equals(other) : false;

        public override int GetHashCode() => 0;
        public override string ToString() => $"({line}, {character})";

    }
}
