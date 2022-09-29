using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RainScript.Real
{
#if FIXED
    /// <summary>
    /// 实数
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Fixed : IComparable<Fixed>, IEquatable<Fixed>
    {
        /// <summary>
        /// 实数能表示的最大值
        /// </summary>
        public static readonly Fixed MaxValue = new Fixed(long.MaxValue);
        /// <summary>
        /// 实数能表示的最小值
        /// </summary>
        public static readonly Fixed MinValue = new Fixed(long.MinValue);
        /// <summary>
        /// 实数能表示的最小正数
        /// </summary>
        public static readonly Fixed Epsilon = new Fixed(1);
        internal const int DECIMAL = 16;
        internal const long MASK_DECIMAL = (1L << DECIMAL) - 1;
        private const long MASK_DOUBLE_DECIMAL = (1L << 52) - 1;
        private const long MASK_DOUBLE_EXPORENT = long.MaxValue & ~MASK_DOUBLE_DECIMAL;
        internal readonly long value;
        internal Fixed(long value) { this.value = value; }
        /// <summary>
        /// 比较两个实数
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Fixed other)
        {
            return value.CompareTo(other.value);
        }
        /// <summary>
        /// 判断两个实数是否相等
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Fixed other)
        {
            return value == other.value;
        }
        /// <summary>
        /// 判断实数是否与目标对象相等
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Fixed r) return r.value == value;
            else return false;
        }
        /// <summary>
        /// 获取实数的哈希值
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
        /// <summary>
        /// 实数转字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this.value == 0) return "0";
            var stringBuilder = new StringBuilder();
            var value = this.value;
            if (value < 0) value = -value;
            var integer = value >> DECIMAL;
            if (integer == 0) stringBuilder.Append('0');
            else stringBuilder.Append(integer);
            var dec = value & MASK_DECIMAL;
            if (dec > 0)
            {
                stringBuilder.Append('.');
                while (dec > 0)
                {
                    dec *= 10;
                    stringBuilder.Append(dec >> DECIMAL);
                    dec &= MASK_DECIMAL;
                }
            }
            if (this.value < 0) stringBuilder.Insert(0, '-');
            return stringBuilder.ToString();
        }
        /// <summary>
        /// 使用指定的格式，将此实例的数值转换为它的等效字符串表示形式。
        /// </summary>
        /// <param name="format">一个数值格式字符串。</param>
        /// <returns>此实例的值的字符串表示形式，由 format 指定。</returns>
        /// <exception cref="System.FormatException"> format 无效。</exception>
        public string ToString(string format)
        {
            return ((double)this).ToString(format);
        }
        /// <summary>
        /// 使用指定的区域性特定格式信息，将此实例的数值转换为它的等效字符串表示形式。
        /// </summary>
        /// <param name="provider">一个提供区域性特定的格式设置信息的对象。</param>
        /// <returns>此实例的值的字符串表示形式，由 provider 指定。</returns>
        public string ToString(IFormatProvider provider)
        {
            return ((double)this).ToString(provider);
        }
        /// <summary>
        /// 使用指定的格式和区域性特定格式信息，将此实例的数值转换为它的等效字符串表示形式。
        /// </summary>
        /// <param name="format">一个数值格式字符串。</param>
        /// <param name="provider">一个提供区域性特定的格式设置信息的对象。</param>
        /// <returns>此实例的值的字符串表示形式，由 format 和 provider 指定。</returns>
        public string ToString(string format, IFormatProvider provider)
        {
            return ((double)this).ToString(format, provider);
        }
        /// <summary>
        /// 转换为字节数组
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return BitConverter.GetBytes(value);
        }
        /// <summary>
        /// 将字节数组解析为一个实数
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static Fixed Parse(byte[] buffer, int startIndex)
        {
            return new Fixed(BitConverter.ToInt64(buffer, startIndex));
        }
        /// <summary>
        /// 解析一个实数字符串
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Fixed Parse(string s)
        {
            if (TryParse(s, out Fixed real)) return real;
            throw new FormatException();
        }
        /// <summary>
        /// 尝试解析一个实数字符串
        /// </summary>
        /// <param name="s"></param>
        /// <param name="real"></param>
        /// <returns></returns>
        public static bool TryParse(string s, out Fixed real)
        {
            var value = 0L;
            var integer = true;
            var index = 0;
            var dec = 0L;
            var digit = 1;
            while (index < s.Length)
            {
                var c = s[index++];
                if (c == '.')
                {
                    if (integer)
                    {
                        integer = false;
                        value <<= DECIMAL;
                        continue;
                    }
                    else
                    {
                        real = default;
                        return false;
                    }
                }
                else if (c < '0' || c > '9')
                {
                    real = default;
                    return false;
                }
                if (integer) value = value * 10 + c - '0';
                else
                {
                    dec = dec * 10 + c - '0';
                    digit *= 10;
                    if (digit >= 10000) break;
                }
            }
            if (integer) value <<= DECIMAL;
            value += (dec << DECIMAL) / digit;
            real = new Fixed(value);
            return true;
        }
        #region operator
        /// <summary>
        /// 实数的各个二进制位取反
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        public static Fixed operator ~(Fixed real)
        {
            return new Fixed(~real.value);
        }
        /// <summary>
        /// 返回原实数
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        public static Fixed operator +(Fixed real)
        {
            return real;
        }
        /// <summary>
        /// 返回实数的相反数
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        public static Fixed operator -(Fixed real)
        {
            return new Fixed(-real.value);
        }
        /// <summary>
        /// 实数自增1
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        public static Fixed operator ++(Fixed real)
        {
            return new Fixed(real.value + (1L << DECIMAL));
        }
        /// <summary>
        /// 实数自减1
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        public static Fixed operator --(Fixed real)
        {
            return new Fixed(real.value - (1L << DECIMAL));
        }
        /// <summary>
        /// 返回两实数之和
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Fixed operator +(Fixed left, Fixed right)
        {
            return new Fixed(left.value + right.value);
        }
        /// <summary>
        /// 返回两实数之差
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Fixed operator -(Fixed left, Fixed right)
        {
            return new Fixed(left.value - right.value);
        }
        /// <summary>
        /// 返回两实数之积
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Fixed operator *(Fixed left, Fixed right)
        {
            return new Fixed((left.value * right.value) >> DECIMAL);
        }
        /// <summary>
        /// 返回两实数之商
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Fixed operator /(Fixed left, Fixed right)
        {
            return new Fixed((left.value << DECIMAL) / right.value);
        }
        /// <summary>
        /// 返回两实数的余数
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Fixed operator %(Fixed left, Fixed right)
        {
            return new Fixed(left.value % right.value);
        }
        /// <summary>
        /// 返回两实数对应位或运算后的实数
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Fixed operator |(Fixed left, Fixed right)
        {
            return new Fixed(left.value | right.value);
        }
        /// <summary>
        /// 返回两实数对应位做与运算后的实数
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Fixed operator &(Fixed left, Fixed right)
        {
            return new Fixed(left.value & right.value);
        }
        /// <summary>
        /// 返回两实数对应位做异或运算后的实数
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Fixed operator ^(Fixed left, Fixed right)
        {
            return new Fixed(left.value ^ right.value);
        }
        /// <summary>
        /// 右移
        /// </summary>
        /// <param name="real"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static Fixed operator >>(Fixed real, int amount)
        {
            return new Fixed(real.value >> amount);
        }
        /// <summary>
        /// 左移
        /// </summary>
        /// <param name="real"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static Fixed operator <<(Fixed real, int amount)
        {
            return new Fixed(real.value << amount);
        }
        /// <summary>
        /// 判断两实数是否相等
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(Fixed left, Fixed right)
        {
            return left.value == right.value;
        }
        /// <summary>
        /// 判断两实数是否不等
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(Fixed left, Fixed right)
        {
            return left.value != right.value;
        }
        /// <summary>
        /// 判断左边是否大于右边
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator >(Fixed left, Fixed right)
        {
            return left.value > right.value;
        }
        /// <summary>
        /// 判断左边是否小于右边
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator <(Fixed left, Fixed right)
        {
            return left.value < right.value;
        }
        /// <summary>
        /// 判断左边是否大于等于右边
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator >=(Fixed left, Fixed right)
        {
            return left.value >= right.value;
        }
        /// <summary>
        /// 判断左边是否小于等于右边
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator <=(Fixed left, Fixed right)
        {
            return left.value <= right.value;
        }
        /// <summary>
        /// 乘法操作（速度较慢但不容易溢出）
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Fixed Multiply(Fixed left, Fixed right)
        {
            var ld = left.value & MASK_DECIMAL;
            var li = left.value >> DECIMAL;
            var rd = right.value & MASK_DECIMAL;
            var ri = right.value >> DECIMAL;
            return (ld * rd >> DECIMAL) + ld * ri + rd * li + (li * ri << DECIMAL);
        }
        #endregion operator
        #region implicit & explicit
        /// <summary>
        /// 实数转长整型
        /// </summary>
        /// <param name="real"></param>
        public static explicit operator long(Fixed real)
        {
            if (real.value < 0)
                return real.value + MASK_DECIMAL >> DECIMAL;
            else
                return real.value >> DECIMAL;
        }
        /// <summary>
        /// 长整型转实数
        /// </summary>
        /// <param name="l"></param>
        public static implicit operator Fixed(long l)
        {
            return new Fixed(l << DECIMAL);
        }
        /// <summary>
        /// 实数转单精度浮点数
        /// </summary>
        /// <param name="real"></param>
        public static implicit operator float(Fixed real)
        {
            return (float)real.value / (1L << DECIMAL);
        }
        /// <summary>
        /// 实数转双精度浮点数
        /// </summary>
        /// <param name="real"></param>
        public static implicit operator double(Fixed real)
        {
            return (double)real.value / (1L << DECIMAL);
        }
        /// <summary>
        /// 双精度浮点数转实数
        /// </summary>
        /// <param name="d"></param>
        public static explicit operator Fixed(double d)
        {
            long v = *(long*)&d;
            long dec = (v & MASK_DOUBLE_DECIMAL) + (1L << 52);
            int pow = (int)(((v & MASK_DOUBLE_EXPORENT) >> 52) - 1023 - 52 + DECIMAL);
            if (pow > 0)
            {
                if (pow > 63) throw new OverflowException();
                dec <<= pow;
            }
            else if (pow < 0)
            {
                if (pow < -63) return new Fixed(0);
                dec >>= -pow;
            }
            dec &= long.MaxValue;
            if (v >= 0)
            {
                return new Fixed(dec);
            }
            else
            {
                return new Fixed(-dec);
            }
        }
        #endregion implicit & explicit
    }
#endif
}
