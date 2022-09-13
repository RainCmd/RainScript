using System;

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
        private readonly Referencable<uint> localTop;
        public VariableGenerator(CollectionPool pool, uint localAddress)
        {
            this.localAddress = localAddress;
            locals = pool.GetDictionary<uint, Variable>();
            statementTemporaries = pool.GetList<Variable>();
            temporaries = pool.GetList<Variable>();
            localTop = new Referencable<uint>(pool);
        }
        public Variable DecareLocal(uint index, CompilingType type)
        {
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
            var temporary = new Variable(pool, temporaryAddress, type);
            temporaryAddress += type.FieldSize;
            temporaries.Add(temporary);
            statementTemporaries.Add(temporary);
            addressTop = Math.Max(temporaryAddress, addressTop);
            return temporary;
        }
        public void GeneratorTemporaryClear(Generator generator)
        {
            foreach (var temporary in statementTemporaries) ClearVariable(generator, temporary.referencable, temporary.type);
            generator.WriteCode(CommandMacro.BASE_Stackzero);
            generator.WriteCode(localTop);
            generator.WriteCode(temporaryAddress);
            temporaryAddress = 0;
            statementTemporaries.Clear();
        }
        public uint Generator(Generator generator)
        {
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
