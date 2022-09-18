using System;
using System.Runtime.InteropServices;
using System.IO;

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
        internal static uint MethodFunction(uint methodIndex, uint functionIndex)
        {
            return (methodIndex << 8) | (functionIndex & 0xff);
        }
        internal static void MethodFunction(uint index, out uint methodIndex, out uint functionIndex)
        {
            methodIndex = index >> 8;
            functionIndex = index & 0xff;
        }
    }
}
