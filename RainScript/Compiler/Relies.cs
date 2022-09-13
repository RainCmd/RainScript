using System.Collections.Generic;

namespace RainScript.Compiler
{
    internal class RelyTypeMap : System.IDisposable
    {
        private readonly uint library;
        private readonly ScopeDictionary<CompilingDefinition, CompilingDefinition> map;
        public RelyTypeMap(uint library, ReferenceLibrary source, IList<RelyLibrary> relies, CollectionPool pool, ExceptionCollector exceptions)
        {
            this.library = library;
            map = pool.GetDictionary<CompilingDefinition, CompilingDefinition>();
            for (uint relyIndex = 0; relyIndex < source.relies.Length; relyIndex++)
            {
                var rely = source.relies[relyIndex];
                foreach (var target in relies)
                {
                    if (target.name == rely.name)
                    {
                        for (uint i = 0; i < rely.definitions.Length; i++)
                            if (target.TryGetDeclaration(rely.definitions[i], out var declaration) && declaration.code == DeclarationCode.Definition)
                                map.Add(new CompilingDefinition(relyIndex, Visibility.Public, TypeCode.Handle, i), new CompilingDefinition(declaration.library, Visibility.Public, TypeCode.Handle, declaration.index));
                            else exceptions.Add(CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND, rely.definitions[i].ToString());
                        for (uint i = 0; i < rely.delegates.Length; i++)
                            if (target.TryGetDeclaration(rely.delegates[i], out var declaration) && declaration.code == DeclarationCode.Delegate)
                                map.Add(new CompilingDefinition(relyIndex, Visibility.Public, TypeCode.Function, i), new CompilingDefinition(declaration.library, Visibility.Public, TypeCode.Function, declaration.index));
                            else exceptions.Add(CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND, rely.delegates[i].ToString());
                        for (uint i = 0; i < rely.coroutines.Length; i++)
                            if (target.TryGetDeclaration(rely.coroutines[i], out var declaration) && declaration.code == DeclarationCode.Coroutine)
                                map.Add(new CompilingDefinition(relyIndex, Visibility.Public, TypeCode.Coroutine, i), new CompilingDefinition(declaration.library, Visibility.Public, TypeCode.Coroutine, declaration.index));
                            else exceptions.Add(CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND, rely.coroutines[i].ToString());
                        for (uint i = 0; i < rely.interfaces.Length; i++)
                            if (target.TryGetDeclaration(rely.interfaces[i], out var declaration) && declaration.code == DeclarationCode.Interface)
                                map.Add(new CompilingDefinition(relyIndex, Visibility.Public, TypeCode.Interface, i), new CompilingDefinition(declaration.library, Visibility.Public, TypeCode.Interface, declaration.index));
                            else exceptions.Add(CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND, rely.interfaces[i].ToString());
                        break;
                    }
                }
            }
        }
        public CompilingDefinition LocalToGlobal(CompilingDefinition definition)
        {
            if (definition.library == LIBRARY.SELF) return new CompilingDefinition(library, definition.visibility, definition.code, definition.index);
            else if (definition.library == LIBRARY.KERNEL) return definition;
            else if (map.TryGetValue(definition, out var result)) return result;
            else return CompilingDefinition.INVALID;
        }
        public CompilingDefinition[] LocalToGlobal(CompilingDefinition[] definitions)
        {
            var result = new CompilingDefinition[definitions.Length];
            for (int i = 0; i < result.Length; i++) result[i] = LocalToGlobal(definitions[i]);
            return result;
        }
        public CompilingType LocalToGlobal(CompilingType type)
        {
            return new CompilingType(LocalToGlobal(type.definition), type.dimension);
        }
        public CompilingType[] LocalToGlobal(CompilingType[] types)
        {
            var results = new CompilingType[types.Length];
            for (int i = 0; i < results.Length; i++) results[i] = LocalToGlobal(types[i]);
            return results;
        }
        public void Dispose()
        {
            map.Dispose();
        }
    }
    internal class RelyDeclaration
    {
        public readonly string name;
        public readonly Declaration declaration;
        public readonly RelySpace space;
        public RelyDeclaration(string name, Declaration declaration, RelySpace space)
        {
            this.name = name;
            this.declaration = declaration;
            this.space = space;
        }
    }
    internal class RelyDefinition : RelyDeclaration, IDefinition
    {
        public class Variable : IMemberVariable
        {
            public readonly string name;
            public readonly Declaration declaration;
            public readonly CompilingType type;
            public Variable(string name, Declaration declaration, CompilingType type)
            {
                this.name = name;
                this.declaration = declaration;
                this.type = type;
            }

            string IMemberVariable.Name => name;
            CompilingType IMemberVariable.Type => type;
            Declaration IMemberVariable.Declaration => declaration;
        }
        public readonly CompilingDefinition parent;
        public readonly CompilingDefinition[] inherits;
        public readonly uint constructors;//使用前注意判断下是否为invalid
        public readonly Variable[] variables;
        public readonly uint[] methods;
        public RelyDefinition(string name, CompilingDefinition parent, CompilingDefinition[] inherits, Declaration declaration, RelySpace space, uint constructors, Variable[] variables, uint[] methods) : base(name, declaration, space)
        {
            this.parent = parent;
            this.inherits = inherits;
            this.constructors = constructors;
            this.variables = variables;
            this.methods = methods;
        }

        ISpace IDeclaramtion.Space => space;
        string IDeclaramtion.Name => name;
        Declaration IDeclaramtion.Declaration => declaration;
        CompilingDefinition IDefinition.Parent { get { return parent; } }
        IList<CompilingDefinition> IInterface.Inherits { get { return inherits; } }
        uint IDefinition.Constructor => constructors;
        int IInterface.MethodCount => methods.Length;
        int IDefinition.MemberVaribaleCount => variables.Length;

        IMemberVariable IDefinition.GetMemberVariable(int index)
        {
            return variables[index];
        }
        IMethod IInterface.GetMethod(int index)
        {
            var space = this.space;
            while (space.parent != null) space = space.parent;
            if (space is RelyKernel) return RelyKernel.methods[methods[index]];
            var library = (RelyLibrary)space;
            return library.methods[methods[index]];
        }

        IMethod IInterface.GetMethod(string name)
        {
            var space = this.space;
            while (space.parent != null) space = space.parent;
            var library = (RelyLibrary)space;
            foreach (var method in methods)
                if (library.methods[method].name == name)
                    return library.methods[method];
            return null;
        }
    }
    internal class RelyVariable : RelyDeclaration
    {
        public readonly bool constant;
        public readonly CompilingType type;

        public RelyVariable(string name, Declaration declaration, RelySpace space, bool constant, CompilingType type) : base(name, declaration, space)
        {
            this.constant = constant;
            this.type = type;
        }
    }
    internal class RelyCoroutine : RelyDeclaration
    {
        public readonly CompilingType[] returns;

        public RelyCoroutine(string name, Declaration declaration, RelySpace space, CompilingType[] returns) : base(name, declaration, space)
        {
            this.returns = returns;
        }
    }
    internal class RelyFunction : RelyDeclaration, IFunction
    {
        public readonly CompilingType[] returns;
        public readonly CompilingType[] parameters;

        public RelyFunction(string name, Declaration declaration, RelySpace space, CompilingType[] returns, CompilingType[] parameters) : base(name, declaration, space)
        {
            this.returns = returns;
            this.parameters = parameters;
        }

        CompilingType[] IFunction.Parameters => parameters;
        CompilingType[] IFunction.Returns => returns;
        Declaration IDeclaramtion.Declaration => declaration;
        ISpace IDeclaramtion.Space => space;
        string IDeclaramtion.Name => name;
    }
    internal class RelyMethod : RelyDeclaration, IMethod
    {
        public readonly RelyFunction[] functions;
        public RelyMethod(string name, Declaration declaration, RelySpace space, RelyFunction[] functions) : base(name, declaration, space)
        {
            this.functions = functions;
        }

        Declaration IMethod.Declaration => declaration;
        int IMethod.FunctionCount => functions.Length;
        string IMethod.Name => name;
        IFunction IMethod.GetFunction(int index)
        {
            return functions[index];
        }
    }
    internal class RelyInterface : RelyDeclaration, IInterface
    {
        public readonly CompilingDefinition[] inherits;
        public readonly RelyMethod[] methods;
        public RelyInterface(string name, CompilingDefinition[] inherits, Declaration declaration, RelySpace space, RelyMethod[] methods) : base(name, declaration, space)
        {
            this.inherits = inherits;
            this.methods = methods;
        }

        IList<CompilingDefinition> IInterface.Inherits => inherits;
        int IInterface.MethodCount => methods.Length;
        Declaration IDeclaramtion.Declaration => declaration;
        ISpace IDeclaramtion.Space => space;
        string IDeclaramtion.Name => name;
        IMethod IInterface.GetMethod(int index)
        {
            return methods[index];
        }
        IMethod IInterface.GetMethod(string name)
        {
            foreach (var method in methods)
                if (method.name == name)
                    return method;
            return null;
        }
    }
    internal class RelySpace : ISpace
    {
        public readonly RelySpace parent;
        public readonly string name;
        public readonly Dictionary<string, RelySpace> children = new Dictionary<string, RelySpace>();
        public readonly Dictionary<string, Declaration> declarations = new Dictionary<string, Declaration>();
        protected RelySpace(string name)
        {
            this.name = name;
        }
        protected RelySpace(ReferenceSpace source)
        {
            parent = null;
            name = source.name;
        }
        protected RelySpace(RelySpace parent, ReferenceSpace source, RelyLibrary library, ReferenceLibrary sourceLibrary, RelyTypeMap map)
        {
            this.parent = parent;
            name = source.name;
            Init(source, library, sourceLibrary, map);
        }
        protected void Init(ReferenceSpace source, RelyLibrary library, ReferenceLibrary sourceLibrary, RelyTypeMap map)
        {
            foreach (var child in source.children) children.Add(child.name, new RelySpace(this, child, library, sourceLibrary, map));
            foreach (var index in source.definitionIndices)
            {
                var definition = sourceLibrary.definitions[index];
                var declaration = new Declaration(library.library, Visibility.Public, DeclarationCode.Definition, index, 0, 0);
                declarations.Add(definition.name, declaration);
                var variables = new RelyDefinition.Variable[definition.variables.Length];
                for (uint i = 0; i < variables.Length; i++)
                {
                    var variable = definition.variables[i];
                    variables[i] = new RelyDefinition.Variable(variable.name, new Declaration(library.library, variable.visibility, DeclarationCode.MemberVariable, i, 0, index), map.LocalToGlobal(variable.type));
                }

                if (definition.constructors != LIBRARY.METHOD_INVALID)
                {
                    var constructors = sourceLibrary.methods[definition.constructors];
                    var constructorFunctions = new RelyFunction[constructors.functions.Length];
                    for (uint i = 0; i < constructorFunctions.Length; i++)
                    {
                        var function = constructors.functions[i];
                        constructorFunctions[i] = new RelyFunction(constructors.name, new Declaration(library.library, function.visibility, DeclarationCode.ConstructorFunction, definition.constructors, i, index), this, map.LocalToGlobal(function.returns), map.LocalToGlobal(function.parameters));
                    }
                    library.methods[definition.constructors] = new RelyMethod(constructors.name, new Declaration(library.library, constructors.visibility, DeclarationCode.Constructor, definition.constructors, 0, index), this, constructorFunctions);
                }

                for (uint methodIndex = 0; methodIndex < definition.methods.Length; methodIndex++)
                {
                    var method = sourceLibrary.methods[definition.methods[methodIndex]];
                    var functions = new RelyFunction[method.functions.Length];
                    for (uint functionIndex = 0; functionIndex < functions.Length; functionIndex++)
                    {
                        var function = method.functions[functionIndex];
                        functions[functionIndex] = new RelyFunction(method.name, new Declaration(library.library, function.visibility, DeclarationCode.MemberFunction, methodIndex, functionIndex, index), this, map.LocalToGlobal(function.returns), map.LocalToGlobal(function.parameters));
                    }
                    library.methods[definition.methods[methodIndex]] = new RelyMethod(method.name, new Declaration(library.library, method.visibility, DeclarationCode.MemberMethod, methodIndex, 0, index), this, functions);
                }
                library.definitions[index] = new RelyDefinition(definition.name, map.LocalToGlobal(definition.parent), map.LocalToGlobal(definition.inherits), declaration, this, definition.constructors, variables, definition.methods);
            }
            foreach (var index in source.variableIndices)
            {
                var variable = sourceLibrary.variables[index];
                var declaratioin = new Declaration(library.library, Visibility.Public, DeclarationCode.GlobalVariable, index, 0, 0);
                declarations.Add(variable.name, declaratioin);
                library.variables[index] = new RelyVariable(variable.name, declaratioin, this, variable.constant, map.LocalToGlobal(variable.type));
            }
            foreach (var index in source.delegateIndices)
            {
                var function = sourceLibrary.delegates[index];
                var declaration = new Declaration(library.library, Visibility.Public, DeclarationCode.Delegate, index, 0, 0);
                declarations.Add(function.name, declaration);
                library.delegates[index] = new RelyFunction(function.name, declaration, this, map.LocalToGlobal(function.returns), map.LocalToGlobal(function.parameters));
            }
            foreach (var index in source.coroutineIndices)
            {
                var coroutine = sourceLibrary.coroutines[index];
                var declaration = new Declaration(library.library, Visibility.Public, DeclarationCode.Coroutine, index, 0, 0);
                declarations.Add(coroutine.name, declaration);
                library.coroutines[index] = new RelyCoroutine(coroutine.name, declaration, this, map.LocalToGlobal(coroutine.returns));
            }
            foreach (var index in source.methodsIndices)
            {
                var method = sourceLibrary.methods[index];
                var declaration = new Declaration(library.library, method.visibility, DeclarationCode.GlobalMethod, index, 0, 0);
                var functions = new RelyFunction[method.functions.Length];
                for (uint i = 0; i < functions.Length; i++)
                {
                    var function = method.functions[i];
                    functions[i] = new RelyFunction(method.name, new Declaration(library.library, function.visibility, DeclarationCode.GlobalFunction, index, i, 0), this, map.LocalToGlobal(function.returns), map.LocalToGlobal(function.parameters));
                }
                library.methods[index] = new RelyMethod(method.name, declaration, this, functions);
            }
            foreach (var index in source.interfaceIndices)
            {
                var referenceInterface = sourceLibrary.interfaces[index];
                var declaration = new Declaration(library.library, Visibility.Public, DeclarationCode.Interface, index, 0, 0);
                var methods = new RelyMethod[referenceInterface.methods.Length];
                for (uint methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                {
                    var method = referenceInterface.methods[methodIndex];
                    var methodDeclaration = new Declaration(library.library, Visibility.Public, DeclarationCode.InterfaceMethod, methodIndex, 0, index);
                    var functions = new RelyFunction[method.functions.Length];
                    for (uint i = 0; i < functions.Length; i++)
                    {
                        var function = method.functions[i];
                        var functionDeclaration = new Declaration(library.library, Visibility.Public, DeclarationCode.InterfaceFunction, methodIndex, i, index);
                        functions[i] = new RelyFunction(method.name, functionDeclaration, this, map.LocalToGlobal(function.returns), map.LocalToGlobal(function.parameters));
                    }
                    methods[methodIndex] = new RelyMethod(method.name, methodDeclaration, this, functions);
                }
                library.interfaces[index] = new RelyInterface(referenceInterface.name, map.LocalToGlobal(referenceInterface.inherits), declaration, this, methods);
            }
            foreach (var index in source.nativeIndices)
            {
                var native = sourceLibrary.natives[index];
                var declaration = new Declaration(library.library, native.visibility, DeclarationCode.NativeMethod, index, 0, 0);
                var functions = new RelyFunction[native.functions.Length];
                for (uint i = 0; i < functions.Length; i++)
                {
                    var function = native.functions[i];
                    functions[i] = new RelyFunction(native.name, new Declaration(library.library, function.visibility, DeclarationCode.NativeFunction, index, i, 0), this, map.LocalToGlobal(function.returns), map.LocalToGlobal(function.parameters));
                }
                library.natives[index] = new RelyMethod(native.name, declaration, this, functions);
            }
        }

        string ISpace.Name => name;
        ISpace ISpace.Parent => parent;
        bool ISpace.TryFindSpace(StringSegment name, out ISpace space)
        {
            for (space = this; space != null; space = space.Parent)
                if (space.Name == name) return true;
                else if (space.TryFindChild(name, out var result))
                {
                    space = result;
                    return true;
                }
            return false;
        }
        public bool TryFindChild(StringSegment name, out ISpace child)
        {
            if (children.TryGetValue(name, out var result))
            {
                child = result;
                return true;
            }
            else
            {
                child = default;
                return false;
            }
        }
        public bool TryFindDeclaration(StringSegment name, out Declaration declaration)
        {
            return declarations.TryGetValue(name, out declaration);
        }
        bool ISpace.Contain(ISpace space)
        {
            for (var index = space; index != null; index = index.Parent)
                if (index == this) return true;
            return false;
        }
    }
    internal class RelyLibrary : RelySpace
    {
        public readonly uint library;
        public readonly RelyDefinition[] definitions;
        public readonly RelyVariable[] variables;
        public readonly RelyFunction[] delegates;
        public readonly RelyCoroutine[] coroutines;
        public readonly RelyMethod[] methods;
        public readonly RelyInterface[] interfaces;
        public readonly RelyMethod[] natives;
        public RelyLibrary(ReferenceLibrary source, IList<RelyLibrary> relies, CollectionPool pool, ExceptionCollector exceptions) : base(source)
        {
            library = (uint)relies.Count;
            definitions = new RelyDefinition[source.definitions.Length];
            variables = new RelyVariable[source.variables.Length];
            delegates = new RelyFunction[source.delegates.Length];
            coroutines = new RelyCoroutine[source.coroutines.Length];
            methods = new RelyMethod[source.methods.Length];
            interfaces = new RelyInterface[source.interfaces.Length];
            natives = new RelyMethod[source.natives.Length];
            using (var map = new RelyTypeMap(library, source, relies, pool, exceptions))
                Init(source, this, source, map);
        }
        public bool TryGetDeclaration(ReferenceRelyDeclaration declaration, out Declaration result)
        {
            if (TryGetSpace(declaration.space, out var space))
                return space.declarations.TryGetValue(declaration.name, out result);
            result = default;
            return false;
        }
        private bool TryGetSpace(ReferenceRelySpace import, out RelySpace space)
        {
            if (import.parent != null)
            {
                if (TryGetSpace(import.parent, out space))
                    return space.children.TryGetValue(import.name, out space);
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
