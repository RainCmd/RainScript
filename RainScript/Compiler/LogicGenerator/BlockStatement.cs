namespace RainScript.Compiler.LogicGenerator
{
    using Expressions;
    internal class BlockStatement : Statement
    {
        public readonly ScopeList<Statement> statements;
        public BlockStatement(Anchor anchor, ScopeList<Statement> statements) : base(anchor)
        {
            this.statements = statements;
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
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
            var endPoint = new Referencable<CodeAddress>(parameter.pool);
            var truePoint = new Referencable<CodeAddress>(parameter.pool);
            using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
            {
                var conditionParameter = new Expressions.GeneratorParameter(parameter, 1);
                condition.Generator(conditionParameter);
                conditionParameter.CheckResult(condition.anchor, new CompilingType(RelyKernel.BOOL, 0));
                parameter.library.WriteCode(CommandMacro.BASE_Flag_1);
                parameter.library.WriteCode(conditionParameter.results[0]);
            }
            parameter.library.WriteCode(CommandMacro.BASE_ConditionJump);
            parameter.library.WriteCode(truePoint);
            falseBranch.Generator(parameter, exitPoint);
            parameter.library.WriteCode(CommandMacro.BASE_Jump);
            parameter.library.WriteCode(endPoint);
            parameter.library.SetCodeAddress(truePoint);
            trueBranch.Generator(parameter, exitPoint);
            parameter.library.SetCodeAddress(endPoint);
            endPoint.Dispose();
            truePoint.Dispose();
        }
        public override void Dispose()
        {
            trueBranch.Dispose();
            falseBranch.Dispose();
        }
    }
    internal class LoopStatement : Statement
    {
        public readonly Expression condition;
        public readonly BlockStatement loopBlock, elseBlock;
        public LoopStatement(Anchor anchor, Expression condition, BlockStatement loopBlock, BlockStatement elseBlock) : base(anchor)
        {
            this.condition = condition;
            this.loopBlock = loopBlock;
            this.elseBlock = elseBlock;
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            var loopPoint = new Referencable<CodeAddress>(parameter.pool);
            var loopBlockPoint = new Referencable<CodeAddress>(parameter.pool);
            var breakPoint = new Referencable<CodeAddress>(parameter.pool);
            InitJumpTarget(loopBlock, breakPoint, loopPoint);
            parameter.library.SetCodeAddress(loopPoint);
            if (condition != null)
            {
                using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
                {
                    var conditionParameter = new Expressions.GeneratorParameter(parameter, 1);
                    condition.Generator(conditionParameter);
                    conditionParameter.CheckResult(condition.anchor, new CompilingType(RelyKernel.BOOL, 0));
                    parameter.library.WriteCode(CommandMacro.BASE_Flag_1);
                    parameter.library.WriteCode(conditionParameter.results[0]);
                }
                parameter.library.WriteCode(CommandMacro.BASE_ConditionJump);
                parameter.library.WriteCode(loopBlockPoint);
                elseBlock.Generator(parameter, exitPoint);
                parameter.library.WriteCode(CommandMacro.BASE_Jump);
                parameter.library.WriteCode(breakPoint);
            }
            parameter.library.SetCodeAddress(loopBlockPoint);
            loopBlock.Generator(parameter, exitPoint);
            parameter.library.WriteCode(CommandMacro.BASE_Jump);
            parameter.library.WriteCode(loopPoint);
            parameter.library.SetCodeAddress(breakPoint);
            loopPoint.Dispose();
            loopBlockPoint.Dispose();
            breakPoint.Dispose();
        }
        private void InitJumpTarget(BlockStatement block, Referencable<CodeAddress> breakPoint, Referencable<CodeAddress> loopPoint)
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
    internal class BreakStatement : JumpStatement
    {
        public BreakStatement(Anchor anchor, Expression condition) : base(anchor, condition) { }
    }
    internal class ContinueStatement : JumpStatement
    {
        public ContinueStatement(Anchor anchor, Expression condition) : base(anchor, condition) { }
    }
}
