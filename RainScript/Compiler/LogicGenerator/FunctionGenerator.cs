namespace RainScript.Compiler.LogicGenerator
{
    internal class LambdaFunction
    {
        public readonly Declaration declaration;
        public readonly Context context;
        public readonly uint parameterTop = Frame.SIZE;
        public readonly Variable[] parameters;
        public readonly ScopeList<Statement> statements;
        public LambdaFunction(Declaration declaration, Context context, CompilingType[] parameters, ScopeList<Statement> statements)
        {
            this.declaration = declaration;
            this.context = context;
            this.parameters = new Variable[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                this.parameters[i] = new Variable(parameterTop, parameters[i]);
                parameterTop += parameters[i].FieldSize;
            }
            this.statements = statements;
        }
        public void Generate(Generator generator, GeneratorParameter parameter)
        {
            using (var variable = new VariableGenerator(parameter.pool, parameterTop))
            {
                var topValue = new Referencable<uint>(parameter.pool);
                generator.WriteCode(CommandMacro.FUNCTION_Entrance);
                generator.WriteCode(parameterTop);
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
    }
    internal class FunctionGenerator : LambdaFunction
    {
        public FunctionGenerator(Context context, ScopeList<Statement> statements) : base(context, statements)
        {
        }
    }
}
