using RainScript.Vector;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript.Compiler.LogicGenerator
{
    using Expressions;
    using RainScript.Compiler.Compiling;

    internal class LambdaFunction : System.IDisposable
    {
        private readonly Referencable<CodeAddress> entry;
        private uint returnSize;
        public readonly CompilingType[] parameters;
        public readonly ScopeList<Statement> statements;
        public LambdaFunction(Referencable<CodeAddress> entry, CompilingType[] parameters, CollectionPool pool)
        {
            this.entry = entry;
            this.parameters = parameters;
            statements = pool.GetList<Statement>();
        }
        public void SetReturnCount(uint returnCount)
        {
            returnSize = returnCount * 4;
        }
        public void Generate(GeneratorParameter parameter, Generator generator)
        {
            generator.SetCodeAddress(entry);
            using (var variable = new VariableGenerator(parameter.pool, Frame.SIZE + returnSize))
            {
                var parameterSize = 0u;
                for (uint i = 0; i < parameters.Length; i++)
                {
                    variable.DecareLocal(i, parameters[i]);
                    parameterSize += parameters[i].FieldSize;
                }
                var topValue = new Referencable<uint>(parameter.pool);
                generator.WriteCode(CommandMacro.FUNCTION_Entrance);
                generator.WriteCode(Frame.SIZE + returnSize + parameterSize);
                generator.WriteCode(topValue);
                using (var finallyPoint = new Referencable<CodeAddress>(parameter.pool))
                {
                    foreach (var statement in statements) statement.Generator(new StatementGeneratorParameter(parameter, generator, variable), finallyPoint);
                    generator.SetCodeAddress(finallyPoint);
                }
                topValue.SetValue(generator, variable.Generator(generator));
                topValue.Dispose();
            }
            generator.WriteCode(CommandMacro.FUNCTION_Return);
        }
        public void Dispose()
        {
            foreach (var statement in statements) statement.Dispose();
            statements.Dispose();
        }
    }
    internal class FunctionGenerator : System.IDisposable
    {
        public readonly Context context;
        public readonly CompilingType[] parameters;
        public readonly CompilingType[] returns;
        public readonly ScopeList<Statement> statements;
        public FunctionGenerator(GeneratorParameter parameter, Generator generator)
        {
            parameters = returns = new CompilingType[0];
            statements = parameter.pool.GetList<Statement>();
            foreach (var variable in parameter.manager.library.variables)
                if (variable.constant)
                    using (var lexicals = parameter.pool.GetList<Lexical>())
                        if (Lexical.TryAnalysis(lexicals, variable.expression.exprssion.textInfo, variable.expression.exprssion.Segment, parameter.exceptions))
                        {
                            var context = new Context(variable.space, null, variable.expression.compilings, variable.expression.references);
                            using (var localContext = new LocalContext(parameter.pool))
                            {
                                var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                                if (parser.TryParseTuple(lexicals, out var expressions))
                                {
                                    if (parser.TryAssignmentConvert(expressions, new CompilingType[] { variable.type }, out var result, out _))
                                    {
                                        if (variable.type == RelyKernel.BOOL_TYPE)
                                        {
                                            if (result.TryEvaluation(out bool value)) generator.WriteData(1, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.INTEGER_TYPE)
                                        {
                                            if (result.TryEvaluation(out long value)) generator.WriteData(8, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.REAL_TYPE)
                                        {
                                            if (result.TryEvaluation(out real value)) generator.WriteData(8, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.REAL2_TYPE)
                                        {
                                            if (result.TryEvaluation(out Real2 value)) generator.WriteData(16, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.REAL3_TYPE)
                                        {
                                            if (result.TryEvaluation(out Real3 value)) generator.WriteData(24, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.REAL4_TYPE)
                                        {
                                            if (result.TryEvaluation(out Real4 value)) generator.WriteData(32, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.STRING_TYPE)
                                        {
                                            if (result.TryEvaluation(out string value)) generator.WriteData(value, variable.address, parameter.pool);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type.IsHandle)
                                        {
                                            if (result.TryEvaluationNull()) generator.WriteData(4, 0u, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.ENTITY_TYPE)
                                        {
                                            if (result.TryEvaluationNull()) generator.WriteData(8, Entity.NULL, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else parameter.exceptions.Add(variable.name, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                    }
                                    else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                }
                            }
                        }
            foreach (var variable in parameter.manager.library.variables)
                if (!variable.constant && variable.expression != null)
                    using (var lexicals = parameter.pool.GetList<Lexical>())
                        if (Lexical.TryAnalysis(lexicals, variable.expression.exprssion.textInfo, variable.expression.exprssion.Segment, parameter.exceptions))
                        {
                            var context = new Context(variable.space, null, variable.expression.compilings, variable.expression.references);
                            using (var localContext = new LocalContext(parameter.pool))
                            {
                                var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                                if (parser.TryParseTuple(lexicals, out var expressions))
                                {
                                    if (parser.TryAssignmentConvert(expressions, new CompilingType[] { variable.type }, out var result, out _))
                                        statements.Add(new ExpressionStatement(new VariableAssignmentExpression(variable.name, new VariableGlobalExpression(variable.name, variable.declaration, variable.constant, variable.type), result, variable.type)));
                                    else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                }
                            }
                        }
        }
        public FunctionGenerator(GeneratorParameter parameter, Function function)
        {

        }
        public FunctionGenerator(GeneratorParameter parameter, LogicBody destructor)
        {
        }
        public void Generate(GeneratorParameter parameter, Generator generator)
        {
            using (var variable = new VariableGenerator(parameter.pool, Frame.SIZE + (uint)returns.Length))
            {
                var parameterSize = 0u;
                if (context.definition != null)
                {
                    variable.DecareLocal(0, new CompilingType(new CompilingDefinition(context.definition.Declaration), 0));
                    for (uint i = 0; i < parameters.Length; i++)
                    {
                        variable.DecareLocal(i + 1, parameters[i]);
                        parameterSize += parameters[i].FieldSize;
                    }
                }
                else for (uint i = 0; i < parameters.Length; i++)
                    {
                        variable.DecareLocal(i, parameters[i]);
                        parameterSize += parameters[i].FieldSize;
                    }
                var topValue = new Referencable<uint>(parameter.pool);
                generator.WriteCode(CommandMacro.FUNCTION_Entrance);
                generator.WriteCode(Frame.SIZE + (uint)returns.Length + parameterSize);
                generator.WriteCode(topValue);
                using (var finallyPoint = new Referencable<CodeAddress>(parameter.pool))
                {
                    foreach (var statement in statements) statement.Generator(new StatementGeneratorParameter(parameter, generator, variable), finallyPoint);
                    generator.SetCodeAddress(finallyPoint);
                }
                topValue.SetValue(generator, variable.Generator(generator));
                topValue.Dispose();
            }
            generator.WriteCode(CommandMacro.FUNCTION_Return);
        }
        public void Dispose()
        {
            foreach (var statement in statements) statement.Dispose();
            statements.Dispose();
        }
    }
}
