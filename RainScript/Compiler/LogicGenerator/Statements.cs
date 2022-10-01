namespace RainScript.Compiler.LogicGenerator
{
    using Expressions;
    internal struct StatementGeneratorParameter
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
            generator.WriteCode(false);
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
            using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
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
                using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
                {
                    var returnParameter = new Expressions.GeneratorParameter(parameter, expression.returns.Length);
                    expression.Generator(returnParameter);
                    uint returnPoint = Frame.SIZE;
                    foreach (var item in returnParameter.results)
                    {
                        if (item.type.dimension > 0) parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_Handle);
                        else if (item.type == RelyKernel.BOOL_TYPE) parameter.generator.WriteCode(CommandMacro.FUNCTION_ReturnPoint_1);
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
                using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
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
            else using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
                {
                    var waitParameter = new Expressions.GeneratorParameter(parameter, 1);
                    expression.Generator(waitParameter);
                    parameter.generator.WriteCode(CommandMacro.BASE_WaitFrame);
                    parameter.generator.WriteCode(waitParameter.results[0]);
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
            using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
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
