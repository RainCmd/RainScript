using System;
using System.Collections.Generic;
using RainScript.Vector;
using RainScript.VirtualMachine;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif
namespace RainScript
{
    partial class SymbolTable
    {
        /// <summary>
        /// 获取栈信息
        /// </summary>
        /// <param name="frame">栈数据</param>
        /// <param name="file">文件</param>
        /// <param name="function">函数</param>
        /// <param name="line">行数</param>
        public void GetInfo(StackFrame frame, out string file, out string function, out uint line)
        {
            if (functions.Length > 0)
            {
                var start = 0;
                var end = functions.Length;
                while (start < end)
                {
                    var middle = (start + end) >> 1;
                    if (frame.address < functions[middle].point) end = middle;
                    else if (frame.address >= functions[middle].point) start = middle + 1;
                }
                if (start > 0)
                {
                    start--;
                    file = files[functions[start].file];
                    function = functions[start].function;
                }
                else file = function = "";
            }
            else file = function = "";
            if (lines.Length > 0)
            {
                var start = 0;
                var end = lines.Length;
                while (start < end)
                {
                    var middle = (start + end) >> 1;
                    if (frame.address < lines[middle].point) end = middle;
                    else if (frame.address >= lines[middle].point) start = middle + 1;
                }
                if (start > 0) line = lines[start - 1].line;
                else line = 0;
            }
            else line = 0;
        }
    }
}
namespace RainScript.VirtualMachine
{
    /// <summary>
    /// 调用状态
    /// </summary>
    public enum InvokerState
    {
        /// <summary>
        /// 未开始调用
        /// </summary>
        Unstarted,
        /// <summary>
        /// 执行中
        /// </summary>
        Running,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        /// <summary>
        /// 已取消
        /// </summary>
        Aborted,
        /// <summary>
        /// 已失效
        /// </summary>
        Invalid,
    }
    /// <summary>
    /// 调用帧数据
    /// </summary>
    public struct StackFrame
    {
        /// <summary>
        /// 程序集名
        /// </summary>
        public readonly string library;
        /// <summary>
        /// 指令地址
        /// </summary>
        public readonly uint address;
        internal StackFrame(string library, uint point)
        {
            this.library = library;
            this.address = point;
        }
        /// <summary>
        /// 帧数据
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[library:{0} address:{1:X}]".Format(library, address);
        }
    }
    /// <summary>
    /// 调用句柄
    /// </summary>
    public unsafe struct InvokerHandle : IDisposable
    {
        private readonly ulong instanceID;
        private readonly Invoker invoker;
        /// <summary>
        /// 调用状态
        /// </summary>
        public InvokerState State
        {
            get
            {
                if (Valid) return invoker.state;
                return InvokerState.Invalid;
            }
        }
        /// <summary>
        /// 是个有效句柄
        /// </summary>
        public bool Valid { get { return invoker != null && invoker.instanceID == instanceID; } }
        /// <summary>
        /// 退出代码
        /// </summary>
        public long ExitCode
        {
            get
            {
                DisposedAssert();
                return invoker.exit;
            }
        }
        /// <summary>
        /// 暂停
        /// </summary>
        public bool IsPause
        {
            get { return invoker != null && invoker.IsPause; }
            set
            {
                DisposedAssert();
                invoker.IsPause = value;
            }
        }
        internal InvokerHandle(Invoker invoker)
        {
            instanceID = invoker.instanceID;
            this.invoker = invoker;
        }
        /// <summary>
        /// 获取调用帧数据
        /// </summary>
        /// <returns></returns>
        public StackFrame[] GetStackFrames()
        {
            DisposedAssert();
            return invoker.GetStackFrames();
        }
        /// <summary>
        /// 开始调用
        /// </summary>
        /// <param name="immediately">立刻开始调用，为false则会在<see cref="Kernel.Update"/>中才第一次调用</param>
        /// <param name="ignoreWait">忽略代码中的等待命令</param>
        public void Start(bool immediately, bool ignoreWait)
        {
            DisposedAssert();
            invoker.Start(immediately, ignoreWait);
        }
        /// <summary>
        /// 取消调用
        /// </summary>
        /// <param name="code">退出代码（必须非0才会生效）</param>
        public void Abort(long code)
        {
            DisposedAssert();
            invoker.Abort(code);
        }
        /// <summary>
        /// 释放调用句柄
        /// </summary>
        public void Dispose()
        {
            if (Valid) invoker.Recycle();
        }
        /// <summary>
        /// 获取布尔返回值
        /// </summary>
        /// <param name="index">返回值索引</param>
        /// <returns></returns>
        public bool GetBoolReturnValue(int index)
        {
            DisposedAssert();
            return invoker.GetBoolReturnValue(index) > 0;
        }
        /// <summary>
        /// 获取整数返回值
        /// </summary>
        /// <param name="index">返回值索引</param>
        /// <returns></returns>
        public long GetIntegerReturnValue(int index)
        {
            DisposedAssert();
            return invoker.GetIntegerReturnValue(index);
        }
        /// <summary>
        /// 获取实数返回值
        /// </summary>
        /// <param name="index">返回值索引</param>
        /// <returns></returns>
        public real GetRealReturnValue(int index)
        {
            DisposedAssert();
            return invoker.GetRealReturnValue(index);
        }
        /// <summary>
        /// 获取实数2返回值
        /// </summary>
        /// <param name="index">返回值索引</param>
        /// <returns></returns>
        public Real2 GetReal2ReturnValue(int index)
        {
            DisposedAssert();
            return invoker.GetReal2ReturnValue(index);
        }
        /// <summary>
        /// 获取实数3返回值
        /// </summary>
        /// <param name="index">返回值索引</param>
        /// <returns></returns>
        public Real3 GetReal3ReturnValue(int index)
        {
            DisposedAssert();
            return invoker.GetReal3ReturnValue(index);
        }
        /// <summary>
        /// 获取实数4返回值
        /// </summary>
        /// <param name="index">返回值索引</param>
        /// <returns></returns>
        public Real4 GetReal4ReturnValue(int index)
        {
            DisposedAssert();
            return invoker.GetReal4ReturnValue(index);
        }
        /// <summary>
        /// 获取字符串返回值
        /// </summary>
        /// <param name="index">返回值索引</param>
        /// <returns></returns>
        public string GetStringReturnValue(int index)
        {
            DisposedAssert();
            return invoker.GetStringReturnValue(index);
        }
        /// <summary>
        /// 获取实体返回值
        /// </summary>
        /// <param name="index">返回值索引</param>
        /// <returns></returns>
        public IEntity GetEntityReturnValue(int index)
        {
            DisposedAssert();
            return invoker.GetEntityObjectReturnValue(index);
        }

        /// <summary>
        /// 设置布尔参数
        /// </summary>
        /// <param name="index">参数索引</param>
        /// <param name="value">参数值</param>
        public void SetParameter(int index, bool value)
        {
            DisposedAssert();
            invoker.SetParameter(index, (byte)(value ? 1 : 0));
        }
        /// <summary>
        /// 设置整数参数
        /// </summary>
        /// <param name="index">参数索引</param>
        /// <param name="value">参数值</param>
        public void SetParameter(int index, long value)
        {
            DisposedAssert();
            invoker.SetParameter(index, value);
        }
        /// <summary>
        /// 设置实数参数
        /// </summary>
        /// <param name="index">参数索引</param>
        /// <param name="value">参数值</param>
        public void SetParameter(int index, real value)
        {
            DisposedAssert();
            invoker.SetParameter(index, value);
        }
        /// <summary>
        /// 设置实数2参数
        /// </summary>
        /// <param name="index">参数索引</param>
        /// <param name="value">参数值</param>
        public void SetParameter(int index, Real2 value)
        {
            DisposedAssert();
            invoker.SetParameter(index, value);
        }
        /// <summary>
        /// 设置实数3参数
        /// </summary>
        /// <param name="index">参数索引</param>
        /// <param name="value">参数值</param>
        public void SetParameter(int index, Real3 value)
        {
            DisposedAssert();
            invoker.SetParameter(index, value);
        }
        /// <summary>
        /// 设置实数4参数
        /// </summary>
        /// <param name="index">参数索引</param>
        /// <param name="value">参数值</param>
        public void SetParameter(int index, Real4 value)
        {
            DisposedAssert();
            invoker.SetParameter(index, value);
        }
        /// <summary>
        /// 设置字符串参数
        /// </summary>
        /// <param name="index">参数索引</param>
        /// <param name="value">参数值</param>
        public void SetParameter(int index, string value)
        {
            DisposedAssert();
            invoker.SetParameter(index, value);
        }
        /// <summary>
        /// 设置实体参数
        /// </summary>
        /// <param name="index">参数索引</param>
        /// <param name="value">参数值</param>
        public void SetParameter(int index, IEntity value)
        {
            DisposedAssert();
            invoker.SetParameter(index, value);
        }

        private void DisposedAssert()
        {
            if (!Valid) throw ExceptionGeneratorVM.ObjectDisposed();
        }
        /// <summary>
        /// 判断句柄是否有效
        /// </summary>
        /// <param name="handle"></param>
        public static implicit operator bool(InvokerHandle handle)
        {
            return handle.Valid;
        }
    }
    internal unsafe class Invoker : IDisposable
    {
        public ulong instanceID;
        public InvokerState state;
        public FunctionHandle handle;
        public long exit;
        private uint size;
        private byte* data = null;
        private readonly List<StackFrame> frames = new List<StackFrame>();
        internal Coroutine coroutine;
        public bool IsPause
        {
            get
            {
                StateAssert(InvokerState.Running);
                return coroutine.pause;
            }
            set
            {
                StateAssert(InvokerState.Running);
                coroutine.pause = value;
            }
        }
        public Invoker(ulong instanceIndex)
        {
            instanceID = instanceIndex;
        }
        ~Invoker()
        {
            Dispose();
        }
        public byte GetBoolReturnValue(int index)
        {
            ReturnTypeAssert(index, TypeCode.Bool);
            return data[handle.returnPoints[index]];
        }
        public long GetIntegerReturnValue(int index)
        {
            ReturnTypeAssert(index, TypeCode.Integer);
            return *(long*)(data + handle.returnPoints[index]);
        }
        public real GetRealReturnValue(int index)
        {
            ReturnTypeAssert(index, TypeCode.Real);
            return *(real*)(data + handle.returnPoints[index]);
        }
        public Real2 GetReal2ReturnValue(int index)
        {
            ReturnTypeAssert(index, TypeCode.Real2);
            return *(Real2*)(data + handle.returnPoints[index]);
        }
        public Real3 GetReal3ReturnValue(int index)
        {
            ReturnTypeAssert(index, TypeCode.Real3);
            return *(Real3*)(data + handle.returnPoints[index]);
        }
        public Real4 GetReal4ReturnValue(int index)
        {
            ReturnTypeAssert(index, TypeCode.Real4);
            return *(Real4*)(data + handle.returnPoints[index]);
        }
        public string GetStringReturnValue(int index)
        {
            ReturnTypeAssert(index, TypeCode.String);
            return handle.library.kernel.stringAgency.Get(*(uint*)(data + handle.returnPoints[index]));
        }
        public uint GetStringHandleReturnValue(int index)
        {
            ReturnTypeAssert(index, TypeCode.String);
            return *(uint*)(data + handle.returnPoints[index]);
        }
        internal uint GetHeapHandleReturnValue(int index)
        {
            ReturnTypeAssert(index, TypeCode.Handle);
            return *(uint*)(data + handle.returnPoints[index]);
        }
        internal Entity GetEntityReturnValue(int index)
        {
            ReturnTypeAssert(index, TypeCode.Entity);
            return *(Entity*)(data + handle.returnPoints[index]);
        }
        public IEntity GetEntityObjectReturnValue(int index)
        {
            return handle.library.kernel.manipulator.Get(GetEntityReturnValue(index));
        }

        public void SetParameter(int index, byte value)
        {
            ParameterTypeAssert(index, TypeCode.Bool);
            data[handle.parameterPoints[index]] = value;
        }
        public void SetParameter(int index, long value)
        {
            ParameterTypeAssert(index, TypeCode.Integer);
            *(long*)(data + handle.parameterPoints[index]) = value;
        }
        public void SetParameter(int index, real value)
        {
            ParameterTypeAssert(index, TypeCode.Real);
            *(real*)(data + handle.parameterPoints[index]) = value;
        }
        public void SetParameter(int index, Real2 value)
        {
            ParameterTypeAssert(index, TypeCode.Real2);
            *(Real2*)(data + handle.parameterPoints[index]) = value;
        }
        public void SetParameter(int index, Real3 value)
        {
            ParameterTypeAssert(index, TypeCode.Real3);
            *(Real3*)(data + handle.parameterPoints[index]) = value;
        }
        public void SetParameter(int index, Real4 value)
        {
            ParameterTypeAssert(index, TypeCode.Real4);
            *(Real4*)(data + handle.parameterPoints[index]) = value;
        }
        public void SetParameter(int index, string value)
        {
            ParameterTypeAssert(index, TypeCode.String);
            var point = (uint*)(data + handle.parameterPoints[index]);
            var agency = handle.library.kernel.stringAgency;
            var result = agency.Add(value);
            agency.Reference(result);
            agency.Release(*point);
            *point = result;
        }
        public void SetStringHandleParameter(int index, uint stringHandle)
        {
            ParameterTypeAssert(index, TypeCode.String);
            var point = (uint*)(data + handle.parameterPoints[index]);
            var agency = handle.library.kernel.stringAgency;
            agency.Reference(stringHandle);
            agency.Release(*point);
            *point = stringHandle;
        }
        internal void SetHeapHandleParameter(int index, uint value)
        {
            ParameterTypeAssert(index, TypeCode.Handle);
            var point = (uint*)(data + handle.parameterPoints[index]);
            var agency = handle.library.kernel.heapAgency;
            agency.Reference(value);
            agency.Release(*point);
            *point = value;
        }
        internal void SetParameter(int index, Entity value)
        {
            ParameterTypeAssert(index, TypeCode.Entity);
            var point = (Entity*)(data + handle.parameterPoints[index]);
            var manipulator = handle.library.kernel.manipulator;
            manipulator.Reference(value);
            manipulator.Release(*point);
            *point = value;
        }
        public void SetParameter(int index, IEntity value)
        {
            SetParameter(index, handle.library.kernel.manipulator.Add(value));
        }

        public void Start(bool immediately, bool ignoreWait)
        {
            StateAssert(InvokerState.Unstarted);
            state = InvokerState.Running;
            handle.library.kernel.coroutineAgency.Start(this, immediately, ignoreWait);
        }
        public void Abort(long code)
        {
            if (code != 0)
            {
                StateAssert(InvokerState.Running);
                coroutine.exit = code;
                state = InvokerState.Aborted;
            }
        }
        private void Reset(uint size)
        {
            if (this.size < size)
            {
                if (data != null) Tools.Free(data);
                this.size = size;
                data = Tools.MAlloc((int)size);
            }
        }
        public void CopyTo(byte* point, uint size)
        {
            for (uint i = 0; i < size; i++) point[i] = data[i];
        }
        public void CopyFrom(byte* point, uint size)
        {
            Reset(size);
            for (uint i = 0; i < size; i++) data[i] = point[i];
        }
        public void PushStack(uint library, uint point)
        {
            frames.Add(new StackFrame(handle.library.kernel.libraryAgency[library].name, point));
        }
        public void Initialize(FunctionHandle handle)
        {
            this.handle = handle;
            state = InvokerState.Unstarted;
            Reset(handle.parameterSize);
            for (int i = 0; i < size; i++) data[i] = 0;
        }
        public StackFrame[] GetStackFrames()
        {
            switch (state)
            {
                case InvokerState.Unstarted:
                    break;
                case InvokerState.Running: return coroutine.GetStackFrames();
                case InvokerState.Completed:
                case InvokerState.Aborted: return frames.ToArray();
                case InvokerState.Invalid:
                    break;
            }
            throw ExceptionGeneratorVM.InvalidOperation(state);
        }
        internal void ClearParameters()
        {
            var kernel = handle.library.kernel;
            var parameters = handle.function.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].dimension > 0) kernel.heapAgency.Release(*(uint*)(data + handle.parameterPoints[i]));
                else switch (parameters[i].definition.code)
                    {
                        case TypeCode.String:
                            kernel.stringAgency.Release(*(uint*)(data + handle.parameterPoints[i]));
                            break;
                        case TypeCode.Handle:
                        case TypeCode.Interface:
                        case TypeCode.Function:
                        case TypeCode.Coroutine:
                            kernel.heapAgency.Release(*(uint*)(data + handle.parameterPoints[i]));
                            break;
                        case TypeCode.Entity:
                            kernel.manipulator.Release(*(Entity*)(data + handle.parameterPoints[i]));
                            break;
                    }
            }
            var size = handle.parameterSize;
            while (size-- > 0) data[size] = 0;
        }
        internal void ClearReturns()
        {
            var kernel = handle.library.kernel;
            var returns = handle.function.returns;
            for (int i = 0; i < returns.Length; i++)
            {
                if (returns[i].dimension > 0) kernel.heapAgency.Release(*(uint*)(data + handle.returnPoints[i]));
                else switch (returns[i].definition.code)
                    {
                        case TypeCode.String:
                            kernel.stringAgency.Release(*(uint*)(data + handle.returnPoints[i]));
                            break;
                        case TypeCode.Handle:
                        case TypeCode.Interface:
                        case TypeCode.Function:
                        case TypeCode.Coroutine:
                            kernel.heapAgency.Release(*(uint*)(data + handle.returnPoints[i]));
                            break;
                        case TypeCode.Entity:
                            kernel.manipulator.Release(*(Entity*)(data + handle.returnPoints[i]));
                            break;
                    }
            }
            var size = handle.returnSize;
            while (size-- > 0) data[size] = 0;
        }
        public void Recycle()
        {
            switch (state)
            {
                case InvokerState.Unstarted:
                    {
                        ClearParameters();
                        handle.library.kernel.coroutineAgency.Recycle(this);
                    }
                    break;
                case InvokerState.Running: break;
                case InvokerState.Completed:
                    {
                        ClearReturns();
                        handle.library.kernel.coroutineAgency.Recycle(this);
                    }
                    break;
                case InvokerState.Aborted:
                case InvokerState.Invalid: break;
            }
            state = InvokerState.Invalid;
            frames.Clear();
        }
        private void ParameterTypeAssert(int index, TypeCode code)
        {
            StateAssert(InvokerState.Unstarted);
            var type = handle.function.parameters[index];
            if (code == TypeCode.Handle)
            {
                if (type.dimension > 0) return;
                if (type.definition.code == TypeCode.Handle) return;
                if (type.definition.code == TypeCode.Interface) return;
                if (type.definition.code == TypeCode.Function) return;
                if (type.definition.code == TypeCode.Coroutine) return;
            }
            else if (type.definition.code == code) return;
            throw ExceptionGeneratorVM.InvokerTypeCastFail(handle, code, type.definition.code);
        }
        private void ReturnTypeAssert(int index, TypeCode code)
        {
            StateAssert(InvokerState.Completed);
            var type = handle.function.returns[index];
            if (code == TypeCode.Handle)
            {
                if (type.dimension > 0) return;
                if (type.definition.code == TypeCode.Handle) return;
                if (type.definition.code == TypeCode.Interface) return;
                if (type.definition.code == TypeCode.Function) return;
                if (type.definition.code == TypeCode.Coroutine) return;
            }
            else if (type.definition.code == code) return;
            throw ExceptionGeneratorVM.InvokerTypeCastFail(handle, code, type.definition.code);
        }
        private void StateAssert(InvokerState state)
        {
            if (this.state != state) ExceptionGeneratorVM.InvalidOperation(state);
        }
        public void Dispose()
        {
            if (data != null) Tools.Free(data);
            data = null;
            state = InvokerState.Invalid;
        }
    }
}
