using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class TupleExpression : Expression
    {
        public readonly Expression[] expressions;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        private TupleExpression(Anchor anchor, Expression[] expressions, CompilingType[] returns) : base(anchor, returns)
        {
            this.expressions = expressions;
            attribute = TokenAttribute.Assignable;
            foreach (var item in expressions) attribute &= item.Attribute;
            attribute |= TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var index = 0;
            foreach (var expression in expressions)
            {
                var expressionParameter = new GeneratorParameter(parameter, expression.returns.Length);
                expression.Generator(expressionParameter);
                Array.Copy(expressionParameter.results, 0, parameter.results, index, expressionParameter.results.Length);
                index += expression.returns.Length;
            }
        }
        public void GeneratorAssignment(GeneratorParameter parameter)
        {
            var index = 0;
            foreach (var item in expressions)
            {
                if (item is VariableExpression variable)
                {
                    var itemParameter = new GeneratorParameter(parameter, 1);
                    itemParameter.results[0] = parameter.results[index];
                    variable.GeneratorAssignment(itemParameter);
                    index++;
                }
                else if (item is TupleExpression tuple)
                {
                    var itemParameter = new GeneratorParameter(parameter, tuple.returns.Length);
                    Array.Copy(parameter.results, index, itemParameter.results, 0, itemParameter.results.Length);
                    tuple.GeneratorAssignment(itemParameter);
                    index += tuple.returns.Length;
                }
                else parameter.exceptions.Add(item.anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
            }
        }
        public static TupleExpression Combine(Expression left, Expression right)
        {
            var returns = new CompilingType[left.returns.Length + right.returns.Length];
            Array.Copy(left.returns, returns, left.returns.Length);
            Array.Copy(right.returns, 0, returns, left.returns.Length, right.returns.Length);
            return new TupleExpression(new Anchor(left.anchor.textInfo, left.anchor.start, right.anchor.end), new Expression[] { left, right }, returns);
        }
        public static Expression Combine(params Expression[] expressions)
        {
            if (expressions.Length == 0) return new TupleExpression(default, expressions, new CompilingType[0]);
            if (expressions.Length == 1) return expressions[0];
            var count = 0;
            foreach (var item in expressions)
                count += item.returns.Length;
            var returns = new CompilingType[count];
            count = 0;
            foreach (var item in expressions)
            {
                Array.Copy(item.returns, 0, returns, count, item.returns.Length);
                count += item.returns.Length;
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
            var sourceParameter = new GeneratorParameter(parameter, returns.Length);
            source.Generator(sourceParameter);
            foreach (var index in convers)
            {
                if (returns[index] == RelyKernel.REAL_TYPE)
                {
                    if (source.returns[index] == RelyKernel.INTEGER_TYPE)
                    {
                        var variable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL_TYPE);
                        parameter.generator.WriteCode(CommandMacro.CASTING_I2R);
                        parameter.generator.WriteCode(variable);
                        parameter.generator.WriteCode(sourceParameter.results[index]);
                        sourceParameter.results[index] = variable;
                    }
                    else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH, index.ToString());
                }
                else if (returns[index] == RelyKernel.REAL2_TYPE)
                {
                    if (source.returns[index] == RelyKernel.REAL3_TYPE)
                    {
                        var variable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL2_TYPE);
                        parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_16);
                        parameter.generator.WriteCode(variable);
                        parameter.generator.WriteCode(sourceParameter.results[index]);
                        sourceParameter.results[index] = variable;
                    }
                    else if (source.returns[index] == RelyKernel.REAL4_TYPE)
                    {
                        var variable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL2_TYPE);
                        parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_16);
                        parameter.generator.WriteCode(variable);
                        parameter.generator.WriteCode(sourceParameter.results[index]);
                        sourceParameter.results[index] = variable;
                    }
                    else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH, index.ToString());
                }
                else if (returns[index] == RelyKernel.REAL3_TYPE)
                {
                    if (source.returns[index] == RelyKernel.REAL2_TYPE)
                    {
                        var variable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL3_TYPE);
                        parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_16);
                        parameter.generator.WriteCode(variable);
                        parameter.generator.WriteCode(sourceParameter.results[index]);
                        sourceParameter.results[index] = variable;
                    }
                    else if (source.returns[index] == RelyKernel.REAL4_TYPE)
                    {
                        var variable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL3_TYPE);
                        parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_24);
                        parameter.generator.WriteCode(variable);
                        parameter.generator.WriteCode(sourceParameter.results[index]);
                        sourceParameter.results[index] = variable;
                    }
                    else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH, index.ToString());
                }
                else if (returns[index] == RelyKernel.REAL4_TYPE)
                {
                    if (source.returns[index] == RelyKernel.REAL2_TYPE)
                    {
                        var variable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL4_TYPE);
                        parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_16);
                        parameter.generator.WriteCode(variable);
                        parameter.generator.WriteCode(sourceParameter.results[index]);
                        sourceParameter.results[index] = variable;
                    }
                    else if (source.returns[index] == RelyKernel.REAL3_TYPE)
                    {
                        var variable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.REAL4_TYPE);
                        parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_24);
                        parameter.generator.WriteCode(variable);
                        parameter.generator.WriteCode(sourceParameter.results[index]);
                        sourceParameter.results[index] = variable;
                    }
                    else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH, index.ToString());
                }
                else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH, index.ToString());
            }
            Array.Copy(sourceParameter.results, parameter.results, returns.Length);
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
            if (returns.Length == 1) attribute = TokenAttribute.Value.AddTypeAttribute(returns[0]);
            else attribute = TokenAttribute.Tuple;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var sourceParameter = new GeneratorParameter(parameter, source.returns.Length);
            source.Generator(sourceParameter);
            for (int i = 0; i < parameter.results.Length; i++)
                parameter.results[i] = sourceParameter.results[elementIndices[i]];
        }
    }
    internal class TupleAssignmentExpression : Expression
    {
        private readonly Expression left;
        private readonly Expression right;
        public override TokenAttribute Attribute => TokenAttribute.Tuple;
        public TupleAssignmentExpression(Anchor anchor, Expression left, Expression right, CompilingType[] returns) : base(anchor, returns)
        {
            this.left = left;
            this.right = right;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            if (left is TupleExpression tuple)
            {
                right.Generator(parameter);
                tuple.GeneratorAssignment(parameter);
            }
            else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
        }
    }
}
