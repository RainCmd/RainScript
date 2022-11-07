namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal abstract class InvokerExpression : Expression
    {
        public readonly Expression parameter;
        protected InvokerExpression(Anchor anchor, Expression parameter, params CompilingType[] returns) : base(anchor, returns)
        {
            this.parameter = parameter;
        }
        protected uint PushParameter(uint point, Variable variable, Generator generator)
        {
            if (variable.type.IsHandle) generator.WriteCode(CommandMacro.FUNCTION_PushParameter_Handle);
            else if (variable.type == RelyKernel.BOOL_TYPE) generator.WriteCode(CommandMacro.FUNCTION_PushParameter_1);
            else if (variable.type == RelyKernel.INTEGER_TYPE || variable.type == RelyKernel.REAL_TYPE) generator.WriteCode(CommandMacro.FUNCTION_PushParameter_8);
            else if (variable.type == RelyKernel.REAL2_TYPE) generator.WriteCode(CommandMacro.FUNCTION_PushParameter_16);
            else if (variable.type == RelyKernel.REAL3_TYPE) generator.WriteCode(CommandMacro.FUNCTION_PushParameter_24);
            else if (variable.type == RelyKernel.REAL4_TYPE) generator.WriteCode(CommandMacro.FUNCTION_PushParameter_32);
            else if (variable.type == RelyKernel.STRING_TYPE) generator.WriteCode(CommandMacro.FUNCTION_PushParameter_String);
            else if (variable.type == RelyKernel.ENTITY_TYPE) generator.WriteCode(CommandMacro.FUNCTION_PushParameter_Entity);
            else throw ExceptionGeneratorCompiler.Unknown();
#if MEMORY_ALIGNMENT_4
            if (variable.type == RelyKernel.REAL_TYPE || variable.type == RelyKernel.REAL2_TYPE || variable.type == RelyKernel.REAL3_TYPE || variable.type == RelyKernel.REAL4_TYPE)
                Tools.MemoryAlignment(ref point);
#endif
            generator.WriteCode(point);
            generator.WriteCode(variable);
            return point + variable.type.FieldSize;
        }
    }
    internal class InvokerDelegateExpression : InvokerExpression
    {
        public readonly Expression invoker;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerDelegateExpression(Anchor anchor, Expression invoker, Expression parameter, CompilingType[] returns) : base(anchor, parameter, returns)
        {
            this.invoker = invoker;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var returnPoint = new Referencable<CodeAddress>(parameter.pool);
            for (int i = 0; i < returns.Length; i++) parameter.results[i] = parameter.variable.DecareTemporary(parameter.pool, returns[i]);
            var invokerParameter = new GeneratorParameter(parameter, 1);
            invoker.Generator(invokerParameter);
            parameter.generator.WriteCode(CommandMacro.HANDLE_CheckNull);
            parameter.generator.WriteCode(invokerParameter.results[0]);
            var parameterParameter = new GeneratorParameter(parameter, this.parameter.returns.Length);
            this.parameter.Generator(parameterParameter);
            var parameterSize = 4u + (uint)returns.Length * 4 + Frame.SIZE;
            foreach (var returnType in this.parameter.returns) parameterSize += returnType.FieldSize;
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Ensure);
            parameter.generator.WriteCode(parameterSize);
            parameter.generator.WriteCode(returnPoint);
            var point = (uint)Frame.SIZE;
            foreach (var variable in parameter.results)
            {
                parameter.generator.WriteCode(CommandMacro.FUNCTION_PushReturnPoint);
                parameter.generator.WriteCode(point);
                parameter.generator.WriteCode(variable);
                point += 4;
            }
            parameter.generator.WriteCode(CommandMacro.FUNCTION_CustomCallPretreater);
            parameter.generator.WriteCode((uint)returns.Length * 4 + Frame.SIZE);
            parameter.generator.WriteCode(invokerParameter.results[0]);
            foreach (var variable in parameterParameter.results) point = PushParameter(point, variable, parameter.generator);
            parameter.generator.WriteCode(CommandMacro.FUNCTION_CustomCall);
            parameter.generator.WriteCode(invokerParameter.results[0]);
            parameter.generator.SetCodeAddress(returnPoint);
            returnPoint.Dispose();
        }
    }
    internal class InvokerQuestionDelegateExpression : InvokerExpression
    {
        public readonly Expression invoker;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerQuestionDelegateExpression(Anchor anchor, Expression invoker, Expression parameter, CompilingType[] returns) : base(anchor, parameter, returns)
        {
            this.invoker = invoker;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var returnPoint = new Referencable<CodeAddress>(parameter.pool);
            for (int i = 0; i < returns.Length; i++) parameter.results[i] = parameter.variable.DecareTemporary(parameter.pool, returns[i]);
            var invokerParameter = new GeneratorParameter(parameter, 1);
            invoker.Generator(invokerParameter);
            parameter.generator.WriteCode(CommandMacro.BASE_NullJump);
            parameter.generator.WriteCode(invokerParameter.results[0]);
            parameter.generator.WriteCode(returnPoint);
            var parameterParameter = new GeneratorParameter(parameter, this.parameter.returns.Length);
            this.parameter.Generator(parameterParameter);
            var parameterSize = 4u + (uint)returns.Length * 4 + Frame.SIZE;
            foreach (var returnType in this.parameter.returns) parameterSize += returnType.FieldSize;
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Ensure);
            parameter.generator.WriteCode(parameterSize);
            parameter.generator.WriteCode(returnPoint);
            var point = (uint)Frame.SIZE;
            foreach (var variable in parameter.results)
            {
                parameter.generator.WriteCode(CommandMacro.FUNCTION_PushReturnPoint);
                parameter.generator.WriteCode(point);
                parameter.generator.WriteCode(variable);
                point += 4;
            }
            parameter.generator.WriteCode(CommandMacro.FUNCTION_CustomCallPretreater);
            parameter.generator.WriteCode((uint)returns.Length * 4 + Frame.SIZE);
            parameter.generator.WriteCode(invokerParameter.results[0]);
            foreach (var variable in parameterParameter.results) point = PushParameter(point, variable, parameter.generator);
            parameter.generator.WriteCode(CommandMacro.FUNCTION_CustomCall);
            parameter.generator.WriteCode(invokerParameter.results[0]);
            parameter.generator.SetCodeAddress(returnPoint);
            returnPoint.Dispose();
            if (!parameter.command.ignoreExit) parameter.generator.WriteCode(CommandMacro.BASE_ExitJump);
        }
    }
    internal class InvokerNativeExpression : InvokerExpression
    {
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerNativeExpression(Anchor anchor, Declaration declaration, Expression parameter, CompilingType[] returns) : base(anchor, parameter, returns)
        {
            this.declaration = declaration;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var returnPoint = new Referencable<CodeAddress>(parameter.pool);
            for (int i = 0; i < returns.Length; i++) parameter.results[i] = parameter.variable.DecareTemporary(parameter.pool, returns[i]);
            var parameterParameter = new GeneratorParameter(parameter, this.parameter.returns.Length);
            this.parameter.Generator(parameterParameter);
            var parameterSize = (uint)returns.Length * 4 + Frame.SIZE;
            foreach (var returnType in this.parameter.returns) parameterSize += returnType.FieldSize;
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Ensure);
            parameter.generator.WriteCode(parameterSize);
            parameter.generator.WriteCode(returnPoint);
            var point = (uint)Frame.SIZE;
            foreach (var variable in parameter.results)
            {
                parameter.generator.WriteCode(CommandMacro.FUNCTION_PushReturnPoint);
                parameter.generator.WriteCode(point);
                parameter.generator.WriteCode(variable);
                point += 4;
            }
            foreach (var variable in parameterParameter.results) point = PushParameter(point, variable, parameter.generator);
            parameter.generator.WriteCode(CommandMacro.FUNCTION_NativeCall);
            var function = parameter.relied.Convert(declaration);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
            parameter.generator.SetCodeAddress(returnPoint);
            returnPoint.Dispose();
            if (!parameter.command.ignoreExit) parameter.generator.WriteCode(CommandMacro.BASE_ExitJump);
        }
    }
    internal class InvokerGlobalExpression : InvokerExpression
    {
        public readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerGlobalExpression(Anchor anchor, Declaration declaration, Expression parameter, CompilingType[] returns) : base(anchor, parameter, returns)
        {
            this.declaration = declaration;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var returnPoint = new Referencable<CodeAddress>(parameter.pool);
            for (int i = 0; i < returns.Length; i++) parameter.results[i] = parameter.variable.DecareTemporary(parameter.pool, returns[i]);
            var parameterParameter = new GeneratorParameter(parameter, this.parameter.returns.Length);
            this.parameter.Generator(parameterParameter);
            var parameterSize = (uint)returns.Length * 4 + Frame.SIZE;
            foreach (var returnType in this.parameter.returns) parameterSize += returnType.FieldSize;
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Ensure);
            parameter.generator.WriteCode(parameterSize);
            parameter.generator.WriteCode(returnPoint);
            var point = (uint)Frame.SIZE;
            foreach (var variable in parameter.results)
            {
                parameter.generator.WriteCode(CommandMacro.FUNCTION_PushReturnPoint);
                parameter.generator.WriteCode(point);
                parameter.generator.WriteCode(variable);
                point += 4;
            }
            foreach (var variable in parameterParameter.results) point = PushParameter(point, variable, parameter.generator);
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Call);
            var function = parameter.relied.Convert(declaration);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
            parameter.generator.SetCodeAddress(returnPoint);
            returnPoint.Dispose();
            if (!parameter.command.ignoreExit) parameter.generator.WriteCode(CommandMacro.BASE_ExitJump);
        }
    }
    internal class InvokerMemberExpression : InvokerExpression
    {
        public readonly Expression target;
        public readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerMemberExpression(Anchor anchor, Declaration declaration, Expression target, Expression parameter, CompilingType[] returns) : base(anchor, parameter, returns)
        {
            this.target = target;
            this.declaration = declaration;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var returnPoint = new Referencable<CodeAddress>(parameter.pool);
            for (int i = 0; i < returns.Length; i++) parameter.results[i] = parameter.variable.DecareTemporary(parameter.pool, returns[i]);
            var targetParameter = new GeneratorParameter(parameter, 1);
            target.Generator(targetParameter);
            if (target.returns[0].IsHandle)
            {
                parameter.generator.WriteCode(CommandMacro.HANDLE_CheckNull);
                parameter.generator.WriteCode(targetParameter.results[0]);
            }
            var parameterParameter = new GeneratorParameter(parameter, this.parameter.returns.Length);
            this.parameter.Generator(parameterParameter);
            var parameterSize = 4u + (uint)returns.Length * 4 + Frame.SIZE;
            foreach (var returnType in this.parameter.returns) parameterSize += returnType.FieldSize;
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Ensure);
            parameter.generator.WriteCode(parameterSize);
            parameter.generator.WriteCode(returnPoint);
            var point = (uint)Frame.SIZE;
            foreach (var variable in parameter.results)
            {
                parameter.generator.WriteCode(CommandMacro.FUNCTION_PushReturnPoint);
                parameter.generator.WriteCode(point);
                parameter.generator.WriteCode(variable);
                point += 4;
            }
            point = PushParameter(point, targetParameter.results[0], parameter.generator);
            foreach (var variable in parameterParameter.results) point = PushParameter(point, variable, parameter.generator);
            if (declaration.code == DeclarationCode.InterfaceFunction) parameter.generator.WriteCode(CommandMacro.FUNCTION_InterfaceCall);
            else parameter.generator.WriteCode(CommandMacro.FUNCTION_MemberCall);
            var function = parameter.relied.Convert(declaration);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(function.definitionIndex);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
            parameter.generator.SetCodeAddress(returnPoint);
            returnPoint.Dispose();
            if (!parameter.command.ignoreExit) parameter.generator.WriteCode(CommandMacro.BASE_ExitJump);
        }
    }
    internal class InvokerVirtualMemberExpression : InvokerExpression
    {
        public readonly Expression target;
        public readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerVirtualMemberExpression(Anchor anchor, Declaration declaration, Expression target, Expression parameter, CompilingType[] returns) : base(anchor, parameter, returns)
        {
            this.target = target;
            this.declaration = declaration;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var returnPoint = new Referencable<CodeAddress>(parameter.pool);
            for (int i = 0; i < returns.Length; i++) parameter.results[i] = parameter.variable.DecareTemporary(parameter.pool, returns[i]);
            var targetParameter = new GeneratorParameter(parameter, 1);
            target.Generator(targetParameter);
            if (target.returns[0].IsHandle)
            {
                parameter.generator.WriteCode(CommandMacro.HANDLE_CheckNull);
                parameter.generator.WriteCode(targetParameter.results[0]);
            }
            var parameterParameter = new GeneratorParameter(parameter, this.parameter.returns.Length);
            this.parameter.Generator(parameterParameter);
            var parameterSize = 4u + (uint)returns.Length * 4 + Frame.SIZE;
            foreach (var returnType in this.parameter.returns) parameterSize += returnType.FieldSize;
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Ensure);
            parameter.generator.WriteCode(parameterSize);
            parameter.generator.WriteCode(returnPoint);
            var point = (uint)Frame.SIZE;
            foreach (var variable in parameter.results)
            {
                parameter.generator.WriteCode(CommandMacro.FUNCTION_PushReturnPoint);
                parameter.generator.WriteCode(point);
                parameter.generator.WriteCode(variable);
                point += 4;
            }
            point = PushParameter(point, targetParameter.results[0], parameter.generator);
            foreach (var variable in parameterParameter.results) point = PushParameter(point, variable, parameter.generator);
            if (declaration.code == DeclarationCode.InterfaceFunction) parameter.generator.WriteCode(CommandMacro.FUNCTION_InterfaceCall);
            else parameter.generator.WriteCode(CommandMacro.FUNCTION_MemberVirtualCall);
            var function = parameter.relied.Convert(declaration);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(function.definitionIndex);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
            parameter.generator.WriteCode(targetParameter.results[0]);
            parameter.generator.SetCodeAddress(returnPoint);
            returnPoint.Dispose();
        }
    }
    internal class InvokerQuestionMemberExpression : InvokerExpression
    {
        public readonly Expression target;
        public readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerQuestionMemberExpression(Anchor anchor, Declaration declaration, Expression target, Expression parameter, CompilingType[] returns) : base(anchor, parameter, returns)
        {
            this.target = target;
            this.declaration = declaration;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var returnPoint = new Referencable<CodeAddress>(parameter.pool);
            for (int i = 0; i < returns.Length; i++) parameter.results[i] = parameter.variable.DecareTemporary(parameter.pool, returns[i]);
            var targetParameter = new GeneratorParameter(parameter, 1);
            target.Generator(targetParameter);
            parameter.generator.WriteCode(CommandMacro.BASE_NullJump);
            parameter.generator.WriteCode(targetParameter.results[0]);
            parameter.generator.WriteCode(returnPoint);
            var parameterParameter = new GeneratorParameter(parameter, this.parameter.returns.Length);
            this.parameter.Generator(parameterParameter);
            var parameterSize = 4u + (uint)returns.Length * 4 + Frame.SIZE;
            foreach (var returnType in this.parameter.returns) parameterSize += returnType.FieldSize;
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Ensure);
            parameter.generator.WriteCode(parameterSize);
            parameter.generator.WriteCode(returnPoint);
            var point = (uint)Frame.SIZE;
            foreach (var variable in parameter.results)
            {
                parameter.generator.WriteCode(CommandMacro.FUNCTION_PushReturnPoint);
                parameter.generator.WriteCode(point);
                parameter.generator.WriteCode(variable);
                point += 4;
            }
            point = PushParameter(point, targetParameter.results[0], parameter.generator);
            foreach (var variable in parameterParameter.results) point = PushParameter(point, variable, parameter.generator);
            if (declaration.code == DeclarationCode.InterfaceFunction) parameter.generator.WriteCode(CommandMacro.FUNCTION_InterfaceCall);
            else parameter.generator.WriteCode(CommandMacro.FUNCTION_MemberVirtualCall);
            var function = parameter.relied.Convert(declaration);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(function.definitionIndex);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
            parameter.generator.WriteCode(targetParameter.results[0]);
            parameter.generator.SetCodeAddress(returnPoint);
            returnPoint.Dispose();
            if (!parameter.command.ignoreExit) parameter.generator.WriteCode(CommandMacro.BASE_ExitJump);
        }
    }
    internal class InvokerConstructorExpression : InvokerExpression
    {
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerConstructorExpression(Anchor anchor, Declaration declaration, Expression parameter, CompilingType type) : base(anchor, parameter, type)
        {
            this.declaration = declaration;
            attribute = TokenAttribute.Value.AddTypeAttribute(type);
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var returnPoint = new Referencable<CodeAddress>(parameter.pool);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(CommandMacro.BASE_CreateObject);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(parameter.relied.Convert(returns[0].definition).RuntimeDefinition);
            var parameterParameter = new GeneratorParameter(parameter, this.parameter.returns.Length);
            this.parameter.Generator(parameterParameter);
            var parameterSize = 4u + Frame.SIZE;
            foreach (var returnType in this.parameter.returns) parameterSize += returnType.FieldSize;
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Ensure);
            parameter.generator.WriteCode(parameterSize);
            parameter.generator.WriteCode(returnPoint);
            var point = (uint)Frame.SIZE;
            point = PushParameter(point, parameter.results[0], parameter.generator);
            foreach (var variable in parameterParameter.results) point = PushParameter(point, variable, parameter.generator);
            parameter.generator.WriteCode(CommandMacro.FUNCTION_MemberCall);
            var function = parameter.relied.Convert(declaration);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(function.definitionIndex);
            parameter.generator.WriteCode(new Function(function.index, function.overloadIndex));
            parameter.generator.SetCodeAddress(returnPoint);
            returnPoint.Dispose();
            if (!parameter.command.ignoreExit) parameter.generator.WriteCode(CommandMacro.BASE_ExitJump);
        }
    }
}
