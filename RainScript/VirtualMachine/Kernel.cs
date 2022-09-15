using System;

namespace RainScript.VirtualMachine
{
    /// <summary>
    /// 核心
    /// </summary>
    public class Kernel : IDisposable
    {
        private bool disposed = false;
        internal readonly Func<string, IPerformer> performerLoader;
        internal readonly EntityManipulator manipulator;
        internal readonly StringAgency stringAgency;
        internal readonly HeapAgency heapAgency;
        internal readonly CoroutineAgency coroutineAgency;
        internal readonly LibraryAgency libraryAgency;
        /// <summary>
        /// 携程非正常退出时触发
        /// </summary>
        public Action<StackFrame[], long> OnExit;
        /// <summary>
        /// 核心
        /// </summary>
        /// <param name="library">启动程序集</param>
        /// <param name="libraryLoader">程序集加载器</param>
        /// <param name="performerLoader">程序集交互对象加载器</param>
        /// <param name="entityCapacity">实体容器初始大小</param>
        public Kernel(Library library, Func<string, Library> libraryLoader, Func<string, IPerformer> performerLoader, int entityCapacity = 1024)
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
        /// 获取栈帧详细数据，需要有
        /// </summary>
        /// <param name="frame">帧数据</param>
        /// <param name="symbolLoader">符号表</param>
        /// <param name="file">文件</param>
        /// <param name="function">函数</param>
        /// <param name="line">行数</param>
        public void GetFrameDetail(StackFrame frame, Func<string, SymbolTable> symbolLoader, out string file, out string function, out uint line)
        {
            var symbol = symbolLoader?.Invoke(libraryAgency[frame.library].name);
            if (symbol == null)
            {
                file = function = "";
                line = 0;
            }
            else symbol.GetInfo(frame.point, out file, out function, out line);
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
