using System;

namespace RainScript.Compiler.LogicGenerator
{
    internal class LogicBlockGenerator : IDisposable
    {
        private readonly bool ignoreExit;
        private readonly Generator generator;
        private readonly VariableGenerator variable;
        private readonly Referencable<uint> temporaryAddress;
        private readonly Referencable<CodeAddress> exitPoint;
        private readonly Referencable<CodeAddress> endPoint;
        public LogicBlockGenerator(StatementGeneratorParameter parameter, Anchor anchor, Referencable<CodeAddress> exitPoint)
        {
            ignoreExit = parameter.command.ignoreExit;
            generator = parameter.generator;
            variable = parameter.variable;
            temporaryAddress = new Referencable<uint>(parameter.pool);
            this.exitPoint = exitPoint;
            endPoint = new Referencable<CodeAddress>(parameter.pool);
            parameter.debug.AddBreakpoint(anchor, generator.Point);
            generator.WriteCode(CommandMacro.BASE_Stackzero);
            generator.WriteCode(variable.localTop);
            generator.WriteCode(temporaryAddress);
            generator.WriteCode(CommandMacro.BASE_Finally);
            generator.WriteCode(endPoint);
        }

        public void Dispose()
        {
            generator.SetCodeAddress(endPoint);
            generator.WriteCode(CommandMacro.BASE_Finally);
            generator.WriteCode(exitPoint);
            temporaryAddress.SetValue(generator, variable.GeneratorTemporaryClear(generator));
            if (!ignoreExit) generator.WriteCode(CommandMacro.BASE_ExitJump);
            endPoint.Dispose();
        }
    }
}
