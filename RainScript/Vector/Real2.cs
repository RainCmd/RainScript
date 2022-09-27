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
    /// 二维向量
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Real2
    {
        /// <summary>
        /// 坐标
        /// </summary>
        public real x, y;
        /// <summary>
        /// 二维向量
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        public Real2(real x, real y)
        {
            this.x = x; this.y = y;
        }
        #region overread
        /// <summary>
        /// 向量转字符串
        /// </summary>
        public override string ToString()
        {
            return string.Format("({0},{1})", x, y);
        }
        /// <summary>
        /// 比较向量坐标值是否相等
        /// </summary>
        /// <param name="obj">比较对象</param>
        public override bool Equals(object obj)
        {
            if (obj is Real2 v) return this == v;
            else return false;
        }
        /// <summary>
        /// 返回此实例的哈希代码
        /// </summary>
        public override int GetHashCode()
        {
            var hashcode = y.GetHashCode();
            hashcode = x.GetHashCode() ^ ((hashcode >> 25) | (hashcode << 7));
            return hashcode;
        }
        #endregion
        #region 运算
        /// <summary>
        /// 向量方向上的单位向量
        /// </summary>
        public Real2 normalized
        {
            get
            {
                if (sqrMagnitude > 0)
                {
                    real l = magnitude;
                    return new Real2(x / l, y / l);
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
                if (sqrMagnitude > 0)
                {
                    real s = value / magnitude;
                    x *= s;
                    y *= s;
                }
            }
        }
        /// <summary>
        /// 向量的插值
        /// </summary>
        public static Real2 Lerp(Real2 v1, Real2 v2, real l)
        {
            return v1 + (v2 - v1) * l;
        }
        /// <summary>
        /// 返回由两个向量中对应坐标值取Max后构成的向量
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real2 Max(Real2 v1, Real2 v2)
        {
            return new Real2(Math.Max(v1.x, v2.x), Math.Max(v1.y, v2.y));
        }
        /// <summary>
        /// 返回由两个向量中对应坐标值取Min后构成的向量
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real2 Min(Real2 v1, Real2 v2)
        {
            return new Real2(Math.Min(v1.x, v2.x), Math.Min(v1.y, v2.y));
        }
        /// <summary>
        /// 获取两个向量的夹角
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static real Angle(Real2 v1, Real2 v2)
        {
            real d = Dot(v1, v2);
            d /= Math.Sqrt(v1.sqrMagnitude * v2.sqrMagnitude);
            return Math.Acos(d);
        }
        /// <summary>
        /// 叉乘
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static real Cross(Real2 v1, Real2 v2)
        {
            return v1.x * v2.y - v2.x * v1.y;
        }
        /// <summary>
        /// 点积
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static real Dot(Real2 v1, Real2 v2)
        {
            return v1.x * v2.x + v2.y * v1.y;
        }
        #endregion
        #region 运算符重载
        /// <summary>
        /// 向量与实数的乘积
        /// </summary>
        /// <param name="v">向量</param>
        /// <param name="r">实数</param>
        public static Real2 operator *(Real2 v, real r)
        {
            return new Real2(v.x * r, v.y * r);
        }
        /// <summary>
        /// 实数与向量的乘积
        /// </summary>
        /// <param name="r">实数</param>
        /// <param name="v">向量</param>
        public static Real2 operator *(real r, Real2 v)
        {
            return new Real2(v.x * r, v.y * r);
        }
        /// <summary>
        /// 缩放
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real2 operator *(Real2 v1, Real2 v2)
        {
            return new Real2(v1.x * v2.x, v1.y * v2.y);
        }
        /// <summary>
        /// 向量与实数相除（向量中每个坐标分别与实数作除法）
        /// </summary>
        /// <param name="v">向量</param>
        /// <param name="r">实数</param>
        public static Real2 operator /(Real2 v, real r)
        {
            return new Real2(v.x / r, v.y / r);
        }
        /// <summary>
        /// 实数与向量相除（实数与向量中每个坐标分别作除法）
        /// </summary>
        /// <param name="r">实数</param>
        /// <param name="v">向量</param>
        public static Real2 operator /(real r, Real2 v)
        {
            return new Real2(r / v.x, r / v.y);
        }
        /// <summary>
        /// 缩放
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real2 operator /(Real2 v1, Real2 v2)
        {
            return new Real2(v1.x / v2.x, v1.y / v2.y);
        }
        /// <summary>
        /// 求余
        /// </summary>
        /// <param name="r">实数</param>
        /// <param name="v">向量</param>
        public static Real2 operator %(real r, Real2 v)
        {
            return new Real2(r % v.x, r % v.y);
        }
        /// <summary>
        /// 求余
        /// </summary>
        /// <param name="v">向量</param>
        /// <param name="r">实数</param>
        public static Real2 operator %(Real2 v, real r)
        {
            return new Real2(v.x % r, v.y % r);
        }
        /// <summary>
        /// 求余
        /// </summary>
        /// <param name="r">向量</param>
        /// <param name="v">向量</param>
        public static Real2 operator %(Real2 r, Real2 v)
        {
            return new Real2(r.x % v.x, r.y % v.y);
        }
        /// <summary>
        /// 向量加法
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real2 operator +(Real2 v1, Real2 v2)
        {
            return new Real2(v2.x + v1.x, v2.y + v1.y);
        }
        /// <summary>
        /// 向量减法
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real2 operator -(Real2 v1, Real2 v2)
        {
            return new Real2(v1.x - v2.x, v1.y - v2.y);
        }
        /// <summary>
        /// 相反向量
        /// </summary>
        /// <param name="v">向量</param>
        public static Real2 operator -(Real2 v)
        {
            return new Real2(-v.x, -v.y);
        }
        /// <summary>
        /// 比较两个向量坐标值是否相等
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static bool operator ==(Real2 v1, Real2 v2)
        {
            return v2.x == v1.x && v2.y == v1.y;
        }
        /// <summary>
        /// 比较两个向量坐标值是否不想等
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static bool operator !=(Real2 v1, Real2 v2)
        {
            return v2.x != v1.x || v2.y != v1.y;
        }
        #endregion
        /// <summary>
        /// 二维向量转三维向量
        /// </summary>
        /// <param name="v">二维向量</param>
        public static implicit operator Real3(Real2 v)
        {
            return new Real3(v.x, v.y, 0);
        }
        /// <summary>
        /// 原点向量
        /// </summary>
        public static Real2 zero { get { return zeroVector; } }
        /// <summary>
        /// 单位向量
        /// </summary>
        public static Real2 one { get { return oneVector; } }
        private static readonly Real2 zeroVector = new Real2(0, 0);
        private static readonly Real2 oneVector = new Real2(1, 1);
    }
}

