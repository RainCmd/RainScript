using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class DelegateCreateLambdaFunctionExpression : Expression
    {
        public readonly Declaration function;
        public override TokenAttribute Attribute => TokenAttribute.Callable | TokenAttribute.Value;
        public DelegateCreateLambdaFunctionExpression(Anchor anchor, Declaration function, CompilingType type) : base(anchor, type)
        {
            this.function = function;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}
