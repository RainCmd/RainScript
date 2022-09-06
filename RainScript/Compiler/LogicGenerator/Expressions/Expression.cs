using RainScript.Vector;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal struct GeneratorParameter
    {
        public readonly DeclarationManager manager;
        public readonly Generator library;
        public readonly VariableGenerator variable;
        public readonly ExceptionCollector exceptions;
        public readonly CollectionPool pool;
        public readonly Variable[] results;
        public GeneratorParameter(StatementGeneratorParameter parameter, int resultCount)
        {
            manager = parameter.manager;
            library = parameter.library;
            variable = parameter.variable;
            exceptions = parameter.exceptions;
            pool = parameter.pool;
            results = new Variable[resultCount];
            for (int i = 0; i < resultCount; i++) results[i] = Variable.INVALID;
        }
        public GeneratorParameter(GeneratorParameter parameter, int resultCount)
        {
            manager = parameter.manager;
            library = parameter.library;
            variable = parameter.variable;
            exceptions = parameter.exceptions;
            pool = parameter.pool;
            results = new Variable[resultCount];
            for (int i = 0; i < resultCount; i++) results[i] = Variable.INVALID;
        }
        public void CheckResult(Anchor anchor, params CompilingType[] types)
        {
            if (types.Length != results.Length) exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
            else for (var i = 0; i < types.Length; i++)
                    if (!manager.TryGetMeasure(types[i], results[i].type, out _))
                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
        }
    }
    internal abstract class Expression
    {
        public readonly Anchor anchor;
        public readonly CompilingType[] returns;
        public abstract TokenAttribute Attribute { get; }
        public Expression(Anchor anchor,params CompilingType[] returns)
        {
            this.anchor = anchor;
            this.returns = returns;
        }
        public virtual bool TryEvaluation(out bool value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out long value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out real value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out Real2 value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out Real3 value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out Real4 value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluation(out string value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluationNull() { return false; }
        public abstract void Generator(GeneratorParameter parameter);
    }
}
