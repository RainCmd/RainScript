using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class CreatCoroutineExpression : Expression
    {
        private readonly Expression invoker;
        public override TokenAttribute Attribute => TokenAttribute.Coroutine | TokenAttribute.Temporary;
        public CreatCoroutineExpression(Anchor anchor, Expression invoker, CompilingType type) : base(anchor, type)
        {
            this.invoker = invoker;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
