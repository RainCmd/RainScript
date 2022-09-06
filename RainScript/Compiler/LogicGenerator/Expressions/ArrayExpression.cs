﻿using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
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
            attribute = TokenAttribute.Variable.AddTypeAttribute(elementType);
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }

        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class ArraySubExpression : Expression
    {
        public readonly Expression array;
        public readonly Expression range;
        public override TokenAttribute Attribute => TokenAttribute.Temporary | TokenAttribute.Array;
        public ArraySubExpression(Anchor anchor, Expression array, Expression range) : base(anchor, array.returns)
        {
            this.array = array;
            this.range = range;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
