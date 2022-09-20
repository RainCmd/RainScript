namespace RainScript.Real
{
#if FIXED
    internal static class Math
    {
        public static readonly Fixed PI = new Fixed(205887);//这个值仅用于Real.DECIMAL=16时
        public static readonly Fixed Rad2Deg = 180 / PI;
        public static readonly Fixed Deg2Rad = PI / 180;
        internal static readonly Fixed HALF_PI = PI >> 1;
        internal static readonly Fixed DOUBLE_PI = PI << 1;
        public static readonly Fixed E = new Fixed(178145);//这个值仅用于Real.DECIMAL=16时
        public static Fixed Max(Fixed a, Fixed b)
        {
            if (a > b) return a;
            else return b;
        }
        public static Fixed Min(Fixed a, Fixed b)
        {
            if (a > b)
            {
                return b;
            }
            else return a;
        }
        public static int Sign(Fixed real)
        {
            if (real > 0)
            {
                return 1;
            }
            else if (real < 0)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
        public static long Round(Fixed real)
        {
            const long HALF = 1 << (Fixed.DECIMAL - 1);
            if (real.value > 0) return (real.value + HALF) >> Fixed.DECIMAL;
            else return ((real.value - HALF - 1) >> Fixed.DECIMAL) + 1;
        }
        public static long Ceil(Fixed real)
        {
            if (real.value > 0) return (real.value - 1 >> Fixed.DECIMAL) + 1;
            else return (real.value + Fixed.MASK_DECIMAL) >> Fixed.DECIMAL;
        }
        public static long Floor(Fixed real)
        {
            if (real.value > 0) return real.value >> Fixed.DECIMAL;
            else return ((real.value - Fixed.MASK_DECIMAL) >> Fixed.DECIMAL) + 1;
        }
        public static Fixed Abs(Fixed real)
        {
            if (real < 0) return -real;
            else return real;
        }
        public static Fixed Lerp(Fixed a, Fixed b, Fixed l)
        {
            return a + (b - a) * l;
        }
        public static Fixed Clamp(Fixed value, Fixed min, Fixed max)
        {
            if (value < min) return min;
            else if (value > max) return max;
            else return value;
        }
        public static Fixed Clamp01(Fixed value)
        {
            if (value < 0) return 0;
            else if (value > 1) return 1;
            else return value;
        }
        private static readonly int[] TAB_64 = {
        63,  0, 58,  1, 59, 47, 53,  2,
        60, 39, 48, 27, 54, 33, 42,  3,
        61, 51, 37, 40, 49, 18, 28, 20,
        55, 30, 34, 11, 43, 14, 22,  4,
        62, 57, 46, 52, 38, 26, 32, 41,
        50, 36, 17, 19, 29, 10, 13, 21,
        56, 45, 25, 31, 35, 16,  9, 12,
        44, 24, 15,  8, 23,  7,  6,  5};
        internal static int Log2(long value)
        {
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value |= value >> 32;
            return TAB_64[((ulong)(value - (value >> 1)) * 0x07EDD5E59A4E28C2) >> 58];
        }
        public static Fixed Sqrt(Fixed real)
        {
            const int DECIMAL = Fixed.DECIMAL;
            if (real.value > 0)
            {
                int i = Log2(real.value);
                i -= DECIMAL;
                bool b = i < 0;
                if (b) i = -i;
                i >>= 1;
                if (b) i = -i;
                i += DECIMAL;
                var tmp = 1L << i;
                while (i > 0)
                {
                    var sqr = (tmp * tmp) >> DECIMAL;
                    if (sqr == real.value) return new Fixed(tmp);
                    else
                    {
                        i--;
                        if (sqr > real.value)
                        {
                            tmp -= 1L << i;
                        }
                        else
                        {
                            tmp += 1L << i;
                        }
                    }
                }
                return new Fixed(tmp);
            }
            else
            {
                return real;
            }
        }
        public static Fixed Sin(Fixed angle)
        {
            int sign = 1;
            angle %= DOUBLE_PI;
            if (angle < 0)
            {
                angle = DOUBLE_PI + angle;
            }
            if ((angle > HALF_PI) && (angle <= PI))
            {
                angle = PI - angle;
            }
            else if ((angle > PI) && (angle <= (PI + HALF_PI)))
            {
                angle -= PI;
                sign = -1;
            }
            else if (angle > (PI + HALF_PI))
            {
                angle = DOUBLE_PI - angle;
                sign = -1;
            }

            var sqr = angle * angle;
            var result = new Fixed(498) * sqr;
            result -= new Fixed(10882);
            result *= sqr;
            result++;
            result *= angle;
            return sign * result;
        }
        public static Fixed Cos(Fixed angle)
        {
            return Sin(HALF_PI - angle);
        }
        public static Fixed Tan(Fixed angle)
        {
            return Sin(angle) / Cos(angle);
        }
        public static Fixed Asin(Fixed real)
        {
            bool isMoreThan45 = false;
            bool isNegForSin = false;
            if (real < 0)
            {
                isNegForSin = true;
                real = -real;
            }
            if (real > new Fixed(46333) && real != 0)
            {
                isMoreThan45 = true;
                real = Sqrt(1 - real * real);
            }
            Fixed x = 0;
            var res = real;
            var coe = real;
            for (int i = 1; i <= 3; i++)
            {
                x += 2;
                coe = coe * (x - 1) * (x - 1) / x / (x + 1) * real * real;
                res += coe;
            }
            if (isMoreThan45)
                res = HALF_PI - res;
            if (isNegForSin)
                res = -res;
            return res;
        }
        public static Fixed Acos(Fixed real)
        {
            return HALF_PI - Asin(real);
        }
        public static Fixed Atan(Fixed real)
        {
            if (real > 60)
                return HALF_PI;
            else if (real < -60)
                return -HALF_PI;
            return Asin(real / Sqrt(1 + real * real));
        }
        public static Fixed Atan2(Fixed y, Fixed x)
        {
            if (x == 0 && y == 0)
                return 0;

            Fixed result;
            if (x > 0)
                result = Atan(y / x);
            else if (x < 0)
                if (y >= 0)
                    result = PI + Atan(y / x);
                else
                    result = Atan(x / y) - PI;
            else
                result = y >= 0 ? HALF_PI : -HALF_PI;

            return result;
        }
    }
#endif
}
