namespace RainScript.Compiler
{
    internal class ReferenceRelyDeclaration
    {
        public readonly string name;
        public readonly ReferenceRelySpace space;
        public ReferenceRelyDeclaration(string name, ReferenceRelySpace space)
        {
            this.name = name;
            this.space = space;
        }
        public override string ToString()
        {
            var fullName = name;
            for (var index = space; index != null; index = index.parent)
                fullName = space.name + "." + fullName;
            return fullName;
        }
    }
    internal class ReferenceRelySpace
    {
        public readonly ReferenceRelySpace parent;
        public readonly string name;
        public ReferenceRelySpace(ReferenceRelySpace parent, string name)
        {
            this.parent = parent;
            this.name = name;
        }
    }
    internal class ReferenceRelyLibrary : ReferenceRelySpace
    {
        public readonly ReferenceRelyDeclaration[] definitions;
        public readonly ReferenceRelyDeclaration[] delegates;
        public readonly ReferenceRelyDeclaration[] coroutines;
        public readonly ReferenceRelyDeclaration[] interfaces;
        public ReferenceRelyLibrary(string name, int definitionCount, int delegateCount, int coroutineCount, int interfaceCount) : base(null, name)
        {
            definitions = new ReferenceRelyDeclaration[definitionCount];
            delegates = new ReferenceRelyDeclaration[delegateCount];
            coroutines = new ReferenceRelyDeclaration[coroutineCount];
            interfaces = new ReferenceRelyDeclaration[interfaceCount];
        }
    }



    internal class ReferenceDeclaration
    {
        public readonly string name;
        public ReferenceDeclaration(string name)
        {
            this.name = name;
        }
    }
    internal class ReferenceDefinition : ReferenceDeclaration
    {
        public struct Variable
        {
            public readonly string name;
            public readonly Visibility visibility;
            public readonly CompilingType type;
            public Variable(string name, Visibility visibility, CompilingType type)
            {
                this.name = name;
                this.visibility = visibility;
                this.type = type;
            }
        }
        public readonly CompilingDefinition parent;
        public readonly CompilingDefinition[] inherits;
        public readonly uint constructors;
        public readonly Variable[] variables;
        public readonly uint[] methods;
        public ReferenceDefinition(string name, CompilingDefinition parent, CompilingDefinition[] inherits, uint constructors, Variable[] variables, uint[] methods) : base(name)
        {
            this.parent = parent;
            this.inherits = inherits;
            this.constructors = constructors;
            this.variables = variables;
            this.methods = methods;
        }
    }
    internal class ReferenceVariable : ReferenceDeclaration
    {
        public readonly bool constant;
        public readonly CompilingType type;
        public ReferenceVariable(string name, bool constant, CompilingType type) : base(name)
        {
            this.constant = constant;
            this.type = type;
        }
    }
    internal class ReferenceDelegate : ReferenceDeclaration
    {
        public readonly CompilingType[] returns;
        public readonly CompilingType[] parameters;
        public ReferenceDelegate(string name, CompilingType[] returns, CompilingType[] parameters) : base(name)
        {
            this.returns = returns;
            this.parameters = parameters;
        }
    }
    internal class ReferenceCoroutine : ReferenceDeclaration
    {
        public readonly CompilingType[] returns;
        public ReferenceCoroutine(string name, CompilingType[] returns) : base(name)
        {
            this.returns = returns;
        }
    }
    internal class ReferenceFunction
    {
        public readonly Visibility visibility;
        public readonly CompilingType[] returns;
        public readonly CompilingType[] parameters;
        public ReferenceFunction(Visibility visibility, CompilingType[] returns, CompilingType[] parameters)
        {
            this.visibility = visibility;
            this.returns = returns;
            this.parameters = parameters;
        }
    }
    internal class ReferenceMetohd : ReferenceDeclaration
    {
        public readonly Visibility visibility;
        public readonly ReferenceFunction[] functions;
        public ReferenceMetohd(string name, Visibility visibility, ReferenceFunction[] functions) : base(name)
        {
            this.visibility = visibility;
            this.functions = functions;
        }
    }
    internal class ReferenceInterface : ReferenceDeclaration
    {
        public readonly CompilingDefinition[] inherits;
        public readonly ReferenceMetohd[] methods;
        public ReferenceInterface(string name, CompilingDefinition[] inherits, ReferenceMetohd[] methods) : base(name)
        {
            this.inherits = inherits;
            this.methods = methods;
        }
    }
    /// <summary>
    /// 命名空间
    /// </summary>
    public class ReferenceSpace
    {
        public readonly string name;
        internal readonly ReferenceSpace[] children;
        internal readonly uint[] definitionIndices;
        internal readonly uint[] variableIndices;
        internal readonly uint[] delegateIndices;
        internal readonly uint[] coroutineIndices;
        internal readonly uint[] methodsIndices;
        internal readonly uint[] interfaceIndices;
        internal readonly uint[] nativeIndices;

        internal ReferenceSpace(string name, ReferenceSpace[] children, uint[] definitionIndices, uint[] variableIndices, uint[] delegateIndices, uint[] coroutineIndices, uint[] methodsIndices, uint[] interfaceIndices, uint[] nativeIndices)
        {
            this.name = name;
            this.children = children;
            this.definitionIndices = definitionIndices;
            this.variableIndices = variableIndices;
            this.delegateIndices = delegateIndices;
            this.coroutineIndices = coroutineIndices;
            this.methodsIndices = methodsIndices;
            this.interfaceIndices = interfaceIndices;
            this.nativeIndices = nativeIndices;
        }
    }
    /// <summary>
    /// 库
    /// </summary>
    public class ReferenceLibrary : ReferenceSpace
    {
        internal readonly ReferenceRelyLibrary[] relies;
        internal readonly ReferenceDefinition[] definitions;
        internal readonly ReferenceVariable[] variables;
        internal readonly ReferenceDelegate[] delegates;
        internal readonly ReferenceCoroutine[] coroutines;
        internal readonly ReferenceMetohd[] methods;
        internal readonly ReferenceInterface[] interfaces;
        internal readonly ReferenceMetohd[] natives;
        internal ReferenceLibrary(string name, ReferenceSpace[] children, uint[] definitionIndices, uint[] variableIndices, uint[] delegateIndices, uint[] coroutineIndices, uint[] methodsIndices, uint[] interfaceIndices, uint[] nativeIndices, ReferenceRelyLibrary[] relies, ReferenceDefinition[] definitions, ReferenceVariable[] variables, ReferenceDelegate[] delegates, ReferenceCoroutine[] coroutines, ReferenceMetohd[] methods, ReferenceInterface[] interfaces, ReferenceMetohd[] natives) : base(name, children, definitionIndices, variableIndices, delegateIndices, coroutineIndices, methodsIndices, interfaceIndices, nativeIndices)
        {
            this.relies = relies;
            this.definitions = definitions;
            this.variables = variables;
            this.delegates = delegates;
            this.coroutines = coroutines;
            this.methods = methods;
            this.interfaces = interfaces;
            this.natives = natives;
        }
    }
}
