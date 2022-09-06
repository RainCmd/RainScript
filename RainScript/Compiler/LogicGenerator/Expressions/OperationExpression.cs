namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class OperationExpression : Expression
    {
        private readonly Expression left;
        private readonly Expression right;
        private readonly CommandMacro command;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public OperationExpression(Anchor anchor, CommandMacro command, Expression left, Expression right, CompilingType type) : base(anchor, type)
        {
            this.command = command;
            this.left = left;
            this.right = right;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
}
