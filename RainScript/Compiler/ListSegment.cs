using System;
using System.Collections;
using System.Collections.Generic;

namespace RainScript.Compiler
{
    /// <summary>
    /// [,]
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct ListSegment<T> : IList<T>
    {
        private readonly IList<T> list;
        private readonly int start, end;
        public ListSegment(IList<T> list) : this(list, 0, -1) { }
        public ListSegment(IList<T> list, int start) : this(list, start, -1) { }
        public ListSegment(IList<T> list, int start, int end)
        {
            this.list = list;
            this.start = start < 0 ? list.Count + start : start;
            this.end = end < 0 ? list.Count + end : end;
            if (start < 0 || start >= list.Count) throw new IndexOutOfRangeException();
            if (end < 0 || end >= list.Count) throw new IndexOutOfRangeException();
        }
        public T this[int index]
        {
            get
            {
                if (index < 0) index += Count;
                if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
                return list[start < end ? start + index : start - index];
            }
            set
            {
                if (index < 0) index += Count;
                if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
                list[start < end ? start + index : start - index] = value;
            }
        }
        public ListSegment<T> this[int start, int end]
        {
            get
            {
                var count = Count;
                if (start < 0) start += count;
                if (end < 0) end += count;
                if (start < 0 || start >= count) throw new IndexOutOfRangeException();
                if (end < 0 || end >= count) throw new IndexOutOfRangeException();
                if (this.start < this.end)
                {
                    start += this.start;
                    end += this.start;
                }
                else
                {
                    start = this.start - start;
                    end = this.start - end;
                }
                return new ListSegment<T>(list, start, end);
            }
        }

        /// <summary>
        /// 元素数量至少为1
        /// </summary>
        public int Count => start < end ? end - start + 1 : start - end + 1;

        public bool IsReadOnly => list.IsReadOnly;

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            foreach (var value in this) if (Equals(item, value)) return true;
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this) array[arrayIndex++] = item;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++) yield return this[i];
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < Count; i++) if (Equals(item, list[i])) return i;
            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < Count; i++) yield return this[i];
        }
        public static implicit operator ListSegment<T>(ScopeList<T> list)
        {
            return new ListSegment<T>(list);
        }
        public static implicit operator ListSegment<T>(List<T> list)
        {
            return new ListSegment<T>(list);
        }
        public static implicit operator ListSegment<T>(T[] array)
        {
            return new ListSegment<T>(array);
        }
        private static bool Equals(T a, T b)
        {
            if (a == null) return b == null;
            return a.Equals(b);
        }
    }
}
