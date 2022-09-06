namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class TypeExpression : Expression
    {
        public readonly CompilingType type;
        public override TokenAttribute Attribute => TokenAttribute.Type;
        public TypeExpression(Anchor anchor, CompilingType type) : base(anchor)
        {
            this.type = type;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
}
