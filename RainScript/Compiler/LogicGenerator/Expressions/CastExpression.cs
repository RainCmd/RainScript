namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal abstract class CastExpression : Expression
    {
        protected Expression expression;
        public override TokenAttribute Attribute => TokenAttribute.Temporary;
        protected CastExpression(Anchor anchor, Expression expression, CompilingType type) : base(anchor, type)
        {
            this.expression = expression;
        }
    }
    internal class IntegerToRealExpression : CastExpression
    {
        public IntegerToRealExpression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class RealToIntegerExpression : CastExpression
    {
        public RealToIntegerExpression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.INTEGER_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class Real2ToReal3Expression : CastExpression
    {
        public Real2ToReal3Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL3_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class Real2ToReal4Expression : CastExpression
    {
        public Real2ToReal4Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL4_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class Real3ToReal2Expression : CastExpression
    {
        public Real3ToReal2Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL2_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class Real3ToReal4Expression : CastExpression
    {
        public Real3ToReal4Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL4_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class Real4ToReal2Expression : CastExpression
    {
        public Real4ToReal2Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL2_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class Real4ToReal3Expression : CastExpression
    {
        public Real4ToReal3Expression(Anchor anchor, Expression expression) : base(anchor, expression, RelyKernel.REAL3_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
        }
    }
    internal class AsExpression : CastExpression
    {
        public AsExpression(Anchor anchor, Expression expression, CompilingType type) : base(anchor, expression, type) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class CastingExpression : CastExpression
    {
        public CastingExpression(Anchor anchor, Expression expression, CompilingType type) : base(anchor, expression, type) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
}
