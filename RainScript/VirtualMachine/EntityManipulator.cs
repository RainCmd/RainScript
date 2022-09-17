using System.Collections.Generic;

namespace RainScript.VirtualMachine
{
    /// <summary>
    /// 实体对象接口
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// 当实体对象在虚拟机中引用数量归零时调用
        /// </summary>
        void OnRecycle();
    }
    internal class EntityManipulator
    {
        private struct Slot
        {
            public IEntity value;
            public uint reference;
            public uint next;
        }
        private Slot[] slots;
        private uint top = 1;
        private uint free = 0;
        private readonly Dictionary<IEntity, ulong> map = new Dictionary<IEntity, ulong>();
        public EntityManipulator(int capacity)
        {
            slots = new Slot[capacity];
        }
        public Entity Add(IEntity value)
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
                    entity = top++;
                    if (top >= slots.Length)
                    {
                        var size = 1;
                        while (size < top) size <<= 1;
                        var temp = new Slot[size];
                        System.Array.Copy(slots, temp, slots.Length);
                        slots = temp;
                    }
                    slots[entity].next = 0;
                    slots[entity].value = value;
                    slots[entity].reference = 0;
                }
            }
            return (Entity)entity;
        }
        public IEntity Get(Entity entity)
        {
            if (Valid(entity)) return slots[entity.entity].value;
            else return null;
        }
        private void Remove(Entity entity)
        {
            slots[entity.entity].value.OnRecycle();
            slots[entity.entity].value = null;
            slots[entity.entity].next = free;
            free = (uint)entity.entity;
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
            return entity.entity > 0 && entity.entity < top && slots[entity.entity].next == 0;
        }
        internal bool IsEquals(Entity a, Entity b)
        {
            return a.entity == b.entity || (!Valid(a) && !Valid(b));
        }
    }
}
