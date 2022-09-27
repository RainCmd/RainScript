using System;

namespace RainScript
{
    /// <summary>
    /// 符号表
    /// </summary>
    [Serializable]
    public partial class SymbolTable
    {
        [Serializable]
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
        [Serializable]
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
    }
}
