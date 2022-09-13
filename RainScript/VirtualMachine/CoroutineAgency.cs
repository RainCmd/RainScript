using System;
using System.Collections.Generic;

namespace RainScript.VirtualMachine
{
    internal class CoroutineAgency : IDisposable
    {
        private readonly Kernel kernel;
        private Coroutine head, abort;
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
            if (immediately && !coroutine.Update()) Recycle(coroutine);
            else
            {
                coroutine.next = head;
                head = coroutine;
                count++;
            }
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
        internal void Abort(Invoker invoker, long code)
        {
            if (code != 0)
            {
                if (Remove(invoker, ref head, out var coroutine))
                {
                    count--;
                    coroutine.exit = code;
                    coroutine.next = abort;
                    abort = coroutine;
                }
            }
        }
        internal Coroutine GetCoroutine(Invoker invoker)
        {
            for (var index = head; index != null; index = index.next)
                if (index.invoker == invoker)
                    return index;
            return null;
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
            for (var index = head; index != null; index = index.next)
                if (!index.pause) coroutines[count++] = index;
            for (var i = 0; i < count; i++)
            {
                var coroutine = coroutines[i];
                if (!coroutine.pause && coroutine.exit == 0 && !coroutine.Update())
                    for (Coroutine index = head, prev = null; index != null; prev = index, index = index.next)
                        if (index == coroutine)
                        {
                            if (prev == null) head = index.next;
                            else prev.next = index.next;
                            count--;
                            Recycle(coroutine);
                            break;
                        }
            }

            for (var index = abort; abort != null; index = abort)
            {
                abort = abort.next;
                index.Abort();
                Recycle(index);
            }
        }
        private void Recycle(Coroutine coroutine)
        {
            coroutine.next = free;
            free = coroutine;
            free.Recycle();
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
            Dispose(abort);
            Dispose(free);
            head = abort = free = null;
            invokerCount = 1;
        }
    }
}
