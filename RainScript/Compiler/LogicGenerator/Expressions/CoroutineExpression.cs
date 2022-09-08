using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class CoroutineCreateExpression : Expression
    {
        private readonly Expression invoker;
        public override TokenAttribute Attribute => TokenAttribute.Coroutine | TokenAttribute.Value;
        public CoroutineCreateExpression(Anchor anchor, Expression invoker, CompilingType type) : base(anchor, type)
        {
            this.invoker = invoker;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class CoroutineEvaluationExpression : Expression
    {
        private readonly Expression source;
        private readonly long[] indices;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public CoroutineEvaluationExpression(Anchor anchor, Expression source, long[] indices, params CompilingType[] returns) : base(anchor, returns)
        {
            this.source = source;
            this.indices = indices;
            attribute = TokenAttribute.Value;
            if (returns.Length == 1) attribute = attribute.AddTypeAttribute(returns[0]);
            else attribute |= TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
