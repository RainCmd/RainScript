namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal abstract class OperationExpression : Expression
    {
        private readonly CommandMacro command;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        protected OperationExpression(Anchor anchor, CommandMacro command, CompilingType type) : base(anchor, type)
        {
            this.command = command;
            attribute = TokenAttribute.Value.AddTypeAttribute(type);
        }
    }
    internal class UnaryOperationExpression : OperationExpression
    {
        private readonly Expression expression;
        public UnaryOperationExpression(Anchor anchor, CommandMacro command, Expression expression) : base(anchor, command, expression.returns[0])
        {
            this.expression = expression;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class BinaryOperationExpression : OperationExpression
    {
        private readonly Expression left;
        private readonly Expression right;
        public BinaryOperationExpression(Anchor anchor, CommandMacro command, Expression left, Expression right, CompilingType type) : base(anchor, command, type)
        {
            this.left = left;
            this.right = right;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class OperationPostIncrementExpression : OperationExpression//x++ x--
    {
        private readonly VariableExpression variable;
        public OperationPostIncrementExpression(Anchor anchor, CommandMacro command, VariableExpression variable) : base(anchor, command, variable.returns[0])
        {
            this.variable = variable;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class OperationPrevIncrementExpression : OperationExpression//++x --x
    {
        private readonly VariableExpression variable;
        public OperationPrevIncrementExpression(Anchor anchor, CommandMacro command, VariableExpression variable) : base(anchor, command, variable.returns[0])
        {
            this.variable = variable;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
}
