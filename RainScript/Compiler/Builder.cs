using System;
using System.Collections.Generic;
using RainScript.Compiler.LogicGenerator;

namespace RainScript.Compiler
{
    /// <summary>
    /// 编译状态
    /// </summary>
    public enum CompileState
    {
        /// <summary>
        /// 未开始
        /// </summary>
        Unstart,
        /// <summary>
        /// 正在编译
        /// </summary>
        Compiling,
        /// <summary>
        /// 已失败
        /// </summary>
        Failed,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        /// <summary>
        /// 编译器内部发生异常
        /// </summary>
        Accident,
    }
    /// <summary>
    /// 文件信息
    /// </summary>
    public interface IFileInfo
    {
        /// <summary>
        /// 路径
        /// </summary>
        string Path { get; }
        /// <summary>
        /// 内容
        /// </summary>
        string Context { get; }
    }
    /// <summary>
    /// 编译命令选项
    /// </summary>
    public struct CompilerCommand
    {
        /// <summary>
        /// 生成符号表
        /// </summary>
        internal readonly bool generatorSymbolTable;
        /// <summary>
        /// 忽略Exit功能
        /// </summary>
        public readonly bool ignoreExit;
        /// <summary>
        /// 编译命令选项
        /// </summary>
        /// <param name="ignoreExit"></param>
        public CompilerCommand(bool ignoreExit)
        {
            generatorSymbolTable = false;
            this.ignoreExit = ignoreExit;
        }
    }
    /// <summary>
    /// 编译器
    /// </summary>
    public class Builder
    {
        /// <summary>
        /// 当前编译状态
        /// </summary>
        public CompileState State { get; private set; }
        /// <summary>
        /// 库名
        /// </summary>
        public readonly string name;
        private readonly CollectionPool pool = new CollectionPool();
        /// <summary>
        /// 编译异常
        /// </summary>
        public readonly ExceptionCollector exceptions = new ExceptionCollector();
        private readonly List<TextInfo> textInfos = new List<TextInfo>();
        private readonly RelyLibrary[] relies;
        /// <summary>
        /// 编译完成后的库
        /// </summary>
        public Library Library { get; private set; }
        /// <summary>
        /// 给其他库引用的信息
        /// </summary>
        public ReferenceLibrary ReferenceLibrary { get; private set; }
        /// <summary>
        /// 编译器
        /// </summary>
        /// <param name="name">目标库名</param>
        /// <param name="files">文件集</param>
        /// <param name="references">引用集</param>
        public Builder(string name, IEnumerable<IFileInfo> files, IEnumerable<ReferenceLibrary> references)
        {
            this.name = name;
            foreach (var item in files) textInfos.Add(new TextInfo(item.Path, item.Context));
            relies = InitRelies(references);

            State = CompileState.Unstart;
        }
        /// <summary>
        /// 开始编译
        /// </summary>
        /// <param name="command">编译参数</param>
        public void Compile(CompilerCommand command)
        {
            if (State == CompileState.Compiling) throw ExceptionGeneratorCompiler.InvalidCompilingState(State);
            State = CompileState.Compiling;
            pool.Clear();
            exceptions.Clear();
            try
            {
                using (var manager = new DeclarationManager(name, relies, pool))
                {
                    using (var fileSpaces = pool.GetList<File.Space>())
                    {
                        foreach (var item in textInfos) fileSpaces.Add(new File.Space(manager.library, item, pool, exceptions));
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DeclaractionParseFail();

                        foreach (var item in fileSpaces) item.Tidy(manager, pool, exceptions);
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DeclaractionTidyFail();

                        foreach (var item in fileSpaces) item.Link(manager, pool, exceptions);
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DeclaractionLinkFail();

                        foreach (var item in fileSpaces) item.Dispose();
                    }
                    manager.library.DeclarationValidityCheck(pool, exceptions);
                    if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DeclaractionIllegal();

                    manager.library.InterfaceImplementsCheck(manager, pool, exceptions);
                    if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.InterfaceImplements();

                    using (var referenceGenerator = new ReferenceGenerator.Library(manager, pool, exceptions))
                    {
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.ReferenceGeneratorFail();
                        ReferenceLibrary = referenceGenerator.GeneratorLibrary(pool);
                    }

                    manager.library.CalculatedVariableAddress();

                    using (var relied = new ReliedGenerator(manager, pool))
                    using (var libraryGenerator = new Generator(manager.library.ConstantData, pool))
                    {
                        libraryGenerator.GeneratorLibrary(new GeneratorParameter(command, manager, relied, pool, exceptions), out var code, out var codeStrings, out var dataStrings);
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.LogicGeneratorFail();
                        var library = GeneratorLibrary(manager, manager.library, relied, code, codeStrings, dataStrings);
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.LibraryGeneratorFail();
                        Library = library;
                    }
                }
                State = CompileState.Completed;
            }
            catch (ExpectedException)
            {
                State = CompileState.Failed;
                throw;
            }
            catch (Exception)
            {
                State = CompileState.Accident;
                throw;
            }
        }
        private RelyLibrary[] InitRelies(IEnumerable<ReferenceLibrary> sourceReferences)
        {
            using (var references = pool.GetDictionary<string, ReferenceLibrary>())
            {
                foreach (var reference in sourceReferences)
                {
                    if (references.ContainsKey(reference.name)) exceptions.Add(CompilingExceptionCode.COMPILING_DUPLICATE_LIBRARY_NAMES, reference.name);
                    else references.Add(reference.name, reference);
                }
                if (references.ContainsKey(name)) exceptions.Add(CompilingExceptionCode.COMPILING_DUPLICATE_LIBRARY_NAMES, name);
                if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DuplicateLibraryNames();
                using (var nameSet = pool.GetSet<string>())
                using (var checkedSet = pool.GetSet<ReferenceLibrary>())
                    foreach (var reference in sourceReferences)
                        CheckCircularReference(reference, references, nameSet, checkedSet);
                if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.CircularRely();
            }
            using (var references = pool.GetList<ReferenceLibrary>())
            {
                references.AddRange(sourceReferences);
                using (var completedRelies = pool.GetSet<string>())
                using (var relies = pool.GetList<RelyLibrary>())
                {
                    while (references.Count > 0)
                    {
                        var count = references.Count;
                        for (int i = 0; i < references.Count; i++)
                        {
                            var reference = references[i];
                            foreach (var rely in reference.relies) if (!completedRelies.Contains(rely.name)) goto next;
                            completedRelies.Add(reference.name);
                            references.RemoveAt(i--);
                            relies.Add(new RelyLibrary(reference, relies, pool, exceptions));
                        next:;
                        }
                        if (count == references.Count) throw ExceptionGeneratorCompiler.UnknownRelyError();
                    }
                    if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.RelyInitFail();
                    return relies.ToArray();
                }

            }
        }
        private void CheckCircularReference(ReferenceLibrary library, Dictionary<string, ReferenceLibrary> relies, HashSet<string> nameSet, HashSet<ReferenceLibrary> checkedSet)
        {
            if (nameSet.Add(library.name))
            {
                if (checkedSet.Add(library))
                {
                    foreach (var item in library.relies)
                        if (relies.TryGetValue(item.name, out ReferenceLibrary reference)) CheckCircularReference(reference, relies, nameSet, checkedSet);
                        else exceptions.Add(CompilingExceptionCode.COMPILING_LIBRARY_NOT_FOUND, item.name);
                }
                nameSet.Remove(library.name);
            }
            else
            {
                var nameList = name;
                foreach (var item in nameSet) nameList += " -> " + item;
                exceptions.Add(CompilingExceptionCode.COMPILING_CIRCULAR_RELY, nameList);
            }
        }
        private Library GeneratorLibrary(DeclarationManager manager, Compiling.Library library, ReliedGenerator relied, byte[] code, string[] codeStrings, Dictionary<string, uint[]> dataStrings)
        {
            var definitions = new DefinitionInfo[library.definitions.Count];
            for (int i = 0; i < definitions.Length; i++)
            {
                var definition = library.definitions[i];
                var parent = relied.Convert(manager.GetParent(new CompilingDefinition(definition.declaration))).RuntimeDefinition;
                var inherits = new TypeDefinition[definition.inherits.Count];
                for (int index = 0; index < inherits.Length; index++) inherits[index] = relied.Convert(definition.inherits[index]).RuntimeDefinition;
                var memberVariables = new Type[definition.variables.Length];
                for (int index = 0; index < memberVariables.Length; index++) memberVariables[index] = relied.Convert(definition.variables[index].type).RuntimeType;
                var memberMethods = new uint[definition.methods.Length + 1];
                Array.Copy(definition.methods, memberMethods, definition.methods.Length);
                memberMethods[definition.methods.Length] = definition.constructors;
                var relocations = pool.GetList<Relocation>();
                foreach (var method in definition.methods)
                    foreach (var function in library.methods[(int)method])
                        if (manager.TryGetDefinition(definition.parent, out var parentDefinition) && TryFindOverride(manager, function, parentDefinition, out var overrideFunction))
                        {
                            overrideFunction = relied.Convert(overrideFunction);
                            relocations.Add(new Relocation(new DefinitionFunction(new TypeDefinition(overrideFunction.library, TypeCode.Handle, overrideFunction.definitionIndex), new Function(overrideFunction.index, overrideFunction.overloadIndex)), new DefinitionFunction(new CompilingDefinition(definition.declaration).RuntimeDefinition, new Function(function.declaration.index, function.declaration.overloadIndex))));
                        }
                foreach (var inherit in definition.inherits)
                    if (manager.TryGetInterface(inherit, out var result))
                        FindOverride(manager, relied, relocations, result, definition);
                    else throw ExceptionGeneratorCompiler.Unknown();
                definitions[i] = new DefinitionInfo(parent, inherits, memberVariables, memberMethods, relocations.ToArray(), definition.destructorEntry);
                relocations.Dispose();
            }
            var variables = new VariableInfo[library.variables.Count];
            for (int i = 0; i < variables.Length; i++)
            {
                var variable = library.variables[i];
                variables[i] = new VariableInfo(variable.address, relied.Convert(variable.type).RuntimeType);
            }
            var delegates = new FunctionInfo[library.delegates.Count];
            for (int i = 0; i < delegates.Length; i++)
            {
                var function = library.delegates[i];
                delegates[i] = new FunctionInfo(relied.Convert(function.parameters), relied.Convert(function.returns));
            }
            var coroutines = new CoroutineInfo[library.coroutines.Count];
            for (int i = 0; i < coroutines.Length; i++)
            {
                var coroutine = library.coroutines[i];
                coroutines[i] = new CoroutineInfo(relied.Convert(coroutine.returns));
            }
            var methods = new MethodInfo[library.methods.Count];
            for (int i = 0; i < methods.Length; i++)
            {
                var method = library.methods[i];
                var entries = new uint[method.Count];
                var functions = new FunctionInfo[method.Count];
                for (int index = 0; index < method.Count; index++)
                {
                    var function = method[index];
                    entries[index] = function.entry.Value.address;
                    functions[index] = new FunctionInfo(relied.Convert(function.parameters), relied.Convert(function.returns));
                }
                methods[i] = new MethodInfo(entries, functions);
            }
            var interfaces = new InterfaceInfo[library.interfaces.Count];
            for (int i = 0; i < interfaces.Length; i++)
            {
                var definition = library.interfaces[i];
                var relocations = pool.GetList<Relocation>();
                var interfaceMethods = new InterfaceMethodInfo[definition.methods.Length];
                for (int index = 0; index < definition.methods.Length; index++)
                {
                    var method = definition.methods[index];
                    var functions = new FunctionInfo[method.functions.Length];
                    for (int functionIndex = 0; functionIndex < functions.Length; functionIndex++)
                        functions[functionIndex] = new FunctionInfo(relied.Convert(method.functions[functionIndex].parameters), relied.Convert(method.functions[functionIndex].returns));
                    interfaceMethods[index] = new InterfaceMethodInfo(functions);
                }
                foreach (var method in definition.methods)
                    foreach (var function in method.functions)
                        foreach (var inhert in definition.inherits)
                            if (manager.TryGetInterface(inhert, out var result) && TryFindOverride(manager, function, result, out var overrideFunction))
                            {
                                overrideFunction = relied.Convert(overrideFunction);
                                relocations.Add(new Relocation(new DefinitionFunction(new TypeDefinition(overrideFunction.library, TypeCode.Handle, overrideFunction.definitionIndex), new Function(overrideFunction.index, overrideFunction.overloadIndex)), new DefinitionFunction(new CompilingDefinition(definition.declaration).RuntimeDefinition, new Function(function.declaration.index, function.declaration.overloadIndex))));
                            }
                interfaces[i] = new InterfaceInfo(relied.Convert(definition.inherits), relocations.ToArray(), interfaceMethods);
                relocations.Dispose();
            }
            var natives = new NativeMethodInfo[library.natives.Count];
            for (int i = 0; i < natives.Length; i++)
            {
                var native = library.natives[i];
                var functions = new FunctionInfo[native.Count];
                for (int index = 0; index < functions.Length; index++)
                    functions[index] = new FunctionInfo(relied.Convert(native[index].parameters), relied.Convert(native[index].returns));
                natives[i] = new NativeMethodInfo(native.name, functions);
            }
            GeneratorSpace(library, library, out var children, out var exportDefinitions, out var exportVariables, out var exportDelegates, out var exportCoroutines, out var exportMethods, out var exportInterfaces, out var exportNatives);
            return new Library(library.name, code, library.ConstantData, library.DataSize, definitions, variables, delegates, coroutines, methods, interfaces, natives, relied.Generator(), codeStrings, dataStrings,
                children, exportDefinitions, exportVariables, exportDelegates, exportCoroutines, exportMethods, exportInterfaces, exportNatives);
        }
        private void GeneratorSpace(Compiling.Library library, Compiling.Space space, out Space[] children, out ExportDefinition[] exportDefinitions, out ExportIndex[] exportVariables, out ExportIndex[] exportDelegates, out ExportIndex[] exportCoroutines, out ExportMethod[] exportMethods, out ExportInterface[] exportInterfaces, out ExportMethod[] exportNatives)
        {
            children = new Space[space.children.Count];
            var index = 0u;
            foreach (var child in space.children)
            {
                GeneratorSpace(library, child.Value, out var childChildren, out var definitions, out var variables, out var delegates, out var coroutines, out var methods, out var interfaces, out var natives);
                children[index++] = new Space(child.Key, childChildren, definitions, variables, delegates, coroutines, methods, interfaces, natives);
            }
            var definitionList = pool.GetList<ExportDefinition>();
            var variableList = pool.GetList<ExportIndex>();
            var delegateList = pool.GetList<ExportIndex>();
            var coroutineList = pool.GetList<ExportIndex>();
            var methodList = pool.GetList<ExportMethod>();
            var interfaceList = pool.GetList<ExportInterface>();
            var nativeList = pool.GetList<ExportMethod>();
            foreach (var pair in space.declarations)
                if (pair.Value.visibility == Visibility.Public)
                {
                    var name = pair.Key;
                    var declaratioin = pair.Value;
                    switch (declaratioin.code)
                    {
                        case DeclarationCode.Invalid: goto default;
                        case DeclarationCode.Definition:
                            {
                                var definition = library.definitions[(int)declaratioin.index];
                                var variables = pool.GetList<ExportIndex>();
                                foreach (var variable in definition.variables)
                                    if (variable.declaration.visibility.ContainAny(Visibility.Public) || variable.declaration.visibility == Visibility.Protected)
                                        variables.Add(new ExportIndex(variable.name.Segment, variable.declaration.index));
                                var methods = pool.GetList<ExportMethod>();
                                foreach (var methodIndex in definition.methods)
                                    using (var functions = pool.GetList<uint>())
                                    {
                                        var method = library.methods[(int)methodIndex];
                                        foreach (var function in method)
                                            if (function.declaration.visibility.ContainAny(Visibility.Public) || function.declaration.visibility == Visibility.Protected)
                                                functions.Add(function.declaration.overloadIndex);
                                        if (functions.Count > 0) methods.Add(new ExportMethod(method.name, (uint)methods.Count, functions.ToArray()));
                                    }
                                using (var functions = pool.GetList<uint>())
                                {
                                    var method = library.methods[(int)definition.constructors];
                                    foreach (var function in method)
                                        if (function.declaration.visibility.ContainAny(Visibility.Public) || function.declaration.visibility == Visibility.Protected)
                                            functions.Add(function.declaration.overloadIndex);
                                    if (functions.Count > 0) methods.Add(new ExportMethod(definition.name.Segment, (uint)methods.Count, functions.ToArray()));
                                }
                                definitionList.Add(new ExportDefinition(pair.Key, declaratioin.index, variables.ToArray(), methods.ToArray()));
                                variables.Dispose();
                                methods.Dispose();
                            }
                            break;
                        case DeclarationCode.MemberVariable:
                        case DeclarationCode.MemberMethod:
                        case DeclarationCode.MemberFunction:
                        case DeclarationCode.Constructor:
                        case DeclarationCode.ConstructorFunction: goto default;
                        case DeclarationCode.Delegate:
                            delegateList.Add(new ExportIndex(name, declaratioin.index));
                            break;
                        case DeclarationCode.Coroutine:
                            coroutineList.Add(new ExportIndex(name, declaratioin.index));
                            break;
                        case DeclarationCode.Interface:
                            {
                                var definition = library.interfaces[(int)declaratioin.index];
                                var methods = new ExportMethod[definition.methods.Length];
                                for (int i = 0; i < methods.Length; i++)
                                {
                                    var method = definition.methods[i];
                                    var functions = new uint[method.functions.Length];
                                    for (index = 0; index < functions.Length; index++) functions[index] = index;
                                    methods[i] = new ExportMethod(method.name, (uint)i, functions);
                                }
                                interfaceList.Add(new ExportInterface(name, declaratioin.index, methods));
                            }
                            break;
                        case DeclarationCode.InterfaceMethod:
                        case DeclarationCode.InterfaceFunction: goto default;
                        case DeclarationCode.GlobalVariable:
                            variableList.Add(new ExportIndex(name, declaratioin.index));
                            break;
                        case DeclarationCode.GlobalMethod:
                            {
                                var method = library.methods[(int)declaratioin.index];
                                using (var functions = pool.GetList<uint>())
                                {
                                    foreach (var function in method)
                                        if (function.declaration.visibility.ContainAny(Visibility.Public))
                                            functions.Add(function.declaration.overloadIndex);
                                    if (functions.Count > 0)
                                        methodList.Add(new ExportMethod(name, declaratioin.index, functions.ToArray()));
                                }
                            }
                            break;
                        case DeclarationCode.GlobalFunction: goto default;
                        case DeclarationCode.NativeMethod:
                            {
                                var method = library.natives[(int)declaratioin.index];
                                using (var functions = pool.GetList<uint>())
                                {
                                    foreach (var function in method)
                                        if (function.declaration.visibility.ContainAny(Visibility.Public))
                                            functions.Add(function.declaration.overloadIndex);
                                    if (functions.Count > 0)
                                        nativeList.Add(new ExportMethod(name, declaratioin.index, functions.ToArray()));
                                }
                            }
                            break;
                        case DeclarationCode.NativeFunction:
                        case DeclarationCode.Lambda:
                        case DeclarationCode.LocalVariable:
                        default: throw ExceptionGeneratorCompiler.Unknown();
                    }
                }
            exportDefinitions = definitionList.ToArray();
            exportVariables = variableList.ToArray();
            exportDelegates = delegateList.ToArray();
            exportCoroutines = coroutineList.ToArray();
            exportMethods = methodList.ToArray();
            exportInterfaces = interfaceList.ToArray();
            exportNatives = nativeList.ToArray();
            definitionList.Dispose();
            variableList.Dispose();
            delegateList.Dispose();
            coroutineList.Dispose();
            methodList.Dispose();
            interfaceList.Dispose();
            nativeList.Dispose();
        }
        private void FindOverride(DeclarationManager manager, ReliedGenerator relied, ScopeList<Relocation> relocations, IInterface inherit, IDefinition definition)
        {
            for (int methodIndex = 0; methodIndex < inherit.MethodCount; methodIndex++)
            {
                var method = inherit.GetMethod(methodIndex);
                for (int functionIndex = 0; functionIndex < method.FunctionCount; functionIndex++)
                {
                    var function = method.GetFunction(functionIndex);
                    if (TryFindOverride(manager, function, definition, out var overrideFunction))
                    {
                        overrideFunction = relied.Convert(overrideFunction);
                        relocations.Add(new Relocation(new DefinitionFunction(new CompilingDefinition(inherit.Declaration).RuntimeDefinition, new Function(function.Declaration.index, function.Declaration.overloadIndex)), new DefinitionFunction(new TypeDefinition(overrideFunction.library, TypeCode.Handle, overrideFunction.definitionIndex), new Function(overrideFunction.index, overrideFunction.overloadIndex))));
                    }
                }
            }
            for (int i = 0; i < inherit.Inherits.Count; i++)
                if (manager.TryGetInterface(inherit.Inherits[i], out var result))
                    FindOverride(manager, relied, relocations, result, definition);
        }
        private bool TryFindOverride(DeclarationManager manager, IFunction function, IDefinition definition, out Declaration overrideFunction)
        {
            if (TryFindDefinitionOverride(manager, function, definition, out overrideFunction)) return true;
            if (manager.TryGetDefinition(definition.Parent, out definition)) return TryFindOverride(manager, function, definition, out overrideFunction);
            overrideFunction = default;
            return false;
        }
        private bool TryFindOverride(DeclarationManager manager, IFunction function, IInterface definition, out Declaration overrideFunction)
        {
            if (TryFindDefinitionOverride(manager, function, definition, out overrideFunction)) return true;
            for (int i = 0; i < definition.Inherits.Count; i++)
                if (manager.TryGetInterface(definition.Inherits[i], out var result) && TryFindOverride(manager, function, result, out overrideFunction))
                    return true;
            overrideFunction = default;
            return false;
        }
        private bool TryFindDefinitionOverride(DeclarationManager manager, IFunction function, IInterface definition, out Declaration overrideFunction)
        {
            var method = definition.GetMethod(function.Name);
            if (method != null)
                for (int i = 0; i < method.FunctionCount; i++)
                {
                    var index = method.GetFunction(i);
                    if (CompilingType.IsEquals(index.Parameters, function.Parameters))
                    {
                        if (!CompilingType.IsEquals(index.Returns, function.Returns))
                        {
                            exceptions.Add(CompilingExceptionCode.GENERATOR_TYPE_MISMATCH, manager.GetDeclarationFullName(index.Declaration));
                            exceptions.Add(CompilingExceptionCode.GENERATOR_TYPE_MISMATCH, manager.GetDeclarationFullName(function.Declaration));
                        }
                        overrideFunction = index.Declaration;
                        return true;
                    }
                }
            overrideFunction = default;
            return false;
        }
    }
}
