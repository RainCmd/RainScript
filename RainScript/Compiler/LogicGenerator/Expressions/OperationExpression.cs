namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal abstract class OperationExpression : Expression
    {
        protected readonly CommandMacro command;
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
            var expressionParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(expressionParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(command);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(expressionParameter.results[0]);
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
            var leftParameter = new GeneratorParameter(parameter, 1);
            left.Generator(leftParameter);
            var rightParameter = new GeneratorParameter(parameter, 1);
            right.Generator(rightParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(command);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(leftParameter.results[0]);
            parameter.generator.WriteCode(rightParameter.results[0]);
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
            var variableParameter = new GeneratorParameter(parameter, 1);
            variable.Generator(variableParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_8);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(variableParameter.results[0]);
            parameter.generator.WriteCode(command);
            parameter.generator.WriteCode(variableParameter.results[0]);
            if (!(variable is VariableLocalExpression))
                variable.GeneratorAssignment(variableParameter);
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
            variable.Generator(parameter);
            parameter.generator.WriteCode(command);
            parameter.generator.WriteCode(parameter.results[0]);
            if (!(variable is VariableLocalExpression))
                variable.GeneratorAssignment(parameter);
        }
    }
}
