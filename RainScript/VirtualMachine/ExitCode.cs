namespace RainScript.VirtualMachine
{
    /// <summary>
    /// 退出原因
    /// </summary>
    public enum ExitCode : ulong
    {
        /// <summary>
        /// 没有错误
        /// </summary>
        None = 0,
        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 0x7000_0000_0000_0000,
        /// <summary>
        /// 空指针
        /// </summary>
        NullReference,
        /// <summary>
        /// 越界
        /// </summary>
        OutOfRange,
        /// <summary>
        /// 除零
        /// </summary>
        DivideByZero,
        /// <summary>
        /// 无效的类型转换
        /// </summary>
        InvalidCast,
        /// <summary>
        /// 无效的携程
        /// </summary>
        InvalidCoroutine,
        /// <summary>
        /// 携程未完成
        /// </summary>
        CoroutineNotCompleted,
        /// <summary>
        /// 内部函数调用异常
        /// </summary>
        NativeException,
    }
}
