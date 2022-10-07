using System;
using System.Collections.Generic;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript
{
    /// <summary>
    /// 调试表
    /// </summary>
    [Serializable]
    public class DebugTable
    {
        internal const int SPACE_INDEX = 0x1000_0000;
        [Serializable]
        internal struct GlobalVariable
        {
            public readonly string name;
            public readonly Type type;
            public readonly uint library;
            public readonly uint index;
            public GlobalVariable(string name, Type type, uint library, uint index)
            {
                this.name = name;
                this.type = type;
                this.library = library;
                this.index = index;
            }
        }
        [Serializable]
        internal class Space
        {
            public readonly int index;
            public readonly string name;
            public readonly List<Space> spaces = new List<Space>();
            public readonly List<int> variables = new List<int>();
            public Space(int index, string name)
            {
                this.index = index;
                this.name = name;
            }
        }
        [Serializable]
        internal struct GlobalVariableSegment
        {
            public readonly int line;
            public readonly int column;
            public readonly int length;
            public readonly int index;
            public GlobalVariableSegment(int line, int column, int length, int index)
            {
                this.line = line;
                this.column = column;
                this.length = length;
                this.index = index;
            }
        }
        [Serializable]
        internal struct VariableInfo
        {
            public readonly string name;
            public readonly Type type;
            public readonly uint address;
            public VariableInfo(string name, Type type, uint address)
            {
                this.name = name;
                this.type = type;
                this.address = address;
            }
        }
        [Serializable]
        internal struct Segment
        {
            public readonly int line;
            public readonly int column;
            public readonly int length;
            public Segment(int line, int column, int length)
            {
                this.line = line;
                this.column = column;
                this.length = length;
            }
        }
        [Serializable]
        internal struct Variable
        {
            public readonly string name;
            public readonly Type type;
            public readonly List<Segment> segments;

            public Variable(string name, Type type)
            {
                this.name = name;
                this.type = type;
                segments = new List<Segment>();
            }
        }
        [Serializable]
        internal struct Breakpoint
        {
            public readonly uint line;
            public readonly uint point;

            public Breakpoint(uint line, uint point)
            {
                this.line = line;
                this.point = point;
            }
        }
        [Serializable]
        internal class Function
        {
            public readonly int line;
            public readonly uint point;
            public int endLine;
            public readonly Dictionary<int, uint> points = new Dictionary<int, uint>();//line => address
            public readonly Dictionary<uint, Variable> variables = new Dictionary<uint, Variable>();
            public readonly List<GlobalVariableSegment> globalVariables = new List<GlobalVariableSegment>();
            public Function(int line, uint point)
            {
                this.line = line;
                this.point = point;
            }
        }
        /// <summary>
        /// 程序集名
        /// </summary>
        public readonly string name;
        internal readonly Dictionary<string, List<Function>> files = new Dictionary<string, List<Function>>();
        internal readonly List<Space> spaces = new List<Space>();
        internal readonly List<GlobalVariable> globalVariables = new List<GlobalVariable>();
        internal readonly List<string> definitions = new List<string>();
        internal readonly List<string> functions = new List<string>();
        internal readonly List<string> coroutines = new List<string>();

        internal DebugTable(string name)
        {
            this.name = name;
        }
        internal bool TryGetSpace(int index, out Space result)
        {
            foreach (var space in spaces)
                if (TryGetSpace(space, index, out result))
                    return true;
            result = default;
            return false;
        }
        private bool TryGetSpace(Space space, int index, out Space result)
        {
            if (space.index == index)
            {
                result = space;
                return true;
            }
            foreach (var item in space.spaces)
                if (TryGetSpace(item, index, out result))
                    return true;
            result = default;
            return false;
        }
        private bool TryGetFunction(string path, int line, out Function result)
        {
            if (files.TryGetValue(path, out var functions))
            {
                foreach (var function in functions)
                {
                    if (function.line < line && function.endLine >= line)
                    {
                        result = function;
                        return true;
                    }
                }
            }
            result = default;
            return false;
        }
        internal bool TryGetBreakpoint(string path, int line, out uint point)
        {
            if (TryGetFunction(path, line, out var function) && function.points.TryGetValue(line, out point))
                return true;
            point = 0;
            return false;
        }
        internal bool TryGetVariable(string path, int line, int column, out VariableInfo variable)
        {
            if (TryGetFunction(path, line, out var function))
            {
                foreach (var local in function.variables)
                {
                    foreach (var segment in local.Value.segments)
                    {
                        if (segment.line == line)
                        {
                            if (segment.column <= column && segment.column + segment.length >= column)
                            {
                                variable = new VariableInfo(local.Value.name, local.Value.type, local.Key);
                                return true;
                            }
                        }
                    }
                }
            }
            variable = default;
            return false;
        }
        internal bool TryGetGlobalVariable(string path, int line, int column, out GlobalVariable variable)
        {
            if (TryGetFunction(path, line, out var function))
            {
                foreach (var segment in function.globalVariables)
                {
                    if (segment.line == line)
                    {
                        if (segment.column <= column && segment.column + segment.length >= column)
                        {
                            variable = globalVariables[segment.index];
                            return true;
                        }
                    }
                }
            }
            variable = default;
            return false;
        }
        internal IEnumerable<VariableInfo> GetVariables(uint point)
        {
            Function function = null;
            var dis = uint.MaxValue;
            foreach (var file in files)
                foreach (var item in file.Value)
                    if (item.point < point && point - item.point < dis)
                    {
                        function = item;
                        dis = point - item.point;
                    }
            if (function != null)
                foreach (var item in function.variables)
                    yield return new VariableInfo(item.Value.name, item.Value.type, item.Key);
        }
        internal bool TryGetFunctionInfo(uint point, out string path, out string fn, out uint line)
        {
            Function function = null;
            var dis = uint.MaxValue;
            path = fn = default; line = 0;
            foreach (var file in files)
                foreach (var item in file.Value)
                    if (item.point < point && point - item.point < dis)
                    {
                        path = file.Key;
                        function = item;
                        dis = point - item.point;
                    }
            if (function != null)
            {
                fn = string.Format("line:{0} - {1}", function.line, function.endLine);
                foreach (var item in function.points)
                    if (item.Value == point)
                    {
                        line = (uint)item.Key;
                        break;
                    }
            }
            return function != null;
        }
    }
}
