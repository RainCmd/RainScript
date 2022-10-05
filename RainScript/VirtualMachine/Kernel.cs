using System;

namespace RainScript.VirtualMachine
{
    /// <summary>
    /// 虚拟机状态
    /// </summary>
    public struct KernelState
    {
        /// <summary>
        /// 字符串数量
        /// </summary>
        public readonly long stringCount;
        /// <summary>
        /// 句柄数量
        /// </summary>
        public readonly long handleCount;
        /// <summary>
        /// 携程数量
        /// </summary>
        public readonly long coroutineCount;
        /// <summary>
        /// 实体数量
        /// </summary>
        public readonly long entityCount;
        /// <summary>
        /// 托管堆大小
        /// </summary>
        public readonly long heapTotalMemory;
        internal KernelState(long stringCount, long handleCount, long coroutineCount, long entityCount, long heapTotalMemory)
        {
            this.stringCount = stringCount;
            this.handleCount = handleCount;
            this.coroutineCount = coroutineCount;
            this.entityCount = entityCount;
            this.heapTotalMemory = heapTotalMemory;
        }
    }
    /// <summary>
    /// 携程退出
    /// </summary>
    /// <param name="stacks">栈</param>
    /// <param name="code">退出码</param>
    public delegate void CoroutineExit(StackFrame[] stacks, long code);
    /// <summary>
    /// 命中断点
    /// </summary>
    public delegate void HitBreakpoint();
    /// <summary>
    /// 核心
    /// </summary>
    public class Kernel : IDisposable
    {
        private bool disposed = false;
        internal bool step = false;
        internal readonly Func<string, IPerformer> performerLoader;
        internal readonly EntityManipulator manipulator;
        internal readonly StringAgency stringAgency;
        internal readonly HeapAgency heapAgency;
        internal readonly CoroutineAgency coroutineAgency;
        internal readonly LibraryAgency libraryAgency;
        internal readonly Random random = new Random();
        /// <summary>
        /// 携程非正常退出时触发
        /// </summary>
        public event CoroutineExit OnExit;
        /// <summary>
        /// 触发断点
        /// </summary>
        public event Action OnHitBreakpoint;
        /// <summary>
        /// 核心
        /// </summary>
        /// <param name="library">启动程序集</param>
        /// <param name="libraryLoader">程序集加载器</param>
        /// <param name="performerLoader">程序集交互对象加载器</param>
        /// <param name="entityCapacity">实体容器初始大小</param>
        public Kernel(Library library, Func<string, Library> libraryLoader, Func<string, IPerformer> performerLoader, int entityCapacity = 128)
        {
            this.performerLoader = performerLoader;
            manipulator = new EntityManipulator(entityCapacity);
            stringAgency = new StringAgency();
            heapAgency = new HeapAgency(this);
            coroutineAgency = new CoroutineAgency(this);
            libraryAgency = new LibraryAgency(this, libraryLoader);
            libraryAgency.Init(library);
        }
        /// <summary>
        /// 获取函数句柄
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="libraryName"></param>
        /// <returns></returns>
        public FunctionHandle GetFunctionHandle(string functionName, string libraryName = null)
        {
            if (string.IsNullOrEmpty(libraryName)) return libraryAgency.GetFunctionHandle(functionName);
            else return libraryAgency.GetFunctionHandle(functionName, libraryName);
        }
        /// <summary>
        /// 获取函数所有重载的句柄
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="libraryName"></param>
        /// <returns></returns>
        public FunctionHandle[] GetFunctionHandles(string functionName, string libraryName)
        {
            return libraryAgency.GetFunctionHandles(functionName, libraryName);
        }
        /// <summary>
        /// 获取一个函数的调用
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public InvokerHandle Invoker(FunctionHandle handle)
        {
            return new InvokerHandle(coroutineAgency.Invoker(handle));
        }
        /// <summary>
        /// 主逻辑更新
        /// </summary>
        public void Update()
        {
            coroutineAgency.Update();
        }
        /// <summary>
        /// 获取当前状态
        /// </summary>
        /// <returns></returns>
        public KernelState GetState()
        {
            return new KernelState(stringAgency.GetStringCount(), heapAgency.GetHandleCount(), coroutineAgency.GetCoroutineCount(), manipulator.GetEntityCount(), heapAgency.GetHeapTop());
        }
        /// <summary>
        /// 获取当前正在执行的携程的栈帧
        /// </summary>
        /// <returns></returns>
        public StackFrame[] GetInvokingStackFrames()
        {
            return coroutineAgency.GetInvokingStackFrames();
        }
        /// <summary>
        /// 托管堆回收
        /// </summary>
        public void Collect()
        {
            heapAgency.GC();
        }
        internal void OnExitEvent(StackFrame[] stacks, long code)
        {
            OnExit?.Invoke(stacks, code);
        }
        internal void OnHitBreakpointEvent()
        {
            OnHitBreakpoint?.Invoke();
        }
        /// <summary>
        /// 析构
        /// </summary>
        ~Kernel() { if (!disposed) Dispose(); }
        /// <summary>
        /// 销毁
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Dispose()
        {
            if (disposed) throw new ObjectDisposedException(nameof(Kernel));
            disposed = true;
            libraryAgency.Dispose();
            coroutineAgency.Dispose();
            heapAgency.Dispose();
            stringAgency.Dispose();
        }
        /// <summary>
        /// 有效的
        /// </summary>
        /// <param name="kernel"></param>
        public static implicit operator bool(Kernel kernel)
        {
            return kernel != null && !kernel.disposed;
        }
    }
}
