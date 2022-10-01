namespace RainScript.Compiler
{
    internal class SymbolTableGenerator : System.IDisposable
    {
        private readonly bool enable;
        private readonly ScopeDictionary<string, uint> files;
        private readonly ScopeList<SymbolTable.Function> functions;
        private readonly ScopeList<SymbolTable.Line> lines;
        public SymbolTableGenerator(CompilerCommand command, CollectionPool pool)
        {
            enable = command.generatorSymbolTable;
            if (enable)
            {
                files = pool.GetDictionary<string, uint>();
                functions = pool.GetList<SymbolTable.Function>();
                lines = pool.GetList<SymbolTable.Line>();
            }
        }
        public void WriteFunction(uint point, string file, string function)
        {
            if (!enable) return;
            if (!files.TryGetValue(file, out var fileIndex))
            {
                fileIndex = (uint)files.Count;
                files[file] = fileIndex;
            }
            functions.Add(new SymbolTable.Function(point, fileIndex, function));
        }
        public void WriteLine(uint point, uint line)
        {
            if (lines.Count == 0 || lines[-1].line != line) lines.Add(new SymbolTable.Line(point, line));
        }
        public SymbolTable Generator()
        {
            if (!enable) return null;
            var files = new string[this.files.Count];
            foreach (var item in this.files) files[item.Value] = item.Key;
            return new SymbolTable(files, functions.ToArray(), lines.ToArray());
        }
        public void Dispose()
        {
            if (enable)
            {
                files.Dispose();
                functions.Dispose();
                lines.Dispose();
            }
        }
    }
}
