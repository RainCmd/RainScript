using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class InvokerDelegateExpression : Expression
    {
        private readonly Expression invoker;
        private readonly Expression parameter;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerDelegateExpression(Anchor anchor, Expression invoker, Expression parameter, CompilingType[] returns) : base(anchor, returns)
        {
            this.invoker = invoker;
            this.parameter = parameter;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class InvokerQuestionDelegateExpression : Expression
    {
        private readonly Expression invoker;
        private readonly Expression parameter;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerQuestionDelegateExpression(Anchor anchor, Expression invoker, Expression parameter, CompilingType[] returns) : base(anchor, returns)
        {
            this.invoker = invoker;
            this.parameter = parameter;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class InvokerNativeExpression : Expression
    {
        private readonly Declaration declaration;
        private readonly Expression parameter;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerNativeExpression(Anchor anchor, Declaration declaration, Expression parameter, CompilingType[] returns) : base(anchor, returns)
        {
            this.declaration = declaration;
            this.parameter = parameter;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class InvokerGlobalExpression : Expression
    {
        private readonly Declaration declaration;
        private readonly Expression parameter;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerGlobalExpression(Anchor anchor, Declaration declaration, Expression parameter, CompilingType[] returns) : base(anchor, returns)
        {
            this.declaration = declaration;
            this.parameter = parameter;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class InvokerMemberExpression : Expression
    {
        private readonly Expression target;
        private readonly Expression parameter;
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerMemberExpression(Anchor anchor, Declaration declaration, Expression target, Expression parameter, CompilingType[] returns) : base(anchor, returns)
        {
            this.target = target;
            this.parameter = parameter;
            this.declaration = declaration;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class InvokerVirtualMemberExpression : Expression
    {
        private readonly Expression target;
        private readonly Expression parameter;
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerVirtualMemberExpression(Anchor anchor, Declaration declaration, Expression target, Expression parameter, CompilingType[] returns) : base(anchor, returns)
        {
            this.target = target;
            this.parameter = parameter;
            this.declaration = declaration;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class InvokerQuestionMemberExpression : Expression
    {
        private readonly Expression target;
        private readonly Expression parameter;
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerQuestionMemberExpression(Anchor anchor, Declaration declaration, Expression target, Expression parameter, CompilingType[] returns) : base(anchor, returns)
        {
            this.target = target;
            this.parameter = parameter;
            this.declaration = declaration;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class InvokerConstructorExpression : Expression
    {
        private readonly Declaration declaration;
        private readonly Expression parameter;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerConstructorExpression(Anchor anchor, Declaration declaration, Expression parameter, CompilingType type) : base(anchor, type)
        {
            this.declaration = declaration;
            this.parameter = parameter;
            attribute = TokenAttribute.Value.AddTypeAttribute(type);
        }
        public override void Generator(GeneratorParameter parameter)
        {
            //todo 调用之前先自己创建个未初始化对象作为this参数
            throw new NotImplementedException();
        }
    }
}
