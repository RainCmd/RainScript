using static RainScript.Compiler.File.Definition;

namespace RainScript.Compiler.ReferenceGenerator
{
    internal class Definition : System.IDisposable
    {
        public readonly string name;
        public readonly CompilingDefinition parent;
        public readonly CompilingDefinition[] inherits;
        public readonly ScopeList<ReferenceDefinition.Variable> variables;
        public readonly uint constructors;
        public readonly ScopeList<uint> methods;
        public Definition(DeclarationManager manager, Library library, Compiling.Definition definition, CollectionPool pool, ExceptionCollector exceptions)
        {
            variables = pool.GetList<ReferenceDefinition.Variable>();
            methods = pool.GetList<uint>();

            name = definition.name.Segment;
            parent = library.CompilingToReference(manager, definition.name, definition.parent, pool, exceptions);
            inherits = new CompilingDefinition[definition.inherits.Count];
            for (int i = 0; i < inherits.Length; i++)
                inherits[i] = library.CompilingToReference(manager, definition.name, definition.inherits[i], pool, exceptions);
            foreach (var item in definition.variables)
                if (item.declaration.visibility.ContainAll(Visibility.Public))
                    variables.Add(new ReferenceDefinition.Variable(item.name.Segment, item.declaration.visibility, library.CompilingToReference(manager, item.name, item.type, pool, exceptions)));
            var constructorMethod = manager.library.methods[(int)definition.constructors];
            var constructorVisibility = Visibility.None;
            foreach (var item in constructorMethod)
                if (item.declaration.visibility == Visibility.Public || item.declaration.visibility == Visibility.Protected) constructorVisibility |= item.declaration.visibility;
            if (constructorVisibility == Visibility.None) constructors = LIBRARY.METHOD_INVALID;
            else
            {
                var method = new Method(definition.name.Segment, constructorVisibility, pool);
                foreach (var item in constructorMethod)
                    if (item.declaration.visibility == Visibility.Public || item.declaration.visibility == Visibility.Protected)
                        method.functions.Add(new ReferenceFunction(item.declaration.visibility, library.CompilingToReference(manager, item.name, item.returns, pool, exceptions), library.CompilingToReference(manager, item.parameters, item.parameterNames, pool, exceptions)));
                constructors = (uint)library.methods.Count;
                library.methods.Add(method);
            }
            foreach (var item in definition.methods)
            {
                var sourceMethod = manager.library.methods[(int)item];
                var methodVisibility = Visibility.None;
                foreach (var function in sourceMethod)
                    if (function.declaration.visibility == Visibility.Public || function.declaration.visibility == Visibility.Protected)
                        methodVisibility |= function.declaration.visibility;
                if (methodVisibility != Visibility.None)
                {
                    var method = new Method(sourceMethod.name, methodVisibility, pool);
                    foreach (var function in sourceMethod)
                        if (function.declaration.visibility == Visibility.Public || function.declaration.visibility == Visibility.Protected)
                            method.functions.Add(new ReferenceFunction(function.declaration.visibility, library.CompilingToReference(manager, function.name, function.returns, pool, exceptions), library.CompilingToReference(manager, function.parameters, function.parameterNames, pool, exceptions)));
                    methods.Add((uint)library.methods.Count);
                    library.methods.Add(method);
                }
            }
        }
        public ReferenceDefinition Generator()
        {
            return new ReferenceDefinition(name, parent, inherits, constructors, variables.ToArray(), methods.ToArray());
        }
        public void Dispose()
        {
            variables.Dispose();
            methods.Dispose();
        }
    }
    internal class Method : System.IDisposable
    {
        public readonly string name;
        public readonly Visibility visibility;
        public readonly ScopeList<ReferenceFunction> functions;
        public Method(string name, Visibility visibility, CollectionPool pool)
        {
            this.name = name;
            this.visibility = visibility;
            functions = pool.GetList<ReferenceFunction>();
        }
        public ReferenceMetohd Generator()
        {
            return new ReferenceMetohd(name, visibility, functions.ToArray());
        }
        public void Dispose()
        {
            functions.Dispose();
        }
    }
    internal class Interface : System.IDisposable
    {
        public readonly string name;
        public readonly ScopeList<CompilingDefinition> inherits;
        public readonly ScopeList<Method> methods;
        public Interface(string name, CollectionPool pool)
        {
            this.name = name;
            inherits = pool.GetList<CompilingDefinition>();
            methods = pool.GetList<Method>();
        }
        public ReferenceInterface Generator()
        {
            var methods = new ReferenceMetohd[this.methods.Count];
            for (int i = 0; i < methods.Length; i++) methods[i] = this.methods[i].Generator();
            return new ReferenceInterface(name, inherits.ToArray(), methods);
        }
        public void Dispose()
        {
            foreach (var item in methods) item.Dispose();
            methods.Dispose();
        }
    }
    internal class Space : System.IDisposable
    {
        public readonly string name;
        public readonly ScopeList<Space> children;
        public readonly ScopeList<Declaration> declarations;
        public Space(string name, CollectionPool pool)
        {
            this.name = name;
            children = pool.GetList<Space>();
            declarations = pool.GetList<Declaration>();
        }
        public Space GetChild(string name, CollectionPool pool)
        {
            foreach (var item in children)
                if (item.name == name)
                    return item;
            var result = new Space(name, pool);
            children.Add(result);
            return result;
        }
        public void GetDeclarationIndices(CollectionPool pool, out uint[] definitions, out uint[] variables, out uint[] delegates, out uint[] coroutines, out uint[] methods, out uint[] interfaces, out uint[] natives)
        {
            var definitionList = pool.GetList<uint>();
            var variableList = pool.GetList<uint>();
            var delegateList = pool.GetList<uint>();
            var coroutineList = pool.GetList<uint>();
            var methodList = pool.GetList<uint>();
            var interfaceList = pool.GetList<uint>();
            var nativeList = pool.GetList<uint>();
            foreach (var item in declarations)
            {
                switch (item.code)
                {
                    case DeclarationCode.Invalid:
                        break;
                    case DeclarationCode.Definition:
                        definitionList.Add(item.index);
                        break;
                    case DeclarationCode.MemberVariable:
                    case DeclarationCode.MemberMethod:
                    case DeclarationCode.MemberFunction:
                    case DeclarationCode.Constructor:
                    case DeclarationCode.ConstructorFunction:
                        break;
                    case DeclarationCode.Delegate:
                        delegateList.Add(item.index);
                        break;
                    case DeclarationCode.Coroutine:
                        coroutineList.Add(item.index);
                        break;
                    case DeclarationCode.Interface:
                        interfaceList.Add(item.index);
                        break;
                    case DeclarationCode.InterfaceMethod:
                    case DeclarationCode.InterfaceFunction:
                    case DeclarationCode.GlobalVariable:
                        variableList.Add(item.index);
                        break;
                    case DeclarationCode.GlobalMethod:
                        methodList.Add(item.index);
                        break;
                    case DeclarationCode.GlobalFunction:
                        break;
                    case DeclarationCode.NativeMethod:
                        nativeList.Add(item.index);
                        break;
                    case DeclarationCode.NativeFunction:
                        break;
                    case DeclarationCode.LocalVariable:
                        break;
                    default:
                        break;
                }
            }
            definitions = definitionList.ToArray();
            variables = variableList.ToArray();
            delegates = delegateList.ToArray();
            coroutines = coroutineList.ToArray();
            methods = methodList.ToArray();
            interfaces = interfaceList.ToArray();
            natives = nativeList.ToArray();

            definitionList.Dispose();
            variableList.Dispose();
            delegateList.Dispose();
            coroutineList.Dispose();
            methodList.Dispose();
            interfaceList.Dispose();
            nativeList.Dispose();
        }
        public ReferenceSpace GeneratorSpace(CollectionPool pool)
        {
            var children = new ReferenceSpace[this.children.Count];
            for (int i = 0; i < children.Length; i++) children[i] = this.children[i].GeneratorSpace(pool);
            GetDeclarationIndices(pool, out var definitions, out var variables, out var delegates, out var coroutines, out var methods, out var interfaces, out var natives);
            return new ReferenceSpace(name, children, definitions, variables, delegates, coroutines, methods, interfaces, natives);
        }
        public virtual void Dispose()
        {
            foreach (var item in children) item.Dispose();
            children.Dispose();
            declarations.Dispose();
        }
    }
    internal class RelyLibrary : Space
    {
        public readonly ScopeDictionary<CompilingDefinition, CompilingDefinition> map;
        public readonly ScopeList<string> definitions;
        public readonly ScopeList<string> interfaces;
        public readonly ScopeList<string> delegates;
        public readonly ScopeList<string> coroutines;
        public RelyLibrary(string name, CollectionPool pool) : base(name, pool)
        {
            map = pool.GetDictionary<CompilingDefinition, CompilingDefinition>();
            definitions = pool.GetList<string>();
            interfaces = pool.GetList<string>();
            delegates = pool.GetList<string>();
            coroutines = pool.GetList<string>();
        }
        public Space GetSpace(RelySpace space, CollectionPool pool)
        {
            if (space.parent == null) return this;
            else return GetSpace(space.parent, pool).GetChild(space.name, pool);
        }
        private void Generator(ReferenceRelyLibrary library, Space index, ReferenceRelySpace relyIndex)
        {
            foreach (var item in index.children)
                Generator(library, item, new ReferenceRelySpace(relyIndex, item.name));
            foreach (var item in index.declarations)
                if (item.code == DeclarationCode.Definition) library.definitions[item.index] = new ReferenceRelyDeclaration(definitions[(int)item.index], relyIndex);
                else if (item.code == DeclarationCode.Delegate) library.delegates[item.index] = new ReferenceRelyDeclaration(delegates[(int)item.index], relyIndex);
                else if (item.code == DeclarationCode.Coroutine) library.coroutines[item.index] = new ReferenceRelyDeclaration(coroutines[(int)item.index], relyIndex);
                else if (item.code == DeclarationCode.Interface) library.interfaces[item.index] = new ReferenceRelyDeclaration(interfaces[(int)item.index], relyIndex);
        }
        public ReferenceRelyLibrary Generator()
        {
            var library = new ReferenceRelyLibrary(name, definitions.Count, interfaces.Count, delegates.Count, coroutines.Count);
            Generator(library, this, library);
            return library;
        }
        public override void Dispose()
        {
            base.Dispose();
            map.Dispose();
            interfaces.Dispose();
            definitions.Dispose();
            delegates.Dispose();
            coroutines.Dispose();
        }
    }
    internal class Library : Space
    {
        public readonly ScopeDictionary<Declaration, Declaration> map;
        public readonly ScopeList<RelyLibrary> relies;
        public readonly ScopeList<Definition> definitions;
        public readonly ScopeList<ReferenceVariable> variables;
        public readonly ScopeList<ReferenceDelegate> delegates;
        public readonly ScopeList<ReferenceCoroutine> coroutines;
        public readonly ScopeList<Method> methods;
        public readonly ScopeList<Interface> interfaces;
        public readonly ScopeList<Method> natives;
        public Library(DeclarationManager manager, CollectionPool pool, ExceptionCollector exceptions) : base(manager.library.name, pool)
        {
            map = pool.GetDictionary<Declaration, Declaration>();
            relies = pool.GetList<RelyLibrary>();
            definitions = pool.GetList<Definition>();
            variables = pool.GetList<ReferenceVariable>();
            delegates = pool.GetList<ReferenceDelegate>();
            coroutines = pool.GetList<ReferenceCoroutine>();
            methods = pool.GetList<Method>();
            interfaces = pool.GetList<Interface>();
            natives = pool.GetList<Method>();

            var index = 0u;
            foreach (var item in manager.library.definitions)
                if (item.declaration.visibility.ContainAll(Visibility.Public))
                    map.Add(item.declaration, new Declaration(LIBRARY.SELF, Visibility.Public, DeclarationCode.Definition, index++, 0, 0));
            index = 0;
            foreach (var item in manager.library.interfaces)
                if (item.declaration.visibility.ContainAll(Visibility.Public))
                    map.Add(item.declaration, new Declaration(LIBRARY.SELF, Visibility.Public, DeclarationCode.Interface, index++, 0, 0));
            index = 0;
            foreach (var item in manager.library.delegates)
                if (item.declaration.visibility.ContainAll(Visibility.Public))
                    map.Add(item.declaration, new Declaration(LIBRARY.SELF, Visibility.Public, DeclarationCode.Delegate, index++, 0, 0));
            index = 0;
            foreach (var item in manager.library.coroutines)
                if (item.declaration.visibility.ContainAll(Visibility.Public))
                    map.Add(item.declaration, new Declaration(LIBRARY.SELF, Visibility.Public, DeclarationCode.Coroutine, index++, 0, 0));

            foreach (var item in manager.library.definitions)
                if (item.declaration.visibility.ContainAll(Visibility.Public))
                    definitions.Add(new Definition(manager, this, item, pool, exceptions));

            foreach (var item in manager.library.variables)
                if (item.declaration.visibility.ContainAll(Visibility.Public))
                    variables.Add(new ReferenceVariable(item.name.Segment, item.constant, CompilingToReference(manager, item.name, item.type, pool, exceptions)));

            foreach (var item in manager.library.delegates)
                if (item.declaration.visibility.ContainAll(Visibility.Public))
                    delegates.Add(new ReferenceDelegate(item.name.Segment, CompilingToReference(manager, item.name, item.returns, pool, exceptions), CompilingToReference(manager, item.parameters, item.parameterNames, pool, exceptions)));

            foreach (var item in manager.library.coroutines)
                if (item.declaration.visibility.ContainAll(Visibility.Public))
                    coroutines.Add(new ReferenceCoroutine(item.name.Segment, CompilingToReference(manager, item.name, item.returns, pool, exceptions)));

            foreach (var item in manager.library.methods)
                if (item.Declaration.visibility.ContainAny(Visibility.Public))
                {
                    var method = new Method(item.name, Visibility.Public, pool);
                    foreach (var function in item)
                        if (function.declaration.visibility.ContainAll(Visibility.Public))
                            method.functions.Add(new ReferenceFunction(Visibility.Public, CompilingToReference(manager, function.name, function.returns, pool, exceptions), CompilingToReference(manager, function.parameters, function.parameterNames, pool, exceptions)));
                    methods.Add(method);
                }

            foreach (var item in manager.library.interfaces)
                if (item.declaration.visibility.ContainAll(Visibility.Public))
                {
                    var definition = new Interface(item.name.Segment, pool);
                    foreach (var inherit in item.inherits)
                        definition.inherits.Add(CompilingToReference(manager, item.name, inherit, pool, exceptions));
                    foreach (var sourceMethod in item.methods)
                    {
                        var method = new Method(sourceMethod.name, Visibility.Public, pool);
                        foreach (var function in sourceMethod.functions)
                            method.functions.Add(new ReferenceFunction(Visibility.Public, CompilingToReference(manager, function.name, function.returns, pool, exceptions), CompilingToReference(manager, function.parameters, function.parameterNames, pool, exceptions)));
                        definition.methods.Add(method);
                    }
                }

            foreach (var item in manager.library.natives)
                if (item.Declaration.visibility.ContainAny(Visibility.Public))
                {
                    var native = new Method(item.name, Visibility.Public, pool);
                    foreach (var function in item)
                        if (function.declaration.visibility.ContainAll(Visibility.Public))
                            native.functions.Add(new ReferenceFunction(Visibility.Public, CompilingToReference(manager, function.name, function.returns, pool, exceptions), CompilingToReference(manager, function.parameters, function.parameterNames, pool, exceptions)));
                    natives.Add(native);
                }
        }
        public CompilingType[] CompilingToReference(DeclarationManager manager, CompilingType[] parameters, Anchor[] parameterNames, CollectionPool pool, ExceptionCollector exceptions)
        {
            var result = new CompilingType[parameters.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = CompilingToReference(manager, parameterNames[i], parameters[i], pool, exceptions);
            return result;
        }
        public CompilingType[] CompilingToReference(DeclarationManager manager, Anchor anchor, CompilingType[] types, CollectionPool pool, ExceptionCollector exceptions)
        {
            var results = new CompilingType[types.Length];
            for (int i = 0; i < types.Length; i++)
                results[i] = CompilingToReference(manager, anchor, types[i], pool, exceptions);
            return results;
        }
        public CompilingType CompilingToReference(DeclarationManager manager, Anchor anchor, CompilingType type, CollectionPool pool, ExceptionCollector exceptions)
        {
            return new CompilingType(CompilingToReference(manager, anchor, type.definition, pool, exceptions), type.dimension);
        }
        public CompilingDefinition CompilingToReference(DeclarationManager manager, Anchor anchor, CompilingDefinition definition, CollectionPool pool, ExceptionCollector exceptions)
        {
            if (definition.library == LIBRARY.KERNEL || definition.library == LIBRARY.INVALID) return definition;
            else if (definition.library == LIBRARY.SELF)
            {
                if (map.TryGetValue(definition.Declaration, out var result)) return new CompilingDefinition(result);
                else
                {
                    exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_DEFINITION_NOT_PUBLIC);
                    return CompilingDefinition.INVALID;
                }
            }
            else
            {
                var rely = manager.relies[definition.library];
                var index = relies.FindIndex(value => value.name == rely.name);
                if (index < 0)
                {
                    index = relies.Count;
                    relies.Add(new RelyLibrary(rely.name, pool));
                }
                var referenceRely = relies[index];
                if (referenceRely.map.TryGetValue(definition, out var result)) return result;
                else
                {
                    switch (definition.Declaration.code)
                    {
                        case DeclarationCode.Invalid:
                            break;
                        case DeclarationCode.Definition:
                            {
                                var declaration = rely.definitions[definition.index];
                                result = new CompilingDefinition((uint)index, Visibility.Public, TypeCode.Handle, (uint)referenceRely.definitions.Count);
                                referenceRely.map.Add(definition, result);
                                referenceRely.GetSpace(declaration.space, pool).declarations.Add(declaration.declaration);
                                return result;
                            }
                        case DeclarationCode.MemberVariable:
                        case DeclarationCode.MemberMethod:
                        case DeclarationCode.MemberFunction:
                        case DeclarationCode.Constructor:
                        case DeclarationCode.ConstructorFunction:
                            break;
                        case DeclarationCode.Delegate:
                            {
                                var declaration = rely.delegates[definition.index];
                                result = new CompilingDefinition((uint)index, Visibility.Public, TypeCode.Function, (uint)referenceRely.delegates.Count);
                                referenceRely.map.Add(definition, result);
                                referenceRely.GetSpace(declaration.space, pool).declarations.Add(declaration.declaration);
                                return result;
                            }
                        case DeclarationCode.Coroutine:
                            {
                                var declaration = rely.coroutines[definition.index];
                                result = new CompilingDefinition((uint)index, Visibility.Public, TypeCode.Coroutine, (uint)referenceRely.coroutines.Count);
                                referenceRely.map.Add(definition, result);
                                referenceRely.GetSpace(declaration.space, pool).declarations.Add(declaration.declaration);
                                return result;
                            }
                        case DeclarationCode.Interface:
                            {
                                var declaration = rely.interfaces[definition.index];
                                result = new CompilingDefinition((uint)index, Visibility.Public, TypeCode.Interface, (uint)referenceRely.interfaces.Count);
                                referenceRely.map.Add(definition, result);
                                referenceRely.GetSpace(declaration.space, pool).declarations.Add(declaration.declaration);
                                return result;
                            }
                        case DeclarationCode.InterfaceMethod:
                        case DeclarationCode.InterfaceFunction:
                        case DeclarationCode.GlobalVariable:
                        case DeclarationCode.GlobalMethod:
                        case DeclarationCode.GlobalFunction:
                        case DeclarationCode.NativeMethod:
                        case DeclarationCode.NativeFunction:
                        case DeclarationCode.LocalVariable:
                        default:
                            break;
                    }
                    return CompilingDefinition.INVALID;
                }
            }
        }
        public ReferenceLibrary GeneratorLibrary(CollectionPool pool)
        {
            var relies = new ReferenceRelyLibrary[this.relies.Count];
            for (int i = 0; i < relies.Length; i++) relies[i] = this.relies[i].Generator();
            var definitions = new ReferenceDefinition[this.definitions.Count];
            for (int i = 0; i < definitions.Length; i++) definitions[i] = this.definitions[i].Generator();
            var methods = new ReferenceMetohd[this.methods.Count];
            for (int i = 0; i < methods.Length; i++) methods[i] = this.methods[i].Generator();
            var interfaces = new ReferenceInterface[this.interfaces.Count];
            for (int i = 0; i < interfaces.Length; i++) interfaces[i] = this.interfaces[i].Generator();
            var natives = new ReferenceMetohd[this.natives.Count];
            for (int i = 0; i < natives.Length; i++) natives[i] = this.natives[i].Generator();

            var children = new ReferenceSpace[this.children.Count];
            for (int i = 0; i < children.Length; i++) children[i] = this.children[i].GeneratorSpace(pool);
            GetDeclarationIndices(pool, out var definitionIndices, out var variableIndices, out var delegateIndices, out var coroutineIndices, out var methodIndices, out var interfaceIndices, out var nativeIndices);
            return new ReferenceLibrary(name, children, definitionIndices, variableIndices, delegateIndices, coroutineIndices, methodIndices, interfaceIndices, nativeIndices, relies, definitions, variables.ToArray(), delegates.ToArray(), coroutines.ToArray(), methods, interfaces, natives);
        }
        public override void Dispose()
        {
            base.Dispose();
            foreach (var item in relies) item.Dispose();
            relies.Dispose();
            foreach (var item in definitions) item.Dispose();
            definitions.Dispose();
            variables.Dispose();
            delegates.Dispose();
            coroutines.Dispose();
            foreach (var item in methods) item.Dispose();
            methods.Dispose();
            foreach (var item in interfaces) item.Dispose();
            interfaces.Dispose();
            foreach (var item in natives) item.Dispose();
            natives.Dispose();
        }
    }
}
