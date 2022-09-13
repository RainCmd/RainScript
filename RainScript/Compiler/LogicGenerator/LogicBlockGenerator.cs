using System;

namespace RainScript.Compiler.LogicGenerator
{
    internal class LogicBlockGenerator : IDisposable
    {
        private readonly bool ignoreExit;
        private readonly Generator generator;
        private readonly VariableGenerator variable;
        private readonly Referencable<CodeAddress> exitPoint;
        private readonly Referencable<CodeAddress> endPoint;
        public LogicBlockGenerator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            ignoreExit = parameter.command.ignoreExit;
            generator = parameter.generator;
            variable = parameter.variable;
            this.exitPoint = exitPoint;
            endPoint = new Referencable<CodeAddress>(parameter.pool);
            generator.WriteCode(CommandMacro.BASE_Finally);
            generator.WriteCode(endPoint);
        }

        public void Dispose()
        {
            generator.SetCodeAddress(endPoint);
            generator.WriteCode(CommandMacro.BASE_Finally);
            generator.WriteCode(exitPoint);
            variable.GeneratorTemporaryClear(generator);
            if (!ignoreExit) generator.WriteCode(CommandMacro.BASE_ExitJump);
            endPoint.Dispose();
        }
    }
}
