using System.Runtime.InteropServices;

namespace RainScript
{
    /*
     * 帧：stack+bottom
     * [0,3]    库编号
     * [4,7]    前帧栈底
     * [8,11]   返回指令地址
     * [12,15]  函数清理指令地址(这个地址在函数执行的第一条指令写入)
     * [-,-]    返回值栈地址列表
     * [-,-]    参数列表
     */
    [StructLayout(LayoutKind.Explicit)]
    internal struct FrameInfo
    {
        [FieldOffset(0)]
        public readonly uint libraryIndex;
        [FieldOffset(4)]
        public readonly uint bottom;
        [FieldOffset(8)]
        public readonly uint point;
        public FrameInfo(uint libraryIndex, uint bottom, uint point)
        {
            this.libraryIndex = libraryIndex;
            this.bottom = bottom;
            this.point = point;
        }
        public const byte FINALLY = 12;
        public const byte SIZE = 16;
    }
}
