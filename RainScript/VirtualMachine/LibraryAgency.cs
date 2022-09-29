using System;
using System.Collections.Generic;

namespace RainScript.VirtualMachine
{
    /// <summary>
    /// 方法句柄
    /// </summary>
    public class FunctionHandle
    {
        internal readonly RuntimeLibraryInfo library;
        internal readonly uint entry;
        internal readonly FunctionInfo function;
        internal readonly uint[] parameterPoints;
        internal readonly uint parameterSize;
        internal readonly uint[] returnPoints;
        internal readonly uint returnSize;
        internal FunctionHandle(RuntimeLibraryInfo library, uint entry, FunctionInfo function)
        {
            this.library = library;
            this.entry = entry;
            this.function = function;
            returnPoints = new uint[function.returns.Length];
            returnSize = 0u;
            for (int i = 0; i < function.returns.Length; i++)
            {
                returnPoints[i] = returnSize;
                returnSize += function.returns[i].FieldSize;
            }
            parameterPoints = new uint[function.parameters.Length];
            parameterSize = 0u;
            for (int i = 0; i < function.parameters.Length; i++)
            {
                parameterPoints[i] = parameterSize;
                parameterSize += function.parameters[i].FieldSize;
            }
        }
        internal static FunctionHandle CreateMemberFunctionHandle(RuntimeLibraryInfo library, uint entry, TypeDefinition definition, FunctionInfo function)
        {
            var parameters = new Type[function.parameters.Length + 1];
            parameters[0] = new Type(definition, 0);
            Array.Copy(function.parameters, 0, parameters, 1, function.parameters.Length);
            function = new FunctionInfo(parameters, function.returns);
            return new FunctionHandle(library, entry, function);
        }
        /// <summary>
        /// 判断是否是个有效的方法句柄
        /// </summary>
        /// <param name="handle"></param>
        public unsafe static implicit operator bool(FunctionHandle handle)
        {
            return handle != null && handle.library != null && handle.function != null && handle.library.code[handle.entry] == (byte)CommandMacro.FUNCTION_Entrance;
        }
    }
    internal class LibraryAgency : IDisposable
    {
        private readonly Kernel kernel;
        private readonly RuntimeLibraryInfo kernelLibrary;
        private readonly Func<string, Library> libraryLoader;
        internal readonly List<RuntimeLibraryInfo> libraries = new List<RuntimeLibraryInfo>();
        private uint functionCharacteristic = 0;
        internal RuntimeLibraryInfo this[uint index]
        {
            get
            {
                if (index == LIBRARY.KERNEL) return kernelLibrary;
                return libraries[(int)index];
            }
        }
        internal LibraryAgency(Kernel kernel, Func<string, Library> libraryLoader)
        {
            this.kernel = kernel;
            kernelLibrary = new RuntimeLibraryInfo(kernel, LIBRARY.KERNEL, Library.kernel);
            this.libraryLoader = libraryLoader;
        }
        internal void Init(Library library)
        {
            kernelLibrary.InitRuntimeData();
            Load(library);
        }
        internal RuntimeLibraryInfo Load(string name)
        {
            foreach (var library in libraries) if (library.name == name) return library;
            var result = libraryLoader(name);
            if (result == null) throw ExceptionGeneratorVM.LibraryLoadFail(name);
            if (result.name != name) throw ExceptionGeneratorVM.LibraryLoadError(name, result.name);
            return Load(result);
        }
        private RuntimeLibraryInfo Load(Library library)
        {
            var result = new RuntimeLibraryInfo(kernel, (uint)libraries.Count, library);
            libraries.Add(result);
            result.InitRuntimeData();
            var invoker = kernel.coroutineAgency.Invoker(new FunctionHandle(result, 0, FunctionInfo.EMPTY));
            invoker.Start(true, true);
            invoker.Recycle();
            return result;
        }
        internal uint CreateFunctionCharacteristic()
        {
            return ++functionCharacteristic;
        }
        internal uint GetFunctionCharacteristic(DefinitionFunction function)
        {
            if (function.definition.code == TypeCode.Interface) return this[function.definition.library].interfaces[function.definition.index].methods[function.funtion.method].characteristics[function.funtion.index];
            else return this[function.definition.library].definitions[function.definition.index].methods[function.funtion.method].characteristic[function.funtion.index];
        }
        internal bool TryGetInheritDepth(TypeDefinition baseDefinition, TypeDefinition subDefinition, out uint depth)
        {
            if (baseDefinition.library == LIBRARY.KERNEL) baseDefinition = new TypeDefinition(LIBRARY.KERNEL, (TypeCode)baseDefinition.index, baseDefinition.index);
            if (baseDefinition == subDefinition)
            {
                depth = 0;
                return true;
            }
            else if (subDefinition.code == TypeCode.Handle)
            {
                if (baseDefinition == KERNEL_TYPE.HANDLE.definition)
                {
                    depth = 1;
                    return true;
                }
                else if (baseDefinition.code == TypeCode.Handle)
                {
                    var index = subDefinition;
                    depth = 1;
                    while (index != TypeDefinition.INVALID)
                    {
                        index = this[index.library].definitions[index.index].parent;
                        if (baseDefinition == index) return true;
                        else depth++;
                    }
                }
                else if (baseDefinition.code == TypeCode.Interface)
                {
                    var index = subDefinition;
                    depth = 1;
                    while (index != TypeDefinition.INVALID)
                    {
                        var definition = this[index.library].definitions[index.index];
                        foreach (var item in definition.inherits)
                            if (TryGetInheritDepth(baseDefinition, item, out var result))
                            {
                                depth += result;
                                return true;
                            }
                        depth++;
                        index = definition.parent;
                    }
                }
            }
            else if (subDefinition.code == TypeCode.Interface)
            {
                if (baseDefinition == KERNEL_TYPE.HANDLE.definition)
                {
                    depth = 2;
                    return true;
                }
                else if (baseDefinition == KERNEL_TYPE.INTERFACE.definition)
                {
                    depth = 1;
                    return true;
                }
                else if (baseDefinition.code == TypeCode.Interface)
                {
                    var flag = false;
                    depth = default;
                    foreach (var parent in this[subDefinition.library].interfaces[subDefinition.index].inherits)
                        if (TryGetInheritDepth(baseDefinition, parent, out var result))
                            if (!flag || depth > result)
                            {
                                flag = true;
                                depth = result;
                            }
                    if (flag)
                    {
                        depth++;
                        return true;
                    }
                }
            }
            else if (subDefinition.code == TypeCode.Function)
            {
                if (baseDefinition == KERNEL_TYPE.HANDLE.definition)
                {
                    depth = 2;
                    return true;
                }
                else if (baseDefinition == KERNEL_TYPE.FUNCTION.definition)
                {
                    depth = 1;
                    return true;
                }
            }
            else if (subDefinition.code == TypeCode.Coroutine)
            {
                if (baseDefinition == KERNEL_TYPE.HANDLE.definition)
                {
                    depth = 2;
                    return true;
                }
                else if (baseDefinition == KERNEL_TYPE.COROUTINE.definition)
                {
                    depth = 1;
                    return true;
                }
            }
            depth = default;
            return false;
        }
        internal bool TryGetInheritDepth(Type baseType, Type subType, out uint depth)
        {
            if (baseType == subType)
            {
                depth = 0;
                return true;
            }
            else if (baseType.dimension == 0)
            {
                if (subType.dimension > 0)
                {
                    if (baseType == KERNEL_TYPE.ARRAY)
                    {
                        depth = 1;
                        return true;
                    }
                    else if (baseType == KERNEL_TYPE.HANDLE)
                    {
                        depth = 2;
                        return true;
                    }
                }
                else return TryGetInheritDepth(baseType.definition, subType.definition, out depth);
            }
            depth = default;
            return false;
        }
        internal bool GetFunction(DefinitionFunction function, Type type, out DefinitionFunction targetFunction)
        {
            if (type.dimension > 0) type = KERNEL_TYPE.ARRAY;
            if (TryGetInheritDepth(new Type(function.definition, 0), type, out var depth))
            {
                if (depth > 0)
                {
                    var characteristic = GetFunctionCharacteristic(function);
                    var definition = this[type.definition.library].definitions[type.definition.index];
                    foreach (var relocation in definition.relocations)
                        if (relocation.characteristics == characteristic)
                        {
                            targetFunction = relocation.function;
                            return true;
                        }
                    if (definition.parent != TypeDefinition.INVALID) return GetFunction(function, new Type(definition.parent, 0), out targetFunction);
                }
                else
                {
                    targetFunction = function;
                    return true;
                }
            }
            throw ExceptionGeneratorVM.EntryNotFound(this[function.definition.library].name, function, type);
        }
        internal FunctionHandle GetFunctionHandle(DefinitionFunction function)
        {
            if (function.definition.code != TypeCode.Handle && function.definition.library != LIBRARY.KERNEL) throw ExceptionGenerator.InvalidTypeCode(function.definition.code);
            var library = this[function.definition.library];
            return library.GetFunctionHandle(function.definition, new Function(library.definitions[function.definition.index].methods[function.funtion.method].method, function.funtion.index));
        }
        internal FunctionHandle GetFunctionHandle(string methodName)
        {
            foreach (var library in libraries)
                if (library.TryGetExportMethod(methodName, out ExportMethod method))
                    return library.GetFunctionHandle(new Function(method.method, method.functions[0]));
            return default;
        }
        internal FunctionHandle GetFunctionHandle(string methodName, string libraryName)
        {
            foreach (var library in libraries)
                if (library.name == libraryName && library.TryGetExportMethod(methodName, out ExportMethod method))
                    return library.GetFunctionHandle(new Function(method.method, method.functions[0]));
            return default;
        }
        internal FunctionHandle[] GetFunctionHandles(string methodName, string libraryName)
        {
            foreach (var library in libraries)
                if (library.name == libraryName && library.TryGetExportMethod(methodName, out ExportMethod method))
                {
                    var handles = new FunctionHandle[method.functions.Length];
                    for (int i = 0; i < handles.Length; i++) handles[i] = library.GetFunctionHandle(new Function(method.method, method.functions[i]));
                    return handles;
                }
            return default;
        }
        public void Dispose()
        {
            foreach (var library in libraries) library.Dispose();
            libraries.Clear();
        }
    }
}
