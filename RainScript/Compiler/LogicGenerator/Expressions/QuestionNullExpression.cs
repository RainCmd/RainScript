namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class QuestionNullExpression : Expression
    {
        private readonly Expression left;
        private readonly Expression right;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public QuestionNullExpression(Anchor anchor, Expression left, Expression right) : base(anchor, left.returns)
        {
            this.left = left;
            this.right = right;
            attribute = TokenAttribute.Value.AddTypeAttribute(left.returns[0]);
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var address = new Referencable<CodeAddress>(parameter.pool);
            left.Generator(parameter);
            if (left.returns[0] == RelyKernel.ENTITY_TYPE)
            {
                var condition = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.BOOL_TYPE);
                var nullVariable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.ENTITY_TYPE);
                parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_EntityNull);
                parameter.generator.WriteCode(nullVariable);
                parameter.generator.WriteCode(CommandMacro.ENTITY_NotEquals);
                parameter.generator.WriteCode(condition);
                parameter.generator.WriteCode(nullVariable);
                parameter.generator.WriteCode(parameter.results[0]);
                parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
                parameter.generator.WriteCode(condition);
                parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
                parameter.generator.WriteCode(address);
            }
            else
            {
                var nullAddress = new Referencable<CodeAddress>(parameter.pool);
                parameter.generator.WriteCode(CommandMacro.BASE_NullJump);
                parameter.generator.WriteCode(parameter.results[0]);
                parameter.generator.WriteCode(nullAddress);
                parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                parameter.generator.WriteCode(address);
                parameter.generator.SetCodeAddress(nullAddress);
                nullAddress.Dispose();
            }
            var rightParameter = new GeneratorParameter(parameter, returns.Length);
            right.Generator(rightParameter);
            if (left.returns[0] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Entity);
            else parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Handle);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(rightParameter.results[0]);
            parameter.generator.SetCodeAddress(address);
            address.Dispose();
        }
    }
}
