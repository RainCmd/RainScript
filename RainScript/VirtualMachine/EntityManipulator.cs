using System;
using System.Collections.Generic;

namespace RainScript.VirtualMachine
{
    internal class EntityManipulator
    {
        private struct Slot
        {
            public object value;
            public uint reference;
            public uint next;
        }
        private Slot[] slots;
        private uint top = 1;
        private uint free = 0;
        private readonly Action<object> reference, release;
        private readonly Dictionary<object, ulong> map = new Dictionary<object, ulong>();
        public int GetEntityCount()
        {
            return map.Count;
        }
        public EntityManipulator(KernelParameter parameter)
        {
            slots = new Slot[parameter.entityCapacity];
            reference = parameter.entityReference;
            release = parameter.entityRelease;
        }
        public Entity Add(object value)
        {
            if (value == null) return Entity.NULL;
            if (!map.TryGetValue(value, out var entity))
            {
                if (free > 0)
                {
                    entity = free;
                    free = slots[free].next;
                }
                else
                {
                    if (top == slots.Length)
                    {
                        var temp = new Slot[top << 1];
                        Array.Copy(slots, temp, slots.Length);
                        slots = temp;
                    }
                    entity = top++;
                }
                slots[entity].next = 0;
                slots[entity].value = value;
                slots[entity].reference = 0;
                map.Add(value, entity);
                reference?.Invoke(value);
            }
            return new Entity(entity);
        }
        public object Get(Entity entity)
        {
            if (Valid(entity)) return slots[entity.entity].value;
            else return null;
        }
        private void Remove(Entity entity)
        {
            var value = slots[entity.entity].value;
            map.Remove(value);
            slots[entity.entity].value = null;
            slots[entity.entity].next = free;
            free = (uint)entity.entity;
            release?.Invoke(value);
        }
        public void Reference(Entity entity)
        {
            if (Valid(entity)) slots[entity.entity].reference++;
        }
        public void Release(Entity entity)
        {
            if (Valid(entity))
            {
                slots[entity.entity].reference--;
                if (slots[entity.entity].reference == 0) Remove(entity);
            }
        }
        public bool Valid(Entity entity)
        {
            return entity.entity > 0 && entity.entity < top && slots[entity.entity].value != null;
        }
        internal bool IsEquals(Entity a, Entity b)
        {
            return a.entity == b.entity || (!Valid(a) && !Valid(b));
        }
    }
}
