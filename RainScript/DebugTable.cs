using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using RainScript.Vector;
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
        /// <summary>
        /// 全局变量
        /// </summary>
        [Serializable]
        public struct GlobalVariable
        {
            /// <summary>
            /// 变量名
            /// </summary>
            public readonly string name;
            internal readonly Type type;
            internal readonly uint library;
            internal readonly uint index;
            internal GlobalVariable(string name, Type type, uint library, uint index)
            {
                this.name = name;
                this.type = type;
                this.library = library;
                this.index = index;
            }
        }
        /// <summary>
        /// 空间
        /// </summary>
        [Serializable]
        public class Space
        {
            /// <summary>
            /// 编号
            /// </summary>
            public readonly int index;
            /// <summary>
            /// 名称
            /// </summary>
            public readonly string name;
            internal readonly List<Space> spaces = new List<Space>();
            internal readonly List<int> variables = new List<int>();
            internal Space(int index, string name)
            {
                this.index = index;
                this.name = name;
            }
            /// <summary>
            /// 遍历当前空间下的全局变量
            /// </summary>
            /// <param name="table"></param>
            /// <returns></returns>
            public IEnumerable<GlobalVariable> GetVariables(DebugTable table)
            {
                foreach (var item in variables)
                {
                    yield return table.globalVariables[item];
                }
            }
            /// <summary>
            /// 遍历子空间
            /// </summary>
            /// <returns></returns>
            public IEnumerator<Space> GetEnumerator()
            {
                foreach (var space in spaces)
                {
                    yield return space;
                }
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
        /// <summary>
        /// 变量信息
        /// </summary>
        [Serializable]
        public struct VariableInfo
        {
            /// <summary>
            /// 变量名
            /// </summary>
            public readonly string name;
            internal readonly Type type;
            /// <summary>
            /// 地址
            /// </summary>
            public readonly uint address;
            internal VariableInfo(string name, Type type, uint address)
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
            internal Segment(int line, int column, int length)
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

            internal Breakpoint(uint line, uint point)
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
        /// <summary>
        /// 遍历程序集
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Space> GetSpaces()
        {
            return spaces;
        }
        /// <summary>
        /// 获取指定编号的空间
        /// </summary>
        /// <param name="index"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetSpace(int index, out Space result)
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

        internal DebugTable(string name)
        {
            this.name = name;
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
        /// <summary>
        /// 获取断点位置
        /// </summary>
        /// <param name="path"></param>
        /// <param name="line"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool TryGetBreakpoint(string path, int line, out uint point)
        {
            if (TryGetFunction(path, line, out var function) && function.points.TryGetValue(line, out point))
                return true;
            point = 0;
            return false;
        }
        /// <summary>
        /// 获取文本所属变量
        /// </summary>
        /// <param name="path"></param>
        /// <param name="line"></param>
        /// <param name="column"></param>
        /// <param name="variable"></param>
        /// <returns></returns>
        public bool TryGetVariable(string path, int line, int column, out VariableInfo variable)
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
        /// <summary>
        /// 获取文本所属变量
        /// </summary>
        /// <param name="path"></param>
        /// <param name="line"></param>
        /// <param name="column"></param>
        /// <param name="variable"></param>
        /// <returns></returns>
        public bool TryGetGlobalVariable(string path, int line, int column, out GlobalVariable variable)
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
        /// <summary>
        /// 当前上下文中的局部变量列表(可能有重复变量名)
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public IEnumerable<VariableInfo> GetVariables(uint point)
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
        /// <summary>
        /// 获取函数信息
        /// </summary>
        /// <param name="point"></param>
        /// <param name="path"></param>
        /// <param name="fn"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public bool TryGetFunctionInfo(uint point, out string path, out string fn, out uint line)
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
        private static unsafe string Evaluate(VirtualMachine.Kernel kernel, Type type, byte* address)
        {
            if (type.dimension == 0)
            {
                switch (type.definition.code)
                {
                    case TypeCode.Invalid: goto default;
                    case TypeCode.Bool: return ((bool*)(address))->ToString();
                    case TypeCode.Integer: return ((long*)(address))->ToString();
                    case TypeCode.Real: return ((real*)(address))->ToString();
                    case TypeCode.Real2: return ((Real2*)(address))->ToString();
                    case TypeCode.Real3: return ((Real3*)(address))->ToString();
                    case TypeCode.Real4: return ((Real4*)(address))->ToString();
                    case TypeCode.String: return kernel.stringAgency.Get(*(uint*)address);
                    case TypeCode.Handle: return string.Format("托管类型：{0}", *(uint*)address);
                    case TypeCode.Interface: return string.Format("接口类型：{0}", *(uint*)address);
                    case TypeCode.Function: return string.Format("函数指针：{0}", *(uint*)address);
                    case TypeCode.Coroutine: return string.Format("协程：{0}", *(uint*)address);
                    case TypeCode.Entity: return string.Format("实体：{0}", ((Entity*)address)->entity);
                    default: return "未知的类型";
                }
            }
            if (kernel.heapAgency.TryGetArrayLength(*(uint*)address, out var length) == VirtualMachine.ExitCode.None) return string.Format("数组[{0}]：{1}", length, *(uint*)address);
            else return string.Format("数组：{1}", length, *(uint*)address);
        }
        /// <summary>
        /// 计算变量值
        /// </summary>
        /// <param name="kernel"></param>
        /// <param name="info"></param>
        /// <param name="stack"></param>
        /// <returns></returns>
        public static unsafe string Evaluate(VirtualMachine.Kernel kernel, VariableInfo info, byte* stack)
        {
            return Evaluate(kernel, info.type, stack + info.address);
        }
        /// <summary>
        /// 计算变量值
        /// </summary>
        /// <param name="kernel"></param>
        /// <param name="info"></param>
        /// <param name="library"></param>
        /// <returns></returns>
        public static unsafe string Evaluate(VirtualMachine.Kernel kernel, GlobalVariable info, uint library)
        {
            kernel.libraryAgency[library].LocalToGlobal(info.library, info.index, out var globalLibrary, out var globalVariable);
            var targetLibrary = kernel.libraryAgency[globalLibrary];
            return Evaluate(kernel, info.type, targetLibrary.data + targetLibrary.variables[globalVariable]);
        }
        /// <summary>
        /// 获取库的代码指针
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public static unsafe byte* GetCode(object library)
        {
            if (library is VirtualMachine.RuntimeLibraryInfo runtime) return runtime.code;
            return null;
        }
        /// <summary>
        /// 获取库的数据指针
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public static unsafe byte* GetData(object library)
        {
            if (library is VirtualMachine.RuntimeLibraryInfo runtime) return runtime.data;
            return null;
        }
        /// <summary>
        /// 获取携程当前栈
        /// </summary>
        /// <param name="coroutine"></param>
        /// <returns></returns>
        public static unsafe byte* GetStack(object coroutine)
        {
            if (coroutine is VirtualMachine.Coroutine cor) return cor.stack;
            else return null;
        }
    }
}
