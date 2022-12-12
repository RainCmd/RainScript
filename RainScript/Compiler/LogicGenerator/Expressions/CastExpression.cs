namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal abstract class CastExpression : Expression
    {
        protected Expression expression;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        protected CastExpression(Anchor anchor, Expression expression, CompilingType type) : base(anchor, type)
        {
            this.expression = expression;
            attribute = TokenAttribute.Value.AddTypeAttribute(type);
            if (expression.Attribute.ContainAny(TokenAttribute.Constant)) attribute |= TokenAttribute.Constant;
        }
    }
    internal class IntegerToByteExpression : CastExpression
    {
        public IntegerToByteExpression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.BYTE_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.BYTE_TYPE);
            parameter.generator.WriteCode(CommandMacro.CASTING_I2B);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class ByteToIntegerExpression : CastExpression
    {
        public ByteToIntegerExpression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.INTEGER_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.INTEGER_TYPE);
            parameter.generator.WriteCode(CommandMacro.CASTING_B2I);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class IntegerToRealExpression : CastExpression
    {
        public IntegerToRealExpression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL_TYPE);
            parameter.generator.WriteCode(CommandMacro.CASTING_I2R);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class RealToIntegerExpression : CastExpression
    {
        public RealToIntegerExpression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.INTEGER_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.INTEGER_TYPE);
            parameter.generator.WriteCode(CommandMacro.CASTING_R2I);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class Real2ToReal3Expression : CastExpression
    {
        public Real2ToReal3Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL3_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL3_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_16);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class Real2ToReal4Expression : CastExpression
    {
        public Real2ToReal4Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL4_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL4_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_16);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class Real3ToReal2Expression : CastExpression
    {
        public Real3ToReal2Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL2_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL2_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_16);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class Real3ToReal4Expression : CastExpression
    {
        public Real3ToReal4Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL4_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL4_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_24);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class Real4ToReal2Expression : CastExpression
    {
        public Real4ToReal2Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL2_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL2_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_16);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class Real4ToReal3Expression : CastExpression
    {
        public Real4ToReal3Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL3_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(sourceParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL3_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_24);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(sourceParameter.results[0]);
        }
    }
    internal class IsExpression : CastExpression
    {
        private readonly VariableLocalExpression local;
        private readonly CompilingType type;
        public IsExpression(Anchor anchor, Expression expression, CompilingType type, VariableLocalExpression local) : base(anchor, expression, RelyKernel.BOOL_TYPE)
        {
            this.local = local;
            this.type = type;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var targetParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(targetParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.BOOL_TYPE);
            parameter.generator.WriteCode(CommandMacro.CASTING_IS);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(targetParameter.results[0]);
            parameter.generator.WriteCode(parameter.relied.Convert(type).RuntimeType);
            if (local != null)
            {
                var endAddress = new Referencable<CodeAddress>(parameter.pool);
                var assignmentAddress = new Referencable<CodeAddress>(parameter.pool);
                parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
                parameter.generator.WriteCode(parameter.results[0]);
                parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
                parameter.generator.WriteCode(assignmentAddress);
                parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                parameter.generator.WriteCode(endAddress);
                parameter.generator.SetCodeAddress(assignmentAddress);
                local.GeneratorAssignment(targetParameter);
                parameter.generator.SetCodeAddress(endAddress);
                assignmentAddress.Dispose();
                endAddress.Dispose();
            }
        }
    }
    internal class AsExpression : CastExpression
    {
        public AsExpression(Anchor anchor, Expression expression, CompilingType type) : base(anchor, expression, type) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var targetParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(targetParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(CommandMacro.CASTING_AS);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(targetParameter.results[0]);
            parameter.generator.WriteCode(parameter.relied.Convert(returns[0]).RuntimeType);
        }
    }
    internal class CastHandleExpression : CastExpression
    {
        public CastHandleExpression(Anchor anchor, Expression expression, CompilingType type) : base(anchor, expression, type) { }
        public override void Generator(GeneratorParameter parameter)
        {
            var targetParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(targetParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(CommandMacro.CASTING);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(targetParameter.results[0]);
            parameter.generator.WriteCode(parameter.relied.Convert(returns[0]).RuntimeType);
        }
    }
}
