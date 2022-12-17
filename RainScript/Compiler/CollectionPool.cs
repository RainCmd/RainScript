using System;
using System.Collections;
using System.Collections.Generic;

namespace RainScript.Compiler
{
    internal interface IRecyclable
    {
        void OnInit();
        void OnRecycle();
    }
    internal struct RecyclableDetector : IRecyclable
    {
        private bool valid;
        public void OnInit()
        {
            valid = !valid;
            Assert();
        }
        public void OnRecycle()
        {
            Assert();
            valid = !valid;
        }
        private void Assert()
        {
            if (!valid) throw ExceptionGeneratorCompiler.InvalidRecycleOperation();
        }
    }
    internal class ScopeList<T> : IList<T>, IRecyclable, IDisposable
    {
        private readonly CollectionPool pool;
        private readonly List<T> list;
        internal ScopeList(CollectionPool pool)
        {
            this.pool = pool;
            list = new List<T>();
        }
        public void Dispose()
        {
            pool.Recycle(this);
        }

        public T this[int index]
        {
            get
            {
                if (index < 0) index += list.Count;
                return list[index];
            }
            set
            {
                if (index < 0) index += list.Count;
                list[index] = value;
            }
        }

        public ListSegment<T> this[int start, int end]
        {
            get
            {
                return new ListSegment<T>(this, start, end);
            }
        }

        public int Count => list.Count;
        public bool IsReadOnly => false;
        public void Add(T item)
        {
            list.Add(item);
        }
        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection) list.Add(item);
        }
        public void Clear()
        {
            list.Clear();
        }
        public bool Contains(T item)
        {
            return list.Contains(item);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }
        public T[] ToArray()
        {
            return list.ToArray();
        }
        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }
        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }
        public int FindIndex(Predicate<T> match)
        {
            for (int i = 0; i < Count; i++) if (match(list[i])) return i;
            return -1;
        }
        public void Insert(int index, T item)
        {
            if (index < 0) index += Count;
            list.Insert(index, item);
        }
        public bool Remove(T item)
        {
            return list.Remove(item);
        }
        public void RemoveAt(int index)
        {
            if (index < 0) index += Count;
            list.RemoveAt(index);
        }
        /// <summary>
        /// 会改变元素顺序
        /// </summary>
        /// <param name="index"></param>
        public void FastRemoveAt(int index)
        {
            this[index] = this[-1];
            RemoveAt(-1);
        }
        public int RemoveAll(Predicate<T> match)
        {
            return list.RemoveAll(match);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)list).GetEnumerator();
        }

        private RecyclableDetector detector = new RecyclableDetector();
        void IRecyclable.OnInit()
        {
            detector.OnInit();
        }
        void IRecyclable.OnRecycle()
        {
            detector.OnRecycle();
            list.Clear();
        }
    }
    internal class ScopeStack<T> : Stack<T>, IRecyclable, IDisposable
    {
        private readonly CollectionPool pool;
        public ScopeStack(CollectionPool pool)
        {
            this.pool = pool;
        }
        public void Dispose()
        {
            pool.Recycle(this);
        }

        private RecyclableDetector detector = new RecyclableDetector();
        void IRecyclable.OnInit()
        {
            detector.OnInit();
        }
        void IRecyclable.OnRecycle()
        {
            detector.OnRecycle();
            Clear();
        }
    }
    internal class ScopeSet<T> : HashSet<T>, IRecyclable, IDisposable
    {
        private readonly CollectionPool pool;
        internal ScopeSet(CollectionPool pool)
        {
            this.pool = pool;
        }
        public void Dispose()
        {
            pool.Recycle(this);
        }

        private RecyclableDetector detector = new RecyclableDetector();
        void IRecyclable.OnInit()
        {
            detector.OnInit();
        }
        void IRecyclable.OnRecycle()
        {
            detector.OnRecycle();
            Clear();
        }
    }
    internal class ScopeDictionary<K, V> : Dictionary<K, V>, IRecyclable, IDisposable
    {
        private readonly CollectionPool pool;
        internal ScopeDictionary(CollectionPool pool)
        {
            this.pool = pool;
        }
        public void Dispose()
        {
            pool.Recycle(this);
        }

        private RecyclableDetector detector = new RecyclableDetector();
        void IRecyclable.OnInit()
        {
            detector.OnInit();
        }
        void IRecyclable.OnRecycle()
        {
            detector.OnRecycle();
            Clear();
        }
    }
    internal class CollectionPool
    {
        private readonly Dictionary<System.Type, Stack<object>> listPools = new Dictionary<System.Type, Stack<object>>();
        private readonly Dictionary<System.Type, Stack<object>> stackPools = new Dictionary<System.Type, Stack<object>>();
        private readonly Dictionary<System.Type, Stack<object>> setPools = new Dictionary<System.Type, Stack<object>>();
        private readonly Dictionary<System.Type, Stack<object>> dictionaryPools = new Dictionary<System.Type, Stack<object>>();
        public ScopeList<T> GetList<T>()
        {
            return Get(listPools, () => new ScopeList<T>(this));
        }
        internal void Recycle<T>(ScopeList<T> list)
        {
            Recycle(listPools, list);
        }
        public ScopeStack<T> GetStack<T>()
        {
            return Get(stackPools, () => new ScopeStack<T>(this));
        }
        internal void Recycle<T>(ScopeStack<T> stack)
        {
            Recycle(stackPools, stack);
        }
        public ScopeSet<T> GetSet<T>()
        {
            return Get(setPools, () => new ScopeSet<T>(this));
        }
        internal void Recycle<T>(ScopeSet<T> set)
        {
            Recycle(setPools, set);
        }
        public ScopeDictionary<K, V> GetDictionary<K, V>()
        {
            return Get(dictionaryPools, () => new ScopeDictionary<K, V>(this));
        }
        internal void Recycle<K, V>(ScopeDictionary<K, V> dictionary)
        {
            Recycle(dictionaryPools, dictionary);
        }
        private T Get<T>(Dictionary<System.Type, Stack<object>> pool, Func<T> create) where T : IRecyclable
        {
            var result = (pool.TryGetValue(typeof(T), out var stack) && stack.Count > 0) ? (T)stack.Pop() : create();
            result.OnInit();
            return result;
        }
        private void Recycle<T>(Dictionary<System.Type, Stack<object>> pool, T value) where T : IRecyclable
        {
            if (!pool.TryGetValue(typeof(T), out var stack))
            {
                stack = new Stack<object>();
                pool.Add(typeof(T), stack);
            }
            value.OnRecycle();
            stack.Push(value);
        }
        public void Clear()
        {
            listPools.Clear();
            setPools.Clear();
            dictionaryPools.Clear();
        }
    }
}
