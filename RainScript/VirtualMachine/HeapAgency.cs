using System;

namespace RainScript.VirtualMachine
{
    internal unsafe class HeapAgency : IDisposable
    {
        private struct Head
        {
            public uint point, reference, softReference, generation, size, next;
            public Type type;
            public bool flag;
        }
        private readonly Kernel kernel;
        private Head[] heads = new Head[16];
        private byte* heap;
        private uint headTop = 1, free = 0, head = 0, tail = 0, soft = 0, heapTop = 0, heapSize;
        private readonly uint generation;
        private bool flag = false, gc = false;
        public HeapAgency(Kernel kernel, KernelParameter parameter)
        {
            heapSize = parameter.heapCapacity;
            heap = Tools.MAlloc((int)heapSize);
            this.kernel = kernel;
            generation = parameter.generation;
        }
        private void EnsureCapacity(uint size)
        {
            if (heapSize < heapTop + size)
            {
                GC(false);
                if (heapSize < heapTop + size)
                {
                    GC(true);
                    if (heapSize < heapTop + size)
                    {
                        while (heapSize < heapTop + size) heapSize <<= 1;
                        var nhs = Tools.MAlloc((int)heapSize);
                        Tools.Copy(heap, nhs, heapTop);
                        Tools.Free(heap);
                        heap = nhs;
                    }
                }
            }
        }
        public uint AllocArray(Type type, uint length)
        {
            var handle = Alloc(type.FieldSize * length + 4);
            heads[handle].type = new Type(type.definition, type.dimension + 1);
            *(uint*)(heap + heads[handle].point) = length;
            return handle;
        }
        public uint Alloc(TypeDefinition definition)
        {
            uint size;
            if (definition.code == TypeCode.Handle)
            {
                var info = kernel.libraryAgency[definition.library].definitions[definition.index];
                size = info.baseOffset + info.size;
            }
            else size = definition.code.HeapSize();
            var handle = Alloc(size);
            heads[handle].type = new Type(definition, 0);
            return handle;
        }
        private uint Alloc(uint size)
        {
            if (gc) throw ExceptionGeneratorVM.InvalidAllocOperation();
            uint handle;
            if (free > 0)
            {
                handle = free;
                free = heads[handle].next;
            }
            else
            {
                if (headTop == heads.Length)
                {
                    var nhs = new Head[headTop << 1];
                    Array.Copy(heads, nhs, headTop);
                    heads = nhs;
                }
                handle = headTop++;
            }
            EnsureCapacity(size);
            heads[handle].point = heapTop;
            heads[handle].flag = flag;
            heads[handle].type = Type.INVALID;
            heads[handle].size = size;
            heads[handle].next = 0;
            heads[handle].reference = 0;
            heads[handle].softReference = 0;
            heads[handle].generation = 0;
            var point = heapTop;
            heapTop += size;
            while (point < heapTop) heap[point++] = 0;
            if (tail > 0) heads[tail].next = handle;
            else head = soft = handle;
            tail = handle;
            return handle;
        }
        private void Free(uint handle, RuntimeDefinitionInfo definition, uint point)
        {
            if (definition.destructor != null)
            {
                var invoker = kernel.coroutineAgency.Invoker(definition.destructor);
                invoker.SetHeapHandleParameter(0, handle);
                kernel.coroutineAgency.Start(invoker, true, true);
                invoker.Recycle();
            }
            if (definition.parent != TypeDefinition.INVALID)
                Free(handle, kernel.libraryAgency[definition.parent.library].definitions[definition.parent.index], point);
            point += definition.baseOffset;
            foreach (var variable in definition.variables)
            {
                if (variable.type.dimension > 0 || variable.type.definition.code == TypeCode.Handle || variable.type.definition.code == TypeCode.Interface || variable.type.definition.code == TypeCode.Function || variable.type.definition.code == TypeCode.Coroutine) SoftRelease(*(uint*)(heap + point));
                else if (variable.type.definition.code == TypeCode.String) kernel.stringAgency.Release(*(uint*)(heap + point));
                else if (variable.type.definition.code == TypeCode.Entity) kernel.manipulator.Release(*(Entity*)(heap + point));
                point += variable.type.FieldSize;
            }
        }
        private void Free(uint handle)
        {
            var type = heads[handle].type;
            var point = heads[handle].point;
            if (type.dimension == 0)
            {
                switch (type.definition.code)
                {
                    case TypeCode.String:
                        kernel.stringAgency.Release(*(uint*)(heap + point));
                        break;
                    case TypeCode.Handle:
                        Free(handle, kernel.libraryAgency[type.definition.library].definitions[type.definition.index], point);
                        break;
                    case TypeCode.Function:
                        SoftRelease(((RuntimeDelegateInfo*)(heap + point))->target);
                        break;
                    case TypeCode.Coroutine:
                        kernel.coroutineAgency.RecycleInternalInvoker(*(ulong*)(heap + point));
                        break;
                    case TypeCode.Entity:
                        kernel.manipulator.Release(*(Entity*)(heap + point));
                        break;
                }
            }
            else if (type.dimension == 1)
            {
                var size = *(uint*)(heap + point);
                switch (type.definition.code)
                {
                    case TypeCode.String:
                        {
                            var index = (uint*)(heap + point + 4);
                            while (size-- > 0)
                            {
                                kernel.stringAgency.Release(*index);
                                index++;
                            }
                        }
                        break;
                    case TypeCode.Handle:
                    case TypeCode.Interface:
                    case TypeCode.Function:
                    case TypeCode.Coroutine:
                        {
                            var index = (uint*)(heap + point + 4);
                            while (size-- > 0)
                            {
                                SoftRelease(*index);
                                index++;
                            }
                        }
                        break;
                    case TypeCode.Entity:
                        {
                            var index = (Entity*)(head + point + 4);
                            while (size-- > 0)
                            {
                                kernel.manipulator.Release(*index);
                                index++;
                            }
                        }
                        break;
                }
            }
            else
            {
                var size = *(uint*)(heap + point);
                var index = (uint*)(heap + point + 4);
                while (size-- > 0)
                {
                    SoftRelease(*index);
                    index++;
                }
            }
        }
        private void Mark(uint handle)
        {
            var head = heads[handle];
            if (head.flag != flag)
            {
                heads[handle].flag = flag;
                var type = head.type;
                if (type.dimension > 0)
                {
                    if (type.dimension > 1 || type.definition.code == TypeCode.Handle || type.definition.code == TypeCode.Function || type.definition.code == TypeCode.Coroutine)
                    {
                        var size = *(uint*)(heap + head.point);
                        var point = (uint*)(heap + head.point + 4);
                        while (size-- > 0) if (IsVaild(point[size])) Mark(point[size]);
                    }
                }
                else if (type.definition.code == TypeCode.Handle)
                {
                    var definition = kernel.libraryAgency[type.definition.library].definitions[type.definition.index];
                    while (definition != null)
                    {
                        var point = head.point + definition.baseOffset;
                        foreach (var variable in definition.variables)
                        {
                            if (variable.type.dimension > 0 || variable.type.definition.code == TypeCode.Handle || variable.type.definition.code == TypeCode.Interface || variable.type.definition.code == TypeCode.Function || variable.type.definition.code == TypeCode.Coroutine) Mark(*(uint*)(heap + point));
                            point += variable.type.FieldSize;
                        }
                        if (definition.parent == TypeDefinition.INVALID) definition = null;
                        else definition = kernel.libraryAgency[definition.parent.library].definitions[definition.parent.index];
                    }
                }
                else if (type.definition.code == TypeCode.Function) Mark(((RuntimeDelegateInfo*)(heap + head.point))->target);
            }
        }

        private bool IsUnrecoverableCoroutine(uint handle)
        {
            if (heads[handle].type.dimension == 0 && heads[handle].type.definition.code == TypeCode.Coroutine)
            {
                var invoker = kernel.coroutineAgency.GetInternalInvoker(*(ulong*)(heap + heads[handle].point));
                return invoker.state != InvokerState.Unstarted && invoker.state != InvokerState.Completed;
            }
            return false;
        }
        private void MemoryMove(uint handle)
        {
            if (heads[handle].point == heapTop) heapTop += heads[handle].size;
            else
            {
                var point = heads[handle].point;
                heads[handle].point = heapTop;
                var size = heads[handle].size;
                while (size-- > 0) heap[heapTop++] = heap[point++];
            }
        }
        private uint RecycleHandle(uint handle)
        {
            var next = heads[handle].next;
            Free(handle);
            heads[handle].next = free;
            heads[handle].type = Type.INVALID;
            free = handle;
            if (tail > 0) heads[tail].next = next;
            return next;
        }
        private void FullGC()
        {
            flag = !flag;
            var index = head;
            while (index > 0)
            {
                if (heads[index].reference > 0 || IsUnrecoverableCoroutine(index)) Mark(index);
                index = heads[index].next;
            }
            index = head;
            heapTop = 0;
            head = tail = soft = 0;
            while (index > 0)
            {
                if (heads[index].flag == flag)
                {
                    tail = index;
                    if (head == 0) head = index;
                    MemoryMove(index);
                    index = heads[index].next;
                }
                else index = RecycleHandle(index);
            }
            soft = tail;
        }
        private void FastGC()
        {
            if (soft > 0)
            {
                heapTop = heads[soft].point + heads[soft].size;
                tail = soft;
                var index = heads[soft].next;
                while (index > 0)
                {
                    if (heads[index].reference > 0 || heads[index].softReference > 0 || IsUnrecoverableCoroutine(index))
                    {
                        if (heads[index].generation++ > generation) soft = tail;
                        tail = index;
                        MemoryMove(index);
                        index = heads[index].next;
                    }
                    else index = RecycleHandle(index);
                }
            }
        }
        public void GC(bool full)
        {
            gc = true;
            if (full) FullGC();
            else FastGC();
            gc = false;
        }
        public void Reference(uint handle)
        {
            if (IsVaild(handle)) heads[handle].reference++;
        }
        public void SoftReference(uint handle)
        {
            if (IsVaild(handle)) heads[handle].softReference++;
        }
        public void Release(uint handle)
        {
            if (IsVaild(handle)) heads[handle].reference--;
        }
        public void SoftRelease(uint handle)
        {
            if (IsVaild(handle)) heads[handle].softReference--;
        }
        public ExitCode TryGetArrayLength(uint handle, out uint length)
        {
            if (IsVaild(handle))
            {
                if (heads[handle].type.dimension > 0)
                {
                    length = *(uint*)(heap + heads[handle].point);
                    return ExitCode.None;
                }
                else
                {
                    length = 0;
                    return ExitCode.Unknown;
                }
            }
            else
            {
                length = 0;
                return ExitCode.NullReference;
            }
        }
        public byte* GetPoint(uint handle)
        {
            return heap + heads[handle].point;
        }
        int i = 0;
        public ExitCode TryGetPoint(uint handle, out byte* point)
        {
            i++;
            if (IsVaild(handle))
            {
                point = heap + heads[handle].point;
                return ExitCode.None;
            }
            else
            {
                point = null;
                return ExitCode.NullReference;
            }
        }
        public byte* GetArrayPoint(uint handle, long index)
        {
            var head = heads[handle];
            var length = *(uint*)(heap + head.point);
            if (index < 0) index += length;
            var point = heap + head.point + 4;
            if (head.type.dimension > 1) return point + TypeCode.Handle.FieldSize() * index;
            else return point + head.type.definition.code.FieldSize() * index;
        }
        public ExitCode TryGetArrayPoint(uint handle, long index, out byte* point)
        {
            if (IsVaild(handle))
            {
                var head = heads[handle];
                if (head.type.dimension > 0)
                {
                    var length = *(uint*)(heap + head.point);
                    if (index < 0) index += length;
                    if (index >= 0 && index < length)
                    {
                        point = heap + head.point + 4;
                        if (head.type.dimension > 1) point += TypeCode.Handle.FieldSize() * index;
                        else point += head.type.definition.code.FieldSize() * index;
                        return ExitCode.None;
                    }
                    else
                    {
                        point = null;
                        return ExitCode.OutOfRange;
                    }
                }
                else
                {
                    point = null;
                    return ExitCode.Unknown;
                }
            }
            else
            {
                point = null;
                return ExitCode.NullReference;
            }
        }
        public Type GetType(uint handle)
        {
            return heads[handle].type;
        }
        public ExitCode TryGetType(uint handle, out Type type)
        {
            if (IsVaild(handle))
            {
                type = heads[handle].type;
                return ExitCode.None;
            }
            else
            {
                type = default;
                return ExitCode.NullReference;
            }
        }
        public bool IsVaild(uint handle)
        {
            return handle > 0 && handle < headTop && heads[handle].type != Type.INVALID;
        }

        public void Dispose()
        {
            if (heap != null) Tools.Free(heap);
            heap = null;
        }
        public bool IsEquals(uint a, uint b)
        {
            return a == b || (!IsVaild(a) && !IsVaild(b));
        }

        public long GetHeapTop()
        {
            return heapTop;
        }
        public long GetHandleCount()
        {
            var count = 0;
            var index = head;
            while (index != 0)
            {
                index = heads[index].next;
                count++;
            }
            return count;
        }

        public bool Delete(uint index, uint handle)
        {
            if (index != handle && IsVaild(index) && IsVaild(handle))
            {
                while (index != 0)
                {
                    if (heads[index].next == handle)
                    {
                        heads[index].next = heads[handle].next;
                        Free(handle);
                        heads[handle].type = Type.INVALID;
                        heads[handle].next = free;
                        free = handle;
                        return true;
                    }
                    index = heads[index].next;
                }
            }
            return false;
        }
        public void Trim(uint index)
        {
            if (IsVaild(index))
            {
                heapTop = heads[index].point;
                index = heads[index].next;
                heapTop += heads[index].size;
                while (index != 0)
                {
                    if (heads[index].point != heapTop)
                    {
                        var point = heads[index].point;
                        heads[index].point = heapTop;
                        var size = heads[index].size;
                        while (size-- > 0) heap[heapTop++] = heap[point++];
                    }
                    index = heads[index].next;
                }
            }
        }
    }
}
