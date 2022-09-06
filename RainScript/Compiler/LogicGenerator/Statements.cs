namespace RainScript.Compiler.LogicGenerator
{
    using Expressions;
    internal struct StatementGeneratorParameter
    {
        public readonly CompilerCommand command;
        public readonly DeclarationManager manager;
        public readonly Generator library;
        public readonly VariableGenerator variable;
        public readonly ExceptionCollector exceptions;
        public readonly CollectionPool pool;
        public StatementGeneratorParameter(GeneratorParameter parameter, Generator library, VariableGenerator variable)
        {
            command = parameter.command;
            manager = parameter.manager;
            this.library = library;
            this.variable = variable;
            exceptions = parameter.exceptions;
            pool = parameter.pool;
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
            using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
            {
                var expressionParameter = new Expressions.GeneratorParameter(parameter, expression.returns.Length);
                expression.Generator(expressionParameter);
                expressionParameter.CheckResult(expression.anchor, expression.returns);
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
            if (expression != null)
                using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
                {
                    var returnParameter = new Expressions.GeneratorParameter(parameter, expression.returns.Length);
                    expression.Generator(returnParameter);
                    uint returnPoint = Frame.SIZE;
                    foreach (var item in returnParameter.results)
                    {
                        if (item.type.dimension > 0) parameter.library.WriteCode(CommandMacro.FUNCTION_ReturnPoint_Handle);
                        else if (item.type.definition == RelyKernel.BOOL) parameter.library.WriteCode(CommandMacro.FUNCTION_ReturnPoint_1);
                        else if (item.type.definition == RelyKernel.INTEGER || item.type.definition == RelyKernel.REAL) parameter.library.WriteCode(CommandMacro.FUNCTION_ReturnPoint_8);
                        else if (item.type.definition == RelyKernel.REAL2) parameter.library.WriteCode(CommandMacro.FUNCTION_ReturnPoint_16);
                        else if (item.type.definition == RelyKernel.REAL3) parameter.library.WriteCode(CommandMacro.FUNCTION_ReturnPoint_24);
                        else if (item.type.definition == RelyKernel.REAL4) parameter.library.WriteCode(CommandMacro.FUNCTION_ReturnPoint_32);
                        else if (item.type.definition == RelyKernel.STRING) parameter.library.WriteCode(CommandMacro.FUNCTION_ReturnPoint_String);
                        else if (item.type.definition == RelyKernel.ENTITY) parameter.library.WriteCode(CommandMacro.FUNCTION_ReturnPoint_Entity);
                        else parameter.library.WriteCode(CommandMacro.FUNCTION_ReturnPoint_Handle);
                        parameter.library.WriteCode(returnPoint);
                        parameter.library.WriteCode(item);
                        returnPoint += 4;
                    }
                }
            parameter.library.WriteCode(CommandMacro.BASE_Jump);
            parameter.library.WriteCode(exitPoint);
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
            if (condition == null)
            {
                parameter.library.WriteCode(CommandMacro.BASE_Jump);
                parameter.library.WriteCode(target);
            }
            else
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
                parameter.library.WriteCode(target);
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
            if (expression == null) parameter.library.WriteCode(CommandMacro.BASE_Wait);
            else using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
                {
                    var waitParameter = new Expressions.GeneratorParameter(parameter, 1);
                    expression.Generator(waitParameter);
                    waitParameter.CheckResult(expression.anchor, new CompilingType(RelyKernel.INTEGER, 0));
                    parameter.library.WriteCode(CommandMacro.BASE_WaitFrame);
                    parameter.library.WriteCode(waitParameter.results[0]);
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
            using (var logicBlockGenerator = new LogicBlockGenerator(parameter, exitPoint))
            {
                var exitParameter = new Expressions.GeneratorParameter(parameter, 1);
                expression.Generator(exitParameter);
                exitParameter.CheckResult(expression.anchor, new CompilingType(RelyKernel.INTEGER, 0));
                parameter.library.WriteCode(CommandMacro.BASE_Flag_8);
                parameter.library.WriteCode(exitParameter.results[0]);
            }
            parameter.library.WriteCode(CommandMacro.BASE_Exit);
        }
    }
}
