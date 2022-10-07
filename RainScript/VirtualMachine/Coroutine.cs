using System;
using RainScript.Vector;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript.VirtualMachine
{
    internal unsafe class Coroutine : IDisposable
    {
        private readonly Kernel kernel;
        private RuntimeLibraryInfo library;
        internal ulong instanceID;
        public Invoker invoker;
        public Coroutine next;
        private bool ignoreWait;
        public bool pause;
        public long exit;
        private uint stackSize, top;
        internal uint bottom, point;
        private long wait, flag;
        internal byte* stack;
        public bool Running { get { return exit == 0 && library != null; } }
        public Coroutine(Kernel kernel)
        {
            this.kernel = kernel;
            stackSize = 1024;
            stack = Tools.MAlloc((int)stackSize);
            for (int i = 0; i < stackSize; i++) stack[i] = 0;
        }
        private void EnsureStackSize(uint hold, uint size)
        {
            if (size > stackSize)
            {
                var nss = stackSize;
                while (nss < size) nss <<= 1;
                var ns = Tools.MAlloc((int)nss);
                while (hold-- > 0) ns[hold] = stack[hold];
                Tools.Free(stack);
                stack = ns;
                stackSize = nss;
            }
        }
        public void Initialize(Invoker invoker, bool ignoreWait)
        {
            pause = false;
            exit = wait = 0;
            library = invoker.handle.library;
            instanceID = invoker.instanceID;
            invoker.coroutine = this;
            this.invoker = invoker;
            this.ignoreWait = ignoreWait;
            this.point = invoker.handle.entry;
            bottom = top = invoker.handle.returnSize;
            EnsureStackSize(0, top + Frame.SIZE + (uint)(invoker.handle.function.returns.Length * 4) + invoker.handle.parameterSize);
            for (int i = 0; i < bottom; i++) stack[i] = 0;
            var point = stack + bottom;
            *(Frame*)point = new Frame(LIBRARY.INVALID, 0, 0);
            point += Frame.SIZE;
            for (int i = 0; i < invoker.handle.function.returns.Length; i++) *(uint*)(point + i * 4) = invoker.handle.returnPoints[i];
            invoker.CopyTo(point + invoker.handle.function.returns.Length * 4, invoker.handle.parameterSize);
        }
        public void Update()
        {
            if (wait > 0) wait--;
            else Run();
        }
        private void Run()
        {
            kernel.coroutineAgency.invoking = this;
            while (library != null)
            {
                switch ((CommandMacro)library.code[point])
                {
                    #region Base
                    case CommandMacro.BASE_Exit:
                        if (exit == 0 && flag != 0)
                        {
                            exit = flag;
                            if (invoker.instanceID == instanceID)
                            {
                                invoker.PushStack(library.index, point);
                                var index = *(Frame*)(stack + bottom);
                                while (index.libraryIndex != LIBRARY.INVALID)
                                {
                                    invoker.PushStack(index.libraryIndex, index.point);
                                    index = *(Frame*)(stack + index.bottom);
                                }
                            }
                            point = *(uint*)(stack + bottom + Frame.FINALLY);
                        }
                        else point++;
                        break;
                    case CommandMacro.BASE_Finally:
                        *(uint*)(stack + bottom + Frame.FINALLY) = *(uint*)(library.code + point + 1);
                        point += 5;
                        break;
                    case CommandMacro.BASE_ExitJump:
                        if (exit == 0) point++;
                        else point = *(uint*)(stack + bottom + Frame.FINALLY);
                        break;
                    case CommandMacro.BASE_Wait:
                        point++;
                        return;
                    case CommandMacro.BASE_WaitFrame:
                        if (!ignoreWait) wait = *(long*)(stack + bottom + *(uint*)(library.code + point + 1));
                        point += 5;
                        if (wait == 0) break;
                        wait--;
                        return;
                    case CommandMacro.BASE_Stackzero:
                        {
                            var address = stack + bottom + *(uint*)(library.code + point + 1);
                            var size = *(int*)(library.code + point + 5);
                            while (size-- > 0) address[size] = 0;
                        }
                        point += 9;
                        break;
                    case CommandMacro.BASE_Jump:
                        point = *(uint*)(library.code + point + 1);
                        break;
                    case CommandMacro.BASE_ConditionJump:
                        if (flag == 0) point += 5;
                        else point = *(uint*)(library.code + point + 1);
                        break;
                    case CommandMacro.BASE_NullJump:
                        if (*(uint*)(stack + bottom + *(uint*)(library.code + point + 1)) == 0) point = *(uint*)(library.code + point + 5);
                        else point += 9;
                        break;
                    case CommandMacro.BASE_Flag_1:
                        flag = *(stack + bottom + *(uint*)(library.code + point + 1));
                        point += 5;
                        break;
                    case CommandMacro.BASE_Flag_8:
                        flag = *(long*)(stack + bottom + *(uint*)(library.code + point + 1));
                        point += 5;
                        break;
                    case CommandMacro.BASE_CreateObject:
                        {
                            var handle = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var definition = library.LocalToGlobal(*(TypeDefinition*)(library.code + point + 5));
                            kernel.heapAgency.Release(*handle);
                            *handle = kernel.heapAgency.Alloc(definition);
                            kernel.heapAgency.Reference(*handle);
                            point += 14;
                        }
                        break;
                    case CommandMacro.BASE_CreateDelegate:
                        // [1,4]委托对象
                        // [5,13]委托类型定义（TypeDefinition）
                        // [14,14]目标函数类型 (FunctionType)
                        // [15,18]库编号
                        // [19,]
                        //      全局函数:
                        //      Native函数:
                        //          [19,26]Function
                        //      成员函数:
                        //      成员函数虚调用:
                        //          [19,22]定义编号
                        //          [23,30]成员Function
                        //          [31,34]目标对象所在局部变量地址
                        //      接口函数:
                        //          [19,22]接口编号
                        //          [23,30]接口Function
                        //          [31,34]目标对象所在局部变量地址
                        {
                            var handle = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var definition = library.LocalToGlobal(*(TypeDefinition*)(library.code + point + 5));
                            kernel.heapAgency.Release(*handle);
                            *handle = kernel.heapAgency.Alloc(definition);
                            kernel.heapAgency.Reference(*handle);
                            var delegateInfo = (RuntimeDelegateInfo*)kernel.heapAgency.GetPoint(*handle);
                            var functionType = *(FunctionType*)(library.code + point + 14);
                            var libraryIndex = *(uint*)(library.code + point + 15);
                            switch (functionType)
                            {
                                case FunctionType.Global:
                                case FunctionType.Native:
                                    {
                                        library.LocalToGlobal(libraryIndex, *(Function*)(library.code + point + 19), out var globalLibrary, out var globalFunction);
                                        *delegateInfo = new RuntimeDelegateInfo(globalLibrary, globalFunction, 0, functionType);
                                        point += 27;
                                    }
                                    break;
                                case FunctionType.Member:
                                    {
                                        var target = *(uint*)(stack + bottom + *(uint*)(library.code + point + 31));
                                        if (kernel.heapAgency.IsVaild(target))
                                        {
                                            var function = new DefinitionFunction(new TypeDefinition(libraryIndex, TypeCode.Handle, *(uint*)(library.code + point + 19)), *(Function*)(library.code + point + 23));
                                            *delegateInfo = new RuntimeDelegateInfo(kernel.libraryAgency, library.LocalToGlobal(function), target, functionType);
                                        }
                                        else
                                        {
                                            flag = (long)ExitCode.NullReference;
                                            goto error;
                                        }
                                        point += 35;
                                    }
                                    break;
                                case FunctionType.Virtual:
                                    {
                                        var target = *(uint*)(stack + bottom + *(uint*)(library.code + point + 31));
                                        flag = (long)kernel.heapAgency.TryGetType(target, out var type);
                                        if (flag != 0) goto error;
                                        var invokerFunction = new DefinitionFunction(new TypeDefinition(libraryIndex, TypeCode.Handle, *(uint*)(library.code + point + 19)), *(Function*)(library.code + point + 23));
                                        invokerFunction = library.LocalToGlobal(invokerFunction);
                                        if (kernel.libraryAgency.GetFunction(invokerFunction, type, out var targetFunction))
                                            *delegateInfo = new RuntimeDelegateInfo(kernel.libraryAgency, targetFunction, target, functionType);
                                        point += 35;
                                    }
                                    break;
                                case FunctionType.Interface:
                                    {
                                        var target = *(uint*)(stack + bottom + *(uint*)(library.code + point + 31));
                                        flag = (long)kernel.heapAgency.TryGetType(target, out var type);
                                        if (flag != 0) goto error;
                                        var invokerFunction = new DefinitionFunction(new TypeDefinition(libraryIndex, TypeCode.Interface, *(uint*)(library.code + point + 19)), *(Function*)(library.code + point + 23));
                                        invokerFunction = library.LocalToGlobal(invokerFunction);
                                        if (kernel.libraryAgency.GetFunction(invokerFunction, type, out var targetFunction))
                                            *delegateInfo = new RuntimeDelegateInfo(kernel.libraryAgency, targetFunction, target, functionType);
                                        point += 35;
                                    }
                                    break;
                                default:
                                    throw ExceptionGeneratorVM.InvalidFunctionType(functionType);
                            }
                            break;
                        error: goto case CommandMacro.BASE_Exit;
                        }
                    case CommandMacro.BASE_CreateCoroutine:
                        // [1,4]携程对象
                        // [5,13]携程类型定义（TypeDefinition）
                        // [14,14]目标函数类型(FunctionType)
                        // [15,18]库编号
                        // [19,]
                        //      全局函数:
                        //          [19,26]Function
                        //      内部函数:
                        //          内部函数不能用携程直接调用
                        //      成员函数:
                        //      成员函数虚调用:
                        //          [19,22]定义编号
                        //          [23,30]成员Function
                        //          [31,34]目标对象所在局部变量地址
                        //      接口函数:
                        //          [19,22]接口编号
                        //          [23,30]接口Function
                        //          [31,34]目标对象所在局部变量地址
                        {
                            var handle = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var definition = library.LocalToGlobal(*(TypeDefinition*)(library.code + point + 5));
                            kernel.heapAgency.Release(*handle);
                            *handle = kernel.heapAgency.Alloc(definition);
                            kernel.heapAgency.Reference(*handle);
                            var coroutine = (ulong*)kernel.heapAgency.GetPoint(*handle);
                            var functionType = *(FunctionType*)(library.code + point + 14);
                            var libraryIndex = *(uint*)(library.code + point + 15);
                            switch (functionType)
                            {
                                case FunctionType.Global:
                                    {
                                        library.LocalToGlobal(libraryIndex, *(Function*)(library.code + point + 19), out var globalLibrary, out var globalFunction);
                                        *coroutine = kernel.coroutineAgency.InternalInvoker(kernel.libraryAgency[globalLibrary].GetFunctionHandle(globalFunction)).instanceID;
                                        flag = 0;
                                        point += 27;
                                    }
                                    break;
                                case FunctionType.Native: throw ExceptionGeneratorVM.InvalidFunctionType(functionType);
                                case FunctionType.Member:
                                    {
                                        var target = *(uint*)(stack + bottom + *(uint*)(library.code + point + 31));
                                        if (kernel.heapAgency.IsVaild(target))
                                        {
                                            var function = new DefinitionFunction(new TypeDefinition(libraryIndex, TypeCode.Handle, *(uint*)(library.code + point + 19)), *(Function*)(library.code + point + 23));
                                            var invoker = kernel.coroutineAgency.InternalInvoker(kernel.libraryAgency.GetFunctionHandle(function));
                                            invoker.SetHeapHandleParameter(0, target);
                                            *coroutine = invoker.instanceID;
                                            flag = 1;
                                        }
                                        else
                                        {
                                            flag = (long)ExitCode.NullReference;
                                            goto error;
                                        }
                                    }
                                    break;
                                case FunctionType.Virtual:
                                    {
                                        var target = *(uint*)(stack + bottom + *(uint*)(library.code + point + 31));
                                        flag = (long)kernel.heapAgency.TryGetType(target, out var type);
                                        if (flag != 0) goto error;
                                        var invokerFunction = new DefinitionFunction(new TypeDefinition(libraryIndex, TypeCode.Handle, *(uint*)(library.code + point + 19)), *(Function*)(library.code + point + 23));
                                        invokerFunction = library.LocalToGlobal(invokerFunction);
                                        if (kernel.libraryAgency.GetFunction(invokerFunction, type, out var targetFunction))
                                        {
                                            var invoker = kernel.coroutineAgency.InternalInvoker(kernel.libraryAgency.GetFunctionHandle(targetFunction));
                                            invoker.SetHeapHandleParameter(0, target);
                                            *coroutine = invoker.instanceID;
                                            flag = 1;
                                        }
                                        point += 35;
                                    }
                                    break;
                                case FunctionType.Interface:
                                    {
                                        var target = *(uint*)(stack + bottom + *(uint*)(library.code + point + 31));
                                        flag = (long)kernel.heapAgency.TryGetType(target, out var type);
                                        if (flag != 0) goto error;
                                        var invokerFunction = new DefinitionFunction(new TypeDefinition(libraryIndex, TypeCode.Interface, *(uint*)(library.code + point + 19)), *(Function*)(library.code + point + 23));
                                        invokerFunction = library.LocalToGlobal(invokerFunction);
                                        if (kernel.libraryAgency.GetFunction(invokerFunction, type, out var targetFunction))
                                        {
                                            var invoker = kernel.coroutineAgency.InternalInvoker(kernel.libraryAgency.GetFunctionHandle(targetFunction));
                                            invoker.SetHeapHandleParameter(0, target);
                                            *coroutine = invoker.instanceID;
                                            flag = 1;
                                        }
                                        point += 35;
                                    }
                                    break;
                                default: throw ExceptionGeneratorVM.InvalidFunctionType(functionType);
                            }
                            break;
                        error: goto case CommandMacro.BASE_Exit;
                        }
                    case CommandMacro.BASE_CreateDelegateCoroutine:
                        // [1,4]携程对象
                        // [5,13]携程类型定义（TypeDefinition）
                        // [14,17]委托对象
                        {
                            var handle = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var definition = library.LocalToGlobal(*(TypeDefinition*)(library.code + point + 5));
                            kernel.heapAgency.Release(*handle);
                            *handle = kernel.heapAgency.Alloc(definition);
                            kernel.heapAgency.Reference(*handle);
                            var coroutine = (ulong*)kernel.heapAgency.GetPoint(*handle);
                            flag = (long)kernel.heapAgency.TryGetPoint(*(uint*)(stack + bottom + *(uint*)(library.code + point + 14)), out var delegatePoint);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            var delegateInfo = (RuntimeDelegateInfo*)delegatePoint;
                            switch (delegateInfo->type)
                            {
                                case FunctionType.Global:
                                    *coroutine = kernel.coroutineAgency.InternalInvoker(kernel.libraryAgency[delegateInfo->library].GetFunctionHandle(delegateInfo->function)).instanceID;
                                    flag = 0;
                                    break;
                                case FunctionType.Native:
                                    flag = (long)ExitCode.InvalidCoroutine;
                                    goto error;
                                case FunctionType.Member:
                                case FunctionType.Virtual:
                                    var target = (long)kernel.heapAgency.TryGetType(delegateInfo->target, out var targetType);
                                    if (flag != 0) goto error;
                                    var invoker = kernel.coroutineAgency.InternalInvoker(kernel.libraryAgency[delegateInfo->library].GetFunctionHandle(targetType.definition, delegateInfo->function));
                                    invoker.SetHeapHandleParameter(0, delegateInfo->target);
                                    *coroutine = invoker.instanceID;
                                    flag = 1;
                                    break;
                                case FunctionType.Interface:
                                default:
                                    flag = (long)ExitCode.InvalidCoroutine;
                                    goto error;
                            }
                            point += 18;
                            break;
                        error: goto case CommandMacro.BASE_Exit;
                        }
                    case CommandMacro.BASE_CreateArray:
                        {
                            var handle = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var type = library.LocalToGlobal(*(Type*)(library.code + point + 5));
                            var length = *(long*)(stack + bottom + *(uint*)(library.code + point + 18));
                            if (length < 0)
                            {
                                flag = (long)ExitCode.OutOfRange;
                                goto case CommandMacro.BASE_Exit;
                            }
                            kernel.heapAgency.Release(*handle);
                            *handle = kernel.heapAgency.AllocArray(type, (uint)length);
                            kernel.heapAgency.Reference(*handle);
                            point += 22;
                        }
                        break;
                    case CommandMacro.BASE_SetCoroutineParameter:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var parameterFlag = (int)flag;
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var coroutinePoint);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            var invoker = kernel.coroutineAgency.GetInternalInvoker(*(ulong*)coroutinePoint);
                            var parameterCount = *(int*)(library.code + point + 5) + parameterFlag;
                            point += 9;
                            for (int i = parameterFlag; i < parameterCount; i++, point += 5)
                                switch ((TypeCode)(*(library.code + point)))
                                {
                                    case TypeCode.Bool:
                                        invoker.SetParameter(i, *(stack + bottom + *(uint*)(library.code + point + 1)));
                                        break;
                                    case TypeCode.Integer:
                                        invoker.SetParameter(i, *(long*)(stack + bottom + *(uint*)(library.code + point + 1)));
                                        break;
                                    case TypeCode.Real:
                                        invoker.SetParameter(i, *(real*)(stack + bottom + *(uint*)(library.code + point + 1)));
                                        break;
                                    case TypeCode.Real2:
                                        invoker.SetParameter(i, *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)));
                                        break;
                                    case TypeCode.Real3:
                                        invoker.SetParameter(i, *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)));
                                        break;
                                    case TypeCode.Real4:
                                        invoker.SetParameter(i, *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)));
                                        break;
                                    case TypeCode.String:
                                        invoker.SetStringHandleParameter(i, *(uint*)(stack + bottom + *(uint*)(library.code + point + 1)));
                                        break;
                                    case TypeCode.Handle:
                                        invoker.SetHeapHandleParameter(i, *(uint*)(stack + bottom + *(uint*)(library.code + point + 1)));
                                        break;
                                    case TypeCode.Interface:
                                    case TypeCode.Function:
                                    case TypeCode.Coroutine://这几个应该当Handle处理了
                                        throw ExceptionGenerator.InvalidTypeCode((TypeCode)(*(library.code + point)));
                                    case TypeCode.Entity:
                                        invoker.SetParameter(i, *(Entity*)(stack + bottom + *(uint*)(library.code + point + 1)));
                                        break;
                                    default:
                                        throw ExceptionGenerator.InvalidTypeCode((TypeCode)(*(library.code + point)));
                                }
                            break;
                        }
                    case CommandMacro.BASE_GetCoroutineResult:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            var invoker = kernel.coroutineAgency.GetInternalInvoker(*(ulong*)address);
                            if (invoker.state != InvokerState.Completed)
                            {
                                flag = (long)ExitCode.CoroutineNotCompleted;
                                goto case CommandMacro.BASE_Exit;
                            }
                            var resultCount = *(uint*)(library.code + point + 5);
                            point += 9;
                            for (var i = 0; i < resultCount; i++, point += 9)
                            {
                                var result = stack + bottom + *(uint*)(library.code + point + 1);
                                var resultIndex = *(int*)(library.code + point + 5);
                                switch ((TypeCode)(*(library.code + point)))
                                {
                                    case TypeCode.Bool:
                                        *result = invoker.GetBoolReturnValue(resultIndex);
                                        break;
                                    case TypeCode.Integer:
                                        *(long*)result = invoker.GetIntegerReturnValue(resultIndex);
                                        break;
                                    case TypeCode.Real:
                                        *(real*)result = invoker.GetRealReturnValue(resultIndex);
                                        break;
                                    case TypeCode.Real2:
                                        *(Real2*)result = invoker.GetReal2ReturnValue(resultIndex);
                                        break;
                                    case TypeCode.Real3:
                                        *(Real3*)result = invoker.GetReal3ReturnValue(resultIndex);
                                        break;
                                    case TypeCode.Real4:
                                        *(Real4*)result = invoker.GetReal4ReturnValue(resultIndex);
                                        break;
                                    case TypeCode.String:
                                        {
                                            var value = invoker.GetStringHandleReturnValue(resultIndex);
                                            kernel.stringAgency.Reference(value);
                                            kernel.stringAgency.Release(*(uint*)result);
                                            *(uint*)result = value;
                                        }
                                        break;
                                    case TypeCode.Handle:
                                        {
                                            var value = invoker.GetHeapHandleReturnValue(resultIndex);
                                            kernel.heapAgency.Reference(value);
                                            kernel.heapAgency.Release(*(uint*)result);
                                            *(uint*)result = value;
                                        }
                                        break;
                                    case TypeCode.Interface:
                                    case TypeCode.Function:
                                    case TypeCode.Coroutine:
                                        throw ExceptionGenerator.InvalidTypeCode((TypeCode)(*(library.code + point)));
                                    case TypeCode.Entity:
                                        {
                                            var value = invoker.GetEntityReturnValue(resultIndex);
                                            kernel.manipulator.Reference(value);
                                            kernel.manipulator.Release(*(Entity*)result);
                                            *(Entity*)result = value;
                                        }
                                        break;
                                    default:
                                        throw ExceptionGenerator.InvalidTypeCode((TypeCode)(*(library.code + point)));
                                }
                            }
                            break;
                        }
                    case CommandMacro.BASE_CoroutineStart:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            var invoker = kernel.coroutineAgency.GetInternalInvoker(*(ulong*)address);
                            invoker.Start(true, ignoreWait);
                            kernel.coroutineAgency.invoking = this;
                            point += 5;
                        }
                        break;
                    #endregion Base
                    #region Function
                    case CommandMacro.FUNCTION_Entrance://[1,4]参数空间大小+Frame.SIZE [5,8]函数执行所需要的最大栈空间大小（包括frame.SIZE、返回值列表和参数列表）
                        top += *(uint*)(library.code + point + 5);
                        {
                            var hold = bottom + *(uint*)(library.code + point + 1);
                            EnsureStackSize(hold, top);
                            while (hold < top) stack[hold++] = 0;
                        }
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_Ensure://[1,4]目标函数参数空间大小+Frame.SIZE(对于委托调用，默认再加4字节) [5,8]函数返回地址
                        EnsureStackSize(top, top + *(uint*)(library.code + point + 1));
                        *(Frame*)(stack + top) = new Frame(library.index, bottom, *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_CustomCallPretreater:// [1,4]第一个参数的偏移值 [5,8]委托地址
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var delegatePoint);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            var runtimeDelegate = (RuntimeDelegateInfo*)delegatePoint;
                            if (runtimeDelegate->type == FunctionType.Member || runtimeDelegate->type == FunctionType.Virtual)
                            {
                                kernel.heapAgency.Reference(runtimeDelegate->target);
                                *(uint*)(stack + top + *(uint*)(library.code + point + 1)) = runtimeDelegate->target;
                                top += 4;
                            }
                            else if (runtimeDelegate->type == FunctionType.Interface) throw ExceptionGeneratorVM.InvalidFunctionType(runtimeDelegate->type);
                        }
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_PushReturnPoint:// [1,4]计算好之后的偏移量，包括Frame.size和4字节返回值编号 [5,8]局部变量地址
                        *(uint*)(stack + top + *(uint*)(library.code + point + 1)) = bottom + *(uint*)(library.code + point + 5);
                        point += 9;
                        break;
                    #region 参数列表入栈
                    case CommandMacro.FUNCTION_PushParameter_1:
                        *(stack + top + *(uint*)(library.code + point + 1)) = *(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_PushParameter_4:
                        *(uint*)(stack + top + *(uint*)(library.code + point + 1)) = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_PushParameter_8:
                        *(ulong*)(stack + top + *(uint*)(library.code + point + 1)) = *(ulong*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_PushParameter_16:
                        *(Real2*)(stack + top + *(uint*)(library.code + point + 1)) = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_PushParameter_24:
                        *(Real3*)(stack + top + *(uint*)(library.code + point + 1)) = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_PushParameter_32:
                        *(Real4*)(stack + top + *(uint*)(library.code + point + 1)) = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_PushParameter_String:
                        {
                            var value = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            kernel.stringAgency.Reference(value);
                            *(uint*)(stack + top + *(uint*)(library.code + point + 1)) = value;
                        }
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_PushParameter_Handle:
                        {
                            var value = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            kernel.heapAgency.Reference(value);
                            *(uint*)(stack + top + *(uint*)(library.code + point + 1)) = value;
                        }
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_PushParameter_Entity:
                        {
                            var value = *(Entity*)(stack + bottom + *(uint*)(library.code + point + 5));
                            kernel.manipulator.Reference(value);
                            *(Entity*)(stack + top + *(uint*)(library.code + point + 1)) = value;
                        }
                        point += 9;
                        break;
                    #endregion 参数列表入栈
                    #region 返回值列表赋值
                    case CommandMacro.FUNCTION_ReturnPoint_1:
                        *(stack + *(uint*)(stack + bottom + *(uint*)(library.code + point + 1))) = *(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_ReturnPoint_4:
                        *(uint*)(stack + *(uint*)(stack + bottom + *(uint*)(library.code + point + 1))) = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_ReturnPoint_8:
                        *(ulong*)(stack + *(uint*)(stack + bottom + *(uint*)(library.code + point + 1))) = *(ulong*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_ReturnPoint_16:
                        *(Real2*)(stack + *(uint*)(stack + bottom + *(uint*)(library.code + point + 1))) = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_ReturnPoint_24:
                        *(Real3*)(stack + *(uint*)(stack + bottom + *(uint*)(library.code + point + 1))) = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_ReturnPoint_32:
                        *(Real4*)(stack + *(uint*)(stack + bottom + *(uint*)(library.code + point + 1))) = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_ReturnPoint_String:
                        {
                            var address = (uint*)(stack + *(uint*)(stack + bottom + *(uint*)(library.code + point + 1)));
                            var result = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            kernel.stringAgency.Reference(result);
                            kernel.stringAgency.Release(*address);
                            *address = result;
                        }
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_ReturnPoint_Handle:
                        {
                            var address = (uint*)(stack + *(uint*)(stack + bottom + *(uint*)(library.code + point + 1)));
                            var result = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            kernel.heapAgency.Reference(result);
                            kernel.heapAgency.Release(*address);
                            *address = result;
                        }
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_ReturnPoint_Entity:
                        {
                            var address = (Entity*)(stack + *(uint*)(stack + bottom + *(uint*)(library.code + point + 1)));
                            var result = *(Entity*)(stack + bottom + *(uint*)(library.code + point + 5));
                            kernel.manipulator.Reference(result);
                            kernel.manipulator.Release(*address);
                            *address = result;
                        }
                        point += 9;
                        break;
                    #endregion 返回值列表赋值
                    case CommandMacro.FUNCTION_Return:
                        {
                            var frame = (Frame*)(stack + bottom);
                            if (frame->libraryIndex == LIBRARY.INVALID) library = null;
                            else
                            {
                                top = bottom;
                                library = kernel.libraryAgency[frame->libraryIndex];
                                bottom = frame->bottom;
                                point = frame->point;
                            }
                        }
                        break;
                    case CommandMacro.FUNCTION_Call://[1,4]library [5,12]Function
                        {
                            bottom = top;
                            library.LocalToGlobal(*(uint*)(library.code + point + 1), *(Function*)(library.code + point + 5), out var globalLibrary, out var globalFunction);
                            library = kernel.libraryAgency[globalLibrary];
                            point = library.GetFunctionEntry(globalFunction);
                        }
                        break;
                    case CommandMacro.FUNCTION_MemberCall://[1,4]library [5,8]定义编号 [9,16]Function
                        {
                            bottom = top;
                            var function = new DefinitionFunction(new TypeDefinition(*(uint*)(library.code + point + 1), TypeCode.Handle, *(uint*)(library.code + point + 5)), *(Function*)(library.code + point + 9));
                            function = library.LocalToGlobal(function);
                            library = kernel.libraryAgency[function.definition.library];
                            point = kernel.libraryAgency.GetFunctionHandle(function).entry;
                        }
                        break;
                    case CommandMacro.FUNCTION_MemberVirtualCall://[1,4]library [5,8]定义编号 [9,16]Function [17,20]目标对象变量地址
                        {
                            var target = *(uint*)(stack + bottom + *(uint*)(library.code + point + 17));
                            flag = (long)kernel.heapAgency.TryGetType(target, out var type);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            bottom = top;
                            var function = new DefinitionFunction(new TypeDefinition(*(uint*)(library.code + point + 1), TypeCode.Handle, *(uint*)(library.code + point + 5)), *(Function*)(library.code + point + 9));
                            function = library.LocalToGlobal(function);
                            if (kernel.libraryAgency.GetFunction(function, type, out var targetFunction))
                            {
                                library = kernel.libraryAgency[targetFunction.definition.library];
                                point = kernel.libraryAgency.GetFunctionHandle(targetFunction).entry;
                            }
                        }
                        break;
                    case CommandMacro.FUNCTION_InterfaceCall://[1,4]library [5,8]定义编号 [9,16]Function [17,20]目标对象变量地址
                        {
                            var target = *(uint*)(stack + bottom + *(uint*)(library.code + point + 17));
                            flag = (long)kernel.heapAgency.TryGetType(target, out var type);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            bottom = top;
                            var function = new DefinitionFunction(new TypeDefinition(*(uint*)(library.code + point + 1), TypeCode.Interface, *(uint*)(library.code + point + 5)), *(Function*)(library.code + point + 9));
                            function = library.LocalToGlobal(function);
                            if (kernel.libraryAgency.GetFunction(function, type, out var targetFunction))
                            {
                                library = kernel.libraryAgency[targetFunction.definition.library];
                                point = kernel.libraryAgency.GetFunctionHandle(targetFunction).entry;
                            }
                        }
                        break;
                    case CommandMacro.FUNCTION_CustomCall://[1,4]委托对象
                        {
                            flag = (long)kernel.heapAgency.TryGetPoint(*(uint*)(stack + bottom + *(uint*)(library.code + point + 1)), out var delegatePoint);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            var delegateInfo = (RuntimeDelegateInfo*)delegatePoint;
                            switch (delegateInfo->type)
                            {
                                case FunctionType.Global:
                                    bottom = top;
                                    library = kernel.libraryAgency[delegateInfo->library];
                                    point = library.GetFunctionEntry(delegateInfo->function);
                                    break;
                                case FunctionType.Native:
                                    kernel.libraryAgency[delegateInfo->library].NativeInvoker(delegateInfo->function, stack, top);
                                    point += 5;
                                    break;
                                case FunctionType.Member:
                                case FunctionType.Virtual:
                                    top -= 4;
                                    bottom = top;
                                    library = kernel.libraryAgency[delegateInfo->library];
                                    point = library.GetFunctionEntry(delegateInfo->function);
                                    break;
                                case FunctionType.Interface:
                                default: throw ExceptionGeneratorVM.InvalidFunctionType(delegateInfo->type);
                            }
                        }
                        break;
                    case CommandMacro.FUNCTION_NativeCall://[1,4]引用程序集编号 [5,12]Function
                        try
                        {
                            kernel.libraryAgency[library.LocalToGlobal(*(uint*)(library.code + point + 1))].NativeInvoker(*(Function*)(library.code + point + 5), stack, top);
                        }
                        catch (Exception)
                        {
                            flag = (long)ExitCode.NativeException;
                            goto case CommandMacro.BASE_Exit;
                        }
                        point += 13;
                        break;
                    case CommandMacro.FUNCTION_KernelCall:
                        try
                        {
                            var function = *(Function*)(library.code + point + 1);
                            KernelMethodInvoker.methods[function.method].invokers[function.index](library.kernel, stack, top);
                        }
                        catch (Exception)
                        {
                            flag = (long)ExitCode.Unknown;
                            goto case CommandMacro.BASE_Exit;
                        }
                        point += 9;
                        break;
                    case CommandMacro.FUNCTION_KernelMemberCall:
                        try
                        {
                            var function = *(Function*)(library.code + point + 1);
                            KernelMemberMethodInvoker.methods[function.method].invokers[function.index](kernel, stack, top);
                        }
                        catch (Exception)
                        {
                            flag = (long)ExitCode.Unknown;
                            goto case CommandMacro.BASE_Exit;
                        }
                        point += 9;
                        break;
                    #endregion Function
                    #region Assignment
                    #region C2L
                    case CommandMacro.ASSIGNMENT_Const2Local_1:
                        *(stack + bottom + *(uint*)(library.code + point + 1)) = *(library.code + point + 5);
                        point += 6;
                        break;
                    case CommandMacro.ASSIGNMENT_Const2Local_4:
                        *(uint*)(stack + bottom + *(uint*)(library.code + point + 1)) = *(uint*)(library.code + point + 5);
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Const2Local_8:
                        *(ulong*)(stack + bottom + *(uint*)(library.code + point + 1)) = *(ulong*)(library.code + point + 5);
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Const2Local_16:
                        *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = *(Real2*)(library.code + point + 5);
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Const2Local_24:
                        *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = *(Real3*)(library.code + point + 5);
                        point += 29;
                        break;
                    case CommandMacro.ASSIGNMENT_Const2Local_32:
                        *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = *(Real4*)(library.code + point + 5);
                        point += 37;
                        break;
                    case CommandMacro.ASSIGNMENT_Const2Local_String:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var value = library.strings[*(uint*)(library.code + point + 5)];
                            kernel.stringAgency.Reference(value);
                            kernel.stringAgency.Release(*result);
                            *result = value;
                        }
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Const2Local_HandleNull:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            kernel.heapAgency.Release(*result);
                            *result = 0;
                        }
                        point += 5;
                        break;
                    case CommandMacro.ASSIGNMENT_Const2Local_EntityNull:
                        {
                            var result = (Entity*)(stack + bottom + *(uint*)(library.code + point + 1));
                            kernel.manipulator.Release(*result);
                            *result = Entity.NULL;
                        }
                        point += 5;
                        break;
                    case CommandMacro.ASSIGNMENT_Const2Local_Vector:
                        {
                            var result = (real*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var index = *(uint*)(library.code + point + 5);
                            result[index] = *(real*)(library.code + point + 9);
                            point += 17;
                        }
                        break;
                    #endregion C2L
                    #region L2L
                    case CommandMacro.ASSIGNMENT_Local2Local_1:
                        *(stack + bottom + *(uint*)(library.code + point + 1)) = *(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Local_4:
                        *(uint*)(stack + bottom + *(uint*)(library.code + point + 1)) = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Local_8:
                        *(ulong*)(stack + bottom + *(uint*)(library.code + point + 1)) = *(ulong*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Local_16:
                        *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Local_24:
                        *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Local_32:
                        *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Local_String:
                        {
                            var address = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var value = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            kernel.stringAgency.Reference(value);
                            kernel.stringAgency.Release(*address);
                            *address = value;
                        }
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Local_Handle:
                        {
                            var address = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var value = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            kernel.heapAgency.Reference(value);
                            kernel.heapAgency.Release(*address);
                            *address = value;
                        }
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Local_Entity:
                        {
                            var address = (Entity*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var value = *(Entity*)(stack + bottom + *(uint*)(library.code + point + 5));
                            kernel.manipulator.Reference(value);
                            kernel.manipulator.Release(*address);
                            *address = value;
                        }
                        point += 9;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Local_Vector:
                        {
                            var result = (real*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var index = *(uint*)(library.code + point + 5);
                            var source = (real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            var sourceIndex = *(uint*)(library.code + point + 13);
                            result[index] = source[sourceIndex];
                        }
                        point += 17;
                        break;
                    #endregion L2L
                    #region L2G [1,4]library      [5,8]全局变量编号 [9,12]局部变量地址
                    case CommandMacro.ASSIGNMENT_Local2Global_1:
                        {
                            library.LocalToGlobal(*(uint*)(library.code + point + 1), *(uint*)(library.code + point + 5), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            *(targetLibrary.data + targetLibrary.variables[globalVariable]) = *(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Global_4:
                        {
                            library.LocalToGlobal(*(uint*)(library.code + point + 1), *(uint*)(library.code + point + 5), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            *(uint*)(targetLibrary.data + targetLibrary.variables[globalVariable]) = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Global_8:
                        {
                            library.LocalToGlobal(*(uint*)(library.code + point + 1), *(uint*)(library.code + point + 5), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            *(ulong*)(targetLibrary.data + targetLibrary.variables[globalVariable]) = *(ulong*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Global_16:
                        {
                            library.LocalToGlobal(*(uint*)(library.code + point + 1), *(uint*)(library.code + point + 5), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            *(Real2*)(targetLibrary.data + targetLibrary.variables[globalVariable]) = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Global_24:
                        {
                            library.LocalToGlobal(*(uint*)(library.code + point + 1), *(uint*)(library.code + point + 5), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            *(Real3*)(targetLibrary.data + targetLibrary.variables[globalVariable]) = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Global_32:
                        {
                            library.LocalToGlobal(*(uint*)(library.code + point + 1), *(uint*)(library.code + point + 5), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            *(Real4*)(targetLibrary.data + targetLibrary.variables[globalVariable]) = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Global_String:
                        {
                            library.LocalToGlobal(*(uint*)(library.code + point + 1), *(uint*)(library.code + point + 5), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var address = (uint*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            var value = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                            kernel.stringAgency.Reference(value);
                            kernel.stringAgency.Release(*address);
                            *address = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Global_Handle:
                        {
                            library.LocalToGlobal(*(uint*)(library.code + point + 1), *(uint*)(library.code + point + 5), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var address = (uint*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            var value = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                            kernel.heapAgency.Reference(value);
                            kernel.heapAgency.Release(*address);
                            *address = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Global_Entity:
                        {
                            library.LocalToGlobal(*(uint*)(library.code + point + 1), *(uint*)(library.code + point + 5), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var address = (Entity*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            var value = *(Entity*)(stack + bottom + *(uint*)(library.code + point + 9));
                            kernel.manipulator.Reference(value);
                            kernel.manipulator.Release(*address);
                            *address = value;
                        }
                        point += 13;
                        break;
                    #endregion L2G
                    #region L2H [1,4]对象变量地址 [5,8]library      [9,16]MemberVariable [17,20]局部变量地址
                    case CommandMacro.ASSIGNMENT_Local2Handle_1:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(MemberVariable*)(library.code + point + 9), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *address = *(stack + bottom + *(uint*)(library.code + point + 17));
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Handle_4:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(MemberVariable*)(library.code + point + 9), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *(uint*)address = *(uint*)(stack + bottom + *(uint*)(library.code + point + 17));
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Handle_8:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(MemberVariable*)(library.code + point + 9), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *(ulong*)address = *(ulong*)(stack + bottom + *(uint*)(library.code + point + 17));
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Handle_16:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(MemberVariable*)(library.code + point + 9), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *(Real2*)address = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 17));
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Handle_24:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(MemberVariable*)(library.code + point + 9), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *(Real3*)address = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 17));
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Handle_32:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(MemberVariable*)(library.code + point + 9), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *(Real4*)address = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 17));
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Handle_String:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(MemberVariable*)(library.code + point + 9), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            var value = *(uint*)(stack + bottom + *(uint*)(library.code + point + 17));
                            kernel.stringAgency.Reference(value);
                            kernel.stringAgency.Release(*(uint*)address);
                            *(uint*)address = value;
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Handle_Handle:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(MemberVariable*)(library.code + point + 9), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *(uint*)address = *(uint*)(stack + bottom + *(uint*)(library.code + point + 17));
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Handle_Entity:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(MemberVariable*)(library.code + point + 9), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            var value = *(Entity*)(stack + bottom + *(uint*)(library.code + point + 17));
                            kernel.manipulator.Reference(value);
                            kernel.manipulator.Release(*(Entity*)address);
                            *(Entity*)address = value;
                        }
                        point += 21;
                        break;
                    #endregion L2H
                    #region L2A [1,4]数组变量地址 [5,12]数组索引    [13,16]局部变量地址
                    case CommandMacro.ASSIGNMENT_Local2Array_1:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 5)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *address = *(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Array_4:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 5)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *(uint*)address = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Array_8:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 5)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *(ulong*)address = *(ulong*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Array_16:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 5)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *(Real2*)address = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Array_24:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 5)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *(Real3*)address = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Array_32:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 5)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *(Real4*)address = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Array_String:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 5)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            var value = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                            kernel.stringAgency.Reference(value);
                            kernel.stringAgency.Release(*(uint*)address);
                            *(uint*)address = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Array_Handle:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 5)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *(uint*)address = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Local2Array_Entity:
                        {
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 5)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            var value = *(Entity*)(stack + bottom + *(uint*)(library.code + point + 9));
                            kernel.manipulator.Reference(value);
                            kernel.manipulator.Release(*(Entity*)address);
                            *(Entity*)address = value;
                        }
                        point += 13;
                        break;
                    #endregion L2A
                    #region G2L [1,4]局部变量地址 [5,8]library      [9,12]全局变量编号
                    case CommandMacro.ASSIGNMENT_Global2Local_1:
                        {
                            var result = (stack + bottom + *(uint*)(library.code + point + 1));
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(uint*)(library.code + point + 9), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var value = *(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            *result = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Global2Local_4:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(uint*)(library.code + point + 9), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var value = *(uint*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            *result = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Global2Local_8:
                        {
                            var result = (ulong*)(stack + bottom + *(uint*)(library.code + point + 1));
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(uint*)(library.code + point + 9), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var value = *(ulong*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            *result = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Global2Local_16:
                        {
                            var result = (Real2*)(stack + bottom + *(uint*)(library.code + point + 1));
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(uint*)(library.code + point + 9), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var value = *(Real2*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            *result = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Global2Local_24:
                        {
                            var result = (Real3*)(stack + bottom + *(uint*)(library.code + point + 1));
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(uint*)(library.code + point + 9), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var value = *(Real3*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            *result = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Global2Local_32:
                        {
                            var result = (Real4*)(stack + bottom + *(uint*)(library.code + point + 1));
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(uint*)(library.code + point + 9), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var value = *(Real4*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            *result = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Global2Local_String:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(uint*)(library.code + point + 9), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var value = *(uint*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            kernel.stringAgency.Reference(value);
                            kernel.stringAgency.Release(*result);
                            *result = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Global2Local_Handle:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(uint*)(library.code + point + 9), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var value = *(uint*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            kernel.heapAgency.Reference(value);
                            kernel.heapAgency.Release(*result);
                            *result = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Global2Local_Entity:
                        {
                            var result = (Entity*)(stack + bottom + *(uint*)(library.code + point + 1));
                            library.LocalToGlobal(*(uint*)(library.code + point + 5), *(uint*)(library.code + point + 9), out var globalLibrary, out var globalVariable);
                            var targetLibrary = kernel.libraryAgency[globalLibrary];
                            var value = *(Entity*)(targetLibrary.data + targetLibrary.variables[globalVariable]);
                            kernel.manipulator.Reference(value);
                            kernel.manipulator.Release(*result);
                            *result = value;
                        }
                        point += 13;
                        break;
                    #endregion G2L
                    #region H2L [1,4]局部变量地址 [5,8]对象变量地址 [9,12]library        [13,20]MemberVariable
                    case CommandMacro.ASSIGNMENT_Handle2Local_1:
                        {
                            var result = (stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 9), *(MemberVariable*)(library.code + point + 13), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *result = *address;
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Handle2Local_4:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 9), *(MemberVariable*)(library.code + point + 13), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *result = *(uint*)address;
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Handle2Local_8:
                        {
                            var result = (ulong*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 9), *(MemberVariable*)(library.code + point + 13), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *result = *(ulong*)address;
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Handle2Local_16:
                        {
                            var result = (Real2*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 9), *(MemberVariable*)(library.code + point + 13), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *result = *(Real2*)address;
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Handle2Local_24:
                        {
                            var result = (Real3*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 9), *(MemberVariable*)(library.code + point + 13), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *result = *(Real3*)address;
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Handle2Local_32:
                        {
                            var result = (Real4*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 9), *(MemberVariable*)(library.code + point + 13), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            *result = *(Real4*)address;
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Handle2Local_String:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 9), *(MemberVariable*)(library.code + point + 13), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            kernel.stringAgency.Reference(*(uint*)address);
                            kernel.stringAgency.Release(*result);
                            *result = *(uint*)address;
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Handle2Local_Handle:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 9), *(MemberVariable*)(library.code + point + 13), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            kernel.heapAgency.Reference(*(uint*)address);
                            kernel.heapAgency.Release(*result);
                            *result = *(uint*)address;
                        }
                        point += 21;
                        break;
                    case CommandMacro.ASSIGNMENT_Handle2Local_Entity:
                        {
                            var result = (Entity*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetPoint(handle, out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            library.LocalToGlobal(*(uint*)(library.code + point + 9), *(MemberVariable*)(library.code + point + 13), out var globalLibrary, out var globalMemberVaribale);
                            var definition = kernel.libraryAgency[globalLibrary].definitions[globalMemberVaribale.definition];
                            address += definition.baseOffset + definition.variables[globalMemberVaribale.index].offset;
                            kernel.manipulator.Reference(*(Entity*)address);
                            kernel.manipulator.Release(*result);
                            *result = *(Entity*)address;
                        }
                        point += 21;
                        break;
                    #endregion H2L
                    #region A2L [1,4]局部变量地址 [5,8]数组变量地址 [9,16]数组索引
                    case CommandMacro.ASSIGNMENT_Array2Local_1:
                        {
                            var result = (stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 9)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *result = *address;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Array2Local_4:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 9)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *result = *(uint*)address;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Array2Local_8:
                        {
                            var result = (ulong*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 9)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *result = *(ulong*)address;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Array2Local_16:
                        {
                            var result = (Real2*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 9)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *result = *(Real2*)address;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Array2Local_24:
                        {
                            var result = (Real3*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 9)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *result = *(Real3*)address;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Array2Local_32:
                        {
                            var result = (Real4*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 9)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *result = *(Real4*)address;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Array2Local_String:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 9)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            kernel.stringAgency.Reference(*(uint*)address);
                            kernel.stringAgency.Release(*result);
                            *result = *(uint*)address;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Array2Local_Handle:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 9)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            kernel.heapAgency.Reference(*(uint*)address);
                            kernel.heapAgency.Release(*result);
                            *result = *(uint*)address;
                        }
                        point += 13;
                        break;
                    case CommandMacro.ASSIGNMENT_Array2Local_Entity:
                        {
                            var result = (Entity*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetArrayPoint(handle, *(long*)(stack + bottom + *(uint*)(library.code + point + 9)), out var address);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            kernel.manipulator.Reference(*(Entity*)address);
                            kernel.manipulator.Release(*result);
                            *result = *(Entity*)address;
                        }
                        point += 13;
                        break;
                    #endregion A2L
                    #endregion Assignment
                    #region Bool
                    case CommandMacro.BOOL_Not:
                        *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = !*(bool*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.BOOL_Or:
                        {
                            var lv = *(stack + bottom + *(uint*)(library.code + point + 5)) > 0;
                            var rv = *(stack + bottom + *(uint*)(library.code + point + 9)) > 0;
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv | rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.BOOL_Xor:
                        {
                            var lv = *(stack + bottom + *(uint*)(library.code + point + 5)) > 0;
                            var rv = *(stack + bottom + *(uint*)(library.code + point + 9)) > 0;
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv ^ rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.BOOL_And:
                        {
                            var lv = *(stack + bottom + *(uint*)(library.code + point + 5)) > 0;
                            var rv = *(stack + bottom + *(uint*)(library.code + point + 9)) > 0;
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv & rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.BOOL_Equals:
                        {
                            var lv = *(stack + bottom + *(uint*)(library.code + point + 5)) > 0;
                            var rv = *(stack + bottom + *(uint*)(library.code + point + 9)) > 0;
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv == rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.BOOL_NotEquals:
                        {
                            var lv = *(stack + bottom + *(uint*)(library.code + point + 5)) > 0;
                            var rv = *(stack + bottom + *(uint*)(library.code + point + 9)) > 0;
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv != rv;
                        }
                        point += 13;
                        break;
                    #endregion Bool
                    #region Integer
                    case CommandMacro.INTEGER_Negative:
                        *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = -*(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.INTEGER_Plus:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv + rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_Minus:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv - rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_Multiply:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_Divide:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_Mod:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_And:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv & rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_Or:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv | rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_Xor:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv ^ rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_Inverse:
                        *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = ~*(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.INTEGER_Equals:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv == rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_NotEquals:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv != rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_Grater:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv > rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_GraterThanOrEquals:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv >= rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_Less:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv < rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_LessThanOrEquals:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv <= rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_LeftShift:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv << (int)rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_RightShift:
                        {
                            var lv = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(long*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv >> (int)rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.INTEGER_Increment:
                        (*(long*)(stack + bottom + *(uint*)(library.code + point + 1)))++;
                        point += 5;
                        break;
                    case CommandMacro.INTEGER_Decrement:
                        (*(long*)(stack + bottom + *(uint*)(library.code + point + 1)))--;
                        point += 5;
                        break;
                    #endregion Integer
                    #region Real
                    case CommandMacro.REAL_Negative:
                        *(real*)(stack + bottom + *(uint*)(library.code + point + 1)) = -*(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.REAL_Plus:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(real*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv + rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_Minus:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(real*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv - rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_Multiply:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(real*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_Divide:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(real*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_Mod:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(real*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_Equals:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv == rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_NotEquals:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv != rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_Grater:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv > rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_GraterThanOrEquals:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv >= rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_Less:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv < rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_LessThanOrEquals:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv <= rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL_Increment:
                        (*(real*)(stack + bottom + *(uint*)(library.code + point + 1)))++;
                        point += 5;
                        break;
                    case CommandMacro.REAL_Decrement:
                        (*(real*)(stack + bottom + *(uint*)(library.code + point + 1)))--;
                        point += 5;
                        break;
                    #endregion Real
                    #region Real2
                    case CommandMacro.REAL2_Negative:
                        *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = -*(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.REAL2_Plus:
                        {
                            var lv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv + rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Minus:
                        {
                            var lv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv - rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Multiply_rv:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Multiply_vr:
                        {
                            var lv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Multiply_vv:
                        {
                            var lv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Divide_rv:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Divide_vr:
                        {
                            var lv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Divide_vv:
                        {
                            var lv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Mod_rv:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Mod_vr:
                        {
                            var lv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Mod_vv:
                        {
                            var lv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real2*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_Equals:
                        {
                            var lv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv == rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL2_NotEquals:
                        {
                            var lv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real2*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv != rv;
                        }
                        point += 13;
                        break;
                    #endregion Real2
                    #region Real3
                    case CommandMacro.REAL3_Negative:
                        *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = -*(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.REAL3_Plus:
                        {
                            var lv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv + rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Minus:
                        {
                            var lv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv - rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Multiply_rv:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Multiply_vr:
                        {
                            var lv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Multiply_vv:
                        {
                            var lv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Divide_rv:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y * rv.z == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Divide_vr:
                        {
                            var lv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Divide_vv:
                        {
                            var lv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y * rv.z == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Mod_rv:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y * rv.z == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Mod_vr:
                        {
                            var lv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Mod_vv:
                        {
                            var lv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y * rv.z == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real3*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_Equals:
                        {
                            var lv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv == rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL3_NotEquals:
                        {
                            var lv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real3*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv != rv;
                        }
                        point += 13;
                        break;
                    #endregion Real3
                    #region Real4
                    case CommandMacro.REAL4_Negative:
                        *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = -*(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                        point += 9;
                        break;
                    case CommandMacro.REAL4_Plus:
                        {
                            var lv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv + rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Minus:
                        {
                            var lv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv - rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Multiply_rv:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Multiply_vr:
                        {
                            var lv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Multiply_vv:
                        {
                            var lv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv * rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Divide_rv:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y * rv.z * rv.w == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Divide_vr:
                        {
                            var lv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Divide_vv:
                        {
                            var lv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y * rv.z * rv.w == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv / rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Mod_rv:
                        {
                            var lv = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y * rv.z * rv.w == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Mod_vr:
                        {
                            var lv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(real*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Mod_vv:
                        {
                            var lv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (rv.x * rv.y * rv.z * rv.w == 0)
                            {
                                flag = (long)ExitCode.DivideByZero;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *(Real4*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv % rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_Equals:
                        {
                            var lv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv == rv;
                        }
                        point += 13;
                        break;
                    case CommandMacro.REAL4_NotEquals:
                        {
                            var lv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Real4*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *(bool*)(stack + bottom + *(uint*)(library.code + point + 1)) = lv != rv;
                        }
                        point += 13;
                        break;
                    #endregion Real4
                    #region String
                    case CommandMacro.STRING_Release:
                        {
                            var address = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            kernel.stringAgency.Release(*address);
                            *address = 0;
                        }
                        point += 5;
                        break;
                    case CommandMacro.STRING_Element:
                        {
                            var result = (long*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var str = kernel.stringAgency.Get(*(uint*)(stack + bottom + *(uint*)(library.code + point + 5)));
                            var index = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (index < 0) index += str.Length;
                            if (index < 0 || index >= str.Length)
                            {
                                flag = (long)ExitCode.OutOfRange;
                                goto case CommandMacro.BASE_Exit;
                            }
                            *result = str[(int)index];
                        }
                        point += 13;
                        break;
                    case CommandMacro.STRING_Combine:
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var a = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var b = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                            var agency = kernel.stringAgency;
                            var value = agency.Add(agency.Get(a) + agency.Get(b));
                            agency.Reference(value);
                            agency.Release(*result);
                            *result = value;
                        }
                        point += 13;
                        break;
                    case CommandMacro.STRING_Sub:
                        {
                            var agency = kernel.stringAgency;
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var source = agency.Get(*(uint*)(stack + bottom + *(uint*)(library.code + point + 5)));
                            var start = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            var end = *(long*)(stack + bottom + *(uint*)(library.code + point + 13));
                            if (start < 0) start += source.Length;
                            if (end < 0) end += source.Length;
                            if (start < 0 || start >= source.Length || end < 0 || end >= source.Length)
                            {
                                flag = (long)ExitCode.OutOfRange;
                                goto case CommandMacro.BASE_Exit;
                            }
                            if (start > end)
                            {
                                source = source.Substring((int)end, (int)(start - end));
                                var builder = new System.Text.StringBuilder();
                                for (int i = 0; i < source.Length; i++) builder.Append(source[source.Length - i - 1]);
                                source = builder.ToString();
                            }
                            else source = source.Substring((int)start, (int)(end - start));
                            var value = agency.Add(source);
                            agency.Reference(value);
                            agency.Release(*result);
                            *result = value;
                        }
                        point += 17;
                        break;
                    case CommandMacro.STRING_Equals:
                        {
                            var result = (bool*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var a = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var b = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *result = kernel.stringAgency.IsEquals(a, b);
                        }
                        point += 13;
                        break;
                    case CommandMacro.STRING_NotEquals:
                        {
                            var result = (bool*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var a = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var b = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *result = !kernel.stringAgency.IsEquals(a, b);
                        }
                        point += 13;
                        break;
                    #endregion String
                    #region Handle
                    case CommandMacro.HANDLE_ArrayCut://[1,4]返回值地址 [5,8]被切割数组 [9,12]起点 [13,16]终点
                        {
                            var heap = kernel.heapAgency;
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var array = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)heap.TryGetArrayLength(array, out var length);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            var start = *(long*)(stack + bottom + *(uint*)(library.code + point + 9));
                            var end = *(long*)(stack + bottom + *(uint*)(library.code + point + 13));
                            if (start < 0) start += length;
                            if (end < 0) end += length;
                            if (start < 0 || start >= length || end < 0 || end >= length)
                            {
                                flag = (long)ExitCode.OutOfRange;
                                goto case CommandMacro.BASE_Exit;
                            }
                            var subLength = Math.Abs(start - end);
                            var type = heap.GetType(array);
                            type = new Type(type.definition, type.dimension - 1);
                            heap.Release(*result);
                            *result = heap.AllocArray(type, (uint)subLength);
                            var source = heap.GetArrayPoint(array, start);
                            var destination = heap.GetArrayPoint(*result, 0);
                            if (type == KERNEL_TYPE.STRING)
                            {
                                for (int index = 0; index < length; index++)
                                {
                                    var value = ((uint*)source)[start < end ? start + index : start - index];
                                    kernel.stringAgency.Reference(value);
                                    ((uint*)destination)[index] = value;
                                }
                            }
                            else if (type == KERNEL_TYPE.ENTITY)
                            {
                                for (int index = 0; index < length; index++)
                                {
                                    var value = ((Entity*)source)[start < end ? start + index : start - index];
                                    kernel.manipulator.Reference(value);
                                    ((Entity*)destination)[index] = value;
                                }
                            }
                            else if (start < end) Tools.Copy(source, destination, type.FieldSize * length);
                            else for (int index = 0, size = (int)type.FieldSize; index < length; index++) Tools.Copy(source - size * index, destination + size * index, (uint)size);
                        }
                        point += 17;
                        break;
                    case CommandMacro.HANDLE_CheckNull:
                        if (!kernel.heapAgency.IsVaild(*(uint*)(stack + bottom + *(uint*)(library.code + point + 1))))
                        {
                            flag = (long)ExitCode.NullReference;
                            goto case CommandMacro.BASE_Exit;
                        }
                        point += 5;
                        break;
                    case CommandMacro.HANDLE_Equals:
                        {
                            var result = (bool*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var lv = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *result = kernel.heapAgency.IsEquals(lv, rv);
                        }
                        point += 13;
                        break;
                    case CommandMacro.HANDLE_NotEquals:
                        {
                            var result = (bool*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var lv = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *result = !kernel.heapAgency.IsEquals(lv, rv);
                        }
                        point += 13;
                        break;
                    #endregion Handle
                    #region Entity
                    case CommandMacro.ENTITY_Equals:
                        {
                            var result = (bool*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var lv = *(Entity*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Entity*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *result = kernel.manipulator.IsEquals(lv, rv);
                        }
                        point += 13;
                        break;
                    case CommandMacro.ENTITY_NotEquals:
                        {
                            var result = (bool*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var lv = *(Entity*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(Entity*)(stack + bottom + *(uint*)(library.code + point + 9));
                            *result = !kernel.manipulator.IsEquals(lv, rv);
                        }
                        point += 13;
                        break;
                    #endregion Entity
                    #region Delegate
                    case CommandMacro.DELEGATE_Equals:
                        {
                            var result = (bool*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var lv = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (lv == rv) *result = true;
                            else
                            {
                                var heap = kernel.heapAgency;
                                var vl = heap.IsVaild(lv);
                                var vr = heap.IsVaild(rv);
                                if (vl && vr) *result = *(RuntimeDelegateInfo*)kernel.heapAgency.GetPoint(lv) == *(RuntimeDelegateInfo*)kernel.heapAgency.GetPoint(rv);
                                else if (!vl && !vr) *result = true;
                                else *result = false;
                            }
                        }
                        point += 13;
                        break;
                    case CommandMacro.DELEGATE_NotEquals:
                        {
                            var result = (bool*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var lv = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            var rv = *(uint*)(stack + bottom + *(uint*)(library.code + point + 9));
                            if (lv == rv) *result = true;
                            else
                            {
                                var heap = kernel.heapAgency;
                                var vl = heap.IsVaild(lv);
                                var vr = heap.IsVaild(rv);
                                if (vl && vr) *result = *(RuntimeDelegateInfo*)kernel.heapAgency.GetPoint(lv) == *(RuntimeDelegateInfo*)kernel.heapAgency.GetPoint(rv);
                                else if (!vl && !vr) *result = true;
                                else *result = false;
                            }
                            *result = !*result;
                        }
                        point += 13;
                        break;
                    #endregion Delegate
                    #region Casting
                    case CommandMacro.CASTING://[1,4]result [5,8]对象所在地址 [9,21]Type
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetType(handle, out var handleType);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            var type = *(Type*)(library.code + point + 9);
                            if (kernel.libraryAgency.TryGetInheritDepth(type, handleType, out _))
                            {
                                flag = (long)ExitCode.InvalidCast;
                                goto case CommandMacro.BASE_Exit;
                            }
                            else
                            {
                                kernel.heapAgency.Release(*result);
                                kernel.heapAgency.Reference(handle);
                                *result = handle;
                            }
                        }
                        point += 22;
                        break;
                    case CommandMacro.CASTING_IS://[1,4]result [5,8]对象所在地址 [9,21]Type
                        {
                            var result = (bool*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetType(handle, out var handleType);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            *result = kernel.libraryAgency.TryGetInheritDepth(library.LocalToGlobal(*(Type*)(library.code + point + 9)), handleType, out _);
                        }
                        point += 22;
                        break;
                    case CommandMacro.CASTING_AS://[1,4]result [5,8]对象所在地址 [9,21]Type
                        {
                            var result = (uint*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var handle = *(uint*)(stack + bottom + *(uint*)(library.code + point + 5));
                            flag = (long)kernel.heapAgency.TryGetType(handle, out var handleType);
                            if (flag != 0) goto case CommandMacro.BASE_Exit;
                            kernel.heapAgency.Release(*result);
                            if (kernel.libraryAgency.TryGetInheritDepth(library.LocalToGlobal(*(Type*)(library.code + point + 9)), handleType, out _))
                            {
                                kernel.heapAgency.Reference(handle);
                                *result = handle;
                            }
                            else *result = 0;
                        }
                        point += 22;
                        break;
                    case CommandMacro.CASTING_R2I:
                        {
                            var result = (long*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var value = *(real*)(stack + bottom + *(uint*)(library.code + point + 5));
                            *result = (long)value;
                        }
                        point += 9;
                        break;
                    case CommandMacro.CASTING_I2R:
                        {
                            var result = (real*)(stack + bottom + *(uint*)(library.code + point + 1));
                            var value = *(long*)(stack + bottom + *(uint*)(library.code + point + 5));
                            *result = (real)value;
                        }
                        point += 9;
                        break;
                    #endregion Casting
                    case CommandMacro.BREAKPOINT:
                        if (kernel.step || *(bool*)(library.code + point + 1))
                        {
                            kernel.step = false;
                            kernel.OnHitBreakpointEvent();
                        }
                        point += 2;
                        break;
                    default: throw ExceptionGeneratorVM.InvalidCommand((CommandMacro)library.code[point]);
                }
            }
            kernel.coroutineAgency.invoking = null;
        }
        public void Abort()
        {
            point = *(uint*)(stack + bottom + Frame.FINALLY);
            Run();
        }
        public StackFrame[] GetStackFrames()
        {
            if (invoker.state != InvokerState.Running) return invoker.GetStackFrames();
            var deep = 1;
            var index = *(Frame*)(stack + bottom);
            while (index.libraryIndex != LIBRARY.INVALID)
            {
                deep++;
                index = *(Frame*)(stack + index.bottom);
            }

            var frames = new StackFrame[deep];
            frames[0] = new StackFrame(library.name, point);
            deep = 1;
            index = *(Frame*)(stack + bottom);
            while (index.libraryIndex != LIBRARY.INVALID)
            {
                frames[deep++] = new StackFrame(kernel.libraryAgency[index.libraryIndex].name, index.point);
                index = *(Frame*)(stack + index.bottom);
            }
            return frames;
        }
        public void Recycle()
        {
            if (invoker.instanceID == instanceID)
            {
                invoker.CopyFrom(stack, invoker.handle.returnSize);
                invoker.exit = exit;
                invoker.state = InvokerState.Completed;
                if (exit != 0)
                {
                    kernel.coroutineAgency.invoking = this;
                    kernel.OnExitEvent(invoker.GetStackFrames(), exit);
                    kernel.coroutineAgency.invoking = null;
                }
                invoker.coroutine = null;
            }
        }
        public void Dispose()
        {
            Tools.Free(stack);
            stack = null;
        }
    }
}
