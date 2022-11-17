using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace RainScript
{
    internal static unsafe class Tools
    {
        internal static string Format(this string format, params object[] args)
        {
            return string.Format(format, args);
        }
        internal static void Write(this Stream stream, uint value)
        {
            var point = (byte*)&value;
            stream.WriteByte(point[0]);
            stream.WriteByte(point[1]);
            stream.WriteByte(point[2]);
            stream.WriteByte(point[3]);
        }
        internal static void Write(this Stream stream, ulong value)
        {
            var point = (uint*)&value;
            Write(stream, point[0]);
            Write(stream, point[1]);
        }
        [Conditional("MEMORY_ALIGNMENT_4")]
        internal static void MemoryAlignment(ref uint point)
        {
#if MEMORY_ALIGNMENT_4
            point = (point + MEMORY_ALIGNMENT) & ~MEMORY_ALIGNMENT;
#endif
        }
        internal static byte* MAlloc(int count)
        {
            return (byte*)Marshal.AllocHGlobal(count);
        }
        internal static void Free(void* point)
        {
            Marshal.FreeHGlobal((IntPtr)point);
        }
        internal static void Copy(byte* src, byte* trg, uint length)
        {
            while (length-- > 0) trg[length] = src[length];
        }
        internal static byte* A2P(byte[] array)
        {
            var result = MAlloc(array.Length);
            for (int i = 0; i < array.Length; i++) result[i] = array[i];
            return result;
        }
        internal static byte[] P2A(byte* point, uint length)
        {
            var result = new byte[length];
            for (int i = 0; i < length; i++) result[i] = point[i];
            return result;
        }
#if MEMORY_ALIGNMENT_4
        internal const uint MEMORY_ALIGNMENT = 3;
#endif
    }
}
