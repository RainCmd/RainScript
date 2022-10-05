namespace RainScript
{
    internal class KernelMethod
    {
        internal struct Function
        {
            internal readonly Type[] parameters;
            internal readonly Type[] returns;
            public Function(Type[] parameters, params Type[] returns)
            {
                this.parameters = parameters;
                this.returns = returns;
            }
        }
        internal readonly string name;
        internal readonly Function[] functions;
        private KernelMethod(string name, params Function[] functions)
        {
            this.name = name;
            this.functions = functions;
        }
        internal static readonly KernelMethod[] methods;
        internal static readonly KernelMethod[] memberMethods;
        static KernelMethod()
        {
            methods = new KernelMethod[]
            {
                new KernelMethod("Abs", new Function(new Type[]{ KERNEL_TYPE.INTEGER }, KERNEL_TYPE.INTEGER), new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL)),
                new KernelMethod("Acos", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL)),
                new KernelMethod("Angle", new Function(new Type[]{ KERNEL_TYPE.REAL2, KERNEL_TYPE.REAL2 }, KERNEL_TYPE.REAL), new Function(new Type[]{ KERNEL_TYPE.REAL3, KERNEL_TYPE.REAL3 }, KERNEL_TYPE.REAL)),
                new KernelMethod("Asin", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL)),
                new KernelMethod("Atan", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL)),
                new KernelMethod("Atan2", new Function(new Type[]{ KERNEL_TYPE.REAL, KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL)),
                new KernelMethod("Ceil", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.INTEGER)),
                new KernelMethod("Clamp", new Function(new Type[]{ KERNEL_TYPE.INTEGER, KERNEL_TYPE.INTEGER, KERNEL_TYPE.INTEGER }, KERNEL_TYPE.INTEGER), new Function(new Type[]{ KERNEL_TYPE.REAL, KERNEL_TYPE.REAL, KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL)),
                new KernelMethod("Clame01", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL)),
                new KernelMethod("Cos", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL)),
                new KernelMethod("Collect", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("CountCoroutine", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("CountEntity", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("CountHandle", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("Cross", new Function(new Type[]{ KERNEL_TYPE.REAL2, KERNEL_TYPE.REAL2 }, KERNEL_TYPE.REAL),  new Function(new Type[]{ KERNEL_TYPE.REAL3, KERNEL_TYPE.REAL3 }, KERNEL_TYPE.REAL3)),
                new KernelMethod("Dot", new Function(new Type[]{ KERNEL_TYPE.REAL2, KERNEL_TYPE.REAL2 }, KERNEL_TYPE.REAL),  new Function(new Type[]{ KERNEL_TYPE.REAL3, KERNEL_TYPE.REAL3 }, KERNEL_TYPE.REAL),  new Function(new Type[]{ KERNEL_TYPE.REAL4, KERNEL_TYPE.REAL4 }, KERNEL_TYPE.REAL)),
                new KernelMethod("Floor", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.INTEGER)),
                new KernelMethod("GetRandomInt", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("GetRandomReal", new Function(new Type[0], KERNEL_TYPE.REAL)),
                new KernelMethod("HeapTotalMemory", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("Lerp", new Function(new Type[]{ KERNEL_TYPE.REAL, KERNEL_TYPE.REAL, KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL), new Function(new Type[]{ KERNEL_TYPE.REAL2, KERNEL_TYPE.REAL2, KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL2), new Function(new Type[]{ KERNEL_TYPE.REAL3, KERNEL_TYPE.REAL3, KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL3), new Function(new Type[]{ KERNEL_TYPE.REAL4, KERNEL_TYPE.REAL4, KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL4)),
                new KernelMethod("Max", new Function(new Type[]{ KERNEL_TYPE.INTEGER, KERNEL_TYPE.INTEGER }, KERNEL_TYPE.INTEGER), new Function(new Type[]{ KERNEL_TYPE.REAL, KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL), new Function(new Type[]{ KERNEL_TYPE.REAL2, KERNEL_TYPE.REAL2 }, KERNEL_TYPE.REAL2), new Function(new Type[]{ KERNEL_TYPE.REAL3, KERNEL_TYPE.REAL3 }, KERNEL_TYPE.REAL3), new Function(new Type[]{ KERNEL_TYPE.REAL4, KERNEL_TYPE.REAL4 }, KERNEL_TYPE.REAL4)),
                new KernelMethod("Min", new Function(new Type[]{ KERNEL_TYPE.INTEGER, KERNEL_TYPE.INTEGER }, KERNEL_TYPE.INTEGER), new Function(new Type[]{ KERNEL_TYPE.REAL, KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL), new Function(new Type[]{ KERNEL_TYPE.REAL2, KERNEL_TYPE.REAL2 }, KERNEL_TYPE.REAL2), new Function(new Type[]{ KERNEL_TYPE.REAL3, KERNEL_TYPE.REAL3 }, KERNEL_TYPE.REAL3), new Function(new Type[]{ KERNEL_TYPE.REAL4, KERNEL_TYPE.REAL4 }, KERNEL_TYPE.REAL4)),
                new KernelMethod("Round", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.INTEGER)),
                new KernelMethod("SetRandomSeed", new Function(new Type[]{ KERNEL_TYPE.INTEGER })),
                new KernelMethod("Sign", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.INTEGER)),
                new KernelMethod("Sin", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL)),
                new KernelMethod("SinCos", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL, KERNEL_TYPE.REAL)),
                new KernelMethod("Sqrt", new Function(new Type[]{ KERNEL_TYPE.REAL }, KERNEL_TYPE.REAL)),
            };
            memberMethods = new KernelMethod[]
            {
                //bool
                new KernelMethod("ToString", new Function(new Type[0], KERNEL_TYPE.STRING)),
                //integer
                new KernelMethod("ToString", new Function(new Type[0], KERNEL_TYPE.STRING)),
                //real
                new KernelMethod("ToString", new Function(new Type[0], KERNEL_TYPE.STRING)),
                //real2
                new KernelMethod("Normalized", new Function(new Type[0], KERNEL_TYPE.REAL2)),
                new KernelMethod("Magnitude", new Function(new Type[0], KERNEL_TYPE.REAL)),
                new KernelMethod("SqrMagnitude", new Function(new Type[0], KERNEL_TYPE.REAL)),
                //real3
                new KernelMethod("Normalized", new Function(new Type[0], KERNEL_TYPE.REAL3)),
                new KernelMethod("Magnitude", new Function(new Type[0], KERNEL_TYPE.REAL)),
                new KernelMethod("SqrMagnitude", new Function(new Type[0], KERNEL_TYPE.REAL)),
                //string
                new KernelMethod("GetLength", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("GetStringID", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("ToBool", new Function(new Type[0], KERNEL_TYPE.BOOL)),
                new KernelMethod("ToInteger", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("ToReal", new Function(new Type[0], KERNEL_TYPE.REAL)),
                //handle
                new KernelMethod("GetHandleID", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                //coroutine
                new KernelMethod("Abort", new Function(new Type[]{ KERNEL_TYPE.INTEGER })),
                new KernelMethod("GetState", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("GetExitCode", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                new KernelMethod("IsPause", new Function(new Type[0], KERNEL_TYPE.BOOL)),
                new KernelMethod("Pause", new Function(new Type[0])),
                new KernelMethod("Resume", new Function(new Type[0])),
                //entity
                new KernelMethod("GetEntityID", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
                //array
                new KernelMethod("GetLength", new Function(new Type[0], KERNEL_TYPE.INTEGER)),
            };
        }
    }
}
