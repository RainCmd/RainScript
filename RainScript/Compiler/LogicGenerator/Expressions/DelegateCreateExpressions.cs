namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class DelegateCreateLambdaFunctionExpression : Expression
    {
        public readonly Declaration lambda;
        public override TokenAttribute Attribute => TokenAttribute.Callable | TokenAttribute.Value;
        public DelegateCreateLambdaFunctionExpression(Anchor anchor, Declaration lambda, CompilingType type) : base(anchor, type)
        {
            this.lambda = lambda;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var lambda = parameter.relied.Convert(this.lambda);
            var definition = parameter.relied.Convert(returns[0].definition).RuntimeDefinition;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(definition);
            parameter.generator.WriteCode(FunctionType.Global);
            parameter.generator.WriteCode(lambda.library);
            parameter.generator.WriteCode(new Function(lambda.index, 0));
        }
    }
    internal class DelegateCreateLambdaClosureFunctionExpression : Expression
    {
        private readonly Declaration definition;
        private readonly Declaration[] sourceVariables;
        private readonly CompilingType[] sourceTypes;
        public override TokenAttribute Attribute => TokenAttribute.Callable | TokenAttribute.Value;
        public DelegateCreateLambdaClosureFunctionExpression(Anchor anchor, Declaration definition, Declaration[] sourceVariables, CompilingType[] sourceTypes, CompilingType type) : base(anchor, type)
        {
            this.definition = definition;
            this.sourceVariables = sourceVariables;
            this.sourceTypes = sourceTypes;
        }
        private Variable GetSourceVariable(GeneratorParameter parameter, int index)
        {
            var source = sourceVariables[index];
            if (source.code == DeclarationCode.LocalVariable)
            {
                if (parameter.variable.TryGetLocal(source.index, out var variable)) return variable;
            }
            else if (source.code == DeclarationCode.LambdaClosureValue)
            {
                if (!parameter.variable.TryGetLocal(0, out var sourceLambdaTarget)) sourceLambdaTarget = parameter.variable.DecareLocal(0, new CompilingType(LIBRARY.SELF, Visibility.Public, TypeCode.Handle, source.definitionIndex, 0));
                var variable = parameter.variable.DecareTemporary(parameter.pool, sourceTypes[index]);
                if (sourceTypes[index].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_Handle);
                else if (sourceTypes[index] == RelyKernel.BOOL_TYPE || sourceTypes[index] == RelyKernel.BYTE_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_1);
                else if (sourceTypes[index] == RelyKernel.INTEGER_TYPE || sourceTypes[index] == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_8);
                else if (sourceTypes[index] == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_16);
                else if (sourceTypes[index] == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_24);
                else if (sourceTypes[index] == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_32);
                else if (sourceTypes[index] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_String);
                else if (sourceTypes[index] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_Entity);
                else throw ExceptionGeneratorCompiler.Unknown();
                parameter.generator.WriteCode(variable);
                parameter.generator.WriteCode(sourceLambdaTarget);
                parameter.generator.WriteCode(LIBRARY.SELF);
                parameter.generator.WriteCode(new MemberVariable(source.definitionIndex, source.index));
                return variable;
            }
            parameter.exceptions.Add(anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
            return default;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var lambdaTarget = parameter.variable.DecareTemporary(parameter.pool, new CompilingType(new CompilingDefinition(this.definition), 0));
            var targetDefinition = parameter.relied.Convert(lambdaTarget.type.definition).RuntimeDefinition;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateObject);
            parameter.generator.WriteCode(lambdaTarget);
            parameter.generator.WriteCode(targetDefinition);

            for (int index = 0; index < sourceVariables.Length; index++)
            {
                var source = GetSourceVariable(parameter, index);

                if (sourceTypes[index].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_Handle);
                else if (sourceTypes[index] == RelyKernel.BOOL_TYPE || sourceTypes[index] == RelyKernel.BYTE_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_1);
                else if (sourceTypes[index] == RelyKernel.INTEGER_TYPE || sourceTypes[index] == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_8);
                else if (sourceTypes[index] == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_16);
                else if (sourceTypes[index] == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_24);
                else if (sourceTypes[index] == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_32);
                else if (sourceTypes[index] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_String);
                else if (sourceTypes[index] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_Entity);
                else throw ExceptionGeneratorCompiler.Unknown();
                parameter.generator.WriteCode(lambdaTarget);
                parameter.generator.WriteCode(LIBRARY.SELF);
                parameter.generator.WriteCode(new MemberVariable(this.definition.index, (uint)index));
                parameter.generator.WriteCode(source);
            }

            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var definition = parameter.relied.Convert(returns[0].definition).RuntimeDefinition;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(definition);
            parameter.generator.WriteCode(FunctionType.Member);
            parameter.generator.WriteCode(targetDefinition.library);
            parameter.generator.WriteCode(targetDefinition.index);
            parameter.generator.WriteCode(new Function(0, 0));
            parameter.generator.WriteCode(lambdaTarget);
        }
    }
    internal class DelegateCreateGlobalFunctionExpression : Expression
    {
        public readonly Declaration function;
        public override TokenAttribute Attribute => TokenAttribute.Callable | TokenAttribute.Value;
        public DelegateCreateGlobalFunctionExpression(Anchor anchor, Declaration function, CompilingType type) : base(anchor, type)
        {
            this.function = function;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var function = parameter.relied.Convert(this.function);
            var definition = parameter.relied.Convert(returns[0].definition).RuntimeDefinition;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(definition);
            parameter.generator.WriteCode(FunctionType.Global);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
        }
    }
    internal class DelegateCreateNativeFunctionExpression : Expression
    {
        public readonly Declaration function;
        public override TokenAttribute Attribute => TokenAttribute.Callable | TokenAttribute.Value;
        public DelegateCreateNativeFunctionExpression(Anchor anchor, Declaration function, CompilingType type) : base(anchor, type)
        {
            this.function = function;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var function = parameter.relied.Convert(this.function);
            var definition = parameter.relied.Convert(returns[0].definition).RuntimeDefinition;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(definition);
            parameter.generator.WriteCode(FunctionType.Native);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
        }
    }
    internal class DelegateCreateMemberFunctionExpression : Expression
    {
        private readonly Expression source;
        public readonly Declaration function;
        public override TokenAttribute Attribute => TokenAttribute.Callable | TokenAttribute.Value;
        public DelegateCreateMemberFunctionExpression(Anchor anchor, Declaration function, Expression source, CompilingType type) : base(anchor, type)
        {
            this.function = function;
            this.source = source;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            if (IsKernelStructMember(this.function)) parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_NOT_HANDLE_MEMBER_METHOD);
            var sourceParameter = new GeneratorParameter(parameter, 1);
            source.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var function = parameter.relied.Convert(this.function);
            var definition = parameter.relied.Convert(returns[0].definition).RuntimeDefinition;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(definition);
            parameter.generator.WriteCode(FunctionType.Member);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(function.definitionIndex);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
        private static bool IsKernelStructMember(Declaration declaration)
        {
            if (declaration.library == LIBRARY.KERNEL)
            {
                switch ((TypeCode)declaration.definitionIndex)
                {
                    case TypeCode.Handle:
                    case TypeCode.Interface:
                    case TypeCode.Function:
                    case TypeCode.Coroutine:
                        break;
                    default: return true;
                }
            }
            return false;
        }
    }
    internal class DelegateCreateVirtualMemberFunctionExpression : Expression
    {
        private readonly Expression source;
        public readonly Declaration function;
        public override TokenAttribute Attribute => TokenAttribute.Callable | TokenAttribute.Value;
        public DelegateCreateVirtualMemberFunctionExpression(Anchor anchor, Declaration function, Expression source, CompilingType type) : base(anchor, type)
        {
            this.function = function;
            this.source = source;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            source.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var function = parameter.relied.Convert(this.function);
            var definition = parameter.relied.Convert(returns[0].definition).RuntimeDefinition;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(definition);
            parameter.generator.WriteCode(FunctionType.Virtual);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(function.definitionIndex);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class DelegateCreateQuestionMemberFunctionExpression : Expression
    {
        private readonly Expression source;
        public readonly Declaration function;
        public override TokenAttribute Attribute => TokenAttribute.Callable | TokenAttribute.Value;
        public DelegateCreateQuestionMemberFunctionExpression(Anchor anchor, Declaration function, Expression source, CompilingType type) : base(anchor, type)
        {
            this.function = function;
            this.source = source;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            if (IsKernelStructMember(this.function)) parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_NOT_HANDLE_MEMBER_METHOD);
            var address = new Referencable<CodeAddress>(parameter.pool);
            var sourceParameter = new GeneratorParameter(parameter, 1);
            source.Generator(sourceParameter);
            parameter.generator.WriteCode(CommandMacro.BASE_NullJump);
            parameter.generator.WriteCode(sourceParameter.results[0]);
            parameter.generator.WriteCode(address);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var function = parameter.relied.Convert(this.function);
            var definition = parameter.relied.Convert(returns[0].definition).RuntimeDefinition;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(definition);
            parameter.generator.WriteCode(FunctionType.Virtual);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(function.definitionIndex);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
            parameter.generator.WriteCode(sourceParameter.results[0]);
            parameter.generator.WriteCode(address);
            address.Dispose();
        }
        private static bool IsKernelStructMember(Declaration declaration)
        {
            if (declaration.library == LIBRARY.KERNEL)
            {
                switch ((TypeCode)declaration.definitionIndex)
                {
                    case TypeCode.Handle:
                    case TypeCode.Interface:
                    case TypeCode.Function:
                    case TypeCode.Coroutine:
                        break;
                    default: return true;
                }
            }
            return false;
        }
    }
}
