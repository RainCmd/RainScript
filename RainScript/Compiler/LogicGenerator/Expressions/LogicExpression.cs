namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class LogicAndExpression : Expression
    {
        private readonly Expression left;
        private readonly Expression right;
        public override TokenAttribute Attribute => TokenAttribute.Value;
        public LogicAndExpression(Anchor anchor, Expression left, Expression right) : base(anchor, RelyKernel.BOOL_TYPE)
        {
            this.left = left;
            this.right = right;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var rightAddress = new Referencable<CodeAddress>(parameter.pool);
            var address = new Referencable<CodeAddress>(parameter.pool);
            left.Generator(parameter);
            parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
            parameter.generator.WriteCode(rightAddress);
            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
            parameter.generator.WriteCode(address);
            parameter.generator.SetCodeAddress(rightAddress);
            rightAddress.Dispose();
            right.Generator(parameter);
            parameter.generator.SetCodeAddress(address);
            address.Dispose();
        }
    }
    internal class LogicOrExpression : Expression
    {
        private readonly Expression left;
        private readonly Expression right;
        public override TokenAttribute Attribute => TokenAttribute.Value;
        public LogicOrExpression(Anchor anchor, Expression left, Expression right) : base(anchor, RelyKernel.BOOL_TYPE)
        {
            this.left = left;
            this.right = right;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var address = new Referencable<CodeAddress>(parameter.pool);
            left.Generator(parameter);
            parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
            parameter.generator.WriteCode(address);
            right.Generator(parameter);
            parameter.generator.SetCodeAddress(address);
            address.Dispose();
        }
    }
}
