using RainScript.Vector;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal readonly struct GeneratorParameter
    {
        public readonly CompilerCommand command;
        public readonly DeclarationManager manager;
        public readonly ReliedGenerator relied;
        public readonly DebugTableGenerator debug;
        public readonly Generator generator;
        public readonly VariableGenerator variable;
        public readonly ExceptionCollector exceptions;
        public readonly CollectionPool pool;
        public readonly Variable[] results;
        public GeneratorParameter(StatementGeneratorParameter parameter, int resultCount)
        {
            command = parameter.command;
            manager = parameter.manager;
            relied = parameter.relied;
            debug = parameter.debug;
            generator = parameter.generator;
            variable = parameter.variable;
            exceptions = parameter.exceptions;
            pool = parameter.pool;
            results = new Variable[resultCount];
            for (int i = 0; i < resultCount; i++) results[i] = Variable.INVALID;
        }
        public GeneratorParameter(GeneratorParameter parameter, int resultCount)
        {
            command = parameter.command;
            manager = parameter.manager;
            relied = parameter.relied;
            debug = parameter.debug;
            generator = parameter.generator;
            variable = parameter.variable;
            exceptions = parameter.exceptions;
            pool = parameter.pool;
            results = new Variable[resultCount];
            for (int i = 0; i < resultCount; i++) results[i] = Variable.INVALID;
        }
    }
    internal readonly struct EvaluationParameter
    {
        public readonly Generator generator;
        public readonly DeclarationManager manager;

        public EvaluationParameter(LogicGenerator.GeneratorParameter parameter)
        {
            generator = parameter.generator;
            manager = parameter.manager;
        }

        public EvaluationParameter(Generator generator, DeclarationManager manager)
        {
            this.generator = generator;
            this.manager = manager;
        }
    }
    internal abstract class Expression
    {
        public readonly Anchor anchor;
        public readonly CompilingType[] returns;
        public abstract TokenAttribute Attribute { get; }
        public Expression(Anchor anchor, params CompilingType[] returns)
        {
            this.anchor = anchor;
            this.returns = returns;
        }
        public virtual bool TryEvaluation(out bool value, EvaluationParameter parameter)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out byte value, EvaluationParameter parameter)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out long value, EvaluationParameter parameter)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out real value, EvaluationParameter parameter)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out Real2 value, EvaluationParameter parameter)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out Real3 value, EvaluationParameter parameter)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out Real4 value, EvaluationParameter parameter)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out string value, EvaluationParameter parameter)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluationNull() { return false; }
        public abstract void Generator(GeneratorParameter parameter);
    }
}
