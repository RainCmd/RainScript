using System;

namespace RainScript.Compiler.LogicGenerator
{
    internal struct CodeAddress
    {
        public readonly uint address;
        public CodeAddress(uint address)
        {
            this.address = address;
        }
    }
    internal class Referencable<T> : IDisposable where T : unmanaged//T必须是4字节大小
    {
        private bool assigned = false;
        private T value;
        private readonly ScopeList<uint> references;
        public T Value { get { return value; } }
        public Referencable(CollectionPool pool)
        {
            references = pool.GetList<uint>();
        }
        internal void AddReference(Generator generator)
        {
            if (assigned) generator.WriteCode(4, value);
            else references.Add(generator.AllocationCode(4));
        }
        public void SetValue(Generator generator, T value)
        {
            if (assigned) throw new Exception("对引用重复赋值");
            else
            {
                this.value = value;
                foreach (var reference in references) generator.WriteCode(value, reference);
                references.Clear();
                assigned = true;
            }
        }
        public void Dispose()
        {
            references.Dispose();
        }
    }
}
