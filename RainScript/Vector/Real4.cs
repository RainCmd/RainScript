#pragma warning disable IDE1006
using System;
using System.Runtime.InteropServices;
#if FIXED
using real = RainScript.Real.Fixed;
using Math = RainScript.Real.Math;
#else
using real = System.Double;
using Math = System.Math;
#endif

namespace RainScript.Vector
{
    /// <summary>
    /// 四维向量
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Real4
    {
        /// <summary>
        /// 向量坐标
        /// </summary>
        public real x, y, z, w;
        /// <summary>
        /// 四维向量
        /// </summary>
        /// <param name="x">x轴坐标</param>
        /// <param name="y">y轴坐标</param>
        /// <param name="z">z轴坐标</param>
        /// <param name="w">w轴坐标</param>
        public Real4(real x, real y, real z, real w)
        {
            this.x = x; this.y = y; this.z = z; this.w = w;
        }
        #region overread
        /// <summary>
        /// 向量转字符串
        /// </summary>
        public override string ToString()
        {
            return string.Format("({0},{1},{2},{3})", x, y, z, w);
        }
        /// <summary>
        /// 比较两个向量坐标值是否相等
        /// </summary>
        /// <param name="obj">比较对象</param>
        public override bool Equals(object obj)
        {
            if (obj is Real4 v) return this == v;
            else return false;
        }
        /// <summary>
        /// 返回此实例的哈希代码
        /// </summary>
        public override int GetHashCode()
        {
            var hashcode = w.GetHashCode();
            hashcode = z.GetHashCode() ^ ((hashcode >> 25) | (hashcode << 7));
            hashcode = y.GetHashCode() ^ ((hashcode >> 25) | (hashcode << 7));
            hashcode = x.GetHashCode() ^ ((hashcode >> 25) | (hashcode << 7));
            return hashcode;
        }
        #endregion
        #region 运算
        /// <summary>
        /// 向量方向上的单位向量
        /// </summary>
        public Real4 normalized
        {
            get
            {
                var sm = sqrMagnitude;
                if (sm > 0)
                {
                    real l = Math.Sqrt(sm);
                    return new Real4(x / l, y / l, z / l, w / l);
                }
                return zero;
            }
        }
        /// <summary>
        /// 向量长度的平方
        /// </summary>
        public real sqrMagnitude
        {
            get
            {
                return Dot(this, this);
            }
        }
        /// <summary>
        /// 向量的长度
        /// </summary>
        public real magnitude
        {
            get
            {
                return Math.Sqrt(sqrMagnitude);
            }
            set
            {
                var sm = sqrMagnitude;
                if (sm > 0)
                {
                    real s = value / Math.Sqrt(sm);
                    x *= s;
                    y *= s;
                    z *= s;
                    w *= s;
                }
            }
        }
        /// <summary>
        /// 插值
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        public static Real4 Lerp(Real4 v1, Real4 v2, real l)
        {
            return v1 + (v2 - v1) * l;
        }
        /// <summary>
        /// 返回由两个向量中对应坐标值取Max后构成的向量
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real4 Max(Real4 v1, Real4 v2)
        {
            return new Real4(Math.Max(v1.x, v2.x), Math.Max(v1.y, v2.y), Math.Max(v1.z, v2.z), Math.Max(v1.w, v2.w));
        }
        /// <summary>
        /// 返回由两个向量中对应坐标值取Min后构成的向量
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real4 Min(Real4 v1, Real4 v2)
        {
            return new Real4(Math.Min(v1.x, v2.x), Math.Min(v1.y, v2.y), Math.Min(v1.z, v2.z), Math.Min(v1.w, v2.w));
        }
        /// <summary>
        /// 获取两个向量的夹角
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static real Angle(Real4 v1, Real4 v2)
        {
            real d = Dot(v1, v2);
            d /= Math.Sqrt(v1.sqrMagnitude * v2.sqrMagnitude);
            return Math.Acos(d);
        }
        /// <summary>
        /// 向量的点乘
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static real Dot(Real4 v1, Real4 v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z + v1.w * v2.w;
        }
        #endregion
        #region 运算符重载
        /// <summary>
        /// 向量与实数相乘
        /// </summary>
        /// <param name="v">向量</param>
        /// <param name="r">实数</param>
        public static Real4 operator *(Real4 v, real r)
        {
            return new Real4(v.x * r, v.y * r, v.z * r, v.w * r);
        }
        /// <summary>
        /// 实数与向量相乘
        /// </summary>
        /// <param name="r">实数</param>
        /// <param name="v">向量</param>
        public static Real4 operator *(real r, Real4 v)
        {
            return new Real4(v.x * r, v.y * r, v.z * r, v.w * r);
        }
        /// <summary>
        /// 缩放
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Real4 operator *(Real4 v1, Real4 v2)
        {
            return new Real4(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z, v1.w * v2.w);
        }
        /// <summary>
        /// 向量与实数相除
        /// </summary>
        /// <param name="v">向量</param>
        /// <param name="r">实数</param>
        public static Real4 operator /(Real4 v, real r)
        {
            return new Real4(v.x / r, v.y / r, v.z / r, v.w / r);
        }
        /// <summary>
        /// 实数与向量相除
        /// </summary>
        /// <param name="r">实数</param>
        /// <param name="v">向量</param>
        public static Real4 operator /(real r, Real4 v)
        {
            return new Real4(r / v.x, r / v.y, r / v.z, r / v.w);
        }
        /// <summary>
        /// 缩放
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Real4 operator /(Real4 v1, Real4 v2)
        {
            return new Real4(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z, v1.w / v2.w);
        }
        /// <summary>
        /// 向量与实数求余
        /// </summary>
        /// <param name="v">向量</param>
        /// <param name="r">实数</param>
        public static Real4 operator %(Real4 v, real r)
        {
            return new Real4(v.x % r, v.y % r, v.z % r, v.w % r);
        }
        /// <summary>
        /// 实数与向量求余
        /// </summary>
        /// <param name="r">实数</param>
        /// <param name="v">向量</param>
        public static Real4 operator %(real r, Real4 v)
        {
            return new Real4(r % v.x, r % v.y, r % v.z, r % v.w);
        }
        /// <summary>
        /// 求余
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Real4 operator %(Real4 v1, Real4 v2)
        {
            return new Real4(v1.x % v2.x, v1.y % v2.y, v1.z % v2.z, v1.w % v2.w);
        }
        /// <summary>
        /// 向量加法
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real4 operator +(Real4 v1, Real4 v2)
        {
            return new Real4(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w);
        }
        /// <summary>
        /// 向量减法
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real4 operator -(Real4 v1, Real4 v2)
        {
            return new Real4(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z, v1.w - v2.w);
        }
        /// <summary>
        /// 向量的反向量
        /// </summary>
        /// <param name="v">向量</param>
        public static Real4 operator -(Real4 v)
        {
            return new Real4(-v.x, -v.y, -v.z, -v.w);
        }
        /// <summary>
        /// 比较两个向量坐标值是否相等
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static bool operator ==(Real4 v1, Real4 v2)
        {
            return (v1.x == v2.x) && (v1.y == v2.y) && (v1.z == v2.z) && (v1.w == v2.w);
        }
        /// <summary>
        /// 比较两个向量坐标值是否不想等
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static bool operator !=(Real4 v1, Real4 v2)
        {
            return (v1.x != v2.x) || (v1.y != v2.y) || (v1.z != v2.z) || (v1.w != v2.w);
        }
        #endregion
        /// <summary>
        /// 转低维向量
        /// </summary>
        /// <param name="v">向量</param>
        public static explicit operator Real2(Real4 v)
        {
            return new Real2(v.x, v.y);
        }
        /// <summary>
        /// 转低维向量
        /// </summary>
        /// <param name="v">向量</param>
        public static explicit operator Real3(Real4 v)
        {
            return new Real3(v.x, v.y, v.z);
        }
        /// <summary>
        /// 转高维向量
        /// </summary>
        /// <param name="v">向量</param>
        public static explicit operator Real4(Real2 v)
        {
            return new Real4(v.x, v.y, 0, 0);
        }
        /// <summary>
        /// 转高维向量
        /// </summary>
        /// <param name="v">向量</param>
        public static explicit operator Real4(Real3 v)
        {
            return new Real4(v.x, v.y, v.z, 0);
        }
        #region 常量
        /// <summary>
        /// 原点向量(0, 0, 0, 0)
        /// </summary>
        public static Real4 zero { get { return zeroVector; } }
        /// <summary>
        /// 单位向量(1, 1, 1, 1)
        /// </summary>
        public static Real4 one { get { return oneVector; } }
        private static readonly Real4 zeroVector = new Real4(0, 0, 0, 0);
        private static readonly Real4 oneVector = new Real4(1, 1, 1, 1);
        #endregion
    }
}
