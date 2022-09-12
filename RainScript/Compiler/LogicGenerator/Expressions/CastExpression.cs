﻿namespace RainScript.Compiler.LogicGenerator.Expressions
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
                var localParameter = new GeneratorParameter(parameter, 1);
                local.GeneratorAssignment(localParameter);
                parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Handle);
                parameter.generator.WriteCode(localParameter.results[0]);
                parameter.generator.WriteCode(targetParameter.results[0]);
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
