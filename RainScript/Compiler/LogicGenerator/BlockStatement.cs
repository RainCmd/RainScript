namespace RainScript.Compiler.LogicGenerator
{
    using Expressions;
    using RainScript.Compiler.File;
    using System.Reflection.Emit;

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
        protected void InitJumpTarget(BlockStatement block, Referencable<CodeAddress> breakPoint, Referencable<CodeAddress> loopPoint)
        {
            foreach (var statement in block.statements)
            {
                if (statement is BreakStatement breakStatement) breakStatement.SetTarget(breakPoint);
                else if (statement is ContinueStatement continueStatement) continueStatement.SetTarget(loopPoint);
                else if (statement is BranchStatement ifStatement)
                {
                    InitJumpTarget(ifStatement.trueBranch, breakPoint, loopPoint);
                    InitJumpTarget(ifStatement.falseBranch, breakPoint, loopPoint);
                }
                else if (statement is LoopStatement loopStatement) InitJumpTarget(loopStatement.elseBlock, breakPoint, loopPoint);
            }
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
        private readonly Expression[] backs;
        public ForStatement(Anchor anchor, Expression front, Expression condition, Expression[] backs, BlockStatement loopBlock, BlockStatement elseBlock) : base(anchor, condition, loopBlock, elseBlock)
        {
            this.front = front;
            this.backs = backs;
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
            if (backs.Length == 0) parameter.generator.SetCodeAddress(loopPoint);
            else using (var conditionPoint = new Referencable<CodeAddress>(parameter.pool))
                {
                    parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                    parameter.generator.WriteCode(conditionPoint);
                    parameter.generator.SetCodeAddress(loopPoint);
                    foreach (var back in backs)
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
        private readonly uint localIndex;
        public TryStatement(Anchor anchor, BlockStatement tryBlock, LocalContext localContext) : base(anchor)
        {
            this.tryBlock = tryBlock;
            localIndex = localContext.AddLocal("$exitcode", anchor, RelyKernel.INTEGER_TYPE).index;
        }
        private void ClearExitcode(Generator generator, Variable exitcodeVariable)
        {
            generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_8);
            generator.WriteCode(exitcodeVariable);
            generator.WriteCode(0L);
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            var finallyPoint = new Referencable<CodeAddress>(parameter.pool);
            var exitcodeVariable = parameter.variable.DecareLocal(localIndex, RelyKernel.INTEGER_TYPE);
            tryBlock.Generator(parameter, finallyPoint);
            if (catchBlock != null)
            {
                parameter.generator.SetCodeAddress(finallyPoint);
                finallyPoint.Dispose();
                finallyPoint = new Referencable<CodeAddress>(parameter.pool);
                parameter.generator.WriteCode(CommandMacro.BASE_PushExitCode);
                parameter.generator.WriteCode(exitcodeVariable);
                using (new LogicBlockGenerator(parameter, finallyPoint))
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
                parameter.generator.WriteCode(finallyPoint);
                if (exitcode == null)
                {
                    ClearExitcode(parameter.generator, exitcodeVariable);
                    catchBlock.Generator(parameter, finallyPoint);
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
                            catchBlock.Generator(parameter, finallyPoint);
                        }
                        else parameter.exceptions.Add(exitcode.anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
                    }
                    else if (exitcode.Attribute.ContainAll(TokenAttribute.Value))
                    {
                        using (new LogicBlockGenerator(parameter, finallyPoint))
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
                        catchBlock.Generator(parameter, finallyPoint);
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
            if (finallyBlock != null)
            {
                parameter.generator.WriteCode(CommandMacro.BASE_PushExitCode);
                parameter.generator.WriteCode(exitcodeVariable);
                finallyBlock.Generator(parameter, exitPoint);
                parameter.generator.WriteCode(CommandMacro.BASE_PopExitCode);
                parameter.generator.WriteCode(exitcodeVariable);
            }
            parameter.generator.WriteCode(CommandMacro.BASE_ExitJump);
        }
        public override void Dispose()
        {
            tryBlock.Dispose();
            catchBlock?.Dispose();
            finallyBlock?.Dispose();
        }
    }
}
