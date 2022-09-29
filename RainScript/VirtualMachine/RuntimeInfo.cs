using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RainScript.VirtualMachine
{
    internal class RuntimeRelocationInfo
    {
        public readonly uint characteristics;
        public readonly DefinitionFunction function;

        public RuntimeRelocationInfo(uint characteristics, DefinitionFunction function)
        {
            this.characteristics = characteristics;
            this.function = function;
        }
    }
    internal class RuntimeDefinitionInfo
    {
        public struct Variable
        {
            public readonly Type type;
            public readonly uint offset;
            public Variable(Type type, uint offset)
            {
                this.type = type;
                this.offset = offset;
            }
        }
        public class Method
        {
            public readonly uint method;
            public readonly uint[] characteristic;
            public Method(uint method, int functionCount)
            {
                this.method = method;
                characteristic = new uint[functionCount];
            }
        }
        public readonly TypeDefinition parent;
        public readonly TypeDefinition[] inherits;
        public readonly TypeDefinition definition;
        public uint baseOffset;
        public readonly uint size;
        public readonly Variable[] variables;
        public readonly Method[] methods;
        public readonly RuntimeRelocationInfo[] relocations;
        public readonly FunctionHandle destructor;
        public RuntimeDefinitionInfo(RuntimeLibraryInfo library, Library sourceLibrary, TypeDefinition definition, DefinitionInfo info)
        {
            parent = library.LocalToGlobal(info.parent);
            inherits = new TypeDefinition[info.inherits.Length];
            for (int i = 0; i < inherits.Length; i++) inherits[i] = library.LocalToGlobal(info.inherits[i]);
            this.definition = definition;
            variables = new Variable[info.varibales.Length];
            size = 0;
            for (int i = 0; i < variables.Length; i++)
            {
                variables[i] = new Variable(library.LocalToGlobal(info.varibales[i]), size);
                size += info.varibales[i].FieldSize;
            }
            methods = new Method[info.methods.Length];
            for (int i = 0; i < methods.Length; i++) methods[i] = new Method(info.methods[i], sourceLibrary.methods[info.methods[i]].functions.Length);
            relocations = new RuntimeRelocationInfo[info.relocations.Length];
            destructor = info.destructor == LIBRARY.ENTRY_INVALID ? null : FunctionHandle.CreateMemberFunctionHandle(library, info.destructor, definition, FunctionInfo.EMPTY);
        }
        public void CalculateBaseOffset(LibraryAgency agency)
        {
            if (parent == TypeDefinition.INVALID) baseOffset = 0;
            else
            {
                var definition = agency[parent.library].definitions[parent.index];
                baseOffset = definition.baseOffset + definition.size;
            }
        }
        public void RelocationMethods(RuntimeLibraryInfo library, DefinitionInfo definition)
        {
            var agency = library.kernel.libraryAgency;
            for (int i = 0; i < relocations.Length; i++)
            {
                var relocation = definition.relocations[i];
                var characteristic = agency.GetFunctionCharacteristic(library.LocalToGlobal(relocation.overrideFunction));
                var realizeFunction = library.LocalToGlobal(relocation.realizeFunction);
                relocations[i] = new RuntimeRelocationInfo(characteristic, realizeFunction);
                if (realizeFunction.definition == this.definition) methods[realizeFunction.funtion.method].characteristic[realizeFunction.funtion.index] = characteristic;
            }
            foreach (var method in methods)
                for (int i = 0; i < method.characteristic.Length; i++)
                    if (method.characteristic[i] == 0)
                        method.characteristic[i] = agency.CreateFunctionCharacteristic();
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    internal struct RuntimeDelegateInfo
    {
        [FieldOffset(0)]
        public readonly uint library;
        [FieldOffset(4)]
        public readonly Function function;
        [FieldOffset(12)]
        public readonly uint target;
        [FieldOffset(16)]
        public readonly FunctionType type;
        public RuntimeDelegateInfo(uint library, Function function, uint target, FunctionType type)
        {
            this.library = library;
            this.function = function;
            this.target = target;
            this.type = type;
        }
        public RuntimeDelegateInfo(LibraryAgency agency, DefinitionFunction function, uint target, FunctionType type)
        {
            var library = agency[function.definition.library];
            this.library = library.index;
            this.function = new Function(library.definitions[function.definition.index].methods[function.funtion.method].method, function.funtion.index);
            this.target = target;
            this.type = type;
        }
        public override bool Equals(object obj)
        {
            return obj is RuntimeDelegateInfo @delegate &&
                   library == @delegate.library &&
                   function == @delegate.function &&
                   target == @delegate.target &&
                   type == @delegate.type;
        }
        public override int GetHashCode()
        {
            int hashCode = -1092528541;
            hashCode = hashCode * -1521134295 + library.GetHashCode();
            hashCode = hashCode * -1521134295 + function.GetHashCode();
            hashCode = hashCode * -1521134295 + target.GetHashCode();
            hashCode = hashCode * -1521134295 + type.GetHashCode();
            return hashCode;
        }
        public static bool operator ==(RuntimeDelegateInfo left, RuntimeDelegateInfo right)
        {
            return left.library == right.library && left.function == right.function && left.target == right.target && left.type == right.type;
        }
        public static bool operator !=(RuntimeDelegateInfo left, RuntimeDelegateInfo right)
        {
            return !(left == right);
        }
    }
    internal class RuntimeMethodInfo
    {
        internal class Function
        {
            public readonly uint entry;
            private FunctionHandle handle;
            public Function(uint entry)
            {
                this.entry = entry;
                handle = null;
            }
            public FunctionHandle GetHandle(RuntimeLibraryInfo library, FunctionInfo info)
            {
                if (handle == null) handle = new FunctionHandle(library, entry, info);
                return handle;
            }
            public FunctionHandle GetHandle(RuntimeLibraryInfo library, TypeDefinition definition, FunctionInfo info)
            {
                if (handle == null) handle = FunctionHandle.CreateMemberFunctionHandle(library, entry, definition, info);
                return handle;
            }
        }
        public readonly Function[] functions;
        public readonly FunctionInfo[] infos;
        public RuntimeMethodInfo(RuntimeLibraryInfo library, MethodInfo method)
        {
            functions = new Function[method.functions.Length];
            for (int i = 0; i < functions.Length; i++) functions[i] = new Function(method.entries[i]);
            infos = new FunctionInfo[method.functions.Length];
            for (int i = 0; i < infos.Length; i++) infos[i] = new FunctionInfo(library.LocalToGlobal(method.functions[i].parameters), library.LocalToGlobal(method.functions[i].returns));
        }
    }
    internal class RuntimeInterfaceInfo
    {
        internal class Method
        {
            public readonly uint[] characteristics;
            public readonly FunctionInfo[] functions;
            public Method(RuntimeLibraryInfo library, FunctionInfo[] functions)
            {
                characteristics = new uint[functions.Length];
                this.functions = new FunctionInfo[functions.Length];
                for (int i = 0; i < functions.Length; i++)
                    this.functions[i] = new FunctionInfo(library.LocalToGlobal(functions[i].parameters), library.LocalToGlobal(functions[i].returns));
            }
        }
        public readonly TypeDefinition definition;
        public readonly TypeDefinition[] inherits;
        public readonly Method[] methods;
        public readonly RuntimeRelocationInfo[] relocations;
        public RuntimeInterfaceInfo(RuntimeLibraryInfo library, TypeDefinition definition, InterfaceInfo info)
        {
            this.definition = definition;
            inherits = new TypeDefinition[info.inherits.Length];
            for (int i = 0; i < inherits.Length; i++) inherits[i] = library.LocalToGlobal(info.inherits[i]);
            methods = new Method[info.methods.Length];
            for (uint i = 0; i < methods.Length; i++) methods[i] = new Method(library, info.methods[i].functions);
            relocations = new RuntimeRelocationInfo[info.relocations.Length];
        }
        public void RelocationMethods(RuntimeLibraryInfo library, InterfaceInfo info)
        {
            var agency = library.kernel.libraryAgency;
            for (int i = 0; i < relocations.Length; i++)
            {
                var relocation = info.relocations[i];
                var characteristic = agency.GetFunctionCharacteristic(library.LocalToGlobal(relocation.overrideFunction));
                var realizeFunction = library.LocalToGlobal(relocation.realizeFunction);
                relocations[i] = new RuntimeRelocationInfo(characteristic, realizeFunction);
                if (realizeFunction.definition == definition) methods[realizeFunction.funtion.method].characteristics[realizeFunction.funtion.index] = characteristic;
            }
            foreach (var method in methods)
                for (int i = 0; i < method.characteristics.Length; i++)
                    if (method.characteristics[i] == 0)
                        method.characteristics[i] = agency.CreateFunctionCharacteristic();
        }
    }
    internal struct RuntimeNativeInfo
    {
        public readonly string name;
        public readonly FunctionInfo[] infos;
        [NonSerialized]
        public readonly NativeInvoker[] invokers;
        public RuntimeNativeInfo(RuntimeLibraryInfo library, NativeMethodInfo native)
        {
            name = native.name;
            invokers = new NativeInvoker[native.functions.Length];
            infos = new FunctionInfo[native.functions.Length];
            for (int i = 0; i < infos.Length; i++) infos[i] = new FunctionInfo(library.LocalToGlobal(native.functions[i].parameters), library.LocalToGlobal(native.functions[i].returns));
        }
    }
    internal unsafe class RuntimeLibraryInfo : IDisposable
    {
        private struct ImportDefinition
        {
            public readonly uint index;
            public readonly uint[] memberVariable;
            public readonly ImportMethod[] methods;//这个里面存的是成员方法和成员函数编号，不是全局的
            public ImportDefinition(uint index, uint[] memberVaribales, ImportMethod[] methods)
            {
                this.index = index;
                this.memberVariable = memberVaribales;
                this.methods = methods;
            }
        }
        private struct ImportMethod
        {
            public readonly uint method;
            public readonly uint[] functions;
            public ImportMethod(uint method, uint[] functions)
            {
                this.method = method;
                this.functions = functions;
            }
        }
        private struct ImportInterface
        {
            public readonly uint index;
            public readonly ImportMethod[] methods;
            public ImportInterface(uint index, ImportMethod[] methods)
            {
                this.index = index;
                this.methods = methods;
            }
        }
        private class ImportLibrary
        {
            public readonly RuntimeLibraryInfo library;
            public readonly ImportDefinition[] definitions;
            public readonly uint[] variables;
            public readonly uint[] delegates;
            public readonly uint[] coroutines;
            public readonly ImportMethod[] methods;
            public readonly ImportInterface[] interfaces;
            public readonly ImportMethod[] natives;

            public ImportLibrary(RuntimeLibraryInfo library, ImportDefinition[] definitions, uint[] variables, uint[] delegates, uint[] coroutines, ImportMethod[] methods, ImportInterface[] interfaces, ImportMethod[] natives)
            {
                this.library = library;
                this.definitions = definitions;
                this.variables = variables;
                this.delegates = delegates;
                this.coroutines = coroutines;
                this.methods = methods;
                this.interfaces = interfaces;
                this.natives = natives;
            }
        }
        public readonly Kernel kernel;
        private readonly Library library;
        public readonly uint index;
        public readonly string name;
        public readonly byte* code, data;
        private readonly ImportLibrary[] imports;
        public readonly uint[] strings;
        public readonly RuntimeDefinitionInfo[] definitions;
        public readonly uint[] variables;
        public readonly FunctionInfo[] delegates;
        public readonly CoroutineInfo[] coroutines;
        public readonly RuntimeMethodInfo[] methods;
        public readonly RuntimeInterfaceInfo[] interfaces;
        private readonly RuntimeNativeInfo[] natives;


        private readonly IDictionary<Function, FunctionHandle> methodHandles = new Dictionary<Function, FunctionHandle>();
        [NonSerialized]
        private IPerformer performer;
        public RuntimeLibraryInfo(Kernel kernel, uint index, Library library)
        {
            this.kernel = kernel;
            this.library = library;
            this.index = index;
            name = library.name;
            code = Tools.A2P(library.code);
            data = Tools.MAlloc((int)library.dataSize);
            for (int i = library.constantData.Length; i < library.dataSize; i++) data[i] = 0;
            fixed (byte* constantData = library.constantData)
                Tools.Copy(constantData, data, (uint)library.constantData.Length);
            imports = new ImportLibrary[library.imports.Length];
            strings = InitStrings();
            definitions = new RuntimeDefinitionInfo[library.definitions.Length];
            for (var i = 0u; i < definitions.Length; i++) definitions[i] = new RuntimeDefinitionInfo(this, library, new TypeDefinition(index, TypeCode.Handle, i), library.definitions[i]);
            variables = new uint[library.variables.Length];
            for (int i = 0; i < library.variables.Length; i++) variables[i] = library.variables[i].address;
            delegates = new FunctionInfo[library.delegates.Length];
            for (int i = 0; i < delegates.Length; i++) delegates[i] = new FunctionInfo(LocalToGlobal(library.delegates[i].parameters), LocalToGlobal(library.delegates[i].returns));
            coroutines = new CoroutineInfo[library.coroutines.Length];
            for (int i = 0; i < coroutines.Length; i++) coroutines[i] = new CoroutineInfo(LocalToGlobal(library.coroutines[i].returns));
            methods = new RuntimeMethodInfo[library.methods.Length];
            for (int i = 0; i < methods.Length; i++) methods[i] = new RuntimeMethodInfo(this, library.methods[i]);
            interfaces = new RuntimeInterfaceInfo[library.interfaces.Length];
            for (uint i = 0; i < interfaces.Length; i++) interfaces[i] = new RuntimeInterfaceInfo(this, new TypeDefinition(index, TypeCode.Interface, i), library.interfaces[i]);
            natives = new RuntimeNativeInfo[library.natives.Length];
            for (int i = 0; i < natives.Length; i++) natives[i] = new RuntimeNativeInfo(this, library.natives[i]);
        }
        public void InitRuntimeData()
        {
            InterfaceFunctionRelocation();
            CalculateDefinitionRuntimeData();
        }
        private uint[] InitStrings()
        {
            foreach (var dataString in library.dataStrings)
            {
                var stringHandle = kernel.stringAgency.Add(dataString.Key);
                foreach (var point in dataString.Value)
                {
                    *(uint*)(data + point) = stringHandle;
                    kernel.stringAgency.Reference(stringHandle);
                }
            }
            var strings = new uint[library.strings.Length];
            for (int i = 0; i < strings.Length; i++)
            {
                strings[i] = kernel.stringAgency.Add(library.strings[i]);
                kernel.stringAgency.Reference(strings[i]);
            }
            return strings;
        }
        private void InterfaceFunctionRelocation()
        {
            var list = new List<uint>();
            for (uint i = 0; i < interfaces.Length; i++) list.Add(i);
            while (list.Count > 0)
            {
                var count = list.Count;
                for (int i = 0; i < list.Count; i++)
                {
                    var definition = interfaces[list[i]];
                    foreach (var item in definition.inherits)
                        if (item.library == index && BinarySearch(list, item.index)) goto next;
                    definition.RelocationMethods(this, library.interfaces[list[i]]);
                    list.RemoveAt(i--);
                next:;
                }
                if (count == list.Count) throw ExceptionGeneratorVM.CyclicInheritance(library.name);
            }
        }
        private void CalculateDefinitionRuntimeData()
        {
            var list = new List<uint>();
            for (uint i = 0; i < definitions.Length; i++) list.Add(i);
            while (list.Count > 0)
            {
                var count = list.Count;
                for (int i = 0; i < list.Count; i++)
                {
                    var definition = definitions[list[i]];
                    if (definition.parent.library != index || !BinarySearch(list, definition.parent.index))
                    {
                        definition.CalculateBaseOffset(kernel.libraryAgency);
                        definition.RelocationMethods(this, library.definitions[list[i]]);
                        list.RemoveAt(i--);
                    }
                }
                if (count == list.Count) throw ExceptionGeneratorVM.CyclicInheritance(library.name);
            }
        }
        public Type LocalToGlobal(Type type)
        {
            return new Type(LocalToGlobal(type.definition), type.dimension);
        }
        public Type[] LocalToGlobal(Type[] types)
        {
            var result = new Type[types.Length];
            for (int i = 0; i < result.Length; i++) result[i] = LocalToGlobal(types[i]);
            return result;
        }
        public TypeDefinition LocalToGlobal(TypeDefinition definition)
        {
            if (definition == TypeDefinition.INVALID) return definition;
            else if (definition.library == LIBRARY.KERNEL) return definition;
            else if (definition.library == LIBRARY.SELF) return new TypeDefinition(index, definition.code, definition.index);
            var import = GetImportLibrary(definition.library);
            if (definition.code == TypeCode.Handle) return new TypeDefinition(import.library.index, TypeCode.Handle, import.definitions[definition.index].index);
            else if (definition.code == TypeCode.Interface) return new TypeDefinition(import.library.index, TypeCode.Interface, import.interfaces[definition.index].index);
            else if (definition.code == TypeCode.Function) return new TypeDefinition(import.library.index, TypeCode.Function, import.delegates[definition.index]);
            else if (definition.code == TypeCode.Coroutine) return new TypeDefinition(import.library.index, TypeCode.Coroutine, import.coroutines[definition.index]);
            throw ExceptionGeneratorVM.MissingDefinition(name, import.library.name, definition.code);
        }
        public uint LocalToGlobal(uint library)
        {
            if (library == LIBRARY.KERNEL) return library;
            else if (library == LIBRARY.SELF) return index;
            else return GetImportLibrary(library).library.index;
        }
        public void LocalToGlobal(uint library, Function function, out uint globalLibrary, out Function globalFunction)
        {
            if (library == LIBRARY.KERNEL)
            {
                globalLibrary = library;
                globalFunction = function;
            }
            else if (library == LIBRARY.SELF)
            {
                globalLibrary = index;
                globalFunction = function;
            }
            else
            {
                var import = GetImportLibrary(library);
                globalLibrary = import.library.index;
                var method = import.methods[function.method];
                globalFunction = new Function(method.method, method.functions[function.index]);
            }
        }
        public void LocalToGlobal(uint library, uint variable, out uint globalLibrary, out uint globalVaribale)
        {
            if (library == LIBRARY.KERNEL)
            {
                globalLibrary = library;
                globalVaribale = variable;
            }
            else if (library == LIBRARY.SELF)
            {
                globalLibrary = index;
                globalVaribale = variable;
            }
            else
            {
                var import = GetImportLibrary(library);
                globalLibrary = import.library.index;
                globalVaribale = import.variables[variable];
            }
        }
        public void LocalToGlobal(uint library, MemberVariable variable, out uint globalLibrary, out MemberVariable globalMemberVaribale)
        {
            if (library == LIBRARY.KERNEL)
            {
                globalLibrary = library;
                globalMemberVaribale = variable;
            }
            else if (library == LIBRARY.SELF)
            {
                globalLibrary = index;
                globalMemberVaribale = variable;
            }
            else
            {
                var import = GetImportLibrary(library);
                globalLibrary = import.library.index;
                globalMemberVaribale = new MemberVariable(import.definitions[variable.definition].index, import.definitions[variable.definition].memberVariable[variable.index]);
            }
        }
        public DefinitionFunction LocalToGlobal(DefinitionFunction function)
        {
            if (function.definition.library == LIBRARY.KERNEL) return function;
            else if (function.definition.library == LIBRARY.SELF) return new DefinitionFunction(new TypeDefinition(index, function.definition.code, function.definition.index), function.funtion);
            else
            {
                var definition = LocalToGlobal(function.definition);
                var import = GetImportLibrary(function.definition.library);
                if (function.definition.code == TypeCode.Interface)
                {
                    var method = import.interfaces[function.definition.index].methods[function.funtion.method];
                    return new DefinitionFunction(definition, new Function(method.method, method.functions[function.funtion.index]));
                }
                else
                {
                    var method = import.definitions[function.definition.index].methods[function.funtion.method];
                    return new DefinitionFunction(definition, new Function(method.method, method.functions[function.funtion.index]));
                }
            }
        }

        public uint GetFunctionEntry(Function function)
        {
            return methods[function.method].functions[function.index].entry;
        }
        public FunctionHandle GetFunctionHandle(Function function)
        {
            var method = methods[function.method];
            return method.functions[function.index].GetHandle(this, method.infos[function.index]);
        }
        public FunctionHandle GetFunctionHandle(TypeDefinition definition, Function function)
        {
            var method = methods[function.method];
            return method.functions[function.index].GetHandle(this, definition, method.infos[function.index]);
        }
        private bool TryGetExportMethod(Space space, string name, out ExportMethod method, bool includeSubSpace)
        {
            foreach (var item in space.exportMethods)
                if (item.name == name)
                {
                    method = item;
                    return true;
                }
            if (includeSubSpace)
                foreach (var item in space.children)
                    if (TryGetExportMethod(item, name, out method, true))
                        return true;
            method = default;
            return false;
        }
        public bool TryGetExportMethod(string methodName, out ExportMethod method)
        {
            if (methodName.IndexOf('.') >= 0)
            {
                var names = methodName.Split('.');
                var index = 0;
                var space = (Space)library;
                if (names[index++] == name)
                {
                    while (index < name.Length - 1)
                    {
                        foreach (var child in space.children)
                            if (child.name == names[index])
                            {
                                space = child;
                                goto next;
                            }
                        goto exit;
                    next: index++;
                    }
                    return TryGetExportMethod(library, methodName, out method, false);
                }
            exit:
                method = default;
                return false;
            }
            else return TryGetExportMethod(library, methodName, out method, true);
        }

        private bool IsEquals(Type[] raw, Type[] targets)
        {
            if (raw.Length != targets.Length) return false;
            for (int i = 0; i < raw.Length; i++)
                if (LocalToGlobal(raw[i]) != targets[i]) return false;
            return true;
        }
        private bool TryGetFunctions(FunctionInfo[] importFunctions, FunctionInfo[] functions, ExportMethod export, out uint[] results)
        {
            results = new uint[importFunctions.Length];
            for (int x = 0; x < results.Length; x++)
            {
                var flag = false;
                for (uint y = 0; y < export.functions.Length; y++)
                    if (IsEquals(importFunctions[x].parameters, functions[export.functions[y]].parameters))
                    {
                        if (IsEquals(importFunctions[x].returns, functions[export.functions[y]].returns))
                        {
                            results[x] = export.functions[y];
                            flag = true;
                        }
                        else return false;
                        break;
                    }
                if (!flag) return false;
            }
            return true;
        }
        private ImportLibrary GetImportLibrary(uint libraryIndex)
        {
            if (imports[libraryIndex] == null)
            {
                var import = this.library.imports[(int)libraryIndex];
                var library = kernel.libraryAgency.Load(import.name);
                var definitions = new ImportDefinition[import.definitions.Length];
                var variables = new uint[import.variables.Length];
                var delegates = new uint[import.delegates.Length];
                var coroutines = new uint[import.coroutines.Length];
                var methods = new ImportMethod[import.methods.Length];
                var interfaces = new ImportInterface[import.interfaces.Length];
                var natives = new ImportMethod[import.natives.Length];

                imports[libraryIndex] = new ImportLibrary(library, definitions, variables, delegates, coroutines, methods, interfaces, natives);
                //初始化定义声明
                for (int i = 0; i < import.definitions.Length; i++)
                {
                    var importDefinition = import.definitions[i];
                    if (!library.library.TryGetDefinition(importDefinition, out var exportDefinition)) throw ExceptionGeneratorVM.MissingDefinition(name, library.name, importDefinition.ToString());
                    definitions[i] = new ImportDefinition(exportDefinition.index, new uint[importDefinition.variables.Length], new ImportMethod[importDefinition.methods.Length]);
                }

                for (int i = 0; i < delegates.Length; i++)
                    if (!library.library.TryGetDelegate(import.delegates[i], out delegates[i]))
                        throw ExceptionGeneratorVM.MissingDefinition(name, library.name, import.delegates[i].ToString());

                for (int i = 0; i < coroutines.Length; i++)
                    if (!library.library.TryGetCoroutine(import.coroutines[i], out coroutines[i]))
                        throw ExceptionGeneratorVM.MissingDefinition(name, library.name, import.coroutines[i].ToString());

                for (int i = 0; i < interfaces.Length; i++)
                {
                    var importInterface = import.interfaces[i];
                    if (!library.library.TryGetInterface(importInterface, out var exportInterface)) throw ExceptionGeneratorVM.MissingDefinition(name, library.name, importInterface.ToString());
                    interfaces[i] = new ImportInterface(exportInterface.index, new ImportMethod[importInterface.methods.Length]);
                }
                //连接
                for (int i = 0; i < import.definitions.Length; i++)
                {
                    var importDefinition = import.definitions[i];
                    if (!library.library.TryGetDefinition(importDefinition, out var exportDefinition)) throw ExceptionGeneratorVM.MissingDefinition(name, library.name, importDefinition.ToString());

                    var memberVaribales = definitions[i].memberVariable;
                    for (uint index = 0; index < memberVaribales.Length; index++)
                    {
                        var flag = false;
                        foreach (var exportVariable in exportDefinition.variables)
                            if (importDefinition.variables[index].name == exportVariable.name)
                            {
                                var variable = library.definitions[exportDefinition.index].variables[exportVariable.index];
                                if (LocalToGlobal(importDefinition.variables[index].type) == variable.type)
                                {
                                    memberVaribales[index] = index;
                                    flag = true;
                                }
                                break;
                            }
                        if (!flag) throw ExceptionGeneratorVM.MissingDefinition(name, library.name, "{0}.{1}".Format(import.definitions[i].ToString(), importDefinition.variables[index].name));
                    }

                    var memberMethods = definitions[i].methods;
                    for (var index = 0; index < memberMethods.Length; index++)
                    {
                        var flag = false;
                        var importMethod = importDefinition.methods[index];
                        foreach (var method in exportDefinition.methods)
                            if (importMethod.name == method.name)
                            {
                                if (TryGetFunctions(importMethod.functions, library.methods[library.definitions[exportDefinition.index].methods[method.method].method].infos, method, out var functions))
                                {
                                    memberMethods[index] = new ImportMethod(method.method, functions);
                                    flag = true;
                                }
                                break;
                            }
                        if (!flag) throw ExceptionGeneratorVM.MissingDefinition(name, library.name, "{0}.{1}".Format(import.definitions[i].ToString(), importDefinition.methods[index].name));
                    }
                }

                for (int i = 0; i < variables.Length; i++)
                    if (!library.library.TryGetVariable(import.variables[i], out variables[i]) ||
                        library.LocalToGlobal(library.library.variables[variables[i]].type) != LocalToGlobal(import.variables[i].type))
                        throw ExceptionGeneratorVM.MissingDefinition(name, library.name, import.variables[i].ToString());

                for (int i = 0; i < delegates.Length; i++)
                    if (!Type.IsEquals(library.delegates[delegates[i]].parameters, LocalToGlobal(import.delegates[i].parameters)) ||
                        !Type.IsEquals(library.delegates[delegates[i]].returns, LocalToGlobal(import.delegates[i].returns)))
                        throw ExceptionGeneratorVM.MissingDefinition(name, library.name, import.delegates[i].ToString());

                for (int i = 0; i < coroutines.Length; i++)
                    if (!Type.IsEquals(library.coroutines[coroutines[i]].returns, LocalToGlobal(import.coroutines[i].returns)))
                        throw ExceptionGeneratorVM.MissingDefinition(name, library.name, import.coroutines[i].ToString());

                for (int i = 0; i < methods.Length; i++)
                    if (library.library.TryGetMethod(import.methods[i], out var method) && TryGetFunctions(import.methods[i].functions, library.methods[method.method].infos, method, out var functions))
                        methods[i] = new ImportMethod(method.method, functions);
                    else throw ExceptionGeneratorVM.MissingDefinition(name, library.name, import.methods[i].ToString());
               
                for (int i = 0; i < interfaces.Length; i++)
                {
                    var importInterface = import.interfaces[i];
                    if (!library.library.TryGetInterface(importInterface, out var exportInterface)) throw ExceptionGeneratorVM.MissingDefinition(name, library.name, importInterface.ToString());
                    var importMethods = interfaces[i].methods;
                    for (int importMethodIndex = 0; importMethodIndex < importMethods.Length; importMethodIndex++)
                    {
                        var importMethod = importInterface.methods[importMethodIndex];
                        var flag = false;
                        for (int exportMethodIndex = 0; exportMethodIndex < exportInterface.methods.Length; exportMethodIndex++)
                        {
                            var exportMethod = exportInterface.methods[exportMethodIndex];
                            if (importMethod.name == exportMethod.name)
                            {
                                var importFunctions = importInterface.methods[importMethodIndex].functions;
                                var interfaceFunctions = library.interfaces[exportInterface.index].methods[exportMethod.method].functions;
                                if (importFunctions.Length == interfaceFunctions.Length && TryGetFunctions(importFunctions, interfaceFunctions, exportMethod, out var functions))
                                {
                                    importMethods[importMethodIndex] = new ImportMethod(exportMethod.method, functions);
                                    flag = true;
                                }
                                break;
                            }
                        }
                        if (!flag) throw ExceptionGeneratorVM.MissingDefinition(name, library.name, importMethod.ToString());
                    }
                }

                for (int i = 0; i < natives.Length; i++)
                    if (library.library.TryGetNative(import.natives[i], out var native) && TryGetFunctions(import.natives[i].functions, library.natives[native.method].infos, native, out var functions))
                        natives[i] = new ImportMethod(native.method, functions);
                    else throw ExceptionGeneratorVM.MissingDefinition(name, library.name, import.natives[i].ToString());
            }
            return imports[libraryIndex];
        }
        public void NativeInvoker(Function native, byte* stack, uint top)
        {
            if (performer == null) performer = kernel.performerLoader(name);
            if (natives[native.method].invokers[native.index] == null) natives[native.method].invokers[native.index] = new NativeInvoker(natives[native.method].name, natives[native.method].infos[native.index], performer);
            natives[native.method].invokers[native.index].invoke?.Invoke(kernel, performer, stack, top);
        }
        public void Dispose()
        {
            Tools.Free(code);
            Tools.Free(data);
        }
        private static bool ContainsDefinition(List<RuntimeDefinitionInfo> list, uint index)
        {
            var min = 0; var max = list.Count;
            while (min < max)
            {
                var middle = (min + max) >> 1;
                var definition = list[middle];
                if (definition.definition.index > index) max = middle;
                else if (definition.definition.index < index) min = middle;
                else return true;
            }
            return false;
        }
        private static bool BinarySearch(List<uint> list, uint value)
        {
            int begin = 0, end = list.Count;
            while (begin < end)
            {
                var index = (begin + end) >> 1;
                if (list[index] > value) end = index;
                else if (list[index] < value) begin = index + 1;
                else return true;
            }
            return false;
        }
    }
}
