namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class VectorMemberExpression : VariableExpression
    {
        private readonly Expression target;
        private readonly uint[] indices;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        private VectorMemberExpression(Anchor anchor, Expression target, uint[] indices, CompilingType type) : base(anchor, type)
        {
            this.target = target;
            this.indices = indices;
            attribute = TokenAttribute.Value.AddTypeAttribute(type);
            if (target.Attribute.ContainAny(TokenAttribute.Assignable)) attribute |= TokenAttribute.Assignable;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var targetParameter = new GeneratorParameter(parameter, 1);
            target.Generator(targetParameter);
            for (uint i = 0; i < indices.Length; i++)
            {
                parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Vector);
                parameter.generator.WriteCode(parameter.results[0]);
                parameter.generator.WriteCode(i);
                parameter.generator.WriteCode(targetParameter.results[0]);
                parameter.generator.WriteCode(indices[i]);
            }
        }
        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            var targetParameter = new GeneratorParameter(parameter, 1);
            target.Generator(targetParameter);
            for (uint i = 0; i < indices.Length; i++)
            {
                parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Vector);
                parameter.generator.WriteCode(targetParameter.results[0]);
                parameter.generator.WriteCode(indices[i]);
                parameter.generator.WriteCode(parameter.results[0]);
                parameter.generator.WriteCode(i);
            }
        }
        public static bool TryCreate(Anchor anchor, Expression target, out VectorMemberExpression result)
        {
            if (target.returns.Length == 1 && target.returns[0].dimension == 0 && TryGetVectorType(anchor.Segment.Length, out var vectorType))
            {
                var type = target.returns[0];
                var indices = new uint[anchor.Segment.Length];
                if (type == RelyKernel.REAL2_TYPE)
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        var c = anchor.Segment[i] | (char)0x20;
                        if (c == 'x') indices[i] = 0;
                        else if (c == 'y') indices[i] = 1;
                        else goto fail;
                    }
                }
                else if (type == RelyKernel.REAL3_TYPE)
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        var c = anchor.Segment[i] | (char)0x20;
                        if (c == 'x') indices[i] = 0;
                        else if (c == 'y') indices[i] = 1;
                        else if (c == 'z') indices[i] = 2;
                        else goto fail;
                    }
                }
                else if (type == RelyKernel.REAL4_TYPE)
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        var c = anchor.Segment[i] | (char)0x20;
                        if (c == 'x' || c == 'r') indices[i] = 0;
                        else if (c == 'y' || c == 'g') indices[i] = 1;
                        else if (c == 'z' || c == 'b') indices[i] = 2;
                        else if (c == 'w' || c == 'a') indices[i] = 3;
                        else goto fail;
                    }
                }
                else goto fail;
                result = new VectorMemberExpression(anchor, target, indices, vectorType);
                return true;
            }
        fail:
            result = default;
            return false;
        }
        private static bool TryGetVectorType(int length, out CompilingType type)
        {
            switch (length)
            {
                case 1:
                    type = RelyKernel.REAL_TYPE;
                    return true;
                case 2:
                    type = RelyKernel.REAL2_TYPE;
                    return true;
                case 3:
                    type = RelyKernel.REAL3_TYPE;
                    return true;
                case 4:
                    type = RelyKernel.REAL4_TYPE;
                    return true;
            }
            type = default;
            return false;
        }
    }
    internal class VectorDeconstructionExpression : Expression
    {
        private readonly Expression expression;
        public override TokenAttribute Attribute => TokenAttribute.Tuple;
        public VectorDeconstructionExpression(Anchor anchor, Expression expression, params CompilingType[] returns) : base(anchor, returns)
        {
            this.expression = expression;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            for (int i = 0; i < parameter.results.Length; i++) parameter.results[i] = parameter.variable.DecareTemporary(parameter.pool, returns[i]);
            var expressionParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(expressionParameter);
            for (uint i = 0; i < returns.Length; i++)
            {
                parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Vector);
                parameter.generator.WriteCode(parameter.results[i]);
                parameter.generator.WriteCode(0u);
                parameter.generator.WriteCode(expressionParameter.results[0]);
                parameter.generator.WriteCode(i);
            }
        }
    }
    internal class VectorCreateExpression : Expression
    {
        private readonly Expression parameter;
        public override TokenAttribute Attribute => TokenAttribute.Value;
        public VectorCreateExpression(Anchor anchor, Expression parameter, CompilingType type) : base(anchor, type)
        {
            this.parameter = parameter;
        }
        private uint Generator(Generator generator, Variable result, Variable variable, uint index, uint dimension)
        {
            for (uint i = 0; i < dimension; i++)
            {
                generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Vector);
                generator.WriteCode(result);
                generator.WriteCode(index++);
                generator.WriteCode(variable);
                generator.WriteCode(i);
            }
            return index;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            var parameterParameter = new GeneratorParameter(parameter, this.parameter.returns.Length);
            this.parameter.Generator(parameterParameter);
            var index = 0u;
            foreach (var variable in parameterParameter.results)
            {
                if (variable.type == RelyKernel.REAL_TYPE) index = Generator(parameter.generator, parameter.results[0], variable, index, 1);
                else if (variable.type == RelyKernel.REAL2_TYPE) index = Generator(parameter.generator, parameter.results[0], variable, index, 2);
                else if (variable.type == RelyKernel.REAL3_TYPE) index = Generator(parameter.generator, parameter.results[0], variable, index, 3);
                else if (variable.type == RelyKernel.REAL4_TYPE) index = Generator(parameter.generator, parameter.results[0], variable, index, 4);
                else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
            }
        }
    }
}
