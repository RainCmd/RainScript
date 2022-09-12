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
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var address = new Referencable<CodeAddress>(parameter.pool);
            var thenAddress = new Referencable<CodeAddress>(parameter.pool);
            var conditionParameter = new GeneratorParameter(parameter, 1);
            condition.Generator(conditionParameter);
            parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
            parameter.generator.WriteCode(conditionParameter.results[0]);
            parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
            parameter.generator.WriteCode(thenAddress);
            right.Generator(parameter);
            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
            parameter.generator.WriteCode(address);
            parameter.generator.SetCodeAddress(thenAddress);
            thenAddress.Dispose();
            left.Generator(parameter);
            parameter.generator.SetCodeAddress(address);
            address.Dispose();
        }
    }
}
