using System;
using System.Collections.Generic;

namespace RainScript.VirtualMachine
{
    internal class CoroutineAgency : IDisposable
    {
        private readonly Kernel kernel;
        private Coroutine head;
        internal Coroutine invoking;
        private int count;
        [NonSerialized]
        private Coroutine[] coroutines;
        [NonSerialized]
        private Coroutine free;
        private uint invokerCount = 1, invokerInstance;
        [NonSerialized]
        private readonly Stack<Invoker> invokerPool = new Stack<Invoker>();
        private readonly Dictionary<ulong, Invoker> invokerMap = new Dictionary<ulong, Invoker>();
        public CoroutineAgency(Kernel kernel)
        {
            this.kernel = kernel;
        }
        internal Invoker Invoker(FunctionHandle handle)
        {
            if (!handle || handle.library.kernel != kernel) throw ExceptionGeneratorVM.InvalidMethodHandle();
            var invoker = invokerPool.Count > 0 ? invokerPool.Pop() : new Invoker(invokerCount++);
            if (invokerInstance == 0) invokerInstance++;
            invoker.instanceID |= ((ulong)invokerInstance++) << 32;
            invoker.Initialize(handle);
            return invoker;
        }
        internal StackFrame[] GetInvokingStackFrames()
        {
            if (invoking == null) return new StackFrame[0];
            return invoking.GetStackFrames();
        }

        internal void Start(Invoker invoker, bool immediately, bool ignoreWait)
        {
            Coroutine coroutine;
            if (free == null) coroutine = new Coroutine(kernel);
            else
            {
                coroutine = free;
                free = free.next;
            }
            coroutine.Initialize(invoker, ignoreWait);
            count++;
            if (immediately) coroutine.Update();
            if (coroutine.Running)
            {
                coroutine.next = head;
                head = coroutine;
            }
            else Recycle(coroutine);
        }
        private bool Remove(Invoker invoker, ref Coroutine head, out Coroutine coroutine)
        {
            for (Coroutine index = head, prev = null; index != null; prev = index, index = index.next)
                if (index.invoker == invoker)
                {
                    if (prev == null) head = index.next;
                    else prev.next = index.next;
                    coroutine = index;
                    return true;
                }
            coroutine = default;
            return false;
        }
        internal void Update()
        {
            var count = this.count;
            if (coroutines == null || coroutines.Length < count)
            {
                count |= count >> 1;
                count |= count >> 2;
                count |= count >> 4;
                count |= count >> 8;
                count |= count >> 16;
                coroutines = new Coroutine[(count << 1) - count + 1];
            }
            count = 0;
            for (var index = head; index != null; index = index.next) coroutines[count++] = index;
            for (var i = 0; i < count; i++)
            {
                var coroutine = coroutines[i];
                if (!coroutine.pause && coroutine.exit == 0)
                    coroutine.Update();
            }
            var idx = head;
            for (int i = 0; i < count; i++)
            {
                var coroutine = coroutines[i];
                if (!coroutine.Running)
                {
                    if (head == coroutine) idx = head = coroutine.next;
                    else
                    {
                        while (idx.next != coroutine) idx = idx.next;
                        idx.next = coroutine.next;
                    }
                    if (coroutine.exit != 0) coroutine.Abort();
                    Recycle(coroutine);
                }
                coroutines[i] = null;
            }
        }
        private void Recycle(Coroutine coroutine)
        {
            coroutine.next = free;
            free = coroutine;
            free.Recycle();
            count--;
        }
        internal void Recycle(Invoker invoker)
        {
            invoker.instanceID &= 0xffff_ffff;
            invokerPool.Push(invoker);
        }

        internal Invoker InternalInvoker(FunctionHandle handle)
        {
            var invoker = Invoker(handle);
            invokerMap.Add(invoker.instanceID, invoker);
            return invoker;
        }
        internal Invoker GetInternalInvoker(ulong instance)
        {
            return invokerMap[instance];
        }
        internal void RecycleInternalInvoker(ulong instance)
        {
            var invoker = invokerMap[instance];
            invokerMap.Remove(instance);
            invoker.Recycle();
        }

        internal long GetCoroutineCount()
        {
            return count;
        }
        internal IEnumerable<Coroutine> GetCoroutines()
        {
            for (var index = head; index != null; index = index.next)
                yield return index;
        }

        private void Dispose(Coroutine coroutine)
        {
            while (coroutine != null)
            {
                coroutine.Dispose();
                coroutine = coroutine.next;
            }
        }
        public void Dispose()
        {
            while (invokerPool.Count > 0) invokerPool.Pop().Dispose();
            Dispose(head);
            Dispose(free);
            head = free = null;
            invokerCount = 1;
        }
    }
}
