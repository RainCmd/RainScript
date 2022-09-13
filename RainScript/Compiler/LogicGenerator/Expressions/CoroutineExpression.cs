namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class CoroutineCreateExpression : Expression
    {
        private readonly BlurryCoroutineExpression source;
        public override TokenAttribute Attribute => TokenAttribute.Coroutine | TokenAttribute.Value;
        public CoroutineCreateExpression(Anchor anchor, BlurryCoroutineExpression source, CompilingType type) : base(anchor, type)
        {
            this.source = source;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            source.Generator(parameter);
        }
    }
    internal class CoroutineEvaluationExpression : Expression
    {
        private readonly Expression source;
        private readonly long[] indices;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public CoroutineEvaluationExpression(Anchor anchor, Expression source, long[] indices, params CompilingType[] returns) : base(anchor, returns)
        {
            this.source = source;
            this.indices = indices;
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            source.Generator(sourceParameter);
            for (int i = 0; i < parameter.results.Length; i++) parameter.results[i] = parameter.variable.DecareTemporary(parameter.pool, returns[i]);
            parameter.generator.WriteCode(CommandMacro.BASE_GetCoroutineResult);
            parameter.generator.WriteCode(sourceParameter.results[0]);
            parameter.generator.WriteCode(indices.Length);
            for (int i = 0; i < indices.Length; i++)
            {
                if (returns[i].IsHandle) parameter.generator.WriteCode(TypeCode.Handle);
                else parameter.generator.WriteCode(returns[i].definition.code);
                parameter.generator.WriteCode(parameter.results[i]);
                parameter.generator.WriteCode((int)indices[i]);
            }
        }
    }
}
