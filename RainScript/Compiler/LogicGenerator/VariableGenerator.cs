using System;
using System.Diagnostics;

namespace RainScript.Compiler.LogicGenerator
{
    internal struct Variable
    {
        public readonly Referencable<CodeAddress> referencable;
        public readonly uint address;//相对于bottom的地址
        public readonly CompilingType type;
        public Variable(uint address, CompilingType type)
        {
            referencable = null;
            this.address = address;
            this.type = type;
        }
        public Variable(CollectionPool pool, uint offset, CompilingType type)
        {
            referencable = new Referencable<CodeAddress>(pool);
            address = offset;
            this.type = type;
        }
        public void SetAddress(Generator generator, uint address)
        {
            referencable.SetValue(generator, new CodeAddress(this.address + address));
        }
        public static readonly Variable INVALID = new Variable(0, CompilingType.INVALID);
    }
    internal class VariableGenerator : IDisposable
    {
        private uint localAddress, temporaryAddress, addressTop;
        private readonly ScopeDictionary<uint, Variable> locals;
        private readonly ScopeList<Variable> statementTemporaries;
        private readonly ScopeList<Variable> temporaries;
        internal readonly Referencable<uint> localTop;
        public VariableGenerator(CollectionPool pool, uint localAddress)
        {
            this.localAddress = localAddress;
            locals = pool.GetDictionary<uint, Variable>();
            statementTemporaries = pool.GetList<Variable>();
            temporaries = pool.GetList<Variable>();
            localTop = new Referencable<uint>(pool);
        }
        [Conditional("MEMORY_ALIGNMENT_4")]
        private void MemoryAlignment(ref uint address, CompilingType type)
        {
#if MEMORY_ALIGNMENT_4
            if (type == RelyKernel.REAL_TYPE || type == RelyKernel.REAL2_TYPE || type == RelyKernel.REAL3_TYPE || type == RelyKernel.REAL4_TYPE)
                Tools.MemoryAlignment(ref address);
#endif
        }
        public Variable DecareLocal(uint index, CompilingType type)
        {
            MemoryAlignment(ref localAddress, type);
            var local = new Variable(localAddress, type);
            localAddress += type.FieldSize;
            locals.Add(index, local);
            return local;
        }
        public bool TryGetLocal(uint index, out Variable variable)
        {
            return locals.TryGetValue(index, out variable);
        }
        public Variable DecareTemporary(CollectionPool pool, CompilingType type)
        {
            MemoryAlignment(ref temporaryAddress, type);
            var temporary = new Variable(pool, temporaryAddress, type);
            temporaryAddress += type.FieldSize;
            temporaries.Add(temporary);
            statementTemporaries.Add(temporary);
            addressTop = Math.Max(temporaryAddress, addressTop);
            return temporary;
        }
        public uint GeneratorTemporaryClear(Generator generator)
        {
            foreach (var temporary in statementTemporaries) ClearVariable(generator, temporary.referencable, temporary.type);
            var temporaryAddress = this.temporaryAddress;
            this.temporaryAddress = 0;
            statementTemporaries.Clear();
            return temporaryAddress;
        }
        public uint Generator(Generator generator)
        {
            Tools.MemoryAlignment(ref localAddress);
            localTop.SetValue(generator, localAddress);
            foreach (var temporary in temporaries) temporary.SetAddress(generator, localAddress);
            foreach (var item in locals) ClearVariable(generator, item.Value.address, item.Value.type);
            return localAddress + addressTop;
        }
        private void ClearVariable(Generator generator, Referencable<CodeAddress> address, CompilingType type)
        {
            if (type.IsHandle)
            {
                generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_HandleNull);
                generator.WriteCode(address);
            }
            else if (type.definition.code == TypeCode.String)
            {
                generator.WriteCode(CommandMacro.STRING_Release);
                generator.WriteCode(address);
            }
            else if (type.definition.code == TypeCode.Entity)
            {
                generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_EntityNull);
                generator.WriteCode(address);
            }
        }
        private void ClearVariable(Generator generator, uint address, CompilingType type)
        {
            if (type.IsHandle)
            {
                generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_HandleNull);
                generator.WriteCode(address);
            }
            else if (type.definition.code == TypeCode.String)
            {
                generator.WriteCode(CommandMacro.STRING_Release);
                generator.WriteCode(address);
            }
            else if (type.definition.code == TypeCode.Entity)
            {
                generator.WriteCode(CommandMacro.ASSIGNMENT_Const2Local_EntityNull);
                generator.WriteCode(address);
            }
        }
        public void Dispose()
        {
            foreach (var item in temporaries) item.referencable.Dispose();
            locals.Dispose();
            statementTemporaries.Dispose();
            temporaries.Dispose();
            localTop.Dispose();
        }
    }
}
