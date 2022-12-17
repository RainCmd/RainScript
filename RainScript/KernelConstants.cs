#if FIXED
using real = RainScript.Real.Fixed;
using Math = RainScript.Real.Math;
#else
using real = System.Double;
using Math = System.Math;
#endif

namespace RainScript
{
    internal unsafe class KernelConstant
    {
        public readonly string name;
        public readonly Type type;
        public readonly real value;
        public KernelConstant(string name, Type type, real value)
        {
            this.name = name;
            this.type = type;
            this.value = value;
        }
        public static readonly KernelConstant[] constants;
        static KernelConstant()
        {
            constants = new KernelConstant[]
            {
                new KernelConstant("PI", KERNEL_TYPE.REAL, Math.PI),
                new KernelConstant("E", KERNEL_TYPE.REAL, Math.E),
#if FIXED
                new KernelConstant("Deg2Rad", KERNEL_TYPE.REAL, Math.Deg2Rad),
                new KernelConstant("Rad2Deg", KERNEL_TYPE.REAL, Math.Rad2Deg),
#else
                new KernelConstant("Deg2Rad", KERNEL_TYPE.REAL, Math.PI / 180),
                new KernelConstant("Rad2Deg", KERNEL_TYPE.REAL, 180 / Math.PI),
#endif
            };
        }
    }
}
