using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class TupleExpression : Expression
    {
        public readonly Expression[] expressions;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute { get { return attribute; } }
        private TupleExpression(Anchor anchor, Expression[] expressions, CompilingType[] returns) : base(anchor, returns)
        {
            this.expressions = expressions;
            attribute = TokenAttribute.Variable;
            foreach (var item in expressions) attribute &= item.Attribute;
            if (attribute != TokenAttribute.Variable) attribute = TokenAttribute.Temporary;
            attribute |= TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {

        }
        public static TupleExpression Combine(Expression left, Expression right)
        {
            var returns = new CompilingType[left.returns.Length + right.returns.Length];
            Array.Copy(left.returns, returns, left.returns.Length);
            Array.Copy(right.returns, 0, returns, left.returns.Length, right.returns.Length);
            return new TupleExpression(new Anchor(left.anchor.textInfo, left.anchor.start, right.anchor.end), new Expression[] { left, right }, returns);
        }
        public static TupleExpression Combine(params Expression[] expressions)
        {
            var count = 0;
            foreach (var item in expressions)
                count += item.returns.Length;
            var returns = new CompilingType[count];
            count = 0;
            foreach (var item in expressions)
            {
                Array.Copy(item.returns, 0, returns, count, returns.Length);
                count += returns.Length;
            }
            return new TupleExpression(new Anchor(expressions[0].anchor.textInfo, expressions[0].anchor.start, expressions[expressions.Length - 1].anchor.end), expressions, returns);
        }
    }
    internal class TupleConvertExpression : Expression
    {
        private readonly Expression source;
        private readonly int[] convers;
        public override TokenAttribute Attribute => TokenAttribute.Tuple;
        public TupleConvertExpression(Anchor anchor, Expression source, int[] convers, CompilingType[] returns) : base(anchor, returns)
        {
            this.source = source;
            this.convers = convers;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class TupleEvaluationExpression : Expression
    {
        private readonly Expression source;
        private readonly long[] elementIndices;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public TupleEvaluationExpression(Anchor anchor, Expression source, long[] elementIndices, CompilingType[] returns) : base(anchor, returns)
        {
            this.source = source;
            this.elementIndices = elementIndices;
            if (returns.Length == 1)
            {
                attribute = source.Attribute.ContainAny(TokenAttribute.Variable) ? TokenAttribute.Variable : TokenAttribute.Temporary;
                attribute = attribute.AddTypeAttribute(returns[0]);
            }
            else
            {
                attribute = TokenAttribute.Tuple;
                if (source.Attribute.ContainAny(TokenAttribute.Variable)) attribute |= TokenAttribute.Variable;
            }
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class TupleAssignmentExpression : Expression
    {
        private readonly Expression[] expressions;
        public override TokenAttribute Attribute { get { return TokenAttribute.Tuple; } }
        public TupleAssignmentExpression(Anchor anchor, Expression[] expressions, CompilingType[] returns) : base(anchor, returns)
        {
            this.expressions = expressions;
        }
        public override void Generator(GeneratorParameter parameter)
        {

        }
    }
}
