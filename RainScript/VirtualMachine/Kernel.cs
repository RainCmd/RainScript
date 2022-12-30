using System;

namespace RainScript.VirtualMachine
{
    /// <summary>
    /// 虚拟机启动参数
    /// </summary>
    public readonly struct KernelParameter
    {
        /// <summary>
        /// 程序集加载器
        /// </summary>
        public readonly Func<string, Library> libraryLoader;
        /// <summary>
        /// 程序集交互对象加载器
        /// </summary>
        public readonly Func<string, IPerformer> performerLoader;
        /// <summary>
        /// 托管堆初始大小
        /// </summary>
        public readonly uint heapCapacity;
        /// <summary>
        /// 托管堆句柄头容器初始大小
        /// </summary>
        public readonly uint handleHeadCapacity;
        /// <summary>
        /// 快速GC最大代数
        /// </summary>
        public readonly uint generation;
        /// <summary>
        /// 字符串容器初始大小
        /// </summary>
        public readonly uint stringCapacity;
        /// <summary>
        /// 携程容器初始大小
        /// </summary>
        public readonly uint coroutineCapacity;
        /// <summary>
        /// 实体容器初始大小
        /// </summary>
        public readonly uint entityCapacity;
        /// <summary>
        /// 当实体对象被添加到虚拟机时调用
        /// </summary>
        public readonly Action<object> entityReference;
        /// <summary>
        /// 当实体对象在虚拟机中引用数量归零时调用
        /// </summary>
        public readonly Action<object> entityRelease;
        /// <summary>
        /// 虚拟机启动参数
        /// </summary>
        /// <param name="libraryLoader">程序集加载器</param>
        /// <param name="performerLoader">交互对象加载器</param>
        /// <param name="heapCapacity">托管堆初始大小</param>
        /// <param name="handleHeadCapacity">托管堆句柄头容器初始大小</param>
        /// <param name="generation">快速GC最大代数</param>
        /// <param name="stringCapacity">字符串初始容器大小</param>
        /// <param name="coroutineCapacity">携程容器初始大小</param>
        /// <param name="entityCapacity">实体容器初始大小</param>
        /// <param name="entityReference">当实体对象被添加到虚拟机时调用</param>
        /// <param name="entityRelease">当实体对象在虚拟机中引用数量归零时调用</param>
        public KernelParameter(Func<string, Library> libraryLoader = null, Func<string, IPerformer> performerLoader = null, uint heapCapacity = 0x10000, uint handleHeadCapacity = 1024, uint generation = 8, uint stringCapacity = 256, uint coroutineCapacity = 8, uint entityCapacity = 64, Action<object> entityReference = null, Action<object> entityRelease = null)
        {
            this.libraryLoader = libraryLoader;
            this.performerLoader = performerLoader;
            this.heapCapacity = heapCapacity;
            this.handleHeadCapacity = handleHeadCapacity;
            this.generation = generation;
            this.stringCapacity = stringCapacity;
            this.coroutineCapacity = coroutineCapacity;
            this.entityCapacity = entityCapacity;
            this.entityReference = entityReference;
            this.entityRelease = entityRelease;
        }
    }
    /// <summary>
    /// 虚拟机状态
    /// </summary>
    public readonly struct KernelState
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
        /// <param name="libraries">启动程序集</param>
        public Kernel(params Library[] libraries) : this(new KernelParameter(), libraries) { }
        /// <summary>
        /// 核心
        /// </summary>
        /// <param name="libraries">启动程序集</param>
        /// <param name="parameter">启动参数</param>
        public Kernel(KernelParameter parameter, params Library[] libraries)
        {
            performerLoader = parameter.performerLoader;
            manipulator = new EntityManipulator(parameter);
            stringAgency = new StringAgency(parameter.stringCapacity);
            heapAgency = new HeapAgency(this, parameter);
            coroutineAgency = new CoroutineAgency(this, parameter);
            libraryAgency = new LibraryAgency(this, parameter);
            libraryAgency.Init(libraries);
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
        /// <param name="full">完全GC</param>
        public void Collect(bool full)
        {
            heapAgency.GC(full);
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
