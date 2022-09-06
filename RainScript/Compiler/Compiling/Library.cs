using System.Collections.Generic;

namespace RainScript.Compiler.Compiling
{
    internal class Logic
    {
        public readonly IList<Space> compilings;
        public readonly IList<RelySpace> references;
        public Logic(IList<Space> compilings, IList<RelySpace> references)
        {
            this.compilings = compilings;
            this.references = references;
        }
    }
    internal class LogicExpression : Logic
    {
        public readonly Anchor exprssion;
        public LogicExpression(IList<Space> compilings, IList<RelySpace> references, Anchor exprssion) : base(compilings, references)
        {
            this.exprssion = exprssion;
        }
    }
    internal class LogicBody : Logic
    {
        public readonly TextSegment body;
        public LogicBody(IList<Space> compilings, IList<RelySpace> references, TextSegment body) : base(compilings, references)
        {
            this.body = body;
        }
    }
    internal class Declaration
    {
        public readonly Anchor name;
        public readonly Compiler.Declaration declaration;
        public readonly Space space;
        public Declaration(Anchor name, Compiler.Declaration declaration, Space space)
        {
            this.name = name;
            this.declaration = declaration;
            this.space = space;
        }
    }
    internal class Definition : Declaration, IDefinition
    {
        public class MemberVariableInfo : Declaration, IMemberVariable
        {
            public CompilingType type;
            public readonly LogicExpression expression;
            public uint offset;
            public MemberVariableInfo(Anchor name, Compiler.Declaration declaration, Space space, LogicExpression expression) : base(name, declaration, space)
            {
                this.expression = expression;
            }

            public string Name => name.Segment;
            CompilingType IMemberVariable.Type => type;
            Compiler.Declaration IMemberVariable.Declaration => declaration;
        }
        public CompilingDefinition parent;
        public readonly List<CompilingDefinition> inherits;
        public readonly uint constructors;
        public readonly LogicExpression[] constructorInvaokerExpressions;
        public readonly MemberVariableInfo[] variables;
        public uint size;
        public readonly uint[] methods;
        public readonly LogicBody destructor;
        public uint destructorEntry;
        public Definition(Anchor name, Compiler.Declaration declaration, Space space, uint constructors, LogicExpression[] constructorInvaokerExpressions, MemberVariableInfo[] variables, uint[] methods, LogicBody destructor) : base(name, declaration, space)
        {
            inherits = new List<CompilingDefinition>();
            this.constructors = constructors;
            this.constructorInvaokerExpressions = constructorInvaokerExpressions;
            this.variables = variables;
            this.methods = methods;
            this.destructor = destructor;
        }

        public ISpace Space => space;
        public Compiler.Declaration Declaration => declaration;
        public string Name => name.Segment;
        CompilingDefinition IDefinition.Parent { get { return parent; } }
        IList<CompilingDefinition> IInterface.Inherits { get { return inherits; } }
        public int MethodCount => methods.Length;
        public int MemberVaribaleCount => variables.Length;
        public IMethod GetMethod(int index)
        {
            var space = this.space;
            while (space.parent != null) space = space.parent;
            var library = (Library)space;
            return library.methods[(int)methods[index]];
        }
        public IMemberVariable GetMemberVariable(int index)
        {
            return variables[index];
        }
    }
    internal class Variable : Declaration
    {
        public readonly bool constant;
        public CompilingType type;
        public readonly LogicExpression expression;
        public uint address;
        public Variable(Anchor name, Compiler.Declaration declaration, Space space, bool constant, LogicExpression expression) : base(name, declaration, space)
        {
            this.constant = constant;
            this.expression = expression;
        }
    }
    internal class Delegate : Declaration, IFunction
    {
        public readonly CompilingType[] returns;
        public readonly CompilingType[] parameters;
        public readonly Anchor[] parameterNames;
        public Delegate(Anchor name, Compiler.Declaration declaration, Space space, int returnCount, int parameterCount) : base(name, declaration, space)
        {
            returns = new CompilingType[returnCount];
            parameters = new CompilingType[parameterCount];
            parameterNames = new Anchor[parameterCount];
        }

        public string Name => name.Segment;
        public ISpace Space => space;
        public Compiler.Declaration Declaration => declaration;
        public CompilingType[] Parameters => parameters;
        public CompilingType[] Returns => returns;
    }
    internal class Coroutine : Declaration
    {
        public readonly CompilingType[] returns;
        public Coroutine(Anchor name, Compiler.Declaration declaration, Space space, int returnCount) : base(name, declaration, space)
        {
            returns = new CompilingType[returnCount];
        }
    }
    internal class Function : Declaration, IFunction
    {
        public readonly CompilingType[] returns;
        public readonly CompilingType[] parameters;
        public readonly Anchor[] parameterNames;
        public readonly LogicBody body;
        public readonly LogicGenerator.Referencable<LogicGenerator.CodeAddress> entry;
        public Function(Anchor name, Compiler.Declaration declaration, Space space, int returnCount, int parameterCount, LogicBody body, CollectionPool pool) : base(name, declaration, space)
        {
            returns = new CompilingType[returnCount];
            parameters = new CompilingType[parameterCount];
            parameterNames = new Anchor[parameterCount];
            this.body = body;
            entry = new LogicGenerator.Referencable<LogicGenerator.CodeAddress>(pool);
        }
        public Function(Compiler.Declaration declaration, Space space, CompilingType[] returns, CompilingType[] parameters, Anchor[] parameterNames, CollectionPool pool) : base(default, declaration, space)
        {
            this.returns = returns;
            this.parameters = parameters;
            this.parameterNames = parameterNames;
            body = default;
            entry = new LogicGenerator.Referencable<LogicGenerator.CodeAddress>(pool);
        }

        public ISpace Space => space;
        public string Name => name.Segment.ToString();
        public Compiler.Declaration Declaration => declaration;
        public CompilingType[] Returns => returns;
        public CompilingType[] Parameters => parameters;
    }
    internal class Method : IMethod
    {
        private Compiler.Declaration declaration;
        private readonly List<Function> functions = new List<Function>();
        public readonly string name;
        public readonly Space space;
        public Compiler.Declaration Declaration { get { return declaration; } }
        public int Count { get { return functions.Count; } }
        public Function this[int index] { get { return functions[index]; } }
        public Method(uint index, DeclarationCode code, string name, Space space)
        {
            declaration = new Compiler.Declaration(LIBRARY.SELF, Visibility.None, code, index, 0, 0);
            this.name = name;
            this.space = space;
        }
        public void AddFunction(Function function)
        {
            functions.Add(function);
            var visibility = declaration.visibility | function.declaration.visibility;
            declaration = new Compiler.Declaration(LIBRARY.SELF, visibility, declaration.code, declaration.index, 0, 0);
        }
        public IEnumerator<Function> GetEnumerator()
        {
            foreach (var item in functions)
                yield return item;
        }

        public string Name { get { return name; } }
        public int FunctionCount => functions.Count;
        public IFunction GetFunction(int index)
        {
            return functions[index];
        }
    }
    internal class InterfaceMethod : IMethod
    {
        public readonly Compiler.Declaration declaration;
        public readonly Space space;
        public readonly string name;
        public readonly Delegate[] functions;
        public InterfaceMethod(Compiler.Declaration declaration, Space space, string name, Delegate[] functions)
        {
            this.declaration = declaration;
            this.space = space;
            this.name = name;
            this.functions = functions;
        }

        public Compiler.Declaration Declaration { get { return declaration; } }
        public string Name { get { return name; } }
        public int FunctionCount => functions.Length;
        public IFunction GetFunction(int index)
        {
            return functions[index];
        }
    }
    internal class Interface : Declaration, IInterface
    {
        public readonly CompilingDefinition[] inherits;
        public readonly InterfaceMethod[] methods;
        public Interface(Anchor name, Compiler.Declaration declaration, Space space, int interfaceCount, InterfaceMethod[] methods) : base(name, declaration, space)
        {
            inherits = new CompilingDefinition[interfaceCount];
            this.methods = methods;
        }

        public ISpace Space => space;
        public string Name => name.Segment;
        public Compiler.Declaration Declaration => declaration;
        IList<CompilingDefinition> IInterface.Inherits { get { return inherits; } }
        public int MethodCount => methods.Length;
        public IMethod GetMethod(int index)
        {
            return methods[index];
        }
    }
    internal class Native : IMethod
    {
        private Compiler.Declaration declaration;
        private readonly List<Delegate> functions = new List<Delegate>();
        public readonly string name;
        public readonly Space space;
        public Compiler.Declaration Declaration { get { return declaration; } }
        public int Count { get { return functions.Count; } }
        public Delegate this[int index] { get { return functions[index]; } }
        public Native(uint index, string name, Space space)
        {
            declaration = new Compiler.Declaration(LIBRARY.SELF, Visibility.None, DeclarationCode.NativeMethod, index, 0, 0);
            this.name = name;
            this.space = space;
        }
        public void AddFunction(Delegate function)
        {
            functions.Add(function);
            var visibility = declaration.visibility | function.declaration.visibility;
            declaration = new Compiler.Declaration(LIBRARY.SELF, visibility, DeclarationCode.NativeMethod, declaration.index, 0, 0);
        }
        public IEnumerator<Delegate> GetEnumerator()
        {
            foreach (var item in functions)
                yield return item;
        }

        public string Name { get { return name; } }
        public int FunctionCount => functions.Count;
        public IFunction GetFunction(int index)
        {
            return functions[index];
        }
    }
    internal class Space : ISpace
    {
        public readonly Space parent;
        public readonly Dictionary<string, Space> children = new Dictionary<string, Space>();
        public readonly string name;
        public readonly Dictionary<string, Compiler.Declaration> declarations = new Dictionary<string, Compiler.Declaration>();


        public Space(Space parent, string name)
        {
            this.parent = parent;
            this.name = name;
        }
        public Space GetChild(string name)
        {
            if (children.TryGetValue(name, out var child)) return child;
            child = new Space(this, name);
            return child;
        }

        public string Name { get { return name; } }
        public ISpace Parent { get { return parent; } }
        public bool Contain(ISpace space)
        {
            for (var index = space; index != null; index = index.Parent)
                if (index == this) return true;
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
        public bool TryFindDeclaration(StringSegment name, out Compiler.Declaration declaration)
        {
            return declarations.TryGetValue(name, out declaration);
        }
        public bool TryFindSpace(StringSegment name, out ISpace space)
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
    }
    internal class Library : Space
    {
        public readonly List<Definition> definitions = new List<Definition>();
        public readonly List<Variable> variables = new List<Variable>();
        public readonly List<Delegate> delegates = new List<Delegate>();
        public readonly List<Coroutine> coroutines = new List<Coroutine>();
        public readonly List<Method> methods = new List<Method>();
        public readonly List<Interface> interfaces = new List<Interface>();
        public readonly List<Native> natives = new List<Native>();
        public readonly List<Function> lambdas = new List<Function>();
        public uint DataSize { get; private set; }
        public Library(string name) : base(null, name) { }
        public void CalculatedVariableAddress()
        {
            foreach (var item in definitions)
                foreach (var variable in item.variables)
                {
                    variable.offset = item.size;
                    if (variable.type.dimension > 0) item.size += TypeCode.Handle.FieldSize();
                    else item.size += variable.type.definition.code.FieldSize();
                }
            foreach (var item in variables)
            {
                item.address = DataSize;
                if (item.type.dimension > 0) DataSize += TypeCode.Handle.FieldSize();
                else DataSize += item.type.definition.code.FieldSize();
            }
        }
        public void DeclarationValidityCheck(CollectionPool pool, ExceptionCollector exceptions)
        {
            using (var checkedDefinitions = pool.GetSet<Definition>())
                foreach (var item in definitions)
                {
                    if (KeyWorld.IsKeyWorld(item.name.Segment)) exceptions.Add(item.name, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
                    using (var set = pool.GetSet<Definition>())
                    {
                        var index = item;
                        while (set.Add(index))
                            if (checkedDefinitions.Add(index) && index.parent.library == LIBRARY.SELF)
                                index = definitions[(int)index.parent.index];
                            else goto pass;
                        foreach (var definition in set)
                            exceptions.Add(definition.name, CompilingExceptionCode.COMPILING_CIRCULAR_INHERIT);
                        pass:;
                    }
                    foreach (var variable in item.variables)
                        if (KeyWorld.IsKeyWorld(variable.name.Segment))
                            exceptions.Add(variable.name, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
                }
            foreach (var item in variables)
                if (KeyWorld.IsKeyWorld(item.name.Segment))
                    exceptions.Add(item.name, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
            foreach (var item in delegates)
                if (KeyWorld.IsKeyWorld(item.name.Segment))
                    exceptions.Add(item.name, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
            foreach (var item in coroutines)
                if (KeyWorld.IsKeyWorld(item.name.Segment))
                    exceptions.Add(item.name, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
            foreach (var item in methods)
            {
                if (KeyWorld.IsKeyWorld(item.name))
                    foreach (var function in item)
                        exceptions.Add(function.name, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
                for (int x = 0; x < item.Count - 1; x++)
                    for (int y = x + 1; y < item.Count; y++)
                        if (CompilingType.IsEquals(item[x].parameters, item[y].parameters))
                        {
                            exceptions.Add(item[x].name, CompilingExceptionCode.COMPILING_FUNCTION_DUPLICATE_DEFINITION);
                            exceptions.Add(item[y].name, CompilingExceptionCode.COMPILING_FUNCTION_DUPLICATE_DEFINITION);
                        }
            }
            using (var checkedInterfaces = pool.GetSet<Interface>())
                foreach (var item in interfaces)
                {
                    if (KeyWorld.IsKeyWorld(item.name.Segment)) exceptions.Add(item.name, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
                    using (var set = pool.GetSet<Interface>())
                        CheckCircularInherit(checkedInterfaces, set, item, exceptions);
                    foreach (var method in item.methods)
                    {
                        if (KeyWorld.IsKeyWorld(method.name))
                            foreach (var function in method.functions)
                                exceptions.Add(function.name, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
                        for (int x = 0; x < method.functions.Length - 1; x++)
                            for (int y = x + 1; y < method.functions.Length; y++)
                                if (CompilingType.IsEquals(method.functions[x].parameters, method.functions[y].parameters))
                                {
                                    exceptions.Add(method.functions[x].name, CompilingExceptionCode.COMPILING_FUNCTION_DUPLICATE_DEFINITION);
                                    exceptions.Add(method.functions[y].name, CompilingExceptionCode.COMPILING_FUNCTION_DUPLICATE_DEFINITION);
                                }
                    }
                }
            foreach (var item in natives)
            {
                if (KeyWorld.IsKeyWorld(item.name))
                    foreach (var function in item)
                        exceptions.Add(function.name, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
                for (int x = 0; x < item.Count - 1; x++)
                    for (int y = x + 1; y < item.Count; y++)
                        if (CompilingType.IsEquals(item[x].parameters, item[y].parameters))
                        {
                            exceptions.Add(item[x].name, CompilingExceptionCode.COMPILING_FUNCTION_DUPLICATE_DEFINITION);
                            exceptions.Add(item[y].name, CompilingExceptionCode.COMPILING_FUNCTION_DUPLICATE_DEFINITION);
                        }
            }
        }
        private void CheckCircularInherit(ScopeSet<Interface> checkedSet, ScopeSet<Interface> inherits, Interface index, ExceptionCollector exceptions)
        {
            if (checkedSet.Contains(index)) return;
            if (inherits.Add(index))
                foreach (var definition in index.inherits)
                {
                    if (definition.library == LIBRARY.SELF)
                        CheckCircularInherit(checkedSet, inherits, interfaces[(int)definition.index], exceptions);
                }
            else foreach (var definition in inherits)
                    exceptions.Add(definition.name, CompilingExceptionCode.COMPILING_CIRCULAR_INHERIT);
            inherits.Remove(index);
            checkedSet.Add(index);
        }
    }
}
