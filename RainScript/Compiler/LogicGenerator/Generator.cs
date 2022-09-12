namespace RainScript.Compiler.LogicGenerator
{
    internal struct GeneratorParameter
    {
        public readonly CompilerCommand command;
        public readonly DeclarationManager manager;
        public readonly ReliedGenerator relied;
        public readonly CollectionPool pool;
        public readonly ExceptionCollector exceptions;
        public GeneratorParameter(CompilerCommand command, DeclarationManager manager, ReliedGenerator relied, CollectionPool pool, ExceptionCollector exceptions)
        {
            this.command = command;
            this.manager = manager;
            this.relied = relied;
            this.pool = pool;
            this.exceptions = exceptions;
        }
    }
    internal unsafe class Generator : System.IDisposable
    {
        private bool disposed = false;
        private byte* code;
        private uint codeTop = 0, codeSize = 1024;
        private byte[] data;
        private readonly ScopeList<string> codeStrings;
        private readonly ScopeDictionary<string, ScopeList<uint>> dataStrings;
        public uint Point { get { return codeTop; } }
        public Generator(uint dataSize, CollectionPool pool)
        {
            code = Tools.MAlloc((int)codeSize);
            data = new byte[dataSize];
            codeStrings = pool.GetList<string>();
            dataStrings = pool.GetDictionary<string, ScopeList<uint>>();
        }
        private void EnsureCodeCapacity(uint size)
        {
            if (codeTop + size > codeSize)
            {
                while (codeTop + size > codeSize) codeSize <<= 1;
                var nc = Tools.MAlloc((int)codeSize);
                Tools.Copy(code, nc, codeTop);
                Tools.Free(code);
                code = nc;
            }
        }
        public void WriteCode<T>(uint size, T value) where T : unmanaged
        {
            EnsureCodeCapacity(size);
            *(T*)(code + codeTop) = value;
            codeTop += size;
        }
        public void WriteCode<T>(uint size, T value, uint point) where T : unmanaged
        {
            var temp = stackalloc byte[sizeof(T)];
            *(T*)temp = value;
            while (size-- > 0) code[point + size] = temp[size];
        }
        public void WriteCode<T>(T value) where T : unmanaged
        {
            WriteCode((uint)sizeof(T), value);
        }
        public void WriteCode<T>(T value, uint point) where T : unmanaged
        {
            WriteCode((uint)sizeof(T), value, point);
        }
        public void WriteCode<T>(Referencable<T> referencable) where T : unmanaged
        {
            referencable.AddReference(this);
        }
        public void WriteCode(Variable variable)
        {
            if (variable.referencable == null) WriteCode(variable.address);
            else WriteCode(variable.referencable);
        }
        public void WriteCode(string value)
        {
            var index = codeStrings.IndexOf(value);
            if (index < 0)
            {
                index = codeStrings.Count;
                codeStrings.Add(value);
            }
            WriteCode((uint)index);
        }
        public uint AllocationCode(uint size)
        {
            EnsureCodeCapacity(size);
            var point = codeTop;
            codeTop += size;
            return point;
        }
        public void CodeKnockout(uint start, uint length)
        {
            while (start + length < codeTop)
            {
                code[start] = code[start + length];
                start++;
            }
            codeTop = start;
        }
        public void SetCodeAddress(Referencable<CodeAddress> referencable)
        {
            referencable.SetValue(this, new CodeAddress(codeTop));
        }

        public void WriteData<T>(uint size, T value, uint point) where T : unmanaged
        {
            var temp = stackalloc byte[sizeof(T)];
            *(T*)temp = value;
            while (size-- > 0) data[point + size] = temp[size];
        }
        public void WriteData(string value, uint point, CollectionPool pool)
        {
            if (!dataStrings.TryGetValue(value, out var address))
            {
                address = pool.GetList<uint>();
                dataStrings.Add(value, address);
            }
            address.Add(point);
        }

        public void GeneratorLibrary2(GeneratorParameter parameter)
        {
            using (var libraryCtor = new FunctionGenerator(parameter, this)) libraryCtor.Generate(parameter, this);
            for (int i = 0, count = parameter.manager.library.methods.Count; i < count; i++)
            {
                var method = parameter.manager.library.methods[i];
                if (method.Declaration.code != DeclarationCode.Lambda)
                    foreach (var function in method)
                        using (var functionGenerator = new FunctionGenerator(parameter, function))
                        {
                            SetCodeAddress(function.entry);
                            functionGenerator.Generate(parameter, this);
                        }
            }
            foreach (var definition in parameter.manager.library.definitions)
                if ((bool)definition.destructor.body)
                    using (var destructorGenerator = new FunctionGenerator(parameter, definition))
                    {
                        definition.destructorEntry = Point;
                        destructorGenerator.Generate(parameter, this);
                    }
                else definition.destructorEntry = LIBRARY.ENTRY_INVALID;
            foreach (var lambda in parameter.manager.lambdas) lambda.Generate(parameter, this);
        }
        public Library GeneratorLibrary(GeneratorParameter parameter)
        {
            var definitions = new DefinitionInfo[parameter.manager.library.definitions.Count];
            var variables = new VariableInfo[parameter.manager.library.variables.Count];
            var delegates = new FunctionInfo[parameter.manager.library.delegates.Count];
            var coroutines = new CoroutineInfo[parameter.manager.library.coroutines.Count];
            var methods = new MethodInfo[parameter.manager.library.methods.Count];
            var interfaces = new InterfaceInfo[parameter.manager.library.interfaces.Count];
            var natives = new NativeMethodInfo[parameter.manager.library.natives.Count];
            var imports = new ImportLibraryInfo[] { };
            var dataStrings = new System.Collections.Generic.Dictionary<string, uint[]>();
            foreach (var item in this.dataStrings) dataStrings.Add(item.Key, item.Value.ToArray());
            var children = new Space[parameter.manager.library.children.Count];
            var exportDefinitions = new ExportDefinition[] { };
            var exportVariables = new ExportIndex[] { };
            var exportDelegates = new ExportIndex[] { };
            var exportCoroutines = new ExportIndex[] { };
            var exportMethods = new ExportMethod[] { };
            var exportInterfaces = new ExportInterface[] { };
            var exportNatoves = new ExportMethod[] { };

            return new Library(parameter.manager.library.name, Tools.P2A(code, codeTop), data, definitions, variables, delegates, coroutines, methods, interfaces, natives, imports, codeStrings.ToArray(), dataStrings, children, exportDefinitions, exportVariables, exportDelegates, exportCoroutines, exportMethods, exportInterfaces, exportNatoves); ;
        }
        ~Generator()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Tools.Free(code);
            codeStrings.Dispose();
            foreach (var item in dataStrings) item.Value.Dispose();
            dataStrings.Dispose();
        }
    }
}
