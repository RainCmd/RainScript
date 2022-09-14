namespace RainScript
{
    /// <summary>
    /// 符号表
    /// </summary>
    public class SymbolTable
    {
        internal struct Function
        {
            public readonly uint point;
            public readonly uint file;
            public readonly string function;
            public Function(uint point, uint file, string function)
            {
                this.point = point;
                this.file = file;
                this.function = function;
            }
        }
        internal struct Line
        {
            public readonly uint point;
            public readonly uint line;
            public Line(uint point, uint line)
            {
                this.point = point;
                this.line = line;
            }
        }
        private readonly string[] files;
        private readonly Function[] functions;
        private readonly Line[] lines;

        internal SymbolTable(string[] files, Function[] functions, Line[] lines)
        {
            this.files = files;
            this.functions = functions;
            this.lines = lines;
        }
        internal void GetInfo(uint point, out string file, out string function, out uint line)
        {
            if (functions.Length > 0)
            {
                var start = 0;
                var end = functions.Length;
                while (start < end)
                {
                    var middle = (start + end) >> 1;
                    if (point < functions[middle].point) end = middle;
                    else if (point >= functions[middle].point) start = middle + 1;
                }
                if (start > 0)
                {
                    start--;
                    file = files[functions[start].file];
                    function = functions[start].function;
                }
                else file = function = "";
            }
            else file = function = "";
            if (lines.Length > 0)
            {
                var start = 0;
                var end = lines.Length;
                while (start < end)
                {
                    var middle = (start + end) >> 1;
                    if (point < lines[middle].point) end = middle;
                    else if (point >= lines[middle].point) start = middle + 1;
                }
                if (start > 0) line = lines[start - 1].line;
                else line = 0;
            }
            else line = 0;
        }
    }
}
