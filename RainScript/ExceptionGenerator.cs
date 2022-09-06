using System;

namespace RainScript
{
    internal class ExceptionGenerator
    {
        public static Exception InvalidTypeCode(TypeCode code)
        {
            return new Exception("无效的类型：{0}".Format(code));
        }
        public static Exception CharIndexOutOfRangeException()
        {
            return new IndexOutOfRangeException("字符索引越界");
        }
    }
}
