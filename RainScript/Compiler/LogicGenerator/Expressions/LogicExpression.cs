using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class LogicAndExpression : Expression
    {
        private readonly Expression left;
        private readonly Expression right;
        public override TokenAttribute Attribute => TokenAttribute.Value;
        public LogicAndExpression(Anchor anchor, Expression left, Expression right) : base(anchor, RelyKernel.BOOL_TYPE)
        {
            this.left = left;
            this.right = right;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class LogicOrExpression : Expression
    {
        private readonly Expression left;
        private readonly Expression right;
        public override TokenAttribute Attribute => TokenAttribute.Value;
        public LogicOrExpression(Anchor anchor, Expression left, Expression right) : base(anchor, RelyKernel.BOOL_TYPE)
        {
            this.left = left;
            this.right = right;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
