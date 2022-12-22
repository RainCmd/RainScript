
#if FIXED
using RainScript.Vector;
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal abstract class OperationExpression : Expression
    {
        protected readonly CommandMacro command;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        protected OperationExpression(Anchor anchor, CommandMacro command, CompilingType type) : base(anchor, type)
        {
            this.command = command;
            attribute = TokenAttribute.Value.AddTypeAttribute(type);
        }
    }
    internal class UnaryOperationExpression : OperationExpression
    {
        private readonly Expression expression;
        public UnaryOperationExpression(Anchor anchor, CommandMacro command, Expression expression) : base(anchor, command, expression.returns[0])
        {
            this.expression = expression;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var expressionParameter = new GeneratorParameter(parameter, 1);
            expression.Generator(expressionParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(command);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(expressionParameter.results[0]);
        }
        public override bool TryEvaluation(out bool value, EvaluationParameter parameter)
        {
            if (command == CommandMacro.BOOL_Not)
            {
                if (expression.TryEvaluation(out bool result, parameter))
                {
                    value = !result;
                    return true;
                }
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out byte value, EvaluationParameter parameter)
        {
            if (command == CommandMacro.CASTING_I2B)
            {
                if (expression.TryEvaluation(out long result, parameter))
                {
                    value = (byte)result;
                    return true;
                }
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out long value, EvaluationParameter parameter)
        {
            if (command == CommandMacro.INTEGER_Negative)
            {
                if (expression.TryEvaluation(out long result, parameter))
                {
                    value = -result;
                    return true;
                }
            }
            else if (command == CommandMacro.INTEGER_Inverse)
            {
                if (expression.TryEvaluation(out long result, parameter))
                {
                    value = ~result;
                    return true;
                }
            }
            else if (command == CommandMacro.CASTING_R2I)
            {
                if (expression.TryEvaluation(out real result, parameter))
                {
                    value = (long)result;
                    return true;
                }
            }
            else if (command == CommandMacro.CASTING_B2I)
            {
                if (expression.TryEvaluation(out byte result, parameter))
                {
                    value = (long)result;
                    return true;
                }
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out real value, EvaluationParameter parameter)
        {
            if (command == CommandMacro.REAL_Negative)
            {
                if (expression.TryEvaluation(out real result, parameter))
                {
                    value = -result;
                    return true;
                }
            }
            else if (command == CommandMacro.CASTING_I2R)
            {
                if (expression.TryEvaluation(out long result, parameter))
                {
                    value = result;
                    return true;
                }
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out Real2 value, EvaluationParameter parameter)
        {
            if (command == CommandMacro.REAL2_Negative)
            {
                if (expression.TryEvaluation(out Real2 result, parameter))
                {
                    value = -result;
                    return true;
                }
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out Real3 value, EvaluationParameter parameter)
        {
            if (command == CommandMacro.REAL3_Negative)
            {
                if (expression.TryEvaluation(out Real3 result, parameter))
                {
                    value = -result;
                    return true;
                }
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out Real4 value, EvaluationParameter parameter)
        {
            if (command == CommandMacro.REAL4_Negative)
            {
                if (expression.TryEvaluation(out Real4 result, parameter))
                {
                    value = -result;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
    internal class BinaryOperationExpression : OperationExpression
    {
        private readonly Expression left;
        private readonly Expression right;
        public BinaryOperationExpression(Anchor anchor, CommandMacro command, Expression left, Expression right, CompilingType type) : base(anchor, command, type)
        {
            this.left = left;
            this.right = right;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var leftParameter = new GeneratorParameter(parameter, 1);
            left.Generator(leftParameter);
            var rightParameter = new GeneratorParameter(parameter, 1);
            right.Generator(rightParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(command);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(leftParameter.results[0]);
            parameter.generator.WriteCode(rightParameter.results[0]);
        }
        public override bool TryEvaluation(out bool value, EvaluationParameter parameter)
        {
            switch (command)
            {
                case CommandMacro.BOOL_Or:
                    {
                        if (left.TryEvaluation(out bool leftResult, parameter) && right.TryEvaluation(out bool rightResult, parameter))
                        {
                            value = leftResult | rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.BOOL_Xor:
                    {
                        if (left.TryEvaluation(out bool leftResult, parameter) && right.TryEvaluation(out bool rightResult, parameter))
                        {
                            value = leftResult ^ rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.BOOL_And:
                    {
                        if (left.TryEvaluation(out bool leftResult, parameter) && right.TryEvaluation(out bool rightResult, parameter))
                        {
                            value = leftResult & rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.BOOL_Equals:
                    {
                        if (left.TryEvaluation(out bool leftResult, parameter) && right.TryEvaluation(out bool rightResult, parameter))
                        {
                            value = leftResult == rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.BOOL_NotEquals:
                    {
                        if (left.TryEvaluation(out bool leftResult, parameter) && right.TryEvaluation(out bool rightResult, parameter))
                        {
                            value = leftResult != rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_Equals:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult == rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_NotEquals:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult != rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_Grater:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult > rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_GraterThanOrEquals:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult >= rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_Less:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult < rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_LessThanOrEquals:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult <= rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL_Equals:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult == rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL_NotEquals:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult != rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL_Grater:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult > rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL_GraterThanOrEquals:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult >= rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL_Less:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult < rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL_LessThanOrEquals:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult <= rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Equals:
                    {
                        if (left.TryEvaluation(out Real2 leftResult, parameter) && right.TryEvaluation(out Real2 rightResult, parameter))
                        {
                            value = leftResult == rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_NotEquals:
                    {
                        if (left.TryEvaluation(out Real2 leftResult, parameter) && right.TryEvaluation(out Real2 rightResult, parameter))
                        {
                            value = leftResult != rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Equals:
                    {
                        if (left.TryEvaluation(out Real3 leftResult, parameter) && right.TryEvaluation(out Real3 rightResult, parameter))
                        {
                            value = leftResult == rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_NotEquals:
                    {
                        if (left.TryEvaluation(out Real3 leftResult, parameter) && right.TryEvaluation(out Real3 rightResult, parameter))
                        {
                            value = leftResult != rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Equals:
                    {
                        if (left.TryEvaluation(out Real4 leftResult, parameter) && right.TryEvaluation(out Real4 rightResult, parameter))
                        {
                            value = leftResult == rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_NotEquals:
                    {
                        if (left.TryEvaluation(out Real4 leftResult, parameter) && right.TryEvaluation(out Real4 rightResult, parameter))
                        {
                            value = leftResult != rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.STRING_Equals:
                    {
                        if (left.TryEvaluation(out string leftResult, parameter) && right.TryEvaluation(out string rightResult, parameter))
                        {
                            value = leftResult == rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.STRING_NotEquals:
                    {
                        if (left.TryEvaluation(out string leftResult, parameter) && right.TryEvaluation(out string rightResult, parameter))
                        {
                            value = leftResult != rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.HANDLE_Equals:
                    if (left.TryEvaluationNull() && right.TryEvaluationNull())
                    {
                        value = true;
                        return true;
                    }
                    break;
                case CommandMacro.HANDLE_NotEquals:
                    if (left.TryEvaluationNull() && right.TryEvaluationNull())
                    {
                        value = false;
                        return true;
                    }
                    break;
                case CommandMacro.ENTITY_Equals:
                    if (left.TryEvaluationNull() && right.TryEvaluationNull())
                    {
                        value = true;
                        return true;
                    }
                    break;
                case CommandMacro.ENTITY_NotEquals:
                    if (left.TryEvaluationNull() && right.TryEvaluationNull())
                    {
                        value = false;
                        return true;
                    }
                    break;
                case CommandMacro.DELEGATE_Equals:
                    if (left.TryEvaluationNull() && right.TryEvaluationNull())
                    {
                        value = true;
                        return true;
                    }
                    break;
                case CommandMacro.DELEGATE_NotEquals:
                    if (left.TryEvaluationNull() && right.TryEvaluationNull())
                    {
                        value = false;
                        return true;
                    }
                    break;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out long value, EvaluationParameter parameter)
        {
            switch (command)
            {
                case CommandMacro.INTEGER_Plus:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult + rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_Minus:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult - rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_Multiply:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_Divide:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_Mod:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_And:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult & rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_Or:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult | rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.INTEGER_Xor:
                    {
                        if (left.TryEvaluation(out long leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            value = leftResult ^ rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.STRING_Element:
                    {
                        if (left.TryEvaluation(out string leftResult, parameter) && right.TryEvaluation(out long rightResult, parameter))
                        {
                            if (rightResult < 0) rightResult += leftResult.Length;
                            value = leftResult[(int)rightResult];
                            return true;
                        }
                    }
                    break;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out real value, EvaluationParameter parameter)
        {
            switch (command)
            {
                case CommandMacro.REAL_Plus:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult + rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL_Minus:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult - rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL_Multiply:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL_Divide:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL_Mod:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out Real2 value, EvaluationParameter parameter)
        {
            switch (command)
            {
                case CommandMacro.REAL2_Plus:
                    {
                        if (left.TryEvaluation(out Real2 leftResult, parameter) && right.TryEvaluation(out Real2 rightResult, parameter))
                        {
                            value = leftResult + rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Minus:
                    {
                        if (left.TryEvaluation(out Real2 leftResult, parameter) && right.TryEvaluation(out Real2 rightResult, parameter))
                        {
                            value = leftResult - rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Multiply_rv:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out Real2 rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Multiply_vr:
                    {
                        if (left.TryEvaluation(out Real2 leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Multiply_vv:
                    {
                        if (left.TryEvaluation(out Real2 leftResult, parameter) && right.TryEvaluation(out Real2 rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Divide_rv:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out Real2 rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Divide_vr:
                    {
                        if (left.TryEvaluation(out Real2 leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Divide_vv:
                    {
                        if (left.TryEvaluation(out Real2 leftResult, parameter) && right.TryEvaluation(out Real2 rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Mod_rv:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out Real2 rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Mod_vr:
                    {
                        if (left.TryEvaluation(out Real2 leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL2_Mod_vv:
                    {
                        if (left.TryEvaluation(out Real2 leftResult, parameter) && right.TryEvaluation(out Real2 rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out Real3 value, EvaluationParameter parameter)
        {
            switch (command)
            {
                case CommandMacro.REAL3_Plus:
                    {
                        if (left.TryEvaluation(out Real3 leftResult, parameter) && right.TryEvaluation(out Real3 rightResult, parameter))
                        {
                            value = leftResult + rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Minus:
                    {
                        if (left.TryEvaluation(out Real3 leftResult, parameter) && right.TryEvaluation(out Real3 rightResult, parameter))
                        {
                            value = leftResult - rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Multiply_rv:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out Real3 rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Multiply_vr:
                    {
                        if (left.TryEvaluation(out Real3 leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Multiply_vv:
                    {
                        if (left.TryEvaluation(out Real3 leftResult, parameter) && right.TryEvaluation(out Real3 rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Divide_rv:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out Real3 rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Divide_vr:
                    {
                        if (left.TryEvaluation(out Real3 leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Divide_vv:
                    {
                        if (left.TryEvaluation(out Real3 leftResult, parameter) && right.TryEvaluation(out Real3 rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Mod_rv:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out Real3 rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Mod_vr:
                    {
                        if (left.TryEvaluation(out Real3 leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL3_Mod_vv:
                    {
                        if (left.TryEvaluation(out Real3 leftResult, parameter) && right.TryEvaluation(out Real3 rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out Real4 value, EvaluationParameter parameter)
        {
            switch (command)
            {
                case CommandMacro.REAL4_Plus:
                    {
                        if (left.TryEvaluation(out Real4 leftResult, parameter) && right.TryEvaluation(out Real4 rightResult, parameter))
                        {
                            value = leftResult + rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Minus:
                    {
                        if (left.TryEvaluation(out Real4 leftResult, parameter) && right.TryEvaluation(out Real4 rightResult, parameter))
                        {
                            value = leftResult - rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Multiply_rv:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out Real4 rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Multiply_vr:
                    {
                        if (left.TryEvaluation(out Real4 leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Multiply_vv:
                    {
                        if (left.TryEvaluation(out Real4 leftResult, parameter) && right.TryEvaluation(out Real4 rightResult, parameter))
                        {
                            value = leftResult * rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Divide_rv:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out Real4 rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Divide_vr:
                    {
                        if (left.TryEvaluation(out Real4 leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Divide_vv:
                    {
                        if (left.TryEvaluation(out Real4 leftResult, parameter) && right.TryEvaluation(out Real4 rightResult, parameter))
                        {
                            value = leftResult / rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Mod_rv:
                    {
                        if (left.TryEvaluation(out real leftResult, parameter) && right.TryEvaluation(out Real4 rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Mod_vr:
                    {
                        if (left.TryEvaluation(out Real4 leftResult, parameter) && right.TryEvaluation(out real rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
                case CommandMacro.REAL4_Mod_vv:
                    {
                        if (left.TryEvaluation(out Real4 leftResult, parameter) && right.TryEvaluation(out Real4 rightResult, parameter))
                        {
                            value = leftResult % rightResult;
                            return true;
                        }
                    }
                    break;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out string value, EvaluationParameter parameter)
        {
            if (command == CommandMacro.STRING_Combine)
            {
                if (left.TryEvaluation(out string leftResult, parameter) && right.TryEvaluation(out string rightResult, parameter))
                {
                    value = leftResult + rightResult;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
    internal class OperationPostIncrementExpression : OperationExpression//x++ x--
    {
        private readonly VariableExpression variable;
        public OperationPostIncrementExpression(Anchor anchor, CommandMacro command, VariableExpression variable) : base(anchor, command, variable.returns[0])
        {
            this.variable = variable;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var variableParameter = new GeneratorParameter(parameter, 1);
            variable.Generator(variableParameter);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_8);
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(variableParameter.results[0]);
            parameter.generator.WriteCode(command);
            parameter.generator.WriteCode(variableParameter.results[0]);
            if (!(variable is VariableLocalExpression))
                variable.GeneratorAssignment(variableParameter);
        }
    }
    internal class OperationPrevIncrementExpression : OperationExpression//++x --x
    {
        private readonly VariableExpression variable;
        public OperationPrevIncrementExpression(Anchor anchor, CommandMacro command, VariableExpression variable) : base(anchor, command, variable.returns[0])
        {
            this.variable = variable;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            variable.Generator(parameter);
            parameter.generator.WriteCode(command);
            parameter.generator.WriteCode(parameter.results[0]);
            if (!(variable is VariableLocalExpression))
                variable.GeneratorAssignment(parameter);
        }
    }
}
