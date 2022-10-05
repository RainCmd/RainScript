using System.IO;
using RainScript.Vector;
using System.Net;
#if FIXED
using real = RainScript.Real.Fixed;
using Math = RainScript.Real.Math;
#else
using real = System.Double;
using Math = System.Math;
#endif

namespace RainScript.VirtualMachine
{
    internal class KernelInvoker
    {
        protected static uint PrepareInvoker(Stream stream, uint parameterSize, uint executeSize, KernelMethod.Function info, out uint finallyPoint)
        {
            stream.WriteByte((byte)CommandMacro.FUNCTION_Entrance);
            stream.Write(parameterSize);
            stream.Write(executeSize);
            stream.WriteByte((byte)CommandMacro.BASE_Stackzero);
            stream.Write(parameterSize);
            stream.Write(executeSize - parameterSize);
            stream.WriteByte((byte)CommandMacro.BASE_Finally);
            finallyPoint = (uint)stream.Position;
            stream.Write(0u);
            stream.WriteByte((byte)CommandMacro.FUNCTION_Ensure);
            stream.Write(parameterSize);
            stream.Write(0u);//invoker调用里没有用CommandMacro.FUNCTION_Return指令，所以这个参数不用写了
            uint returnPoint = Frame.SIZE, localPoint = parameterSize;
            foreach (var item in info.returns)
            {
                stream.WriteByte((byte)CommandMacro.FUNCTION_PushReturnPoint);
                stream.Write(returnPoint);
                stream.Write(localPoint);
                returnPoint += 4;
                localPoint += item.FieldSize;
            }
            return returnPoint;
        }
        protected static uint PushParameter(Stream stream, Type type, uint point)
        {
            if (type.dimension > 0) stream.WriteByte((byte)CommandMacro.FUNCTION_PushParameter_Handle);
            else switch (type.definition.code)
                {
                    case TypeCode.Invalid: goto default;
                    case TypeCode.Bool:
                        stream.WriteByte((byte)CommandMacro.FUNCTION_PushParameter_1);
                        break;
                    case TypeCode.Integer:
                        stream.WriteByte((byte)CommandMacro.FUNCTION_PushParameter_8);
                        break;
                    case TypeCode.Real:
                        stream.WriteByte((byte)CommandMacro.FUNCTION_PushParameter_8);
                        break;
                    case TypeCode.Real2:
                        stream.WriteByte((byte)CommandMacro.FUNCTION_PushParameter_16);
                        break;
                    case TypeCode.Real3:
                        stream.WriteByte((byte)CommandMacro.FUNCTION_PushParameter_24);
                        break;
                    case TypeCode.Real4:
                        stream.WriteByte((byte)CommandMacro.FUNCTION_PushParameter_32);
                        break;
                    case TypeCode.String:
                        stream.WriteByte((byte)CommandMacro.FUNCTION_PushParameter_String);
                        break;
                    case TypeCode.Handle:
                    case TypeCode.Interface:
                    case TypeCode.Function:
                    case TypeCode.Coroutine:
                        stream.WriteByte((byte)CommandMacro.FUNCTION_PushParameter_Handle);
                        break;
                    case TypeCode.Entity:
                        stream.WriteByte((byte)CommandMacro.FUNCTION_PushParameter_Entity);
                        break;
                    default: throw ExceptionGenerator.InvalidTypeCode(type.definition.code);
                }
            stream.Write(point);
            stream.Write(point);
            return point + type.FieldSize;
        }
        protected static uint ClearVariable(Stream stream, Type type, uint point)
        {
            if (type.dimension > 0)
            {
                stream.WriteByte((byte)CommandMacro.ASSIGNMENT_Const2Local_HandleNull);
                stream.Write(point);
            }
            else switch (type.definition.code)
                {
                    case TypeCode.Invalid:
                    case TypeCode.Bool:
                    case TypeCode.Integer:
                    case TypeCode.Real:
                    case TypeCode.Real2:
                    case TypeCode.Real3:
                    case TypeCode.Real4:
                        break;
                    case TypeCode.String:
                        stream.WriteByte((byte)CommandMacro.STRING_Release);
                        stream.Write(point);
                        break;
                    case TypeCode.Handle:
                    case TypeCode.Interface:
                    case TypeCode.Function:
                    case TypeCode.Coroutine:
                        stream.WriteByte((byte)CommandMacro.ASSIGNMENT_Const2Local_HandleNull);
                        stream.Write(point);
                        break;
                    case TypeCode.Entity:
                        stream.WriteByte((byte)CommandMacro.ASSIGNMENT_Const2Local_EntityNull);
                        stream.Write(point);
                        break;
                }
            return point + type.FieldSize;
        }
        protected static void WriteReturns(Stream stream, uint parameterSize, uint finallyPoint, KernelMethod.Function info)
        {
            uint returnPoint = Frame.SIZE, localPoint = parameterSize;
            foreach (var item in info.returns)
            {
                if (item.dimension > 0) stream.WriteByte((byte)CommandMacro.FUNCTION_ReturnPoint_Handle);
                else switch (item.definition.code)
                    {
                        case TypeCode.Invalid: goto default;
                        case TypeCode.Bool:
                            stream.WriteByte((byte)CommandMacro.FUNCTION_ReturnPoint_1);
                            break;
                        case TypeCode.Integer:
                            stream.WriteByte((byte)CommandMacro.FUNCTION_ReturnPoint_8);
                            break;
                        case TypeCode.Real:
                            stream.WriteByte((byte)CommandMacro.FUNCTION_ReturnPoint_8);
                            break;
                        case TypeCode.Real2:
                            stream.WriteByte((byte)CommandMacro.FUNCTION_ReturnPoint_16);
                            break;
                        case TypeCode.Real3:
                            stream.WriteByte((byte)CommandMacro.FUNCTION_ReturnPoint_24);
                            break;
                        case TypeCode.Real4:
                            stream.WriteByte((byte)CommandMacro.FUNCTION_ReturnPoint_32);
                            break;
                        case TypeCode.String:
                            stream.WriteByte((byte)CommandMacro.FUNCTION_ReturnPoint_String);
                            break;
                        case TypeCode.Handle:
                        case TypeCode.Interface:
                        case TypeCode.Function:
                        case TypeCode.Coroutine:
                            stream.WriteByte((byte)CommandMacro.FUNCTION_ReturnPoint_Handle);
                            break;
                        case TypeCode.Entity:
                            stream.WriteByte((byte)CommandMacro.FUNCTION_ReturnPoint_Entity);
                            break;
                        default: throw ExceptionGenerator.InvalidTypeCode(item.definition.code);
                    }
                stream.Write(returnPoint);
                stream.Write(localPoint);
                returnPoint += 4;
                localPoint = ClearVariable(stream, item, localPoint);
            }

            var returnPosition = (uint)stream.Position;
            stream.Seek(finallyPoint, SeekOrigin.Begin);
            stream.Write(returnPosition);
            stream.Seek(returnPosition, SeekOrigin.Begin);
        }
    }
    internal unsafe class KernelMemberMethodInvoker : KernelInvoker
    {
        internal readonly Invoker[] invokers;
        private KernelMemberMethodInvoker(params Invoker[] invokers)
        {
            this.invokers = invokers;
        }
        internal MethodInfo CreateMethodInfo(MemoryStream stream, Type type, uint index)
        {
            var entries = new uint[invokers.Length];
            var functions = new FunctionInfo[invokers.Length];
            for (int i = 0; i < invokers.Length; i++)
            {
                var function = KernelMethod.memberMethods[index].functions[i];
                entries[i] = (uint)stream.Position;
                functions[i] = new FunctionInfo(function.parameters, function.returns);
                CreateFunctionInfo(stream, type, index, (uint)i, function);
            }
            return new MethodInfo(entries, functions);
        }
        private static void CreateFunctionInfo(Stream stream, Type type, uint method, uint function, KernelMethod.Function info)
        {
            var parameterSize = Frame.SIZE + (uint)info.returns.Length * 4 + type.FieldSize;
            foreach (var item in info.parameters) parameterSize += item.FieldSize;
            var executeSize = parameterSize;
            foreach (var item in info.returns) executeSize += item.FieldSize;

            var returnPoint = PrepareInvoker(stream, parameterSize, executeSize, info, out var finallyPoint);

            var parameterPoint = PushParameter(stream, type, returnPoint);
            foreach (var item in info.parameters) parameterPoint = PushParameter(stream, item, parameterPoint);

            stream.WriteByte((byte)CommandMacro.FUNCTION_KernelMemberCall);
            stream.Write(method);
            stream.Write(function);

            WriteReturns(stream, parameterSize, finallyPoint, info);
            returnPoint = ClearVariable(stream, type, returnPoint);
            foreach (var item in info.parameters)
                returnPoint = ClearVariable(stream, item, returnPoint);
            stream.WriteByte((byte)CommandMacro.FUNCTION_Return);
        }
        /// <summary>
        /// 对应<see cref="KernelMethod.memberMethods"/>
        /// </summary>
        internal static readonly KernelMemberMethodInvoker[] methods;
        internal delegate ExitCode Invoker(Kernel kernel, byte* stack, uint top);
        static KernelMemberMethodInvoker()
        {
            methods = new KernelMemberMethodInvoker[]
            {
                new KernelMemberMethodInvoker(bool_ToString),

                new KernelMemberMethodInvoker(integer_ToString),

                new KernelMemberMethodInvoker(real_ToString),

                new KernelMemberMethodInvoker(real2_Normalized),
                new KernelMemberMethodInvoker(real2_Magnitude),
                new KernelMemberMethodInvoker(real2_SqrMagnitude),

                new KernelMemberMethodInvoker(real3_Normalized),
                new KernelMemberMethodInvoker(real3_Magnitude),
                new KernelMemberMethodInvoker(real3_SqrMagnitude),

                new KernelMemberMethodInvoker(string_GetLength),
                new KernelMemberMethodInvoker(string_GetStringID),
                new KernelMemberMethodInvoker(string_ToBool),
                new KernelMemberMethodInvoker(string_ToInteger),
                new KernelMemberMethodInvoker(string_ToReal),

                new KernelMemberMethodInvoker(handle_GetHandleID),

                new KernelMemberMethodInvoker(coroutine_Abort),
                new KernelMemberMethodInvoker(coroutine_GetState),
                new KernelMemberMethodInvoker(coroutine_GetExitCode),
                new KernelMemberMethodInvoker(coroutine_IsPause),
                new KernelMemberMethodInvoker(coroutine_Pause),
                new KernelMemberMethodInvoker(coroutine_Resume),

                new KernelMemberMethodInvoker(entity_GetEntityID),

                new KernelMemberMethodInvoker(array_GetLength),
            };
        }
        #region 函数实现
#pragma warning disable IDE1006
        private static ExitCode bool_ToString(Kernel kernel, byte* stack, uint top)
        {
            var result = (uint*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var value = kernel.stringAgency.Add(((bool*)(stack + top + Frame.SIZE + 4))->ToString());
            kernel.stringAgency.Reference(value);
            kernel.stringAgency.Release(*result);
            *result = value;
            return ExitCode.None;
        }
        private static ExitCode integer_ToString(Kernel kernel, byte* stack, uint top)
        {
            var result = (uint*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var value = kernel.stringAgency.Add(((long*)(stack + top + Frame.SIZE + 4))->ToString());
            kernel.stringAgency.Reference(value);
            kernel.stringAgency.Release(*result);
            *result = value;
            return ExitCode.None;
        }
        private static ExitCode real_ToString(Kernel kernel, byte* stack, uint top)
        {
            var result = (uint*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var value = kernel.stringAgency.Add(((real*)(stack + top + Frame.SIZE + 4))->ToString());
            kernel.stringAgency.Reference(value);
            kernel.stringAgency.Release(*result);
            *result = value;
            return ExitCode.None;
        }
        private static ExitCode real2_Normalized(Kernel kernel, byte* stack, uint top)
        {
            var result = (Real2*)(stack + *(uint*)(stack + top + Frame.SIZE));
            *result = ((Real2*)(stack + top + Frame.SIZE + 4))->normalized;
            return ExitCode.None;
        }
        private static ExitCode real2_Magnitude(Kernel kernel, byte* stack, uint top)
        {
            var result = (real*)(stack + *(uint*)(stack + top + Frame.SIZE));
            *result = ((Real2*)(stack + top + Frame.SIZE + 4))->magnitude;
            return ExitCode.None;
        }
        private static ExitCode real2_SqrMagnitude(Kernel kernel, byte* stack, uint top)
        {
            var result = (real*)(stack + *(uint*)(stack + top + Frame.SIZE));
            *result = ((Real2*)(stack + top + Frame.SIZE + 4))->sqrMagnitude;
            return ExitCode.None;
        }
        private static ExitCode real3_Normalized(Kernel kernel, byte* stack, uint top)
        {
            var result = (Real3*)(stack + *(uint*)(stack + top + Frame.SIZE));
            *result = ((Real3*)(stack + top + Frame.SIZE + 4))->normalized;
            return ExitCode.None;
        }
        private static ExitCode real3_Magnitude(Kernel kernel, byte* stack, uint top)
        {
            var result = (real*)(stack + *(uint*)(stack + top + Frame.SIZE));
            *result = ((Real3*)(stack + top + Frame.SIZE + 4))->magnitude;
            return ExitCode.None;
        }
        private static ExitCode real3_SqrMagnitude(Kernel kernel, byte* stack, uint top)
        {
            var result = (real*)(stack + *(uint*)(stack + top + Frame.SIZE));
            *result = ((Real3*)(stack + top + Frame.SIZE + 4))->sqrMagnitude;
            return ExitCode.None;
        }
        private static ExitCode string_GetLength(Kernel kernel, byte* stack, uint top)
        {
            var result = (long*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var address = (uint*)(stack + top + Frame.SIZE + 4);
            var source = kernel.stringAgency.Get(*address);
            *result = source.Length;
            kernel.stringAgency.Release(*address);
            return ExitCode.None;
        }
        private static ExitCode string_GetStringID(Kernel kernel, byte* stack, uint top)
        {
            var result = (long*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var address = (uint*)(stack + top + Frame.SIZE + 4);
            *result = *address;
            kernel.stringAgency.Release(*address);
            return ExitCode.None;
        }
        private static ExitCode string_ToBool(Kernel kernel, byte* stack, uint top)
        {
            var result = (bool*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var address = (uint*)(stack + top + Frame.SIZE + 4);
            var source = kernel.stringAgency.Get(*address);
            bool.TryParse(source, out var value);
            *result = value;
            kernel.stringAgency.Release(*address);
            return ExitCode.None;
        }
        private static ExitCode string_ToInteger(Kernel kernel, byte* stack, uint top)
        {
            var result = (long*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var address = (uint*)(stack + top + Frame.SIZE + 4);
            var source = kernel.stringAgency.Get(*address);
            long.TryParse(source, out var value);
            *result = value;
            kernel.stringAgency.Release(*address);
            return ExitCode.None;
        }
        private static ExitCode string_ToReal(Kernel kernel, byte* stack, uint top)
        {
            var result = (real*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var address = (uint*)(stack + top + Frame.SIZE + 4);
            var source = kernel.stringAgency.Get(*address);
            real.TryParse(source, out var value);
            *result = value;
            kernel.stringAgency.Release(*address);
            return ExitCode.None;
        }
        private static ExitCode handle_GetHandleID(Kernel kernel, byte* stack, uint top)
        {
            var result = (long*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var handle = *(uint*)(stack + top + Frame.SIZE + 4);
            *result = handle;
            kernel.heapAgency.Release(handle);
            return ExitCode.None;
        }
        private static ExitCode coroutine_Abort(Kernel kernel, byte* stack, uint top)
        {
            var handle = *(uint*)(stack + top + Frame.SIZE);
            var code = kernel.heapAgency.TryGetPoint(handle, out var point);
            var instance = *(ulong*)point;
            kernel.heapAgency.Release(handle);
            if (code != ExitCode.None) return code;
            var exitCode = *(long*)(stack + top + Frame.SIZE + TypeCode.Handle.FieldSize());
            kernel.coroutineAgency.GetInternalInvoker(instance).Abort(exitCode);
            return ExitCode.None;
        }
        private static ExitCode coroutine_GetState(Kernel kernel, byte* stack, uint top)
        {
            var result = (long*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var handle = *(uint*)(stack + top + Frame.SIZE + 4);
            var code = kernel.heapAgency.TryGetPoint(handle, out var point);
            var instance = *(ulong*)point;
            kernel.heapAgency.Release(handle);
            if (code != ExitCode.None) return code;
            *result = (long)kernel.coroutineAgency.GetInternalInvoker(instance).state;
            return ExitCode.None;
        }
        private static ExitCode coroutine_GetExitCode(Kernel kernel, byte* stack, uint top)
        {
            var result = (long*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var handle = *(uint*)(stack + top + Frame.SIZE + 4);
            var code = kernel.heapAgency.TryGetPoint(handle, out var point);
            var instance = *(ulong*)point;
            kernel.heapAgency.Release(handle);
            if (code != ExitCode.None) return code;
            *result = kernel.coroutineAgency.GetInternalInvoker(instance).exit;
            return ExitCode.None;
        }
        private static ExitCode coroutine_IsPause(Kernel kernel, byte* stack, uint top)
        {
            var result = (bool*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var handle = *(uint*)(stack + top + Frame.SIZE + 4);
            var code = kernel.heapAgency.TryGetPoint(handle, out var point);
            var instance = *(ulong*)point;
            kernel.heapAgency.Release(handle);
            if (code != ExitCode.None) return code;
            *result = kernel.coroutineAgency.GetInternalInvoker(instance).IsPause;
            return ExitCode.None;
        }
        private static ExitCode coroutine_Pause(Kernel kernel, byte* stack, uint top)
        {
            var handle = *(uint*)(stack + top + Frame.SIZE);
            var code = kernel.heapAgency.TryGetPoint(handle, out var point);
            var instance = *(ulong*)point;
            kernel.heapAgency.Release(handle);
            if (code != ExitCode.None) return code;
            kernel.coroutineAgency.GetInternalInvoker(instance).IsPause = true;
            return ExitCode.None;
        }
        private static ExitCode coroutine_Resume(Kernel kernel, byte* stack, uint top)
        {
            var handle = *(uint*)(stack + top + Frame.SIZE);
            var code = kernel.heapAgency.TryGetPoint(handle, out var point);
            var instance = *(ulong*)point;
            kernel.heapAgency.Release(handle);
            if (code != ExitCode.None) return code;
            kernel.coroutineAgency.GetInternalInvoker(instance).IsPause = false;
            return ExitCode.None;
        }
        private static ExitCode entity_GetEntityID(Kernel kernel, byte* stack, uint top)
        {
            var result = (long*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var entity = (Entity*)(stack + top + Frame.SIZE + 4);
            *result = (long)entity->entity;
            kernel.manipulator.Release(*entity);
            return ExitCode.None;
        }
        private static ExitCode array_GetLength(Kernel kernel, byte* stack, uint top)
        {
            var result = (long*)(stack + *(uint*)(stack + top + Frame.SIZE));
            var handle = *(uint*)(stack + top + Frame.SIZE + 4);
            var code = kernel.heapAgency.TryGetArrayLength(handle, out var length);
            *result = length;
            kernel.heapAgency.Release(handle);
            return code;
        }
#pragma warning restore IDE1006
        #endregion
    }
    internal unsafe class KernelMethodInvoker : KernelInvoker
    {
        internal readonly Invoker[] invokers;
        private KernelMethodInvoker(params Invoker[] invokers)
        {
            this.invokers = invokers;
        }
        internal MethodInfo CreateMethodInfo(MemoryStream stream, uint index)
        {
            var entries = new uint[invokers.Length];
            var functions = new FunctionInfo[invokers.Length];
            for (int i = 0; i < invokers.Length; i++)
            {
                var function = KernelMethod.methods[index].functions[i];
                entries[i] = (uint)stream.Position;
                functions[i] = new FunctionInfo(function.parameters, function.returns);
                CreateFunctionInfo(stream, index, (uint)i, function);
            }
            return new MethodInfo(entries, functions);
        }
        private static void CreateFunctionInfo(Stream stream, uint method, uint function, KernelMethod.Function info)
        {
            var parameterSize = Frame.SIZE + (uint)info.returns.Length * 4;
            foreach (var item in info.parameters) parameterSize += item.FieldSize;
            var executeSize = parameterSize;
            foreach (var item in info.returns) executeSize += item.FieldSize;

            var parameterPoint = PrepareInvoker(stream, parameterSize, executeSize, info, out var finallyPoint);

            foreach (var item in info.parameters) parameterPoint = PushParameter(stream, item, parameterPoint);

            stream.WriteByte((byte)CommandMacro.FUNCTION_KernelCall);
            stream.Write(method);
            stream.Write(function);

            WriteReturns(stream, parameterSize, finallyPoint, info);
            var returnPoint = Frame.SIZE + (uint)info.returns.Length * 4;
            foreach (var item in info.parameters)
                returnPoint = ClearVariable(stream, item, returnPoint);
            stream.WriteByte((byte)CommandMacro.FUNCTION_Return);
        }
        /// <summary>
        /// 对应<see cref="KernelMethod.methods"/>
        /// </summary>
        internal static readonly KernelMethodInvoker[] methods;
        internal delegate void Invoker(Kernel kernel, byte* stack, uint top);
        static KernelMethodInvoker()
        {
            methods = new KernelMethodInvoker[]
            {
                new KernelMethodInvoker(integer_Abs, real_Abs),
                new KernelMethodInvoker(real_Acos),
                new KernelMethodInvoker(real2_Angle, real3_Angle),
                new KernelMethodInvoker(real_Asin),
                new KernelMethodInvoker(real_Atan),
                new KernelMethodInvoker(real_Atan2),
                new KernelMethodInvoker(real_Ceil),
                new KernelMethodInvoker(integer_Clamp, real_Clamp),
                new KernelMethodInvoker(real_Clamp01),
                new KernelMethodInvoker(real_Cos),
                new KernelMethodInvoker(Collect),
                new KernelMethodInvoker(CountCoroutine),
                new KernelMethodInvoker(CountEntity),
                new KernelMethodInvoker(CountHandle),
                new KernelMethodInvoker(real2_Cross, real3_Cross),
                new KernelMethodInvoker(real2_Dot, real3_Dot, real4_Dot),
                new KernelMethodInvoker(real_Floor),
                new KernelMethodInvoker(integer_GetRandomInt),
                new KernelMethodInvoker(real_GetRandomReal),
                new KernelMethodInvoker(HeapTotalMemory),
                new KernelMethodInvoker(real_Lerp, real2_Lerp, real3_Lerp, real4_Lerp),
                new KernelMethodInvoker(integer_Max, real_Max, real2_Max, real3_Max, real4_Max),
                new KernelMethodInvoker(integer_Min, real_Min, real2_Min, real3_Min, real4_Min),
                new KernelMethodInvoker(real_Round),
                new KernelMethodInvoker(SetRandomSeed),
                new KernelMethodInvoker(real_Sign),
                new KernelMethodInvoker(real_Sin),
                new KernelMethodInvoker(real_SinCos),
                new KernelMethodInvoker(real_Sqrt),
            };
        }
        #region 函数实现
#pragma warning disable IDE1006
        private static void integer_Abs(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(long*)(stack + top + Frame.SIZE + 4);
            *(long*)(stack + returnPoint) = value < 0 ? -value : value;
        }
        private static void integer_Clamp(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(long*)(stack + top + Frame.SIZE + 4);
            var min = *(long*)(stack + top + Frame.SIZE + 4 + TypeCode.Integer.FieldSize());
            var max = *(long*)(stack + top + Frame.SIZE + 4 + TypeCode.Integer.FieldSize() * 2);
            *(long*)(stack + returnPoint) = value < min ? min : value > max ? max : value;
        }
        private static void integer_GetRandomInt(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            *(long*)(stack + returnPoint) = kernel.random.Next();
        }
        private static void integer_Max(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var a = *(long*)(stack + top + Frame.SIZE + 4);
            var b = *(long*)(stack + top + Frame.SIZE + 4 + TypeCode.Integer.FieldSize());
            *(long*)(stack + returnPoint) = a > b ? a : b;
        }
        private static void integer_Min(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var a = *(long*)(stack + top + Frame.SIZE + 4);
            var b = *(long*)(stack + top + Frame.SIZE + 4 + TypeCode.Integer.FieldSize());
            *(long*)(stack + returnPoint) = a < b ? a : b;
        }
        private static void real_Abs(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
            *(real*)(stack + returnPoint) = Math.Abs(value);
        }
        private static void real_Acos(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
            *(real*)(stack + returnPoint) = Math.Acos(value);
        }
        private static void real_Asin(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
            *(real*)(stack + returnPoint) = Math.Asin(value);
        }
        private static void real_Atan(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
            *(real*)(stack + returnPoint) = Math.Atan(value);
        }
        private static void real_Atan2(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var x = *(real*)(stack + top + Frame.SIZE + 4);
            var y = *(real*)(stack + top + Frame.SIZE + 4 + TypeCode.Real.FieldSize());
            *(real*)(stack + returnPoint) = Math.Atan2(x, y);
        }
        private static void real_Ceil(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
#if FIXED
            *(long*)(stack + returnPoint) = Math.Ceil(value);
#else
            *(long*)(stack + returnPoint) = (long)Math.Ceiling(value);
#endif
        }
        private static void real_Clamp(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
            var min = *(real*)(stack + top + Frame.SIZE + 4 + TypeCode.Real.FieldSize());
            var max = *(real*)(stack + top + Frame.SIZE + 4 + TypeCode.Real.FieldSize() * 2);
#if FIXED
            *(real*)(stack + returnPoint) = Math.Clamp(value, min, max);
#else
            *(real*)(stack + returnPoint) = value < min ? min : value > max ? max : value;
#endif
        }
        private static void real_Clamp01(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
#if FIXED
            *(real*)(stack + returnPoint) = Math.Clamp01(value);
#else
            *(real*)(stack + returnPoint) = value < 0 ? 0 : value > 1 ? 1 : value;
#endif
        }
        private static void real_Cos(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
            *(real*)(stack + returnPoint) = Math.Cos(value);
        }
        private static void real_Floor(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
#if FIXED
            *(long*)(stack + returnPoint) = Math.Floor(value);
#else
            *(long*)(stack + returnPoint) = (long)Math.Floor(value);
#endif
        }
        private static void real_GetRandomReal(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            *(real*)(stack + returnPoint) = kernel.random.NextReal();
        }
        private static void real_Lerp(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var a = *(real*)(stack + top + Frame.SIZE + 4);
            var b = *(real*)(stack + top + Frame.SIZE + 4 + TypeCode.Real.FieldSize());
            var l = *(real*)(stack + top + Frame.SIZE + 4 + TypeCode.Real.FieldSize() * 2);
#if FIXED
            *(real*)(stack + returnPoint) = Math.Lerp(a, b, l);
#else
            *(real*)(stack + returnPoint) = a + (b - a) * l;
#endif
        }
        private static void real_Max(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var a = *(real*)(stack + top + Frame.SIZE + 4);
            var b = *(real*)(stack + top + Frame.SIZE + 4 + TypeCode.Real.FieldSize());
            *(real*)(stack + returnPoint) = Math.Max(a, b);
        }
        private static void real_Min(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var a = *(real*)(stack + top + Frame.SIZE + 4);
            var b = *(real*)(stack + top + Frame.SIZE + 4 + TypeCode.Real.FieldSize());
            *(real*)(stack + returnPoint) = Math.Min(a, b);
        }
        private static void real_Round(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
#if FIXED
            *(long*)(stack + returnPoint) = Math.Round(value);
#else
            *(long*)(stack + returnPoint) = (long)Math.Round(value);
#endif
        }
        private static void real_Sign(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
            *(long*)(stack + returnPoint) = Math.Sign(value);
        }
        private static void real_Sin(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
            *(real*)(stack + returnPoint) = Math.Sin(value);
        }
        private static void real_SinCos(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = (uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 8);
            *(real*)(stack + returnPoint[0]) = Math.Sin(value);
            *(real*)(stack + returnPoint[1]) = Math.Cos(value);
        }
        private static void real_Sqrt(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
            *(real*)(stack + returnPoint) = Math.Sqrt(value);
        }
        private static void real_Tan(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var value = *(real*)(stack + top + Frame.SIZE + 4);
            *(real*)(stack + returnPoint) = Math.Tan(value);
        }
        private static void real2_Angle(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real2*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real2*)(stack + top + Frame.SIZE + 20);
            *(real*)(stack + returnPoint) = Real2.Angle(vector1, vector2);
        }
        private static void real2_Cross(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real2*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real2*)(stack + top + Frame.SIZE + 20);
            *(real*)(stack + returnPoint) = Real2.Cross(vector1, vector2);
        }
        private static void real2_Dot(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real2*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real2*)(stack + top + Frame.SIZE + 20);
            *(real*)(stack + returnPoint) = Real2.Dot(vector1, vector2);
        }
        private static void real2_Lerp(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real2*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real2*)(stack + top + Frame.SIZE + 20);
            var lerp = *(real*)(stack + top + Frame.SIZE + 36);
            *(Real2*)(stack + returnPoint) = Real2.Lerp(vector1, vector2, lerp);
        }
        private static void real2_Max(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real2*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real2*)(stack + top + Frame.SIZE + 20);
            *(Real2*)(stack + returnPoint) = Real2.Max(vector1, vector2);
        }
        private static void real2_Min(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real2*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real2*)(stack + top + Frame.SIZE + 20);
            *(Real2*)(stack + returnPoint) = Real2.Min(vector1, vector2);
        }
        private static void real3_Angle(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real3*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real3*)(stack + top + Frame.SIZE + 28);
            *(real*)(stack + returnPoint) = Real3.Angle(vector1, vector2);
        }
        private static void real3_Cross(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real3*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real3*)(stack + top + Frame.SIZE + 28);
            *(Real3*)(stack + returnPoint) = Real3.Cross(vector1, vector2);
        }
        private static void real3_Dot(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real3*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real3*)(stack + top + Frame.SIZE + 28);
            *(real*)(stack + returnPoint) = Real3.Dot(vector1, vector2);
        }
        private static void real3_Lerp(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real3*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real3*)(stack + top + Frame.SIZE + 28);
            var lerp = *(real*)(stack + top + Frame.SIZE + 52);
            *(Real3*)(stack + returnPoint) = Real3.Lerp(vector1, vector2, lerp);
        }
        private static void real3_Max(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real3*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real3*)(stack + top + Frame.SIZE + 28);
            *(Real3*)(stack + returnPoint) = Real3.Max(vector1, vector2);
        }
        private static void real3_Min(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real3*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real3*)(stack + top + Frame.SIZE + 28);
            *(Real3*)(stack + returnPoint) = Real3.Min(vector1, vector2);
        }
        private static void real4_Dot(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real4*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real4*)(stack + top + Frame.SIZE + 36);
            *(real*)(stack + returnPoint) = Real4.Dot(vector1, vector2);
        }
        private static void real4_Lerp(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real4*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real4*)(stack + top + Frame.SIZE + 36);
            var lerp = *(real*)(stack + top + Frame.SIZE + 68);
            *(Real4*)(stack + returnPoint) = Real4.Lerp(vector1, vector2, lerp);
        }
        private static void real4_Max(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real4*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real4*)(stack + top + Frame.SIZE + 36);
            *(Real4*)(stack + returnPoint) = Real4.Max(vector1, vector2);
        }
        private static void real4_Min(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var vector1 = *(Real4*)(stack + top + Frame.SIZE + 4);
            var vector2 = *(Real4*)(stack + top + Frame.SIZE + 36);
            *(Real4*)(stack + returnPoint) = Real4.Min(vector1, vector2);
        }
        private static void Collect(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            var size = kernel.heapAgency.GetHeapTop();
            kernel.heapAgency.GC();
            *(long*)(stack + returnPoint) = size - kernel.heapAgency.GetHeapTop();
        }
        private static void HeapTotalMemory(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            *(long*)(stack + returnPoint) = kernel.heapAgency.GetHeapTop();
        }
        private static void CountCoroutine(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            *(long*)(stack + returnPoint) = kernel.coroutineAgency.GetCoroutineCount();
        }
        private static void CountEntity(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            *(long*)(stack + returnPoint) = kernel.manipulator.GetEntityCount();
        }
        private static void CountHandle(Kernel kernel, byte* stack, uint top)
        {
            var returnPoint = *(uint*)(stack + top + Frame.SIZE);
            *(long*)(stack + returnPoint) = kernel.heapAgency.GetHandleCount();
        }
        private static void SetRandomSeed(Kernel kernel, byte* stack, uint top)
        {
            kernel.random.SetSeed(*(long*)(stack + top + Frame.SIZE + 4));
        }
#pragma warning restore IDE1006
        #endregion
    }
}
