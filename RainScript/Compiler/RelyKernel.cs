namespace RainScript.Compiler
{
    internal class RelyKernel : RelySpace
    {
        private RelyKernel() : base(KeyWord.KERNEL) { }
        public static readonly CompilingDefinition BOOL = new CompilingDefinition(KERNEL_TYPE.BOOL.definition, Visibility.Public);
        public static readonly CompilingDefinition BYTE = new CompilingDefinition(KERNEL_TYPE.BYTE.definition, Visibility.Public);
        public static readonly CompilingDefinition INTEGER = new CompilingDefinition(KERNEL_TYPE.INTEGER.definition, Visibility.Public);
        public static readonly CompilingDefinition REAL = new CompilingDefinition(KERNEL_TYPE.REAL.definition, Visibility.Public);
        public static readonly CompilingDefinition REAL2 = new CompilingDefinition(KERNEL_TYPE.REAL2.definition, Visibility.Public);
        public static readonly CompilingDefinition REAL3 = new CompilingDefinition(KERNEL_TYPE.REAL3.definition, Visibility.Public);
        public static readonly CompilingDefinition REAL4 = new CompilingDefinition(KERNEL_TYPE.REAL4.definition, Visibility.Public);
        public static readonly CompilingDefinition STRING = new CompilingDefinition(KERNEL_TYPE.STRING.definition, Visibility.Public);
        public static readonly CompilingDefinition HANDLE = new CompilingDefinition(KERNEL_TYPE.HANDLE.definition, Visibility.Public);
        public static readonly CompilingDefinition INTERFACE = new CompilingDefinition(KERNEL_TYPE.INTERFACE.definition, Visibility.Public);
        public static readonly CompilingDefinition FUNCTION = new CompilingDefinition(KERNEL_TYPE.FUNCTION.definition, Visibility.Public);
        public static readonly CompilingDefinition COROUTINE = new CompilingDefinition(KERNEL_TYPE.COROUTINE.definition, Visibility.Public);
        public static readonly CompilingDefinition ENTITY = new CompilingDefinition(KERNEL_TYPE.ENTITY.definition, Visibility.Public);
        public static readonly CompilingDefinition ARRAY = new CompilingDefinition(KERNEL_TYPE.ARRAY.definition, Visibility.Public);
        public static readonly CompilingType BOOL_TYPE = new CompilingType(BOOL, 0);
        public static readonly CompilingType BYTE_TYPE = new CompilingType(BYTE, 0);
        public static readonly CompilingType INTEGER_TYPE = new CompilingType(INTEGER, 0);
        public static readonly CompilingType REAL_TYPE = new CompilingType(REAL, 0);
        public static readonly CompilingType REAL2_TYPE = new CompilingType(REAL2, 0);
        public static readonly CompilingType REAL3_TYPE = new CompilingType(REAL3, 0);
        public static readonly CompilingType REAL4_TYPE = new CompilingType(REAL4, 0);
        public static readonly CompilingType STRING_TYPE = new CompilingType(STRING, 0);
        public static readonly CompilingType HANDLE_TYPE = new CompilingType(HANDLE, 0);
        public static readonly CompilingType INTERFACE_TYPE = new CompilingType(INTERFACE, 0);
        public static readonly CompilingType FUNCTION_TYPE = new CompilingType(FUNCTION, 0);
        public static readonly CompilingType COROUTINE_TYPE = new CompilingType(COROUTINE, 0);
        public static readonly CompilingType ENTITY_TYPE = new CompilingType(ENTITY, 0);
        public static readonly CompilingType ARRAY_TYPE = new CompilingType(ARRAY, 0);
        public static readonly CompilingType NULL_TYPE = new CompilingType(LIBRARY.KERNEL, Visibility.Public, (TypeCode)14, 14, 0);
        public static readonly CompilingType BLURRY_TYPE = new CompilingType(LIBRARY.KERNEL, Visibility.Public, (TypeCode)15, 15, 0);
        public static readonly RelyKernel kernel;
        public static readonly RelyDefinition[] definitions;
        public static readonly RelyVariable[] variables;
        public static readonly RelyMethod[] methods;
        public static RelyMethod GetMethod(CompilingDefinition definition, string name)
        {
            foreach (var methodIndex in definitions[definition.index].methods)
                if (methods[methodIndex].name == name)
                    return methods[methodIndex];
            return null;
        }
        private static RelyDefinition CreateDefinition(string name, CompilingDefinition parent, DeclarationCode code, TypeCode type, uint memberMethodStart, uint memberMethodCount)
        {
            var methods = new uint[memberMethodCount];
            while (memberMethodCount > 0) methods[--memberMethodCount] = memberMethodStart++;
            return new RelyDefinition(name, parent, new CompilingDefinition[0], new Declaration(LIBRARY.KERNEL, Visibility.Public, code, (uint)type, 0, 0), kernel, LIBRARY.ENTRY_INVALID, new RelyDefinition.Variable[0], methods);
        }
        private static CompilingType[] RuntimeToCompiling(Type[] types)
        {
            var results = new CompilingType[types.Length];
            for (int i = 0; i < results.Length; i++) results[i] = new CompilingType(types[i], Visibility.Public);
            return results;
        }
        static RelyKernel()
        {
            kernel = new RelyKernel();
            var handleDefinition = new CompilingDefinition(KERNEL_TYPE.HANDLE.definition, Visibility.Public);
            definitions = new RelyDefinition[]
            {
                CreateDefinition("", CompilingDefinition.INVALID, DeclarationCode.Invalid, TypeCode.Invalid, 0, 0),
                CreateDefinition(KeyWord.BOOL, CompilingDefinition.INVALID, DeclarationCode.Definition, TypeCode.Bool, 0, 1),
                CreateDefinition(KeyWord.BYTE, CompilingDefinition.INVALID, DeclarationCode.Definition, TypeCode.Byte, 1, 1),
                CreateDefinition(KeyWord.INTEGER, CompilingDefinition.INVALID, DeclarationCode.Definition, TypeCode.Integer, 2, 1),
                CreateDefinition(KeyWord.REAL, CompilingDefinition.INVALID, DeclarationCode.Definition, TypeCode.Real, 3, 1),
                CreateDefinition(KeyWord.REAL2, CompilingDefinition.INVALID, DeclarationCode.Definition, TypeCode.Real2, 4, 3),
                CreateDefinition(KeyWord.REAL3, CompilingDefinition.INVALID, DeclarationCode.Definition, TypeCode.Real3, 7, 3),
                CreateDefinition(KeyWord.REAL4, CompilingDefinition.INVALID, DeclarationCode.Definition, TypeCode.Real4, 10, 3),
                CreateDefinition(KeyWord.STRING, CompilingDefinition.INVALID, DeclarationCode.Definition, TypeCode.String, 13, 5),
                CreateDefinition(KeyWord.HANDLE, CompilingDefinition.INVALID, DeclarationCode.Definition, TypeCode.Handle, 18, 2),
                CreateDefinition(KeyWord.INTERFACE, handleDefinition, DeclarationCode.Definition, TypeCode.Interface, 20, 0),
                CreateDefinition(KeyWord.FUNCTION, handleDefinition, DeclarationCode.Definition, TypeCode.Function, 20, 0),
                CreateDefinition(KeyWord.COROUTINE, handleDefinition, DeclarationCode.Definition, TypeCode.Coroutine, 20, 7),
                CreateDefinition(KeyWord.ENTITY, CompilingDefinition.INVALID, DeclarationCode.Definition, TypeCode.Entity, 27, 1),
                CreateDefinition(KeyWord.ARRAY, handleDefinition, DeclarationCode.Definition, (TypeCode)14, 28, 1),
            };
            foreach (var item in definitions) kernel.declarations.Add(item.name, item.declaration);

            variables = new RelyVariable[KernelConstant.constants.Length];
            for (int i = 0; i < KernelConstant.constants.Length; i++)
                variables[i] = new RelyVariable(KernelConstant.constants[i].name, new Declaration(LIBRARY.KERNEL, Visibility.Public, DeclarationCode.GlobalVariable, (uint)i, 0, 0), kernel, true, new CompilingType(KernelConstant.constants[i].type, Visibility.Public));
            foreach (var item in variables) kernel.declarations.Add(item.name, item.declaration);

            methods = new RelyMethod[KernelMethod.memberMethods.Length + KernelMethod.methods.Length];
            foreach (var definition in definitions)
                for (uint index = 0; index < definition.methods.Length; index++)
                {
                    var method = KernelMethod.memberMethods[definition.methods[index]];
                    var functions = new RelyFunction[method.functions.Length];
                    for (uint i = 0; i < functions.Length; i++)
                    {
                        var functionDeclaration = new Declaration(LIBRARY.KERNEL, Visibility.Public, DeclarationCode.MemberFunction, index, i, definition.declaration.index);
                        functions[i] = new RelyFunction(method.name, functionDeclaration, kernel, RuntimeToCompiling(method.functions[i].returns), RuntimeToCompiling(method.functions[i].parameters));
                    }
                    var declaration = new Declaration(LIBRARY.KERNEL, Visibility.Public, DeclarationCode.MemberMethod, index, 0, definition.declaration.index);
                    methods[definition.methods[index]] = new RelyMethod(method.name, declaration, kernel, functions);
                }

            var methodIndex = (uint)KernelMethod.memberMethods.Length;
            foreach (var method in KernelMethod.methods)
            {
                var functions = new RelyFunction[method.functions.Length];
                for (int i = 0; i < functions.Length; i++)
                {
                    var functionDeclaration = new Declaration(LIBRARY.KERNEL, Visibility.Public, DeclarationCode.GlobalFunction, methodIndex, (uint)i, 0);
                    functions[i] = new RelyFunction(method.name, functionDeclaration, kernel, RuntimeToCompiling(method.functions[i].returns), RuntimeToCompiling(method.functions[i].parameters));
                }
                var declaration = new Declaration(LIBRARY.KERNEL, Visibility.Public, DeclarationCode.GlobalMethod, methodIndex, 0, 0);
                methods[methodIndex++] = new RelyMethod(method.name, declaration, kernel, functions);
            }
            foreach (var item in methods)
                if (item.declaration.code == DeclarationCode.GlobalMethod)
                    kernel.declarations.Add(item.name, item.declaration);
        }
    }
}
