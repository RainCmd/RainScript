using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class HandleCreateExpression : Expression
    {
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute; 
        public HandleCreateExpression(Anchor anchor, CompilingType type) : base(anchor, type)
        {
            attribute = TokenAttribute.Value.AddTypeAttribute(type);
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
