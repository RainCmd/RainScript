using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class QuestionExpression : Expression
    {
        private readonly Expression condition;
        private readonly Expression left;
        private readonly Expression right;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public QuestionExpression(Anchor anchor, Expression condition, Expression left, Expression right, CompilingType[] returns) : base(anchor, returns)
        {
            this.condition = condition;
            this.left = left;
            this.right = right;
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
