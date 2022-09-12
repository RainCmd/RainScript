using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class ArrayCreateExpression : Expression
    {
        private readonly Expression length;
        public override TokenAttribute Attribute => TokenAttribute.Value | TokenAttribute.Array;
        public ArrayCreateExpression(Anchor anchor, Expression length, CompilingType type) : base(anchor, type)
        {
            this.length = length;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var lengthParameter = new GeneratorParameter(parameter, 1);
            length.Generator(lengthParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(CommandMacro.BASE_CreateArray);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(new CompilingType(parameter.relied.Convert(returns[0].definition), returns[0].dimension - 1).RuntimeType);
            parameter.generator.WriteCode(lengthParameter.results[0]);
        }
    }
    internal class ArrayEvaluationExpression : VariableExpression
    {
        private readonly Expression array;
        private readonly Expression index;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public ArrayEvaluationExpression(Anchor anchor, Expression array, Expression index, CompilingType elementType) : base(anchor, elementType)
        {
            this.array = array;
            this.index = index;
            attribute = TokenAttribute.Assignable.AddTypeAttribute(elementType) | TokenAttribute.Value;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var arrayParameter = new GeneratorParameter(parameter, 1);
            array.Generator(arrayParameter);
            var indexParameter = new GeneratorParameter(parameter, 1);
            index.Generator(indexParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            if (returns[0] == RelyKernel.BOOL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_1);
            else if (returns[0] == RelyKernel.INTEGER_TYPE || returns[0] == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_8);
            else if (returns[0] == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_16);
            else if (returns[0] == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_16);
            else if (returns[0] == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_16);
            else if (returns[0] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_String);
            else if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_Handle);
            else if (returns[0] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Array2Local_Entity);
            else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(arrayParameter.results[0]);
            parameter.generator.WriteCode(indexParameter.results[0]);
        }
        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            var arrayParameter = new GeneratorParameter(parameter, 1);
            array.Generator(arrayParameter);
            var indexParameter = new GeneratorParameter(parameter, 1);
            index.Generator(indexParameter);
            if (returns[0] == RelyKernel.BOOL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Array_1);
            else if (returns[0] == RelyKernel.INTEGER_TYPE || returns[0] == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Array_8);
            else if (returns[0] == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Array_16);
            else if (returns[0] == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Array_16);
            else if (returns[0] == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Array_16);
            else if (returns[0] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Array_String);
            else if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Array_Handle);
            else if (returns[0] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Array_Entity);
            else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
            parameter.generator.WriteCode(arrayParameter.results[0]);
            parameter.generator.WriteCode(indexParameter.results[0]);
            parameter.generator.WriteCode(parameter.results[0]);
        }
    }
    internal class ArraySubExpression : Expression
    {
        public readonly Expression array;
        public readonly Expression range;
        public override TokenAttribute Attribute => TokenAttribute.Value | TokenAttribute.Array;
        public ArraySubExpression(Anchor anchor, Expression array, Expression range) : base(anchor, array.returns)
        {
            this.array = array;
            this.range = range;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var arrayParameter = new GeneratorParameter(parameter, 1);
            array.Generator(arrayParameter);
            var rangeParameter = new GeneratorParameter(parameter, 2);
            range.Generator(rangeParameter);
            parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            if (array.returns[0] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.STRING_Sub);
            else parameter.generator.WriteCode(CommandMacro.HANDLE_ArrayCut);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(arrayParameter.results[0]);
            parameter.generator.WriteCode(rangeParameter.results[0]);
            parameter.generator.WriteCode(rangeParameter.results[1]);
        }
    }
}
