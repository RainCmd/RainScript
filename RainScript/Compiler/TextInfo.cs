using System.Collections.Generic;

namespace RainScript.Compiler
{
    internal struct TextSegment
    {
        public readonly TextInfo text;
        /// <summary>
        /// 行索引[,]
        /// </summary>
        public readonly int start, end;
        /// <summary>
        /// [,]
        /// </summary>
        public TextSegment(TextInfo text, int start, int end)
        {
            this.text = text;
            this.start = start;
            this.end = end;
        }
        public static explicit operator bool(TextSegment segment)
        {
            return segment.text != null && segment.start <= segment.end;
        }
    }
    internal class LineInfo
    {
        public readonly int number;
        public readonly StringSegment segment;
        public readonly int indent;
        public LineInfo(int number, StringSegment segment, int indent)
        {
            this.number = number;
            this.segment = segment;
            this.indent = indent;
        }
        public override string ToString()
        {
            return "line:" + number;
        }
    }
    internal class TextInfo
    {
        public readonly string path;
        public readonly string context;
        private readonly IList<LineInfo> lines;
        public int LineCount { get { return lines.Count; } }
        public LineInfo this[int index] { get { return lines[index]; } }
        public TextInfo(string path, string context)
        {
            this.path = path;
            this.context = context;
            lines = new List<LineInfo>();
            var line = 0;
            var start = 0;
            var indent = 0;
            var blank = true;
            for (int i = 0; i < context.Length; i++)
                switch (context[i])
                {
                    case '\r':
                        break;
                    case '\n':
                        if (!blank) lines.Add(new LineInfo(line, new StringSegment(context, start, i - 1), indent));
                        start = i + 1;
                        line++;
                        blank = true;
                        indent = 0;
                        break;
                    case '\'':
                    case '\"':
                        blank = false;
                        while (++i < context.Length)
                            if (context[i] == '\'' || context[i] == '\"') break;
                            else if (context[i] == '\\') i++;
                            else if (context[i] == '\n')
                            {
                                lines.Add(new LineInfo(line, new StringSegment(context, start, i - 1), indent));
                                start = i + 1;
                                line++;
                                blank = true;
                                indent = 0;
                            }
                        break;
                    case '/':
                        if (i + 1 < context.Length && context[i + 1] == '/')
                        {
                            var index = i - 1;
                            while (++i < context.Length) if (context[i] == '\n') break;
                            if (!blank) lines.Add(new LineInfo(line, new StringSegment(context, start, index), indent));
                            start = i + 1;
                            line++;
                            blank = true;
                            indent = 0;
                        }
                        else blank = false;
                        break;
                    case '\\':
                        while (++i < context.Length)
                            if (context[i] == '\n')
                            {
                                if (blank)
                                {
                                    start = i + 1;
                                    line++;
                                    indent = 0;
                                }
                                break;
                            }
                            else if (context[i] == '/')
                            {
                                if (i + 1 < context.Length && context[i + 1] == '/')
                                {
                                    while (++i < context.Length) if (context[i] == '\n') break;
                                    if (blank)
                                    {
                                        start = i + 1;
                                        line++;
                                        indent = 0;
                                    }
                                }
                                blank = false;
                                break;
                            }
                            else if (!char.IsWhiteSpace(context[i]))
                            {
                                blank = false;
                                break;
                            }
                        break;
                    case ' ':
                        if (blank) indent++;
                        break;
                    case '\t':
                        if (blank) indent += TAB_INDENT;
                        break;
                    default:
                        blank = false;
                        break;
                }
            if (!blank) lines.Add(new LineInfo(line, new StringSegment(context, start, context.Length - 1), indent));
        }
        public bool TryGetLineInfo(int charIndex, out LineInfo line)
        {
            line = default;
            if (charIndex < 0 || charIndex >= context.Length || lines.Count == 0) return false;
            var start = 0;
            var end = lines.Count;
            while (start < end)
            {
                var middle = (start + end) >> 1;
                line = lines[middle];
                if (charIndex < line.segment.start) end = middle;
                else if (charIndex > line.segment.end) start = middle + 1;
                else return true;
            }
            return false;
        }
        public override string ToString()
        {
            return path;
        }
        private const int TAB_INDENT = 4;
    }
}
