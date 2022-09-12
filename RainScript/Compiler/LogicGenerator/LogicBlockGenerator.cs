using System;

namespace RainScript.Compiler.LogicGenerator
{
    internal class LogicBlockGenerator : IDisposable
    {
        private readonly bool ignoreExit;
        private readonly Generator library;
        private readonly VariableGenerator variable;
        private readonly Referencable<CodeAddress> exitPoint;
        private readonly Referencable<CodeAddress> endPoint;
        private readonly uint start, length;
        public LogicBlockGenerator(StatementGeneratorParameter parameter, Referencable<CodeAddress> exitPoint)
        {
            ignoreExit = parameter.command.ignoreExit;
            library = parameter.generator;
            variable = parameter.variable;
            this.exitPoint = exitPoint;
            endPoint = new Referencable<CodeAddress>(parameter.pool);
            start = library.Point;
            library.WriteCode(CommandMacro.BASE_Finally);
            library.WriteCode(endPoint);
            length = library.Point - start;
        }

        public void Dispose()
        {
            var start = library.Point;
            library.SetCodeAddress(endPoint);
            library.WriteCode(CommandMacro.BASE_Finally);
            library.WriteCode(exitPoint);
            var length = library.Point - start;
            if (!variable.GeneratorTemporaryClear(library))
            {
                library.CodeKnockout(start, length);
                library.CodeKnockout(this.start, this.length);
            }
            if (!ignoreExit) library.WriteCode(CommandMacro.BASE_ExitJump);
            endPoint.Dispose();
        }
    }
}
