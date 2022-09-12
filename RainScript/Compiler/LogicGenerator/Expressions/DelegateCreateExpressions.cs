using System;

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
            var type = parameter.relied.Convert(returns[0]).RuntimeType;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(type);
            parameter.generator.WriteCode(FunctionType.Global);
            parameter.generator.WriteCode(lambda.library);
            parameter.generator.WriteCode(new Function(lambda.index, 0));
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
            var type = parameter.relied.Convert(returns[0]).RuntimeType;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(type);
            parameter.generator.WriteCode(FunctionType.Global);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(new Function(function.index, 0));
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
            var type = parameter.relied.Convert(returns[0]).RuntimeType;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(type);
            parameter.generator.WriteCode(FunctionType.Native);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(new Function(function.index, 0));
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
            var sourceParameter = new GeneratorParameter(parameter, 1);
            source.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var function = parameter.relied.Convert(this.function);
            var type = parameter.relied.Convert(returns[0]).RuntimeType;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(type);
            parameter.generator.WriteCode(FunctionType.Member);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(function.definitionIndex);
            parameter.generator.WriteCode(new Function(function.index, 0));
            parameter.generator.WriteCode(sourceParameter.results[0]);
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
            var type = parameter.relied.Convert(returns[0]).RuntimeType;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(type);
            parameter.generator.WriteCode(FunctionType.Virtual);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(function.definitionIndex);
            parameter.generator.WriteCode(new Function(function.index, 0));
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
            var address = new Referencable<CodeAddress>(parameter.pool);
            var sourceParameter = new GeneratorParameter(parameter, 1);
            source.Generator(sourceParameter);
            parameter.generator.WriteCode(CommandMacro.HANDLE_CheckNull);
            parameter.generator.WriteCode(sourceParameter.results[0]);
            parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
            parameter.generator.WriteCode(address);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var function = parameter.relied.Convert(this.function);
            var type = parameter.relied.Convert(returns[0]).RuntimeType;
            parameter.generator.WriteCode(CommandMacro.BASE_CreateDelegate);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(type);
            parameter.generator.WriteCode(FunctionType.Virtual);
            parameter.generator.WriteCode(function.library);
            parameter.generator.WriteCode(function.definitionIndex);
            parameter.generator.WriteCode(new Function(function.index, 0));
            parameter.generator.WriteCode(sourceParameter.results[0]);
            parameter.generator.WriteCode(address);
            address.Dispose();
        }
    }
}
