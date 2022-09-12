using System;

namespace RainScript.Compiler.LogicGenerator
{
    internal class ReliedDeclaration
    {
        public readonly ReliedSpace space;
        public readonly string name;

        public ReliedDeclaration(ReliedSpace space, string name)
        {
            this.space = space;
            this.name = name;
        }
    }
    internal class ReliedDefinitioin : ReliedDeclaration, IDisposable
    {
        public readonly ScopeList<ReliedVariable> variables;
        public readonly ScopeList<ReliedMethod> methods;
        public ReliedDefinitioin(ReliedSpace space, string name, CollectionPool pool) : base(space, name)
        {
            variables = pool.GetList<ReliedVariable>();
            methods = pool.GetList<ReliedMethod>();
        }
        public ImportDefinitionInfo Generator()
        {
            var variables = new ImportDefinitionInfo.Variable[this.variables.Count];
            for (int i = 0; i < variables.Length; i++) variables[i] = this.variables[i].GeneratorMemberVariable();
            var methods = new ImportMethodInfo[this.methods.Count];
            for (int i = 0; i < methods.Length; i++) methods[i] = this.methods[i].Generator();
            return new ImportDefinitionInfo(space.space, name, variables, methods);
        }
        public void Dispose()
        {
            variables.Dispose();
            foreach (var method in methods) method.Dispose();
            methods.Dispose();
        }
    }
    internal class ReliedVariable : ReliedDeclaration
    {
        public readonly CompilingType type;
        public ReliedVariable(ReliedSpace space, string name, CompilingType type) : base(space, name)
        {
            this.type = type;
        }
        public ImportDefinitionInfo.Variable GeneratorMemberVariable()
        {
            return new ImportDefinitionInfo.Variable(name, type.RuntimeType);
        }
        public ImportVariableInfo GeneratorGlobalVariable()
        {
            return new ImportVariableInfo(space.space, name, type.RuntimeType);
        }
    }
    internal class ReliedFunction : ReliedDeclaration
    {
        public readonly CompilingType[] parameters;
        public readonly CompilingType[] returns;
        public ReliedFunction(ReliedSpace space, string name, CompilingType[] parameters, CompilingType[] returns) : base(space, name)
        {
            this.parameters = parameters;
            this.returns = returns;
        }
        private void Generator(out Type[] parameters, out Type[] returns)
        {
            parameters = new Type[this.parameters.Length];
            for (int i = 0; i < parameters.Length; i++) parameters[i] = this.parameters[i].RuntimeType;
            returns = new Type[this.returns.Length];
            for (int i = 0; i < returns.Length; i++) returns[i] = this.returns[i].RuntimeType;
        }
        public FunctionInfo GeneratorFunction()
        {
            Generator(out var parameters, out var returns);
            return new FunctionInfo(parameters, returns);
        }
        public ImportDelegateInfo GeneratorDelegate()
        {
            Generator(out var parameters, out var returns);
            return new ImportDelegateInfo(space.space, name, parameters, returns);
        }
    }
    internal class ReliedCoroutine : ReliedDeclaration
    {
        public readonly CompilingType[] returns;
        public ReliedCoroutine(ReliedSpace space, string name, CompilingType[] returns) : base(space, name)
        {
            this.returns = returns;
        }
        public ImportCoroutineInfo Generator()
        {
            var returns = new Type[this.returns.Length];
            for (int i = 0; i < returns.Length; i++) returns[i] = this.returns[i].RuntimeType;
            return new ImportCoroutineInfo(space.space, name, returns);
        }
    }
    internal class ReliedMethod : ReliedDeclaration, IDisposable
    {
        public readonly ScopeList<ReliedFunction> functions;
        public ReliedMethod(ReliedSpace space, string name, CollectionPool pool) : base(space, name)
        {
            functions = pool.GetList<ReliedFunction>();
        }
        public ImportMethodInfo Generator()
        {
            var functions = new FunctionInfo[this.functions.Count];
            for (int i = 0; i < functions.Length; i++) functions[i] = this.functions[i].GeneratorFunction();
            return new ImportMethodInfo(space.space, name, functions);
        }
        public void Dispose()
        {
            functions.Dispose();
        }
    }
    internal class ReliedInterface : ReliedDeclaration, IDisposable
    {
        public readonly ScopeList<ReliedMethod> methods;
        public ReliedInterface(ReliedSpace space, string name, CollectionPool pool) : base(space, name)
        {
            methods = pool.GetList<ReliedMethod>();
        }
        public ImportInterfaceInfo Generator()
        {
            var methods = new ImportMethodInfo[this.methods.Count];
            for (int i = 0; i < methods.Length; i++) methods[i] = this.methods[i].Generator();
            return new ImportInterfaceInfo(space.space, name, methods);
        }
        public void Dispose()
        {
            methods.Dispose();
        }
    }
    internal class ReliedSpace : IDisposable
    {
        public readonly string name;
        public ImportSpaceInfo space;
        public readonly ReliedSpace parent;
        public readonly ScopeList<ReliedSpace> children;
        public ReliedSpace(string name, ReliedSpace parent, CollectionPool pool)
        {
            this.name = name;
            this.parent = parent;
            children = pool.GetList<ReliedSpace>();
        }
        public ReliedSpace GetChild(string name, CollectionPool pool)
        {
            foreach (var child in children) if (child.name == name) return child;
            var result = new ReliedSpace(name, this, pool);
            children.Add(result);
            return result;
        }
        protected void CreateImportSpace(ImportSpaceInfo space)
        {
            this.space = space;
            foreach (var child in children)
                child.CreateImportSpace(new ImportSpaceInfo(space, child.name));
        }
        public virtual void Dispose()
        {
            foreach (var child in children) child.Dispose();
            children.Dispose();
        }
    }
    internal class ReliedLibrary : ReliedSpace
    {
        public readonly uint library;
        public readonly ScopeList<ReliedDefinitioin> definitioins;
        public readonly ScopeList<ReliedVariable> variables;
        public readonly ScopeList<ReliedFunction> delegates;
        public readonly ScopeList<ReliedCoroutine> coroutines;
        public readonly ScopeList<ReliedMethod> methods;
        public readonly ScopeList<ReliedInterface> interfaces;
        public readonly ScopeList<ReliedMethod> natives;
        public ReliedLibrary(uint library, string name, CollectionPool pool) : base(name, null, pool)
        {
            this.library = library;
            definitioins = pool.GetList<ReliedDefinitioin>();
            variables = pool.GetList<ReliedVariable>();
            delegates = pool.GetList<ReliedFunction>();
            coroutines = pool.GetList<ReliedCoroutine>();
            methods = pool.GetList<ReliedMethod>();
            interfaces = pool.GetList<ReliedInterface>();
            natives = pool.GetList<ReliedMethod>();
        }
        public ReliedSpace GetSpace(RelySpace space, CollectionPool pool)
        {
            if (space.parent == null) return this;
            else return GetSpace(space.parent, pool).GetChild(space.name, pool);
        }
        public ImportLibraryInfo Generator()
        {
            var result = new ImportLibraryInfo(name, new ImportDefinitionInfo[definitioins.Count], new ImportVariableInfo[variables.Count], new ImportDelegateInfo[delegates.Count], new ImportCoroutineInfo[coroutines.Count], new ImportMethodInfo[methods.Count], new ImportInterfaceInfo[interfaces.Count], new ImportMethodInfo[natives.Count]);
            CreateImportSpace(result);
            for (int i = 0; i < definitioins.Count; i++) result.definitions[i] = definitioins[i].Generator();
            for (int i = 0; i < variables.Count; i++) result.variables[i] = variables[i].GeneratorGlobalVariable();
            for (int i = 0; i < delegates.Count; i++) result.delegates[i] = delegates[i].GeneratorDelegate();
            for (int i = 0; i < coroutines.Count; i++) result.coroutines[i] = coroutines[i].Generator();
            for (int i = 0; i < methods.Count; i++) result.methods[i] = methods[i].Generator();
            for (int i = 0; i < interfaces.Count; i++) result.interfaces[i] = interfaces[i].Generator();
            for (int i = 0; i < natives.Count; i++) result.natives[i] = natives[i].Generator();
            return result;
        }
        public override void Dispose()
        {
            base.Dispose();
            foreach (var definitioin in definitioins) definitioin.Dispose();
            definitioins.Dispose();
            variables.Dispose();
            delegates.Dispose();
            coroutines.Dispose();
            foreach (var method in methods) method.Dispose();
            methods.Dispose();
            interfaces.Dispose();
            foreach (var native in natives) native.Dispose();
            natives.Dispose();
        }
    }
    internal class ReliedGenerator : IDisposable
    {
        private readonly DeclarationManager manager;
        private readonly CollectionPool pool;
        private readonly ScopeDictionary<Declaration, Declaration> declarationMap;
        private readonly ScopeList<ReliedLibrary> libraries;
        public ReliedGenerator(DeclarationManager manager, CollectionPool pool)
        {
            this.manager = manager;
            this.pool = pool;
            declarationMap = pool.GetDictionary<Declaration, Declaration>();
            libraries = pool.GetList<ReliedLibrary>();
        }
        public Type[] Convert(CompilingType[] types)
        {
            var result = new Type[types.Length];
            for (int i = 0; i < result.Length; i++) result[i] = Convert(types[i]).RuntimeType;
            return result;
        }
        public TypeDefinition[] Convert(CompilingDefinition[] definitions)
        {
            var result = new TypeDefinition[definitions.Length];
            for (int i = 0; i < result.Length; i++) result[i] = Convert(definitions[i]).RuntimeDefinition;
            return result;
        }
        public CompilingType Convert(CompilingType type)
        {
            return new CompilingType(Convert(type.definition), type.dimension);
        }
        public CompilingDefinition Convert(CompilingDefinition definition)
        {
            return new CompilingDefinition(Convert(definition.Declaration));
        }
        public Declaration Convert(Declaration declaration)
        {
            if (declaration.code == DeclarationCode.LocalVariable) return declaration;
            else if (declaration.library == LIBRARY.SELF || declaration.library == LIBRARY.KERNEL)
            {
                if (declaration.code == DeclarationCode.Constructor)
                {
                    if (!declarationMap.TryGetValue(declaration, out var method))
                    {
                        var definition = manager.library.definitions[(int)declaration.definitionIndex];
                        method = new Declaration(declaration.library, declaration.visibility, declaration.code, (uint)definition.methods.Length, 0, declaration.definitionIndex);
                        declarationMap.Add(declaration, method);
                    }
                    return method;
                }
                else if (declaration.code == DeclarationCode.ConstructorFunction)
                {
                    if (!declarationMap.TryGetValue(declaration, out var function))
                    {
                        var definition = manager.library.definitions[(int)declaration.definitionIndex];
                        function = new Declaration(declaration.library, declaration.visibility, declaration.code, (uint)definition.methods.Length, declaration.overrideIndex, declaration.definitionIndex);
                        declarationMap.Add(declaration, function);
                    }
                    return function;
                }
                return declaration;
            }
            if (!declarationMap.TryGetValue(declaration, out var result))
            {
                var rely = manager.relies[declaration.library];
                var index = libraries.FindIndex(value => value.library == declaration.library);
                if (index < 0)
                {
                    index = libraries.Count;
                    libraries.Add(new ReliedLibrary(declaration.library, rely.name, pool));
                }
                var relied = libraries[index];
                switch (declaration.code)
                {
                    case DeclarationCode.Invalid: goto default;
                    case DeclarationCode.Definition:
                        {
                            var source = rely.definitions[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.definitioins.Count, 0, 0);
                            relied.definitioins.Add(new ReliedDefinitioin(relied.GetSpace(source.space, pool), source.name, pool));
                        }
                        break;
                    case DeclarationCode.MemberVariable:
                        {
                            var sourceDefinition = rely.definitions[declaration.definitionIndex];
                            var source = sourceDefinition.variables[declaration.index];
                            var definitionDeclaration = Convert(sourceDefinition.declaration);
                            var definition = libraries[(int)definitionDeclaration.library].definitioins[(int)definitionDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)definition.variables.Count, 0, definitionDeclaration.index);
                            definition.variables.Add(new ReliedVariable(relied.GetSpace(sourceDefinition.space, pool), source.name, Convert(source.type)));
                        }
                        break;
                    case DeclarationCode.MemberMethod:
                        {
                            var sourceDefinition = rely.definitions[declaration.definitionIndex];
                            var definitionDeclaration = Convert(sourceDefinition.declaration);
                            var definition = libraries[(int)definitionDeclaration.library].definitioins[(int)definitionDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)definition.methods.Count, 0, definitionDeclaration.index);
                            definition.methods.Add(new ReliedMethod(relied.GetSpace(sourceDefinition.space, pool), rely.methods[sourceDefinition.methods[declaration.index]].name, pool));
                        }
                        break;
                    case DeclarationCode.MemberFunction:
                        {
                            var sourceMethod = rely.methods[rely.definitions[declaration.definitionIndex].methods[declaration.index]];
                            var source = sourceMethod.functions[declaration.overrideIndex];
                            var methodDeclaration = Convert(sourceMethod.declaration);
                            var method = libraries[(int)methodDeclaration.library].definitioins[(int)methodDeclaration.definitionIndex].methods[(int)declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, methodDeclaration.index, (uint)method.functions.Count, methodDeclaration.definitionIndex);
                            var function = new ReliedFunction(relied.GetSpace(source.space, pool), source.name, new CompilingType[source.parameters.Length], new CompilingType[source.returns.Length]);
                            for (int i = 0; i < source.parameters.Length; i++) function.parameters[i] = Convert(source.parameters[i]);
                            for (int i = 0; i < source.returns.Length; i++) function.returns[i] = Convert(source.returns[i]);
                            method.functions.Add(function);
                        }
                        break;
                    case DeclarationCode.Constructor:
                        {
                            var sourceDefinition = rely.definitions[declaration.definitionIndex];
                            var definitionDeclaration = Convert(sourceDefinition.declaration);
                            var definition = libraries[(int)definitionDeclaration.library].definitioins[(int)definitionDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)definition.methods.Count, 0, definitionDeclaration.index);
                            definition.methods.Add(new ReliedMethod(relied.GetSpace(sourceDefinition.space, pool), definition.name, pool));
                        }
                        break;
                    case DeclarationCode.ConstructorFunction:
                        {
                            var source = rely.methods[declaration.index].functions[declaration.overrideIndex];
                            var methodDeclaration = Convert(rely.methods[declaration.index].declaration);
                            var method = libraries[(int)methodDeclaration.library].definitioins[(int)methodDeclaration.definitionIndex].methods[(int)declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, methodDeclaration.index, (uint)method.functions.Count, methodDeclaration.definitionIndex);
                            var function = new ReliedFunction(relied.GetSpace(source.space, pool), source.name, new CompilingType[source.parameters.Length], new CompilingType[source.returns.Length]);
                            for (int i = 0; i < source.parameters.Length; i++) function.parameters[i] = Convert(source.parameters[i]);
                            for (int i = 0; i < source.returns.Length; i++) function.returns[i] = Convert(source.returns[i]);
                            method.functions.Add(function);
                        }
                        break;
                    case DeclarationCode.Delegate:
                        {
                            var source = rely.delegates[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.delegates.Count, 0, 0);
                            var function = new ReliedFunction(relied.GetSpace(source.space, pool), source.name, new CompilingType[source.parameters.Length], new CompilingType[source.returns.Length]);
                            for (int i = 0; i < source.parameters.Length; i++) function.parameters[i] = Convert(source.parameters[i]);
                            for (int i = 0; i < source.returns.Length; i++) function.returns[i] = Convert(source.returns[i]);
                            relied.delegates.Add(function);
                        }
                        break;
                    case DeclarationCode.Coroutine:
                        {
                            var source = rely.coroutines[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.coroutines.Count, 0, 0);
                            var coroutine = new ReliedCoroutine(relied.GetSpace(source.space, pool), source.name, new CompilingType[source.returns.Length]);
                            for (int i = 0; i < coroutine.returns.Length; i++) coroutine.returns[i] = Convert(source.returns[i]);
                            relied.coroutines.Add(coroutine);
                        }
                        break;
                    case DeclarationCode.Interface:
                        {
                            var source = rely.interfaces[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.interfaces.Count, 0, 0);
                            relied.interfaces.Add(new ReliedInterface(relied.GetSpace(source.space, pool), source.name, pool));
                        }
                        break;
                    case DeclarationCode.InterfaceMethod:
                        {
                            var sourceInterface = rely.interfaces[declaration.index];
                            var interfaceDeclaration = Convert(sourceInterface.declaration);
                            var definition = libraries[(int)interfaceDeclaration.library].interfaces[(int)interfaceDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)definition.methods.Count, 0, interfaceDeclaration.index);
                            definition.methods.Add(new ReliedMethod(relied.GetSpace(sourceInterface.space, pool), definition.name, pool));
                        }
                        break;
                    case DeclarationCode.InterfaceFunction:
                        {
                            var sourceMethod = rely.interfaces[declaration.definitionIndex].methods[declaration.index];
                            var source = sourceMethod.functions[declaration.overrideIndex];
                            var methodDeclaration = Convert(sourceMethod.declaration);
                            var method = libraries[(int)methodDeclaration.library].interfaces[(int)methodDeclaration.definitionIndex].methods[(int)declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, methodDeclaration.index, (uint)method.functions.Count, methodDeclaration.definitionIndex);
                            var function = new ReliedFunction(relied.GetSpace(source.space, pool), source.name, new CompilingType[source.parameters.Length], new CompilingType[source.returns.Length]);
                            for (int i = 0; i < source.parameters.Length; i++) function.parameters[i] = Convert(source.parameters[i]);
                            for (int i = 0; i < source.returns.Length; i++) function.returns[i] = Convert(source.returns[i]);
                            method.functions.Add(function);
                        }
                        break;
                    case DeclarationCode.GlobalVariable:
                        {
                            var source = rely.variables[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.variables.Count, 0, 0);
                            relied.variables.Add(new ReliedVariable(relied.GetSpace(source.space, pool), source.name, Convert(source.type)));
                        }
                        break;
                    case DeclarationCode.GlobalMethod:
                        {
                            var source = rely.methods[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.methods.Count, 0, 0);
                            relied.methods.Add(new ReliedMethod(relied.GetSpace(source.space, pool), source.name, pool));
                        }
                        break;
                    case DeclarationCode.GlobalFunction:
                        {
                            var source = rely.methods[declaration.index].functions[declaration.overrideIndex];
                            var methodDeclaration = Convert(rely.methods[declaration.index].declaration);
                            var method = libraries[(int)methodDeclaration.library].methods[(int)methodDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, methodDeclaration.index, (uint)method.functions.Count, 0);
                            var function = new ReliedFunction(relied.GetSpace(source.space, pool), source.name, new CompilingType[source.parameters.Length], new CompilingType[source.returns.Length]);
                            for (int i = 0; i < source.parameters.Length; i++) function.parameters[i] = Convert(source.parameters[i]);
                            for (int i = 0; i < source.returns.Length; i++) function.returns[i] = Convert(source.returns[i]);
                            method.functions.Add(function);
                        }
                        break;
                    case DeclarationCode.NativeMethod:
                        {
                            var source = rely.natives[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.natives.Count, 0, 0);
                            relied.natives.Add(new ReliedMethod(relied.GetSpace(source.space, pool), source.name, pool));
                        }
                        break;
                    case DeclarationCode.NativeFunction:
                        {
                            var source = rely.natives[declaration.index].functions[declaration.overrideIndex];
                            var methodDeclaration = Convert(rely.natives[declaration.index].declaration);
                            var native = libraries[(int)methodDeclaration.library].natives[(int)methodDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, methodDeclaration.index, (uint)native.functions.Count, 0);
                            var function = new ReliedFunction(relied.GetSpace(source.space, pool), source.name, new CompilingType[source.parameters.Length], new CompilingType[source.returns.Length]);
                            for (int i = 0; i < source.parameters.Length; i++) function.parameters[i] = Convert(source.parameters[i]);
                            for (int i = 0; i < source.returns.Length; i++) function.returns[i] = Convert(source.returns[i]);
                            native.functions.Add(function);
                        }
                        break;
                    case DeclarationCode.Lambda:
                    case DeclarationCode.LocalVariable:
                    default: throw ExceptionGeneratorCompiler.Unknown();
                }
                declarationMap.Add(declaration, result);
            }
            return result;
        }
        public ImportLibraryInfo[] Generator()
        {
            var result = new ImportLibraryInfo[libraries.Count];
            for (int i = 0; i < result.Length; i++) result[i] = libraries[i].Generator();
            return result;
        }
        public void Dispose()
        {
            declarationMap.Dispose();
            foreach (var library in libraries) library.Dispose();
            libraries.Dispose();
        }
    }
}
