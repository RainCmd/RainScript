namespace RainScript
{
    internal unsafe class KernelConstant
    {
        public readonly string name;
        public readonly Type type;

        public KernelConstant(string name, Type type)
        {
            this.name = name;
            this.type = type;
        }
        public static readonly KernelConstant[] constants;
        static KernelConstant()
        {
            constants = new KernelConstant[]
            {
                new KernelConstant("PI", KERNEL_TYPE.REAL),
                new KernelConstant("E", KERNEL_TYPE.REAL),
                new KernelConstant("Deg2Rad", KERNEL_TYPE.REAL),
                new KernelConstant("Rad2Deg", KERNEL_TYPE.REAL),
            };
        }
    }
}
