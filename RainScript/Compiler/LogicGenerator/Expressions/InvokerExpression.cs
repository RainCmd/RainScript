using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class InvokerGlobalExpression : Expression
    {
        private readonly Expression[] parameters;
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerGlobalExpression(Anchor anchor, Declaration declaration, Expression[] parameters, CompilingType[] returns) : base(anchor, returns)
        {
            this.parameters = parameters;
            this.declaration = declaration;
            attribute = TokenAttribute.Temporary;
            if (returns.Length == 1) attribute = attribute.AddTypeAttribute(returns[0]);
            else attribute |= TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class InvokeMemberExpression : Expression
    {
        private readonly Expression target;
        private readonly Expression[] parameters;
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokeMemberExpression(Anchor anchor, Declaration declaration, Expression target, Expression[] parameters, CompilingType[] returns) : base(anchor, returns)
        {
            this.target = target;
            this.parameters = parameters;
            this.declaration = declaration;
            attribute = TokenAttribute.Temporary;
            if (returns.Length == 1) attribute = attribute.AddTypeAttribute(returns[0]);
            else attribute |= TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class InvokerVirtualMemberExpression : Expression
    {
        private readonly Expression target;
        private readonly Expression[] parameters;
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerVirtualMemberExpression(Anchor anchor, Declaration declaration, Expression target, Expression[] parameters, CompilingType[] returns) : base(anchor, returns)
        {
            this.target = target;
            this.parameters = parameters;
            this.declaration = declaration;
            attribute = TokenAttribute.Temporary;
            if (returns.Length == 1) attribute = attribute.AddTypeAttribute(returns[0]);
            else attribute |= TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class InvokerQuestionMemberExpression : Expression
    {
        private readonly Expression target;
        private readonly Expression[] parameters;
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public InvokerQuestionMemberExpression(Anchor anchor, Declaration declaration, Expression target, Expression[] parameters, CompilingType[] returns) : base(anchor, returns)
        {
            this.target = target;
            this.parameters = parameters;
            this.declaration = declaration;
            attribute = TokenAttribute.Temporary;
            if (returns.Length == 1) attribute = attribute.AddTypeAttribute(returns[0]);
            else attribute |= TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class DelegateInvokerExpression : Expression
    {
        public readonly Expression target;
        public readonly Expression[] parameters;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public DelegateInvokerExpression(Anchor anchor, Expression target, Expression[] parameters, CompilingType[] returns) : base(anchor, returns)
        {
            this.target = target;
            this.parameters = parameters;
            attribute = TokenAttribute.Temporary;
            if (returns.Length == 1) attribute = attribute.AddTypeAttribute(returns[0]);
            else attribute |= TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
