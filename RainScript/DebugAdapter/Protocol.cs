using System;
using System.Text;

namespace RainScript.DebugAdapter
{
    internal enum RainSocketHead
    {
        hookup = 1,
        convention,
        heartbeat,
        message,
    }
    internal struct BufferReader
    {
        private int position;
        private readonly byte[] buffer;
        public BufferReader(byte[] buffer)
        {
            position = 0;
            this.buffer = buffer;
        }
        public bool ReadBool()
        {
            return buffer[position++] != 0;
        }
        public byte ReadInt8()
        {
            return buffer[position++];
        }
        public int ReadInt32()
        {
            var result = BitConverter.ToInt32(buffer, position);
            position += 4;
            return result;
        }
        public long ReadInt64()
        {
            var result = BitConverter.ToInt64(buffer, position);
            position += 8;
            return result;
        }
        public string ReadString()
        {
            var length = ReadInt32();
            var result = Encoding.UTF8.GetString(buffer, position, length);
            position += length;
            return result;
        }
        public byte[] ReadBuffer()
        {
            var length = ReadInt32();
            var result = new byte[length];
            Array.Copy(buffer, position, result, 0, length);
            position += length;
            return result;
        }
    }
    internal struct BufferWriter
    {
        private readonly byte[] buffer;
        public int Size { get; private set; }
        public BufferWriter(byte[] buffer)
        {
            this.buffer = buffer;
            Size = 0;
        }
        public void Write(bool value)
        {
            buffer[Size++] = (byte)(value ? 1 : 0);
        }
        public void Write(byte value)
        {
            buffer[Size++] = value;
        }
        public void Write(int value)
        {
            var buffer = BitConverter.GetBytes(value);
            Array.Copy(buffer, 0, this.buffer, Size, buffer.Length);
            Size += buffer.Length;
        }
        public void Write(long value)
        {
            var buffer = BitConverter.GetBytes(value);
            Array.Copy(buffer, 0, this.buffer, Size, buffer.Length);
            Size += buffer.Length;
        }
        public void Write(string value)
        {
            Write(Encoding.UTF8.GetBytes(value));
        }
        public void Write(byte[] buffer)
        {
            Write(buffer.Length);
            Array.Copy(buffer, 0, this.buffer, Size, buffer.Length);
            Size += buffer.Length;
        }
        public void Write(int value, int point)
        {
            var buffer = BitConverter.GetBytes(value);
            Array.Copy(buffer, 0, this.buffer, point, buffer.Length);
        }
    }
}
