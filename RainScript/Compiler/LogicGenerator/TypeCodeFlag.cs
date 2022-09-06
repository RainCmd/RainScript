namespace RainScript.Compiler.LogicGenerator
{
    [System.Flags]
    internal enum TypeCodeFlag
    {
        Bool = 0x0001,
        Integer = 0x0002,
        Real = 0x0004,
        Real2 = 0x0008,
        Real3 = 0x0010,
        Real4 = 0x0020,
        String = 0x0040,
        Handle = 0x0080,
        Interface = 0x0100,
        Function = 0x0200,
        Coroutine = 0x0400,
        Entity = 0x0800,
        Array = 0x1000,
    }
    internal static class TypeCodeFlagExtension
    {
        public static bool ContainAny(this TypeCodeFlag flag, TypeCodeFlag target)
        {
            return (flag & target) > 0;
        }
        public static TypeCodeFlag GetFlag(this CompilingType type)
        {
            if (type.dimension > 0) return TypeCodeFlag.Array;
            else switch (type.definition.code)
                {
                    case TypeCode.Bool: return TypeCodeFlag.Bool;
                    case TypeCode.Integer: return TypeCodeFlag.Integer;
                    case TypeCode.Real: return TypeCodeFlag.Real;
                    case TypeCode.Real2: return TypeCodeFlag.Real2;
                    case TypeCode.Real3: return TypeCodeFlag.Real3;
                    case TypeCode.Real4: return TypeCodeFlag.Real4;
                    case TypeCode.String: return TypeCodeFlag.String;
                    case TypeCode.Handle: return TypeCodeFlag.Handle;
                    case TypeCode.Interface: return TypeCodeFlag.Interface;
                    case TypeCode.Function: return TypeCodeFlag.Function;
                    case TypeCode.Coroutine: return TypeCodeFlag.Coroutine;
                    case TypeCode.Entity: return TypeCodeFlag.Entity;
                }
            return 0;
        }
    }
}
