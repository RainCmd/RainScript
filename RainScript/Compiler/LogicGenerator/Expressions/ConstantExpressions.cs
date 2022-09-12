using RainScript.Vector;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif


namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class ConstantBooleanExpression : Expression
    {
        private readonly bool value;
        public override TokenAttribute Attribute => TokenAttribute.Constant;
        public ConstantBooleanExpression(Anchor anchor, bool value) : base(anchor, RelyKernel.BOOL_TYPE)
        {
            this.value = value;
        }
        public override bool TryEvaluation(out bool value)
        {
            value = this.value;
            return true;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.BOOL_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_1);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(value);
        }
    }
    internal class ConstantIntegerExpression : Expression
    {
        private readonly long value;
        public override TokenAttribute Attribute => TokenAttribute.Constant;
        public ConstantIntegerExpression(Anchor anchor, long value) : base(anchor, RelyKernel.INTEGER_TYPE)
        {
            this.value = value;
        }
        public override bool TryEvaluation(out long value)
        {
            value = this.value;
            return true;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.INTEGER_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_8);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(value);
        }
    }
    internal class ConstantRealExpression : Expression
    {
        private readonly real value;
        public override TokenAttribute Attribute => TokenAttribute.Constant;
        public ConstantRealExpression(Anchor anchor, real value) : base(anchor, RelyKernel.REAL_TYPE)
        {
            this.value = value;
        }
        public override bool TryEvaluation(out real value)
        {
            value = this.value;
            return true;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_8);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(value);
        }
    }
    internal class ConstantReal2Expression : Expression
    {
        private readonly Real2 value;
        public override TokenAttribute Attribute => TokenAttribute.Constant;
        public ConstantReal2Expression(Anchor anchor, Real2 value) : base(anchor, RelyKernel.REAL2_TYPE)
        {
            this.value = value;
        }
        public override bool TryEvaluation(out Real2 value)
        {
            value = this.value;
            return true;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL2_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_16);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(value);
        }
    }
    internal class ConstantReal3Expression : Expression
    {
        private readonly Real3 value;
        public override TokenAttribute Attribute => TokenAttribute.Constant;
        public ConstantReal3Expression(Anchor anchor, Real3 value) : base(anchor, RelyKernel.REAL3_TYPE)
        {
            this.value = value;
        }
        public override bool TryEvaluation(out Real3 value)
        {
            value = this.value;
            return true;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL3_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_24);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(value);
        }
    }
    internal class ConstantReal4Expression : Expression
    {
        private readonly Real4 value;
        public override TokenAttribute Attribute => TokenAttribute.Constant;
        public ConstantReal4Expression(Anchor anchor, Real4 value) : base(anchor, RelyKernel.REAL4_TYPE)
        {
            this.value = value;
        }
        public override bool TryEvaluation(out Real4 value)
        {
            value = this.value;
            return true;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL4_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_32);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(value);
        }
    }
    internal class ConstantStringExpression : Expression
    {
        private readonly string value;
        public override TokenAttribute Attribute => TokenAttribute.Constant | TokenAttribute.Array;
        public ConstantStringExpression(Anchor anchor, string value) : base(anchor, RelyKernel.STRING_TYPE)
        {
            this.value = value;
        }
        public override bool TryEvaluation(out string value)
        {
            value = this.value;
            return true;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.STRING_TYPE);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_String);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(value);
        }
    }
    internal class ConstantNullExpression : Expression
    {
        public override TokenAttribute Attribute => TokenAttribute.Constant;
        public ConstantNullExpression(Anchor anchor) : base(anchor, RelyKernel.NULL_TYPE) { }
        public override bool TryEvaluationNull()
        {
            return true;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.exceptions.Add(anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
        }
    }
    internal class ConstantHandleNullExpression : Expression
    {
        public override TokenAttribute Attribute => TokenAttribute.Constant;
        public ConstantHandleNullExpression(Anchor anchor, CompilingType type) : base(anchor, type) { }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_HandleNull);
            parameter.generator.WriteCode(parameter.results[0]);
        }
    }
    internal class ConstantEntityNullExpression : Expression
    {
        public override TokenAttribute Attribute => TokenAttribute.Constant;
        public ConstantEntityNullExpression(Anchor anchor) : base(anchor, RelyKernel.ENTITY_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_EntityNull);
            parameter.generator.WriteCode(parameter.results[0]);
        }
    }
}
