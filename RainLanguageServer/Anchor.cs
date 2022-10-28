using RainScript;

namespace RainLanguageServer
{
    internal class Anchor
    {
        public Document document;
        public int line, column, length;
        public Anchor(Document document, int line, int column, int length)
        {
            this.document = document;
            this.line = line;
            this.column = column;
            this.length = length;
        }
        public void ApplyChanged(int end, int afterEnd)
        {
            if (line > end) line += afterEnd - end;
        }
        public bool IsEquals(string value)
        {
            if (value.Length == length)
            {
                var line = document.lines[this.line];
                for (int i = 0; i < length; i++)
                    if (value[i] != line[column + i])
                        return false;
                return true;
            }
            return false;
        }
        public bool IsEquals(Anchor other)
        {
            if (other.length == length)
            {
                var thisLine = document.lines[line];
                var otherLine = other.document.lines[other.line];
                for (int i = 0; i < length; i++)
                    if (thisLine[column + i] != otherLine[other.column + i])
                        return false;
                return true;
            }
            return false;
        }
        public bool IsEquals(StringSegment segment)
        {
            if (segment.Length == length)
            {
                var line = document.lines[this.line];
                for (int i = 0; i < length; i++)
                    if (segment[i] != line[column + i])
                        return false;
                return true;
            }
            return false;
        }
        public static explicit operator bool(Anchor anchor)
        {
            return anchor != null && anchor.document != null;
        }
    }
}
