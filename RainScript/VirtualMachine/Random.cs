#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif


namespace RainScript.VirtualMachine
{
#if FIXED
    internal class Random
    {
        private int inext;
        private int inextp;
        private readonly int[] seedArray = new int[56];
        public Random()
        {
            SetSeed(System.Environment.TickCount);
        }
        public void SetSeed(long seed)
        {
            int num = 161803398 - (int)seed;
            seedArray[55] = num;
            int num2 = 1;
            for (int i = 1; i < 55; i++)
            {
                int num3 = 21 * i % 55;
                seedArray[num3] = num2;
                num2 = num - num2;
                if (num2 < 0)
                {
                    num2 += int.MaxValue;
                }

                num = seedArray[num3];
            }

            for (int j = 1; j < 5; j++)
            {
                for (int k = 1; k < 56; k++)
                {
                    seedArray[k] -= seedArray[1 + (k + 30) % 55];
                    if (seedArray[k] < 0)
                    {
                        seedArray[k] += int.MaxValue;
                    }
                }
            }

            inext = 0;
            inextp = 21;
        }
        private int InternalSample()
        {
            int num = inext;
            int num2 = inextp;
            if (++num >= 56)
            {
                num = 1;
            }

            if (++num2 >= 56)
            {
                num2 = 1;
            }

            int num3 = seedArray[num] - seedArray[num2];
            if (num3 < 0)
            {
                num3 += int.MaxValue;
            }

            seedArray[num] = num3;
            inext = num;
            inextp = num2;
            return num3;
        }
        public long Next()
        {
            return ((long)InternalSample() << 32) | (uint)InternalSample();
        }
        public real NextReal()
        {
            return new real(InternalSample() & real.MASK_DECIMAL);
        }
    }
#else
    internal class Random
    {
        private System.Random random = new System.Random();
        public void SetSeed(long seed)
        {
            random = new System.Random((int)seed);
        }
        public long Next()
        {
            return ((long)random.Next() << 32) | (uint)random.Next();
        }
        public real NextReal()
        {
            return random.NextDouble();
        }
    }
#endif
}
