namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class VectorMemberExpression : VariableExpression
    {
        private readonly Expression target;
        private readonly long[] indices;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        private VectorMemberExpression(Anchor anchor, Expression target, long[] indices, CompilingType type) : base(anchor, type)
        {
            this.target = target;
            this.indices = indices;
            if (target.Attribute.ContainAny(TokenAttribute.Variable)) attribute = TokenAttribute.Variable.AddTypeAttribute(type);
            else attribute = TokenAttribute.Temporary.AddTypeAttribute(type);
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
        public static bool TryCreate(Anchor anchor, Expression target, out VectorMemberExpression result)
        {
            if (target.returns.Length == 1 && target.returns[0].dimension == 0 && TryGetVectorType(anchor.Segment.Length, out var vectorType))
            {
                var type = target.returns[0];
                var indices = new long[anchor.Segment.Length];
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
}
