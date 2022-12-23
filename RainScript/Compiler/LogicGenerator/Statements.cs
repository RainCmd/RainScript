namespace RainScript.Compiler.LogicGenerator
{
    using Expressions;
    internal readonly struct StatementGeneratorParameter
    {
        public readonly CompilerCommand command;
        public readonly DeclarationManager manager;
        public readonly ReliedGenerator relied;
        public readonly SymbolTableGenerator symbol;
        public readonly DebugTableGenerator debug;
        public readonly Generator generator;
        public readonly VariableGenerator variable;
        public readonly ExceptionCollector exceptions;
        public readonly CollectionPool pool;
        public StatementGeneratorParameter(GeneratorParameter parameter, Generator generator, VariableGenerator variable)
        {
            command = parameter.command;
            manager = parameter.manager;
            relied = parameter.relied;
            symbol = parameter.symbol;
            debug = parameter.debug;
            this.generator = generator;
            this.variable = variable;
            exceptions = parameter.exceptions;
            pool = parameter.pool;
        }
        public void WriteSymbol(Anchor anchor)
        {
            if (!command.generatorSymbolTable) return;
            if (anchor.textInfo != null && anchor.textInfo.TryGetLineInfo(anchor.start, out var line))
                symbol.WriteLine(generator.Point, (uint)line.number);
        }
        public void AddBreakpoint(Anchor anchor)
        {
            if (!command.generatorDebugTable) return;
            debug.AddBreakpoint(anchor, generator.Point);
            generator.WriteCode(CommandMacro.BREAKPOINT);
        }
    }
    internal abstract class Statement
    {
        public readonly Anchor anchor;
        public Statement(Anchor anchor)
        {
            this.anchor = anchor;
        }
        public abstract void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint);
        public virtual void Dispose() { }
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
                else if (statement is TryStatement tryStatement) tryStatement.SetTarget(breakPoint, loopPoint);
            }
        }
    }
    internal class ExpressionStatement : Statement
    {
        private readonly Expression expression;
        public ExpressionStatement(Expression expression) : base(expression.anchor)
        {
            this.expression = expression;
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            parameter.WriteSymbol(anchor);
            parameter.AddBreakpoint(anchor);
            using (new LogicBlockGenerator(parameter, exitPoint))
            {
                var expressionParameter = new Expressions.GeneratorParameter(parameter, expression.returns.Length);
                expression.Generator(expressionParameter);
            }
        }
    }
    internal class ReturnStatement : Statement
    {
        public readonly Expression expression;
        public ReturnStatement(Anchor anchor, Expression expression) : base(anchor)
        {
            this.expression = expression;
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            parameter.WriteSymbol(anchor);
            parameter.AddBreakpoint(anchor);
            if (expression != null)
                using (new LogicBlockGenerator(parameter, exitPoint))
                {
                    var returnParameter = new Expressions.GeneratorParameter(parameter, expression.returns.Length);
                    expression.Generator(returnParameter);
                    uint returnPoint = Frame.SIZE;
                    foreach (var item in returnParameter.results)
                    {
                        if (item.type.dimension > 0) parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_Handle);
                        else if (item.type == RelyKernel.BOOL_TYPE || item.type == RelyKernel.BYTE_TYPE) parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_1);
                        else if (item.type == RelyKernel.INTEGER_TYPE || item.type == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_8);
                        else if (item.type == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_16);
                        else if (item.type == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_24);
                        else if (item.type == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_32);
                        else if (item.type == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_String);
                        else if (item.type == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_Entity);
                        else parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_Handle);
                        parameter.generator.WriteCode(returnPoint);
                        parameter.generator.WriteCode(item);
                        returnPoint += 4;
                    }
                }
            parameter.generator.WriteCode(CommandMacro.BASE_Jump);
            parameter.generator.WriteCode(exitPoint);
        }
    }
    internal class JumpStatement : Statement
    {
        private readonly Expression condition;
        private Referencable<CodeAddress> target;
        public JumpStatement(Anchor anchor, Expression condition) : base(anchor)
        {
            this.condition = condition;
        }
        public void SetTarget(Referencable<CodeAddress> target)
        {
            this.target = target;
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            parameter.WriteSymbol(anchor);
            parameter.AddBreakpoint(anchor);
            if (condition == null)
            {
                parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                parameter.generator.WriteCode(target);
            }
            else
            {
                using (new LogicBlockGenerator(parameter, exitPoint))
                {
                    var conditionParameter = new Expressions.GeneratorParameter(parameter, 1);
                    condition.Generator(conditionParameter);
                    parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
                    parameter.generator.WriteCode(conditionParameter.results[0]);
                }
                parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
                parameter.generator.WriteCode(target);
            }
        }
    }
    internal class WaitStatement : Statement
    {
        internal class VariableTempExpression : VariableExpression
        {
            private readonly Variable variable;
            public override TokenAttribute Attribute => default;
            public VariableTempExpression(Anchor anchor, Variable variable) : base(anchor, variable.type)
            {
                this.variable = variable;
            }
            public override void Generator(Expressions.GeneratorParameter parameter)
            {
                parameter.results[0] = variable;
            }
            public override void GeneratorAssignment(Expressions.GeneratorParameter parameter)
            {
                throw new System.NotImplementedException();
            }
        }
        private readonly Expression expression;
        public WaitStatement(Anchor anchor, Expression expression) : base(anchor)
        {
            this.expression = expression;
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            parameter.WriteSymbol(anchor);
            parameter.AddBreakpoint(anchor);
            if (expression == null) parameter.generator.WriteCode(CommandMacro.BASE_Wait);
            else using (new LogicBlockGenerator(parameter, exitPoint))
                {
                    if (expression.returns[0] == RelyKernel.BOOL_TYPE)
                    {
                        var loopAddress = new Referencable<CodeAddress>(parameter.pool);
                        var continueAddress = new Referencable<CodeAddress>(parameter.pool);
                        var waitParameter = new Expressions.GeneratorParameter(parameter, 1);
                        parameter.generator.SetCodeAddress(loopAddress);
                        new UnaryOperationExpression(expression.anchor, CommandMacro.BOOL_Not, expression).Generator(waitParameter);
                        parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
                        parameter.generator.WriteCode(waitParameter.results[0]);
                        parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
                        parameter.generator.WriteCode(continueAddress);
                        parameter.generator.WriteCode(CommandMacro.BASE_Wait);
                        parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                        parameter.generator.WriteCode(loopAddress);
                        parameter.generator.SetCodeAddress(continueAddress);
                        loopAddress.Dispose();
                        continueAddress.Dispose();
                    }
                    else if (expression.returns[0] == RelyKernel.INTEGER_TYPE)
                    {
                        var waitParameter = new Expressions.GeneratorParameter(parameter, 1);
                        expression.Generator(waitParameter);
                        parameter.generator.WriteCode(CommandMacro.BASE_WaitFrame);
                        parameter.generator.WriteCode(waitParameter.results[0]);
                    }
                    else
                    {
                        var completeState = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.INTEGER_TYPE);
                        parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_8);
                        parameter.generator.WriteCode(completeState);
#pragma warning disable CS1587 // XML 注释没有放在有效语言元素上
                        parameter.generator.WriteCode(2L);///<see cref="RainScript.VirtualMachine.InvokerState.Completed"/>
#pragma warning restore CS1587 // XML 注释没有放在有效语言元素上

                        var loopAddress = new Referencable<CodeAddress>(parameter.pool);
                        var continueAddress = new Referencable<CodeAddress>(parameter.pool);

                        var coroutineParameter = new Expressions.GeneratorParameter(parameter, 1);
                        expression.Generator(coroutineParameter);
                        parameter.generator.WriteCode(CommandMacro.HANDLE_CheckNull);
                        parameter.generator.WriteCode(coroutineParameter.results[0]);

                        parameter.generator.SetCodeAddress(loopAddress);
                        var getState = RelyKernel.GetMethod(RelyKernel.COROUTINE, "GetState").functions[0];
                        var invokerExpression = new InvokerMemberExpression(expression.anchor, getState.declaration, new VariableTempExpression(expression.anchor, coroutineParameter.results[0]), TupleExpression.Combine(), getState.returns);
                        var invokerParameter = new Expressions.GeneratorParameter(parameter, getState.returns.Length);
                        invokerExpression.Generator(invokerParameter);
                        var waitCondition = parameter.variable.DecareTemporary(parameter.pool, RelyKernel.BOOL_TYPE);
                        parameter.generator.WriteCode(CommandMacro.INTEGER_GraterThanOrEquals);
                        parameter.generator.WriteCode(waitCondition);
                        parameter.generator.WriteCode(invokerParameter.results[0]);
                        parameter.generator.WriteCode(completeState);
                        parameter.generator.WriteCode(CommandMacro.BASE_Flag_1);
                        parameter.generator.WriteCode(waitCondition);
                        parameter.generator.WriteCode(CommandMacro.BASE_ConditionJump);
                        parameter.generator.WriteCode(continueAddress);
                        parameter.generator.WriteCode(CommandMacro.BASE_Wait);
                        parameter.generator.WriteCode(CommandMacro.BASE_Jump);
                        parameter.generator.WriteCode(loopAddress);
                        parameter.generator.SetCodeAddress(continueAddress);
                        loopAddress.Dispose();
                        continueAddress.Dispose();
                    }
                }
        }
    }
    internal class ExitStatement : Statement
    {
        private readonly Expression expression;
        public ExitStatement(Anchor anchor, Expression expression) : base(anchor)
        {
            this.expression = expression;
        }
        public override void Generator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            parameter.WriteSymbol(anchor);
            parameter.AddBreakpoint(anchor);
            using (new LogicBlockGenerator(parameter, exitPoint))
            {
                var exitParameter = new Expressions.GeneratorParameter(parameter, 1);
                expression.Generator(exitParameter);
                parameter.generator.WriteCode(CommandMacro.BASE_Flag_8);
                parameter.generator.WriteCode(exitParameter.results[0]);
            }
            parameter.generator.WriteCode(CommandMacro.BASE_Exit);
        }
    }
}
