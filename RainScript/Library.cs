using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RainScript
{
    internal enum FunctionType : byte
    {
        Global = 1,
        Native = 2,
        Member = 4,
        Virtual = 12,
        Interface = 16,
    }
    internal enum TypeCode : byte
    {
        Invalid,
        Bool,
        Integer,
        Real,
        Real2,
        Real3,
        Real4,
        String,
        Handle,
        Interface,
        Function,
        Coroutine,
        Entity,
    }
    [StructLayout(LayoutKind.Explicit)]
    [System.Serializable]
    internal struct TypeDefinition
    {
        [FieldOffset(0)]
        public readonly uint library;
        [FieldOffset(4)]
        public readonly TypeCode code;
        [FieldOffset(5)]
        public readonly uint index;
        public TypeDefinition(uint library, TypeCode code, uint index)
        {
            this.library = library;
            this.code = code;
            this.index = index;
        }
        public override bool Equals(object obj)
        {
            return obj is TypeDefinition definition && definition == this;
        }
        public override int GetHashCode()
        {
            int hashCode = -1000423025;
            hashCode = hashCode * -1521134295 + library.GetHashCode();
            hashCode = hashCode * -1521134295 + code.GetHashCode();
            hashCode = hashCode * -1521134295 + index.GetHashCode();
            return hashCode;
        }
        public override string ToString()
        {
            return "[{0},{1},{2}]".Format(library, code, index);
        }
        public static bool operator ==(TypeDefinition left, TypeDefinition right)
        {
            return left.library == right.library && left.code == right.code && left.index == right.index;
        }
        public static bool operator !=(TypeDefinition left, TypeDefinition right)
        {
            return !(left == right);
        }
        public static readonly TypeDefinition INVALID = new TypeDefinition(LIBRARY.INVALID, TypeCode.Invalid, 0);
    }
    [StructLayout(LayoutKind.Explicit)]
    [System.Serializable]
    internal struct Type
    {
        [FieldOffset(0)]
        public readonly TypeDefinition definition;
        [FieldOffset(9)]
        public readonly uint dimension;
        public uint FieldSize
        {
            get
            {
                if (dimension > 0) return TypeCode.Handle.FieldSize();
                else return definition.code.FieldSize();
            }
        }
        public Type(TypeDefinition definition, uint dimension)
        {
            this.definition = definition;
            this.dimension = dimension;
        }
        public Type(uint library, TypeCode code, uint index, uint dimension) : this(new TypeDefinition(library, code, index), dimension) { }
        public static bool operator ==(Type a, Type b)
        {
            return a.definition == b.definition && a.dimension == b.dimension;
        }
        public static bool operator !=(Type a, Type b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return obj is Type type && this == type;
        }
        public override int GetHashCode()
        {
            int hashCode = 841965699;
            hashCode = hashCode * -1521134295 + definition.GetHashCode();
            hashCode = hashCode * -1521134295 + dimension.GetHashCode();
            return hashCode;
        }
        public override string ToString()
        {
            var postfix = "";
            for (int i = 0; i < dimension; i++) postfix += "[]";
            return definition + postfix;
        }
        public static bool IsEquals(Type[] left, Type[] right)
        {
            if (left == right) return true;
            if (left == null || right == null) return false;
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i]) return false;
            return true;
        }
        public static readonly Type INVALID = new Type(TypeDefinition.INVALID, 0);
    }
    [StructLayout(LayoutKind.Explicit)]
    [System.Serializable]
    internal struct Function
    {
        [FieldOffset(0)]
        public readonly uint method;
        [FieldOffset(4)]
        public readonly uint index;
        public Function(uint method, uint index)
        {
            this.method = method;
            this.index = index;
        }
        public static bool operator ==(Function a, Function b)
        {
            return a.method == b.method && a.index == b.index;
        }
        public static bool operator !=(Function a, Function b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return obj is Function function &&
                   method == function.method &&
                   index == function.index;
        }
        public override int GetHashCode()
        {
            int hashCode = -1253953445;
            hashCode = hashCode * -1521134295 + method.GetHashCode();
            hashCode = hashCode * -1521134295 + index.GetHashCode();
            return hashCode;
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    [System.Serializable]
    internal struct MemberVariable
    {

        [FieldOffset(0)]
        public readonly uint definition;
        [FieldOffset(4)]
        public readonly uint index;

        public MemberVariable(uint type, uint index)
        {
            this.definition = type;
            this.index = index;
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    [System.Serializable]
    internal struct MemberFunction
    {
        [FieldOffset(0)]
        public readonly uint definition;
        [FieldOffset(4)]
        public readonly uint method;
        [FieldOffset(8)]
        public readonly uint index;
        public MemberFunction(uint definition, uint method, uint index)
        {
            this.definition = definition;
            this.method = method;
            this.index = index;
        }
        public static bool operator ==(MemberFunction a, MemberFunction b)
        {
            return a.definition == b.definition && a.method == b.method && a.index == b.index;
        }
        public static bool operator !=(MemberFunction a, MemberFunction b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return obj is MemberFunction function &&
                   definition == function.definition &&
                   method == function.method &&
                   index == function.index;
        }
        public override int GetHashCode()
        {
            int hashCode = -1253953445;
            hashCode = hashCode * -1521134295 + definition.GetHashCode();
            hashCode = hashCode * -1521134295 + method.GetHashCode();
            hashCode = hashCode * -1521134295 + index.GetHashCode();
            return hashCode;
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    [System.Serializable]
    internal struct InterfaceFunction
    {
        [FieldOffset(0)]
        public readonly uint definition;
        [FieldOffset(4)]
        public readonly uint method;
        [FieldOffset(8)]
        public readonly uint index;
        public InterfaceFunction(uint definition, uint method, uint index)
        {
            this.definition = definition;
            this.method = method;
            this.index = index;
        }
        public static bool operator ==(InterfaceFunction a, InterfaceFunction b)
        {
            return a.definition == b.definition && a.method == b.method && a.index == b.index;
        }
        public static bool operator !=(InterfaceFunction a, InterfaceFunction b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return obj is InterfaceFunction function &&
                   definition == function.definition &&
                   method == function.method &&
                   index == function.index;
        }
        public override int GetHashCode()
        {
            int hashCode = -1253953445;
            hashCode = hashCode * -1521134295 + definition.GetHashCode();
            hashCode = hashCode * -1521134295 + method.GetHashCode();
            hashCode = hashCode * -1521134295 + index.GetHashCode();
            return hashCode;
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    [System.Serializable]
    internal struct DefinitionFunction
    {
        [FieldOffset(0)]
        public readonly TypeDefinition definition;
        [FieldOffset(9)]
        public readonly Function funtion;
        public DefinitionFunction(TypeDefinition definition, Function funtion)
        {
            this.definition = definition;
            this.funtion = funtion;
        }
        public static bool operator ==(DefinitionFunction a, DefinitionFunction b)
        {
            return a.definition == b.definition && a.funtion == b.funtion;
        }
        public static bool operator !=(DefinitionFunction a, DefinitionFunction b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return obj is DefinitionFunction function &&
                   definition == function.definition &&
                   funtion == function.funtion;
        }
        public override int GetHashCode()
        {
            int hashCode = -1253953445;
            hashCode = hashCode * -1521134295 + definition.GetHashCode();
            hashCode = hashCode * -1521134295 + funtion.GetHashCode();
            return hashCode;
        }
    }
    [System.Serializable]
    internal struct Relocation
    {
        public DefinitionFunction overrideFunction;
        public DefinitionFunction realizeFunction;
        public Relocation(DefinitionFunction overrideFunction, DefinitionFunction realizeFunction)
        {
            this.overrideFunction = overrideFunction;
            this.realizeFunction = realizeFunction;
        }
    }
    #region Infos
    [System.Serializable]
    internal class DefinitionInfo
    {
        public readonly TypeDefinition parent;
        public readonly TypeDefinition[] inherits;
        public readonly Type[] varibales;
        public readonly uint[] methods;
        public readonly uint destructor;
        public readonly Relocation[] relocations;
        public DefinitionInfo(TypeDefinition parent, TypeDefinition[] inherits, Type[] varibales, uint[] methods, Relocation[] relocations, uint destructor)
        {
            this.parent = parent;
            this.inherits = inherits;
            this.varibales = varibales;
            this.methods = methods;
            this.relocations = relocations;
            this.destructor = destructor;
        }
    }
    [System.Serializable]
    internal class VariableInfo
    {
        internal readonly uint address;
        internal readonly Type type;
        public VariableInfo(uint address, Type type)
        {
            this.address = address;
            this.type = type;
        }
    }
    [System.Serializable]
    internal class CoroutineInfo
    {
        internal readonly Type[] returns;
        public CoroutineInfo(Type[] returns)
        {
            this.returns = returns;
        }
    }
    [System.Serializable]
    internal class FunctionInfo
    {
        internal readonly Type[] parameters;
        internal readonly Type[] returns;
        internal FunctionInfo(Type[] parameters, Type[] returns)
        {
            this.parameters = parameters;
            this.returns = returns;
        }
        internal static readonly FunctionInfo EMPTY = new FunctionInfo(new Type[0], new Type[0]);
    }
    [System.Serializable]
    internal class MethodInfo
    {
        internal readonly uint[] entries;
        internal readonly FunctionInfo[] functions;
        public MethodInfo(uint[] entries, FunctionInfo[] functions)
        {
            this.entries = entries;
            this.functions = functions;
        }
    }
    [System.Serializable]
    internal class InterfaceMethodInfo
    {
        internal readonly FunctionInfo[] functions;
        public InterfaceMethodInfo(FunctionInfo[] functions)
        {
            this.functions = functions;
        }
    }
    [System.Serializable]
    internal class InterfaceInfo
    {
        internal readonly TypeDefinition[] inherits;
        internal readonly Relocation[] relocations;
        internal readonly InterfaceMethodInfo[] methods;
        public InterfaceInfo(TypeDefinition[] inherits, Relocation[] relocations, InterfaceMethodInfo[] methods)
        {
            this.inherits = inherits;
            this.relocations = relocations;
            this.methods = methods;
        }
    }
    [System.Serializable]
    internal class NativeMethodInfo
    {
        internal readonly string name;
        internal readonly FunctionInfo[] functions;
        internal NativeMethodInfo(string name, FunctionInfo[] functions)
        {
            this.name = name;
            this.functions = functions;
        }
    }
    #endregion Infos
    #region Imports
    [System.Serializable]
    internal class ImportInfo
    {
        internal readonly ImportSpaceInfo space;
        internal readonly string name;
        internal ImportInfo(ImportSpaceInfo space, string name)
        {
            this.space = space;
            this.name = name;
        }
        public override string ToString()
        {
            builder.Length = 0;
            builder.Append(name);
            for (var index = space; index != null; index = index.parent)
            {
                builder.Insert(0, '.');
                builder.Insert(0, index.name);
            }
            return builder.ToString();
        }
        private static readonly System.Text.StringBuilder builder = new System.Text.StringBuilder();
    }
    [System.Serializable]
    internal class ImportDefinitionInfo : ImportInfo
    {
        [System.Serializable]
        internal struct Variable
        {
            public readonly string name;
            public readonly Type type;
            public Variable(string name, Type type)
            {
                this.name = name;
                this.type = type;
            }
        }
        internal readonly Variable[] variables;
        internal readonly ImportMethodInfo[] methods;//方法名为类型名时表示这是个构造函数
        internal ImportDefinitionInfo(ImportSpaceInfo space, string name, Variable[] variables, ImportMethodInfo[] methods) : base(space, name)
        {
            this.variables = variables;
            this.methods = methods;
        }
    }
    [System.Serializable]
    internal class ImportVariableInfo : ImportInfo
    {
        internal readonly Type type;
        public ImportVariableInfo(ImportSpaceInfo space, string name, Type type) : base(space, name)
        {
            this.type = type;
        }
    }
    [System.Serializable]
    internal class ImportDelegateInfo : ImportInfo
    {
        internal readonly Type[] parameters;
        internal readonly Type[] returns;
        public ImportDelegateInfo(ImportSpaceInfo space, string name, Type[] parameters, Type[] returns) : base(space, name)
        {
            this.parameters = parameters;
            this.returns = returns;
        }
    }
    [System.Serializable]
    internal class ImportCoroutineInfo : ImportInfo
    {
        internal readonly Type[] returns;
        public ImportCoroutineInfo(ImportSpaceInfo space, string name, Type[] returns) : base(space, name)
        {
            this.returns = returns;
        }
    }
    [System.Serializable]
    internal class ImportMethodInfo : ImportInfo
    {
        internal readonly FunctionInfo[] functions;
        internal ImportMethodInfo(ImportSpaceInfo space, string name, FunctionInfo[] functions) : base(space, name)
        {
            this.functions = functions;
        }
    }
    [System.Serializable]
    internal class ImportInterfaceInfo : ImportInfo
    {
        internal readonly ImportMethodInfo[] methods;

        internal ImportInterfaceInfo(ImportSpaceInfo space, string name, ImportMethodInfo[] methods) : base(space, name)
        {
            this.methods = methods;
        }
    }
    [System.Serializable]
    internal class ImportSpaceInfo
    {
        internal readonly ImportSpaceInfo parent;
        internal readonly string name;
        internal ImportSpaceInfo(ImportSpaceInfo parent, string name)
        {
            this.parent = parent;
            this.name = name;
        }
    }
    [System.Serializable]
    internal class ImportLibraryInfo : ImportSpaceInfo
    {
        internal readonly ImportDefinitionInfo[] definitions;
        internal readonly ImportVariableInfo[] variables;
        internal readonly ImportDelegateInfo[] delegates;
        internal readonly ImportCoroutineInfo[] coroutines;
        internal readonly ImportMethodInfo[] methods;
        internal readonly ImportInterfaceInfo[] interfaces;
        internal readonly ImportMethodInfo[] natives;

        internal ImportLibraryInfo(string name, ImportDefinitionInfo[] definitions, ImportVariableInfo[] variables, ImportDelegateInfo[] delegates, ImportCoroutineInfo[] coroutines, ImportMethodInfo[] methods, ImportInterfaceInfo[] interfaces, ImportMethodInfo[] natives) : base(null, name)
        {
            this.definitions = definitions;
            this.variables = variables;
            this.delegates = delegates;
            this.coroutines = coroutines;
            this.methods = methods;
            this.interfaces = interfaces;
            this.natives = natives;
        }
    }
    #endregion Imports
    #region Exports
    [System.Serializable]
    internal struct ExportDefinition
    {
        internal readonly string name;
        internal readonly uint index;
        internal readonly ExportIndex[] variables;
        internal readonly ExportMethod[] methods;//(成员/构造)函数在类型中的索引，不是全局的
        internal ExportDefinition(string name, uint index, ExportIndex[] variables, ExportMethod[] methods)
        {
            this.name = name;
            this.index = index;
            this.variables = variables;
            this.methods = methods;
        }
    }
    [System.Serializable]
    internal struct ExportIndex
    {
        internal readonly string name;
        internal readonly uint index;
        internal ExportIndex(string name, uint index)
        {
            this.name = name;
            this.index = index;
        }
    }
    [System.Serializable]
    internal struct ExportMethod
    {
        internal readonly string name;
        internal readonly uint method;
        internal readonly uint[] functions;
        internal ExportMethod(string name, uint method, uint[] functions)
        {
            this.name = name;
            this.method = method;
            this.functions = functions;
        }
    }
    [System.Serializable]
    internal struct ExportInterface
    {
        internal readonly string name;
        internal readonly uint index;
        internal readonly ExportMethod[] methods;
        public ExportInterface(string name, uint index, ExportMethod[] methods)
        {
            this.name = name;
            this.index = index;
            this.methods = methods;
        }
    }
    /// <summary>
    /// 命名空间
    /// </summary>
    [System.Serializable]
    public class Space
    {
        /// <summary>
        /// 名称
        /// </summary>
        public readonly string name;
        internal readonly Space[] children;
        internal readonly ExportDefinition[] exportDefinitions;
        internal readonly ExportIndex[] exportVariables;
        internal readonly ExportIndex[] exportDelegates;
        internal readonly ExportIndex[] exportCoroutines;
        internal readonly ExportMethod[] exportMethods;
        internal readonly ExportInterface[] exportInterfaces;
        internal readonly ExportMethod[] exportNatives;
        internal Space(string name, Space[] children, ExportDefinition[] exportDefinitions, ExportIndex[] exportVariables, ExportIndex[] exportDelegates, ExportIndex[] exportCoroutines, ExportMethod[] exportMethods, ExportInterface[] exportInterfaces, ExportMethod[] exportNatives)
        {
            this.name = name;
            this.children = children;
            this.exportDefinitions = exportDefinitions;
            this.exportVariables = exportVariables;
            this.exportDelegates = exportDelegates;
            this.exportCoroutines = exportCoroutines;
            this.exportMethods = exportMethods;
            this.exportInterfaces = exportInterfaces;
            this.exportNatives = exportNatives;
        }
    }
    #endregion Exports
    /// <summary>
    /// 库
    /// </summary>
    [System.Serializable]
    public partial class Library : Space
    {
        internal readonly byte[] code, constantData;
        internal readonly uint dataSize;
        internal readonly DefinitionInfo[] definitions;
        internal readonly VariableInfo[] variables;
        internal readonly FunctionInfo[] delegates;
        internal readonly CoroutineInfo[] coroutines;
        internal readonly MethodInfo[] methods;
        internal readonly InterfaceInfo[] interfaces;
        internal readonly NativeMethodInfo[] natives;
        internal readonly ImportLibraryInfo[] imports;
        internal readonly string[] strings;
        internal readonly IDictionary<string, uint[]> dataStrings;
        internal Library(string name, byte[] code, byte[] constantData, uint dataSize, DefinitionInfo[] definitions, VariableInfo[] variables, FunctionInfo[] delegates, CoroutineInfo[] coroutines, MethodInfo[] methods, InterfaceInfo[] interfaces, NativeMethodInfo[] natives, ImportLibraryInfo[] imports, string[] strings, IDictionary<string, uint[]> dataStrings,
            Space[] children, ExportDefinition[] exportDefinitions, ExportIndex[] exportVariables, ExportIndex[] exportDelegates, ExportIndex[] exportCoroutines, ExportMethod[] exportMethods, ExportInterface[] exportInterfaces, ExportMethod[] exportNatives) : base(name, children, exportDefinitions, exportVariables, exportDelegates, exportCoroutines, exportMethods, exportInterfaces, exportNatives)
        {
            this.code = code;
            this.constantData = constantData;
            this.dataSize = dataSize;
            this.definitions = definitions;
            this.variables = variables;
            this.delegates = delegates;
            this.coroutines = coroutines;
            this.methods = methods;
            this.interfaces = interfaces;
            this.natives = natives;
            this.imports = imports;
            this.strings = strings;
            this.dataStrings = dataStrings;
        }

        internal bool TryGetDefinition(ImportInfo import, out ExportDefinition result)
        {
            if (TryGetSpace(import.space, out var space))
                foreach (var item in space.exportDefinitions)
                    if (item.name == import.name)
                    {
                        result = item;
                        return true;
                    }
            result = default;
            return false;
        }
        internal bool TryGetVariable(ImportInfo import, out uint result)
        {
            if (TryGetSpace(import.space, out var space))
                foreach (var item in space.exportVariables)
                    if (item.name == import.name)
                    {
                        result = item.index;
                        return true;
                    }
            result = default;
            return false;
        }
        internal bool TryGetDelegate(ImportInfo import, out uint result)
        {
            if (TryGetSpace(import.space, out var space))
                foreach (var item in space.exportDelegates)
                    if (item.name == import.name)
                    {
                        result = item.index;
                        return true;
                    }
            result = default;
            return false;
        }
        internal bool TryGetCoroutine(ImportInfo import, out uint result)
        {
            if (TryGetSpace(import.space, out var space))
                foreach (var item in space.exportCoroutines)
                    if (item.name == import.name)
                    {
                        result = item.index;
                        return true;
                    }
            result = default;
            return false;
        }
        internal bool TryGetMethod(ImportInfo import, out ExportMethod result)
        {
            if (TryGetSpace(import.space, out var space))
                foreach (var item in space.exportMethods)
                    if (item.name == import.name)
                    {
                        result = item;
                        return true;
                    }
            result = default;
            return false;
        }
        internal bool TryGetInterface(ImportInfo import, out ExportInterface result)
        {
            if (TryGetSpace(import.space, out var space))
                foreach (var item in space.exportInterfaces)
                    if (item.name == import.name)
                    {
                        result = item;
                        return true;
                    }
            result = default;
            return false;
        }
        internal bool TryGetNative(ImportInfo import, out ExportMethod result)
        {
            if (TryGetSpace(import.space, out var space))
                foreach (var item in space.exportNatives)
                    if (item.name == import.name)
                    {
                        result = item;
                        return true;
                    }
            result = default;
            return false;
        }
        private bool TryGetSpace(ImportSpaceInfo import, out Space space)
        {
            if (import.parent != null)
            {
                if (TryGetSpace(import.parent, out space))
                    foreach (var item in space.children)
                        if (item.name == import.name)
                        {
                            space = item;
                            return true;
                        }
            }
            else if (import.name == name)
            {
                space = this;
                return true;
            }
            space = default;
            return false;
        }
    }
}
