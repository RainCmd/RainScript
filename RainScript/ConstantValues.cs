#pragma warning disable CS1587 // XML 注释没有放在有效语言元素上
namespace RainScript
{
    internal static class LIBRARY
    {
        public const uint INVALID = 0xffff_ffff;
        public const uint KERNEL = 0xffff_fffe;
        public const uint SELF = 0xffff_fffd;
        public const uint CTOR_ENTRY = 0;
        public const uint ENTRY_INVALID = 0xffff_ffff;
        public const uint METHOD_INVALID = 0xffff_ffff;
    }
    internal static class TYPE_CODE
    {
        public static uint FieldSize(this TypeCode code)
        {
            switch (code)
            {
                case TypeCode.Invalid: break;
                case TypeCode.Bool: return 1;
                case TypeCode.Integer:
                case TypeCode.Real: return 8;
                case TypeCode.Real2: return 16;
                case TypeCode.Real3: return 24;
                case TypeCode.Real4: return 32;
                case TypeCode.String:
                case TypeCode.Handle:
                case TypeCode.Interface:
                case TypeCode.Function:
                case TypeCode.Coroutine: return 4;
                case TypeCode.Entity: return 8;
            }
            throw ExceptionGenerator.InvalidTypeCode(code);
        }
        public static unsafe uint HeapSize(this TypeCode code)
        {
            switch (code)
            {
                case TypeCode.Invalid: break;
                case TypeCode.Bool: return 1;
                case TypeCode.Integer:
                case TypeCode.Real: return 8;
                case TypeCode.Real2: return 16;
                case TypeCode.Real3: return 24;
                case TypeCode.Real4: return 32;
                case TypeCode.String: return 4;
                case TypeCode.Handle:
                case TypeCode.Interface: break;
                case TypeCode.Function: return 17; ///<see cref="VirtualMachine.RuntimeDelegateInfo"/>
                case TypeCode.Coroutine: return 8;
                case TypeCode.Entity: return 8;
            }
            throw ExceptionGenerator.InvalidTypeCode(code);
        }
    }
    internal static class KERNEL_TYPE//类型的Index需要跟KernelLibraryGenerator里的DefinitionInfo对应
    {
        public static readonly Type INVALID = new Type(LIBRARY.KERNEL, TypeCode.Invalid, 0, 0);
        public static readonly Type BOOL = new Type(LIBRARY.KERNEL, TypeCode.Bool, 1, 0);
        public static readonly Type INTEGER = new Type(LIBRARY.KERNEL, TypeCode.Integer, 2, 0);
        public static readonly Type REAL = new Type(LIBRARY.KERNEL, TypeCode.Real, 3, 0);
        public static readonly Type REAL2 = new Type(LIBRARY.KERNEL, TypeCode.Real2, 4, 0);
        public static readonly Type REAL3 = new Type(LIBRARY.KERNEL, TypeCode.Real3, 5, 0);
        public static readonly Type REAL4 = new Type(LIBRARY.KERNEL, TypeCode.Real4, 6, 0);
        public static readonly Type STRING = new Type(LIBRARY.KERNEL, TypeCode.String, 7, 0);
        public static readonly Type HANDLE = new Type(LIBRARY.KERNEL, TypeCode.Handle, 8, 0);
        public static readonly Type INTERFACE = new Type(LIBRARY.KERNEL, TypeCode.Interface, 9, 0);
        public static readonly Type FUNCTION = new Type(LIBRARY.KERNEL, TypeCode.Function, 10, 0);
        public static readonly Type COROUTINE = new Type(LIBRARY.KERNEL, TypeCode.Coroutine, 11, 0);
        public static readonly Type ENTITY = new Type(LIBRARY.KERNEL, TypeCode.Entity, 12, 0);
        public static readonly Type ARRAY = new Type(LIBRARY.KERNEL, TypeCode.Handle, 13, 0);
        private static readonly Type[] types = { INVALID, BOOL, INTEGER, REAL, REAL2, REAL3, REAL4, STRING, HANDLE, INTERFACE, FUNCTION, COROUTINE, ENTITY, ARRAY };
        private static readonly string[] names = { "invalid", KeyWorld.BOOL, KeyWorld.INTEGER, KeyWorld.REAL, KeyWorld.REAL2, KeyWorld.REAL3, KeyWorld.REAL4, KeyWorld.STRING, KeyWorld.HANDLE, KeyWorld.INTERFACE, KeyWorld.FUNCTION, KeyWorld.COROUTINE, KeyWorld.ENTITY, KeyWorld.ARRAY };
        public static Type GetType(int index) { return types[index]; }
        public static string GetName(int index) { return names[index]; }
    }
}
