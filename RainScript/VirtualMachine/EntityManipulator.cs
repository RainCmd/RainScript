using System.Collections.Generic;

namespace RainScript.VirtualMachine
{
    /// <summary>
    /// 实体对象接口
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// 当实体对象被添加到虚拟机时调用
        /// </summary>
        void OnReference();
        /// <summary>
        /// 当实体对象在虚拟机中引用数量归零时调用
        /// </summary>
        void OnRelease();
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
        public int GetEntityCount()
        {
            return map.Count;
        }
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
                    if (top == slots.Length)
                    {
                        var temp = new Slot[top << 1];
                        System.Array.Copy(slots, temp, slots.Length);
                        slots = temp;
                    }
                    entity = top++;
                }
                slots[entity].next = 0;
                slots[entity].value = value;
                slots[entity].reference = 0;
                value.OnReference();
                map.Add(value, entity);
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
            map.Remove(slots[entity.entity].value);
            slots[entity.entity].value.OnRelease();
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
            return entity.entity > 0 && entity.entity < top && slots[entity.entity].value != null;
        }
        internal bool IsEquals(Entity a, Entity b)
        {
            return a.entity == b.entity || (!Valid(a) && !Valid(b));
        }
    }
}
