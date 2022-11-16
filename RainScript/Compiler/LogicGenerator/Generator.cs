using RainScript.Vector;
using System.Diagnostics;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript.Compiler.LogicGenerator
{
    internal readonly struct GeneratorParameter
    {
        public readonly CompilerCommand command;
        public readonly DeclarationManager manager;
        public readonly ReliedGenerator relied;
        public readonly SymbolTableGenerator symbol;
        public readonly DebugTableGenerator debug;
        public readonly CollectionPool pool;
        public readonly ExceptionCollector exceptions;
        public GeneratorParameter(CompilerCommand command, DeclarationManager manager, ReliedGenerator relied, SymbolTableGenerator symbol, DebugTableGenerator debug, CollectionPool pool, ExceptionCollector exceptions)
        {
            this.command = command;
            this.manager = manager;
            this.relied = relied;
            this.symbol = symbol;
            this.debug = debug;
            this.pool = pool;
            this.exceptions = exceptions;
        }
    }
    internal unsafe class Generator : System.IDisposable
    {
        private bool disposed = false;
        private byte* code;
        private uint codeTop = 0, codeSize = 1024;
        private readonly byte[] data;
        private readonly ScopeList<string> codeStrings;
        private readonly ScopeDictionary<string, ScopeList<uint>> dataStrings;
        public uint Point { get { return codeTop; } }
        public Generator(byte[] data, CollectionPool pool)
        {
            code = Tools.MAlloc((int)codeSize);
            this.data = data;
            codeStrings = pool.GetList<string>();
            dataStrings = pool.GetDictionary<string, ScopeList<uint>>();
        }
        private void EnsureCapacity(uint size)
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
        [Conditional("MEMORY_ALIGNMENT_4")]
        public void MemoryAlignment(uint offset)
        {
            var top = codeTop + offset;
#if MEMORY_ALIGNMENT_4
            top = ((top + Tools.MEMORY_ALIGNMENT) & ~Tools.MEMORY_ALIGNMENT) - top;
#endif
            EnsureCapacity(top);
            while (top-- > 0) code[codeTop++] = (byte)CommandMacro.NoOperation;
        }
        public void WriteCode<T>(uint size, T value) where T : unmanaged
        {
            EnsureCapacity(size);
            *(T*)(code + codeTop) = value;
            codeTop += size;
        }
        public void WriteCode(bool value)
        {
            WriteCode(1, value);
        }
        public void WriteCode(int value)
        {
            WriteCode(4, value);
        }
        public void WriteCode(uint value)
        {
            WriteCode(4, value);
        }
        public void WriteCode(long value)
        {
            WriteCode(8, value);
        }
        public void WriteCode(real value)
        {
            WriteCode(8, value);
        }
        public void WriteCode(Real2 value)
        {
            WriteCode(16, value);
        }
        public void WriteCode(Real3 value)
        {
            WriteCode(24, value);
        }
        public void WriteCode(Real4 value)
        {
            WriteCode(32, value);
        }
        public void WriteCode(TypeDefinition definition)
        {
            WriteCode(9, definition);
        }
        public void WriteCode(Type type)
        {
            WriteCode(13, type);
        }
        public void WriteCode(CommandMacro command)
        {
            WriteCode(1, command);
        }
        public void WriteCode(Function function)
        {
            WriteCode(8, function);
        }
        public void WriteCode(MemberVariable variable)
        {
            WriteCode(8, variable);
        }
        public void WriteCode(FunctionType type)
        {
            WriteCode(1, type);
        }
        public void WriteCode(TypeCode type)
        {
            WriteCode(1, type);
        }
        public void WriteCode<T>(uint size, T value, uint point) where T : unmanaged
        {
            var temp = stackalloc byte[sizeof(T)];
            *(T*)temp = value;
            while (size-- > 0) code[point + size] = temp[size];
        }
        public void WriteCodeAt<T>(T value, uint point) where T : unmanaged
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
            EnsureCapacity(size);
            var point = codeTop;
            codeTop += size;
            return point;
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

        public void GeneratorLibrary(GeneratorParameter parameter, out byte[] codes, out string[] codeStrings, out System.Collections.Generic.Dictionary<string, uint[]> dataStrings)
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

            codes = Tools.P2A(code, codeTop);
            codeStrings = this.codeStrings.ToArray();
            dataStrings = new System.Collections.Generic.Dictionary<string, uint[]>();
            foreach (var item in this.dataStrings) dataStrings.Add(item.Key, item.Value.ToArray());
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
