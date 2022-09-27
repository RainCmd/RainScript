using System.Collections.Generic;

namespace RainScript.Compiler
{
    internal class DebugTableGenerator
    {
        private readonly DebugTable table;
        public DebugTableGenerator(CompilerCommand command, string name)
        {
            if (command.generatorDebugTable) table = new DebugTable(name);
        }

        public DebugTable Generator()
        {
            return table;
        }

        internal void AddFunction(string file, int line, uint point)
        {
            if (table == null) return;
            if (!table.files.TryGetValue(file, out var functions))
            {
                functions = new List<DebugTable.Function>();
                table.files.Add(file, functions);
            }
            functions.Add(new DebugTable.Function(line, point));
        }
        private bool TryGetFunction(Anchor anchor, uint point, out DebugTable.Function function)
        {
            function = null;
            if (table == null) return false;
            if (anchor.textInfo == null) return false;
            if (table.files.TryGetValue(anchor.textInfo.path, out var functions))
            {
                foreach (var item in functions)
                    if (item.point <= point)
                        function = item;
            }
            if (function != null)
            {
                function.endLine = anchor.StartLine;
            }
            return function != null;
        }
        internal void AddBreakpoint(Anchor anchor, uint point)
        {
            if (TryGetFunction(anchor, point, out var function))
            {
                function.points.Add(anchor.StartLine, point);
            }
        }
        internal void AddVariable(Anchor anchor, uint point, uint address, Type type)
        {
            if (TryGetFunction(anchor, point, out var function))
            {
                if (!function.variables.TryGetValue(address, out var variable))
                {
                    variable = new DebugTable.Variable(anchor.Segment, type);
                    function.variables.Add(address, variable);
                }
                if (anchor.textInfo != null && anchor.textInfo.TryGetLineInfo(anchor.start, out var line))
                {
                    variable.segments.Add(new DebugTable.Segment(line.number, anchor.start - line.segment.start, anchor.Segment.Length));
                }
            }
        }
        internal void AddGlobalValue(string fullName,uint point,Type type)
        {
            if (table == null) return;
            table.globalVariables.Add(new DebugTable.VariableInfo(fullName, type, point));
        }
    }
}
