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
    /// 三维向量
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Real3
    {
        /// <summary>
        /// 向量坐标
        /// </summary>
        public real x, y, z;
        /// <summary>
        /// 三维向量
        /// </summary>
        /// <param name="x">x轴坐标</param>
        /// <param name="y">y轴坐标</param>
        /// <param name="z">z轴坐标</param>
        public Real3(real x, real y, real z)
        {
            this.x = x; this.y = y; this.z = z;
        }
        #region overread
        /// <summary>
        /// 向量转字符串
        /// </summary>
        public override string ToString()
        {
            return string.Format("({0},{1},{2})", x, y, z);
        }
        /// <summary>
        /// 比较两个向量坐标值是否相等
        /// </summary>
        /// <param name="obj">比较对象</param>
        public override bool Equals(object obj)
        {
            if (obj is Real3 v) return this == v;
            else return false;
        }
        /// <summary>
        /// 返回此实例的哈希代码
        /// </summary>
        public override int GetHashCode()
        {
            var hashcode = z.GetHashCode();
            hashcode = y.GetHashCode() ^ ((hashcode >> 25) | (hashcode << 7));
            hashcode = x.GetHashCode() ^ ((hashcode >> 25) | (hashcode << 7));
            return hashcode;
        }
        #endregion
        #region 运算
        /// <summary>
        /// 向量方向上的单位向量
        /// </summary>
        public Real3 normalized
        {
            get
            {
                var sm = sqrMagnitude;
                if (sm > 0)
                {
                    real l = Math.Sqrt(sm);
                    return new Real3(x / l, y / l, z / l);
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
                }
            }
        }
        /// <summary>
        /// 向量的插值
        /// </summary>
        public static Real3 Lerp(Real3 v1, Real3 v2, real l)
        {
            return v1 + (v2 - v1) * l;
        }
        /// <summary>
        /// 返回由两个向量中对应坐标值取Max后构成的向量
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real3 Max(Real3 v1, Real3 v2)
        {
            return new Real3(Math.Max(v1.x, v2.x), Math.Max(v1.y, v2.y), Math.Max(v1.z, v2.z));
        }
        /// <summary>
        /// 返回由两个向量中对应坐标值取Min后构成的向量
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real3 Min(Real3 v1, Real3 v2)
        {
            return new Real3(Math.Min(v1.x, v2.x), Math.Min(v1.y, v2.y), Math.Min(v1.z, v2.z));
        }
        /// <summary>
        /// 获取两个向量的夹角
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static real Angle(Real3 v1, Real3 v2)
        {
            real d = Dot(v1, v2);
            d /= Math.Sqrt(v1.sqrMagnitude * v2.sqrMagnitude);
            return Math.Acos(d);
        }
        /// <summary>
        /// 获取两个向量的叉乘
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real3 Cross(Real3 v1, Real3 v2)
        {
            return new Real3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
        }
        /// <summary>
        /// 向量的点乘
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static real Dot(Real3 v1, Real3 v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }
        #endregion
        #region 运算符重载
        /// <summary>
        /// 向量与实数相乘
        /// </summary>
        /// <param name="v">向量</param>
        /// <param name="r">实数</param>
        public static Real3 operator *(Real3 v, real r)
        {
            return new Real3(v.x * r, v.y * r, v.z * r);
        }
        /// <summary>
        /// 实数与向量相乘
        /// </summary>
        /// <param name="r">实数</param>
        /// <param name="v">向量</param>
        public static Real3 operator *(real r, Real3 v)
        {
            return new Real3(v.x * r, v.y * r, v.z * r);
        }
        /// <summary>
        /// 缩放
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Real3 operator *(Real3 v1, Real3 v2)
        {
            return new Real3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }
        /// <summary>
        /// 向量与实数相除
        /// </summary>
        /// <param name="v">向量</param>
        /// <param name="r">实数</param>
        public static Real3 operator /(Real3 v, real r)
        {
            return new Real3(v.x / r, v.y / r, v.z / r);
        }
        /// <summary>
        /// 实数与向量相除
        /// </summary>
        /// <param name="r">实数</param>
        /// <param name="v">向量</param>
        public static Real3 operator /(real r, Real3 v)
        {
            return new Real3(r / v.x, r / v.y, r / v.z);
        }
        /// <summary>
        /// 缩放
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Real3 operator /(Real3 v1, Real3 v2)
        {
            return new Real3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
        }
        /// <summary>
        /// 向量与实数求余
        /// </summary>
        /// <param name="v">向量</param>
        /// <param name="r">实数</param>
        public static Real3 operator %(Real3 v, real r)
        {
            return new Real3(v.x % r, v.y % r, v.z % r);
        }
        /// <summary>
        /// 实数与向量求余
        /// </summary>
        /// <param name="r">实数</param>
        /// <param name="v">向量</param>
        public static Real3 operator %(real r, Real3 v)
        {
            return new Real3(r % v.x, r % v.y, r % v.z);
        }
        /// <summary>
        /// 求余
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Real3 operator %(Real3 v1, Real3 v2)
        {
            return new Real3(v1.x % v2.x, v1.y % v2.y, v1.z % v2.z);
        }
        /// <summary>
        /// 向量加法
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real3 operator +(Real3 v1, Real3 v2)
        {
            return new Real3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }
        /// <summary>
        /// 向量减法
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static Real3 operator -(Real3 v1, Real3 v2)
        {
            return new Real3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }
        /// <summary>
        /// 向量的反向量
        /// </summary>
        /// <param name="v">向量</param>
        public static Real3 operator -(Real3 v)
        {
            return new Real3(-v.x, -v.y, -v.z);
        }
        /// <summary>
        /// 比较两个向量坐标值是否相等
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static bool operator ==(Real3 v1, Real3 v2)
        {
            return (v1.x == v2.x) && (v1.y == v2.y) && (v1.z == v2.z);
        }
        /// <summary>
        /// 比较两个向量坐标值是否不想等
        /// </summary>
        /// <param name="v1">向量</param>
        /// <param name="v2">向量</param>
        public static bool operator !=(Real3 v1, Real3 v2)
        {
            return (v1.x != v2.x) || (v1.y != v2.y) || (v1.z != v2.z);
        }
        #endregion
        /// <summary>
        /// 三维向量转二维向量
        /// </summary>
        /// <param name="v">三维向量</param>
        public static explicit operator Real2(Real3 v)
        {
            return new Real2(v.x, v.y);
        }
        #region 常量
        /// <summary>
        /// 原点向量(0, 0, 0)
        /// </summary>
        public static Real3 zero { get { return zeroVector; } }
        /// <summary>
        /// 单位向量(1, 1, 1)
        /// </summary>
        public static Real3 one { get { return oneVector; } }
        /// <summary>
        /// 方向向右的单位向量(1, 0, 0)
        /// </summary>
        public static Real3 right { get { return rightVector; } }
        /// <summary>
        /// 方向向左的单位向量(-1, 0, 0)
        /// </summary>
        public static Real3 left { get { return leftVector; } }
        /// <summary>
        /// 方向上的单位向量(0, 1, 0)
        /// </summary>
        public static Real3 up { get { return upVector; } }
        /// <summary>
        /// 方向向下的单位向量(0, -1, 0)
        /// </summary>
        public static Real3 down { get { return downVector; } }
        /// <summary>
        /// 方向向前的单位向量(0, 0, 1)
        /// </summary>
        public static Real3 forward { get { return forwardVector; } }
        /// <summary>
        /// 方向向后的单位向量(0, 0, -1)
        /// </summary>
        public static Real3 back { get { return backVector; } }
        private static readonly Real3 zeroVector = new Real3(0, 0, 0);
        private static readonly Real3 oneVector = new Real3(1, 1, 1);
        private static readonly Real3 rightVector = new Real3(1, 0, 0);
        private static readonly Real3 leftVector = new Real3(-1, 0, 0);
        private static readonly Real3 upVector = new Real3(0, 1, 0);
        private static readonly Real3 downVector = new Real3(0, -1, 0);
        private static readonly Real3 forwardVector = new Real3(0, 0, 1);
        private static readonly Real3 backVector = new Real3(0, 0, -1);
        #endregion
    }
}
