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
            var leftParameter = new GeneratorParameter(parameter, parameter.results.Length);
            left.Generator(leftParameter);
            for (int i = 0; i < returns.Length; i++)
            {
                if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Handle);
                else if (returns[0] == RelyKernel.BOOL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_1);
                else if (returns[0] == RelyKernel.INTEGER_TYPE || returns[0] == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_8);
                else if (returns[0] == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_16);
                else if (returns[0] == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_24);
                else if (returns[0] == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_32);
                else if (returns[0] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_String);
                else if (returns[0] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Entity);
                else throw ExceptionGeneratorCompiler.Unknown();
                parameter.generator.WriteCode(parameter.results[i]);
                parameter.generator.WriteCode(leftParameter.results[i]);
            }
            parameter.generator.SetCodeAddress(address);
            address.Dispose();
        }
    }
}
