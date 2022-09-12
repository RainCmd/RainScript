namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class BlurryVariableDeclarationExpression : Expression
    {
        public override TokenAttribute Attribute => TokenAttribute.Assignable;
        public BlurryVariableDeclarationExpression(Anchor anchor) : base(anchor, RelyKernel.BLURRY_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.exceptions.Add(anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
        }
    }
    #region Methods
    internal class MethodMemberExpression : Expression
    {
        public readonly Expression target;
        public readonly Declaration declaration;
        public override TokenAttribute Attribute => TokenAttribute.Method | TokenAttribute.Value;
        public MethodMemberExpression(Anchor anchor, Expression target, Declaration declaration) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.target = target;
            this.declaration = declaration;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.exceptions.Add(anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
        }
    }
    internal class MethodVirtualExpression : Expression
    {
        public readonly Expression target;
        public readonly Declaration declaration;
        public override TokenAttribute Attribute => TokenAttribute.Method | TokenAttribute.Value;
        public MethodVirtualExpression(Anchor anchor, Expression target, Declaration declaration) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.target = target;
            this.declaration = declaration;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.exceptions.Add(anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
        }
    }
    internal class MethodQuestionExpression : Expression
    {
        public readonly Expression target;
        public readonly Declaration declaration;
        public override TokenAttribute Attribute => TokenAttribute.Method | TokenAttribute.Value;
        public MethodQuestionExpression(Anchor anchor, Expression target, Declaration declaration) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.target = target;
            this.declaration = declaration;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.exceptions.Add(anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
        }
    }
    internal class MethodGlobalExpression : Expression
    {
        public readonly Declaration declaration;
        public override TokenAttribute Attribute => TokenAttribute.Method | TokenAttribute.Value;
        public MethodGlobalExpression(Anchor anchor, Declaration declaration) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.declaration = declaration;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.exceptions.Add(anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
        }
    }
    internal class MethodNativeExpression : Expression
    {
        public readonly Declaration declaration;
        public override TokenAttribute Attribute => TokenAttribute.Method | TokenAttribute.Value;
        public MethodNativeExpression(Anchor anchor, Declaration declaration) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.declaration = declaration;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.exceptions.Add(anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
        }
    }
    #endregion Methods
    internal class BlurryCoroutineExpression : Expression
    {
        public readonly Expression invoker;
        public override TokenAttribute Attribute => TokenAttribute.Value;
        public BlurryCoroutineExpression(Anchor anchor, Expression invoker) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.invoker = invoker;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.COROUTINE_TYPE);
            if (invoker is InvokerDelegateExpression invokerDelegate)
            {
                var invokerParameter = new GeneratorParameter(parameter, 1);
                invokerDelegate.invoker.Generator(invokerParameter);
                var parameterParameter = new GeneratorParameter(parameter, invokerDelegate.parameter.returns.Length);
                invokerDelegate.parameter.Generator(parameterParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegateCoroutine);
                parameter.generator.WriteCode(parameter.results[0]);
                parameter.generator.WriteCode(RelyKernel.COROUTINE_TYPE);
                parameter.generator.WriteCode(invokerParameter.results[0]);
                Generator(parameter.results[0], parameterParameter);
            }
            else if (invoker is InvokerQuestionDelegateExpression invokerQuestionDelegate)
            {
                var address = new Referencable<CodeAddress>(parameter.pool);
                var invokerParameter = new GeneratorParameter(parameter, 1);
                invokerQuestionDelegate.invoker.Generator(invokerParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_NullJump);
                parameter.generator.WriteCode(invokerParameter.results[0]);
                parameter.generator.WriteCode(address);
                var parameterParameter = new GeneratorParameter(parameter, invokerQuestionDelegate.parameter.returns.Length);
                invokerQuestionDelegate.parameter.Generator(parameterParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegateCoroutine);
                parameter.generator.WriteCode(parameter.results[0]);
                parameter.generator.WriteCode(RelyKernel.COROUTINE_TYPE);
                parameter.generator.WriteCode(invokerParameter.results[0]);
                Generator(parameter.results[0], parameterParameter);
                parameter.generator.SetCodeAddress(address);
                address.Dispose();
            }
            else if (invoker is InvokerNativeExpression) parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_NATIVE_COROUTINE);
            else if (invoker is InvokerGlobalExpression invokerGlobal)
            {
                var parameterParameter = new GeneratorParameter(parameter, invokerGlobal.parameter.returns.Length);
                invokerGlobal.parameter.Generator(parameterParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_CreateCoroutine);
                parameter.generator.WriteCode(RelyKernel.COROUTINE_TYPE);
                parameter.generator.WriteCode(FunctionType.Global);
                var function = parameter.relied.Convert(invokerGlobal.declaration);
                parameter.generator.WriteCode(function.library);
                parameter.generator.WriteCode(new Function(function.index, function.overrideIndex));
                Generator(parameter.results[0], parameterParameter);
            }
            else if (invoker is InvokerMemberExpression invokerMember)
            {
                var targetParameter = new GeneratorParameter(parameter, 1);
                invokerMember.target.Generator(targetParameter);
                var parameterParameter = new GeneratorParameter(parameter, invokerMember.parameter.returns.Length);
                invokerMember.parameter.Generator(parameterParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_CreateCoroutine);
                parameter.generator.WriteCode(RelyKernel.COROUTINE_TYPE);
                parameter.generator.WriteCode(FunctionType.Member);
                var function = parameter.relied.Convert(invokerMember.declaration);
                parameter.generator.WriteCode(function.library);
                parameter.generator.WriteCode(new Function(function.index, function.overrideIndex));
                parameter.generator.WriteCode(targetParameter.results[0]);
                Generator(parameter.results[0], parameterParameter);
            }
            else if (invoker is InvokerVirtualMemberExpression invokerVirtual)
            {
                var targetParameter = new GeneratorParameter(parameter, 1);
                invokerVirtual.target.Generator(targetParameter);
                var parameterParameter = new GeneratorParameter(parameter, invokerVirtual.parameter.returns.Length);
                invokerVirtual.parameter.Generator(parameterParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_CreateCoroutine);
                parameter.generator.WriteCode(RelyKernel.COROUTINE_TYPE);
                parameter.generator.WriteCode(FunctionType.Virtual);
                var function = parameter.relied.Convert(invokerVirtual.declaration);
                parameter.generator.WriteCode(function.library);
                if (invokerVirtual.declaration.code == DeclarationCode.MemberFunction) parameter.generator.WriteCode(function.definitionIndex);
                else if (invokerVirtual.declaration.code == DeclarationCode.InterfaceFunction) parameter.generator.WriteCode(function.definitionIndex);
                else throw ExceptionGeneratorCompiler.Unknown();
                parameter.generator.WriteCode(new Function(function.index, function.overrideIndex));
                parameter.generator.WriteCode(targetParameter.results[0]);
                Generator(parameter.results[0], parameterParameter);
            }
            else if (invoker is InvokerQuestionMemberExpression invokerQuestionMember)
            {
                var address = new Referencable<CodeAddress>(parameter.pool);
                var targetParameter = new GeneratorParameter(parameter, 1);
                invokerQuestionMember.target.Generator(targetParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_NullJump);
                parameter.generator.WriteCode(targetParameter.results[0]);
                parameter.generator.WriteCode(address);
                var parameterParameter = new GeneratorParameter(parameter, invokerQuestionMember.parameter.returns.Length);
                invokerQuestionMember.parameter.Generator(parameterParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_CreateCoroutine);
                parameter.generator.WriteCode(RelyKernel.COROUTINE_TYPE);
                parameter.generator.WriteCode(FunctionType.Virtual);
                var function = parameter.relied.Convert(invokerQuestionMember.declaration);
                parameter.generator.WriteCode(function.library);
                if (invokerQuestionMember.declaration.code == DeclarationCode.MemberFunction) parameter.generator.WriteCode(function.definitionIndex);
                else if (invokerQuestionMember.declaration.code == DeclarationCode.InterfaceFunction) parameter.generator.WriteCode(function.definitionIndex);
                else throw ExceptionGeneratorCompiler.Unknown();
                parameter.generator.WriteCode(new Function(function.index, function.overrideIndex));
                parameter.generator.WriteCode(targetParameter.results[0]);
                Generator(parameter.results[0], parameterParameter);
                parameter.generator.SetCodeAddress(address);
            }
            parameter.generator.WriteCode(CommandMacro.BASE_CoroutineStart);
            parameter.generator.WriteCode(parameter.results[0]);
        }
        private void Generator(Variable coroutine, GeneratorParameter parameter)
        {
            parameter.generator.WriteCode(CommandMacro.BASE_SetCoroutineParameter);
            parameter.generator.WriteCode(coroutine);
            parameter.generator.WriteCode(parameter.results.Length);
            foreach (var variable in parameter.results)
            {
                if(variable.type.IsHandle) parameter.generator.WriteCode((byte)TypeCode.Handle);
                else parameter.generator.WriteCode((byte)variable.type.definition.code);
                parameter.generator.WriteCode(variable);
            }
        }
    }
    internal class BlurryLambdaExpression : Expression
    {
        public readonly Anchor[] parameters;
        public readonly ListSegment<Lexical> body;
        public override TokenAttribute Attribute => TokenAttribute.Value;
        public BlurryLambdaExpression(Anchor anchor, Anchor[] parameters, ListSegment<Lexical> body) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.parameters = parameters;
            this.body = body;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.exceptions.Add(anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
        }
    }
}
