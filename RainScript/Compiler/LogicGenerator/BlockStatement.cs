namespace RainScript.Compiler.LogicGenerator
{
    using Expressions;

    internal class BlockStatement : Statement
    {
        public int indent;
        public readonly ScopeList<Statement> statements;
        public BlockStatement(Anchor anchor, CollectionPool pool) : base(anchor)
        {
            statements = pool.GetList<Statement>();
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            parameter.WriteSymbol(anchor);
            foreach (var item in statements) item.Generator(parameter, exitPoint);
        }
        public override void Dispose()
        {
            statements.Dispose();
        }
    }
    internal class BranchStatement : Statement
    {
        private readonly Expression condition;
        public readonly BlockStatement trueBranch, falseBranch;
        public BranchStatement(Anchor anchor, Expression condition, BlockStatement trueBranch, BlockStatement falseBranch) : base(anchor)
        {
            this.condition = condition;
            this.trueBranch = trueBranch;
            this.falseBranch = falseBranch;
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            parameter.WriteSymbol(anchor);
            var endPoint = new Referencable<CodeAddress>(parameter.pool);
            var truePoint = new Referencable<CodeAddress>(parameter.pool);
            parameter.AddBreakpoint(anchor);
            using (new LogicBlockGenerator(parameter, exitPoint))
            {
                var conditionParameter = new Expressions.GeneratorParameter(parameter, 1);
                condition.Generator(conditionParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
                parameter.generator.WriteCode(conditionParameter.results[0]);
            }
            parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
            parameter.generator.WriteCode(truePoint);
            falseBranch.Generator(parameter, exitPoint);
            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
            parameter.generator.WriteCode(endPoint);
            parameter.generator.SetCodeAddress(truePoint);
            trueBranch.Generator(parameter, exitPoint);
            parameter.generator.SetCodeAddress(endPoint);
            endPoint.Dispose();
            truePoint.Dispose();
        }
        public override void Dispose()
        {
            trueBranch.Dispose();
            falseBranch.Dispose();
        }
    }
    internal abstract class LoopStatement : Statement
    {
        public readonly Expression condition;
        public readonly BlockStatement loopBlock, elseBlock;
        public LoopStatement(Anchor anchor, Expression condition, BlockStatement loopBlock, BlockStatement elseBlock) : base(anchor)
        {
            this.condition = condition;
            this.loopBlock = loopBlock;
            this.elseBlock = elseBlock;
        }
        public override void Dispose()
        {
            loopBlock.Dispose();
            elseBlock.Dispose();
        }
    }
    internal class WhileStatement : LoopStatement
    {
        public WhileStatement(Anchor anchor, Expression condition, BlockStatement loopBlock, BlockStatement elseBlock) : base(anchor, condition, loopBlock, elseBlock) { }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            parameter.WriteSymbol(anchor);
            var loopPoint = new Referencable<CodeAddress>(parameter.pool);
            var loopBlockPoint = new Referencable<CodeAddress>(parameter.pool);
            var breakPoint = new Referencable<CodeAddress>(parameter.pool);
            InitJumpTarget(loopBlock, breakPoint, loopPoint);
            parameter.generator.SetCodeAddress(loopPoint);
            if (condition != null)
            {
                parameter.AddBreakpoint(anchor);
                using (new LogicBlockGenerator(parameter, exitPoint))
                {
                    var conditionParameter = new Expressions.GeneratorParameter(parameter, 1);
                    condition.Generator(conditionParameter);
                    parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
                    parameter.generator.WriteCode(conditionParameter.results[0]);
                }
                parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
                parameter.generator.WriteCode(loopBlockPoint);
                elseBlock.Generator(parameter, exitPoint);
                parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                parameter.generator.WriteCode(breakPoint);
            }
            parameter.generator.SetCodeAddress(loopBlockPoint);
            loopBlock.Generator(parameter, exitPoint);
            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
            parameter.generator.WriteCode(loopPoint);
            parameter.generator.SetCodeAddress(breakPoint);
            loopPoint.Dispose();
            loopBlockPoint.Dispose();
            breakPoint.Dispose();
        }
    }
    internal class ForStatement : LoopStatement
    {
        private readonly Expression front;
        private readonly Expression back;
        public ForStatement(Anchor anchor, Expression front, Expression condition, Expression back, BlockStatement loopBlock, BlockStatement elseBlock) : base(anchor, condition, loopBlock, elseBlock)
        {
            this.front = front;
            this.back = back;
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            parameter.WriteSymbol(anchor);
            var loopPoint = new Referencable<CodeAddress>(parameter.pool);
            var loopBlockPoint = new Referencable<CodeAddress>(parameter.pool);
            var breakPoint = new Referencable<CodeAddress>(parameter.pool);
            InitJumpTarget(loopBlock, breakPoint, loopPoint);
            using (new LogicBlockGenerator(parameter, exitPoint))
            {
                var frontParameter = new Expressions.GeneratorParameter(parameter, front.returns.Length);
                front.Generator(frontParameter);
            }
            if (back == null) parameter.generator.SetCodeAddress(loopPoint);
            else using (var conditionPoint = new Referencable<CodeAddress>(parameter.pool))
                {
                    parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                    parameter.generator.WriteCode(conditionPoint);
                    parameter.generator.SetCodeAddress(loopPoint);
                    using (new LogicBlockGenerator(parameter, exitPoint))
                    {
                        var backParameter = new Expressions.GeneratorParameter(parameter, back.returns.Length);
                        back.Generator(backParameter);
                    }
                    parameter.generator.SetCodeAddress(conditionPoint);
                }
            parameter.AddBreakpoint(anchor);
            using (new LogicBlockGenerator(parameter, exitPoint))
            {
                var conditionParameter = new Expressions.GeneratorParameter(parameter, 1);
                condition.Generator(conditionParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
                parameter.generator.WriteCode(conditionParameter.results[0]);
            }
            parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
            parameter.generator.WriteCode(loopBlockPoint);
            elseBlock.Generator(parameter, exitPoint);
            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
            parameter.generator.WriteCode(breakPoint);

            parameter.generator.SetCodeAddress(loopBlockPoint);
            loopBlock.Generator(parameter, exitPoint);
            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
            parameter.generator.WriteCode(loopPoint);
            parameter.generator.SetCodeAddress(breakPoint);
            loopPoint.Dispose();
            loopBlockPoint.Dispose();
            breakPoint.Dispose();
        }
    }
    internal class ElseStatement : Statement
    {
        public readonly BlockStatement statements;
        public ElseStatement(Anchor anchor, BlockStatement statements) : base(anchor)
        {
            this.statements = statements;
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            parameter.WriteSymbol(anchor);
        }
    }
    internal class BreakStatement : JumpStatement
    {
        public BreakStatement(Anchor anchor, Expression condition) : base(anchor, condition) { }
    }
    internal class ContinueStatement : JumpStatement
    {
        public ContinueStatement(Anchor anchor, Expression condition) : base(anchor, condition) { }
    }
    internal class TryStatement : Statement
    {
        public readonly BlockStatement tryBlock;
        public Expression exitcode;
        public BlockStatement catchBlock, finallyBlock;
        private readonly uint localExitCodeIndex, localFinallyTargetIndex;
        private Referencable<CodeAddress> breakPoint, loopPoint;
        public TryStatement(Anchor anchor, BlockStatement tryBlock, LocalContext localContext) : base(anchor)
        {
            this.tryBlock = tryBlock;
            localExitCodeIndex = localContext.AddLocal("$exitcode", anchor, RelyKernel.INTEGER_TYPE).index;
            localFinallyTargetIndex = localContext.AddLocal("$finallytarget", anchor, RelyKernel.INTEGER_TYPE).index;
        }
        public void SetTarget(Referencable<CodeAddress> breakPoint, Referencable<CodeAddress> loopPoint)
        {
            this.breakPoint = breakPoint;
            this.loopPoint = loopPoint;
        }
        private void ClearExitcode(Generator generator, Variable exitcodeVariable)
        {
            generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_8);
            generator.WriteCode(exitcodeVariable);
            generator.WriteCode(0L);
        }
        private void SetFinallyTarget(StatementGeneratorParameter parameter, Referencable<CodeAddress> address)
        {
            parameter.variable.TryGetLocal(localFinallyTargetIndex, out var variable);
            parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_4);
            parameter.generator.WriteCode(variable);
            parameter.generator.WriteCode(address);
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            var breakPoint = new Referencable<CodeAddress>(parameter.pool);
            var loopPoint = new Referencable<CodeAddress>(parameter.pool);
            var finallyPoint = new Referencable<CodeAddress>(parameter.pool);
            var finallyEntryPoint = new Referencable<CodeAddress>(parameter.pool);
            var finallyTargetPoint = new Referencable<CodeAddress>(parameter.pool);
            var exitcodeVariable = parameter.variable.DecareLocal(localExitCodeIndex, RelyKernel.INTEGER_TYPE);
            var finallyTargetVariable = parameter.variable.DecareLocal(localFinallyTargetIndex, RelyKernel.INTEGER_TYPE);
            InitJumpTarget(tryBlock, breakPoint, loopPoint);
            tryBlock.Generator(parameter, finallyPoint);
            SetFinallyTarget(parameter, finallyTargetPoint);
            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
            parameter.generator.WriteCode(finallyEntryPoint);
            if (catchBlock != null)
            {
                parameter.generator.SetCodeAddress(finallyPoint);
                finallyPoint.Dispose();
                parameter.generator.WriteCode(CommandMacro.BASE_PushExitCode);
                parameter.generator.WriteCode(exitcodeVariable);
                using (new LogicBlockGenerator(parameter, finallyEntryPoint))
                {
                    var conditionVariable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.BOOL_TYPE);
                    var zeroVariable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.INTEGER_TYPE);
                    parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_8);
                    parameter.generator.WriteCode(zeroVariable);
                    parameter.generator.WriteCode(0L);
                    parameter.generator.WriteCode(CommandMacro.INTEGER_Equals);
                    parameter.generator.WriteCode(conditionVariable);
                    parameter.generator.WriteCode(exitcodeVariable);
                    parameter.generator.WriteCode(zeroVariable);
                    parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
                    parameter.generator.WriteCode(conditionVariable);
                }
                parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
                parameter.generator.WriteCode(finallyEntryPoint);
                if (exitcode == null)
                {
                    ClearExitcode(parameter.generator, exitcodeVariable);
                    catchBlock.Generator(parameter, finallyEntryPoint);
                    parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                    parameter.generator.WriteCode(finallyEntryPoint);
                }
                else if (exitcode.returns.Length == 1 && exitcode.returns[0] == RelyKernel.INTEGER_TYPE)
                {
                    if (exitcode.Attribute.ContainAll(TokenAttribute.Assignable))
                    {
                        if (exitcode is VariableExpression variableExpression)
                        {
                            var exitcodeParameter = new Expressions.GeneratorParameter(parameter, 1);
                            exitcodeParameter.results[0] = exitcodeVariable;
                            variableExpression.GeneratorAssignment(exitcodeParameter);
                            ClearExitcode(parameter.generator, exitcodeVariable);
                            catchBlock.Generator(parameter, finallyEntryPoint);
                            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                            parameter.generator.WriteCode(finallyEntryPoint);
                        }
                        else parameter.exceptions.Add(exitcode.anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
                    }
                    else if (exitcode.Attribute.ContainAll(TokenAttribute.Value))
                    {
                        using (new LogicBlockGenerator(parameter, finallyEntryPoint))
                        {
                            var conditionVariable = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.BOOL_TYPE);
                            var exitcodeParameter = new Expressions.GeneratorParameter(parameter, 1);
                            exitcode.Generator(exitcodeParameter);
                            parameter.generator.WriteCode(CommandMacro.INTEGER_NotEquals);
                            parameter.generator.WriteCode(conditionVariable);
                            parameter.generator.WriteCode(exitcodeVariable);
                            parameter.generator.WriteCode(exitcodeParameter.results[0]);
                            parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
                            parameter.generator.WriteCode(conditionVariable);
                        }
                        var failPoint = new Referencable<CodeAddress>(parameter.pool);
                        parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
                        parameter.generator.WriteCode(failPoint);
                        ClearExitcode(parameter.generator, exitcodeVariable);
                        catchBlock.Generator(parameter, finallyEntryPoint);
                        parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                        parameter.generator.WriteCode(finallyEntryPoint);
                        parameter.generator.SetCodeAddress(failPoint);
                        failPoint.Dispose();
                    }
                    else parameter.exceptions.Add(exitcode.anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
                }
                else parameter.exceptions.Add(exitcode.anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
                parameter.generator.WriteCode(CommandMacro.BASE_PopExitCode);
                parameter.generator.WriteCode(exitcodeVariable);
            }
            parameter.generator.SetCodeAddress(finallyPoint);
            finallyPoint.Dispose();
            SetFinallyTarget(parameter, exitPoint);
            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
            parameter.generator.WriteCode(finallyEntryPoint);

            parameter.generator.SetCodeAddress(breakPoint);
            breakPoint.Dispose();
            if (this.breakPoint != null) SetFinallyTarget(parameter, this.breakPoint);
            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
            parameter.generator.WriteCode(finallyEntryPoint);

            parameter.generator.SetCodeAddress(loopPoint);
            loopPoint.Dispose();
            if (this.loopPoint != null) SetFinallyTarget(parameter, this.loopPoint);
            parameter.generator.SetCodeAddress(finallyEntryPoint);
            finallyEntryPoint.Dispose();
            if (finallyBlock != null)
            {
                parameter.generator.WriteCode(CommandMacro.BASE_PushExitCode);
                parameter.generator.WriteCode(exitcodeVariable);
                finallyBlock.Generator(parameter, exitPoint);
                parameter.generator.WriteCode(CommandMacro.BASE_PopExitCode);
                parameter.generator.WriteCode(exitcodeVariable);
            }
            parameter.generator.WriteCode(CommandMacro.BASE_JumpStackAddress);
            parameter.generator.WriteCode(finallyTargetVariable);
            parameter.generator.SetCodeAddress(finallyTargetPoint);
            finallyTargetPoint.Dispose();
        }
        public override void Dispose()
        {
            tryBlock.Dispose();
            catchBlock?.Dispose();
            finallyBlock?.Dispose();
        }
    }
}
