namespace RainScript.Compiler
{
    internal struct Anchor
    {
        public readonly TextInfo textInfo;
        public readonly int start, end;//[,]
        public StringSegment Segment
        {
            get
            {
                if (textInfo == null) return new StringSegment("_", 0, 0);
                return new StringSegment(textInfo.context, start, end);
            }
        }
        public int StartLine
        {
            get
            {
                if ((bool)this) return textInfo.TryGetLineInfo(start, out var line) ? line.number : 0;
                else return 0;
            }
        }
        public Anchor(TextInfo textInfo, StringSegment segment)
        {
            this.textInfo = textInfo;
            if (segment.value != textInfo.context) throw ExceptionGeneratorCompiler.TextInfoMismatching();
            start = segment.start;
            end = segment.end;
        }
        public Anchor(TextInfo textInfo, int start, int end)
        {
            this.textInfo = textInfo;
            this.start = start;
            this.end = end;
        }
        public override bool Equals(object obj)
        {
            if (obj is Anchor anchor) return this == anchor;
            return false;
        }
        public override int GetHashCode()
        {
            return textInfo.path.GetHashCode() + Segment.GetHashCode();
        }
        public override string ToString()
        {
            if (textInfo == null) return "Invalid Anchor";
            else if (textInfo.TryGetLineInfo(start, out var line)) return "{0}[line {1}]:{2}".Format(textInfo.path, line.number, Segment);
            else return "{0}[{1},{2}]:{3}".Format(textInfo.path, start, end, Segment);
        }
        public static bool operator ==(Anchor left, Anchor right)
        {
            return left.textInfo == right.textInfo && left.start == right.start && left.end == right.end;
        }
        public static bool operator !=(Anchor left, Anchor right)
        {
            return !(left == right);
        }
        public static bool operator ==(Anchor anchor, string value)
        {
            return anchor.Segment == value;
        }
        public static bool operator !=(Anchor anchor, string value)
        {
            return !(anchor == value);
        }
        public static explicit operator bool(Anchor anchor)
        {
            return anchor.textInfo != null && anchor.start <= anchor.end;
        }
    }
}
