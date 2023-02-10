using System.Collections.Generic;

namespace RainScript.Compiler
{
    internal class DebugTableGenerator
    {
        private readonly DebugTable table;
        private int index = DebugTable.SPACE_INDEX;
        public DebugTableGenerator(CompilerCommand command, string name)
        {
            if (command.generatorDebugTable) table = new DebugTable(name);
        }

        public DebugTable Generator()
        {
            return table;
        }

        internal void AddDefinition(string fullName)
        {
            table.definitions.Add(fullName);
        }
        internal void AddFunction(string fullName)
        {
            table.functions.Add(fullName);
        }
        internal void AddCoroutine(string fullName)
        {
            table.coroutines.Add(fullName);
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
                function.endLine = System.Math.Max(anchor.StartLine, function.endLine);
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
        internal void AddThisVariable(Anchor anchor, uint point, uint address, Type type)
        {
            if (TryGetFunction(anchor, point, out var function))
                function.variables.Add(address, new DebugTable.Variable(KeyWord.THIS, type));
        }
        internal void AddLocalVariable(Anchor anchor, uint point, uint address, Type type)
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
        internal void AddGlobalVariableSegment(IDeclaration declaration, Anchor anchor, uint point, uint library, uint index, Type type)
        {
            if (table == null) return;
            if (anchor.textInfo.TryGetLineInfo(anchor.start, out var line) && TryGetFunction(anchor, point, out var function))
            {
                var debugIndex = RegistGlobalVariable(declaration, library, index, type);
                function.globalVariables.Add(new DebugTable.GlobalVariableSegment(line.number, anchor.start - line.segment.start, anchor.Segment.Length, debugIndex));
            }
        }
        internal int RegistGlobalVariable(IDeclaration declaration, uint library, uint index, Type type)
        {
            if (table == null) return -1;
            var space = GetSpace(declaration.Space);
            foreach (var item in space.variables)
            {
                var variable = table.globalVariables[item];
                if (variable.library == library && variable.index == index)
                    return item;
            }
            var result = table.globalVariables.Count;
            space.variables.Add(result);
            table.globalVariables.Add(new DebugTable.GlobalVariable(declaration.Name, type, library, index));
            return result;
        }
        private DebugTable.Space GetSpace(ISpace space)
        {
            if (space.Parent == null) return AddSpace(table.spaces, space.Name);
            else return AddSpace(GetSpace(space.Parent).spaces, space.Name);
        }
        private DebugTable.Space AddSpace(IList<DebugTable.Space> spaces, string name)
        {
            foreach (var item in spaces)
                if (item.name == name)
                    return item;
            var result = new DebugTable.Space(index++, name);
            spaces.Add(result);
            return result;
        }
    }
}
