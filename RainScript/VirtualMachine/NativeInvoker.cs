﻿using System.Reflection;
using System.Reflection.Emit;
using RainScript.Vector;
using static System.Reflection.Emit.OpCodes;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript.VirtualMachine
{
    internal unsafe class NativeInvoker
    {
        private static readonly FieldInfo field_kernel_manipulator = typeof(Kernel).GetField("manipulator", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly System.Reflection.MethodInfo method_EntityManipulator_Add = typeof(EntityManipulator).GetMethod("Add");
        private static readonly System.Reflection.MethodInfo method_EntityManipulator_Get = typeof(EntityManipulator).GetMethod("Get");
        private static readonly System.Reflection.MethodInfo method_EntityManipulator_Reference = typeof(EntityManipulator).GetMethod("Reference");
        private static readonly System.Reflection.MethodInfo method_EntityManipulator_Release = typeof(EntityManipulator).GetMethod("Release");
        private static readonly FieldInfo field_Kernel_stringAgency = typeof(Kernel).GetField("stringAgency", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly System.Reflection.MethodInfo method_StringAgency_Add = typeof(StringAgency).GetMethod("Add");
        private static readonly System.Reflection.MethodInfo method_StringAgency_Reference = typeof(StringAgency).GetMethod("Reference");
        private static readonly System.Reflection.MethodInfo method_StringAgency_Release = typeof(StringAgency).GetMethod("Release");
        private static readonly System.Reflection.MethodInfo method_StringAgency_Get = typeof(StringAgency).GetMethod("Get");
        private static readonly System.Type[] parameters = new System.Type[] { typeof(Kernel), typeof(IPerformer), typeof(byte*), typeof(uint) };
        internal delegate void NativeInvoke(Kernel kernel, IPerformer performer, byte* stack, uint top);
        internal readonly NativeInvoke invoke;
        private readonly System.Type returnType;
        public NativeInvoker(string name, FunctionInfo function, IPerformer performer)
        {
            var types = new System.Type[function.parameters.Length];
            for (int i = 0; i < types.Length; i++)
                if (!TryGetType(function.parameters[i], out types[i]))
                    throw ExceptionGeneratorVM.CommunicationNotSupportedType(name, i);
            var methodInfo = performer.GetType().GetMethod(name, types);
            if (methodInfo == null) throw ExceptionGeneratorVM.PerformerMethodNotFound(name);
            returnType = methodInfo.ReturnType;
            if (!CheckReturnTypes(returnType, function.returns)) throw ExceptionGeneratorVM.ReturnTypeInconsistency(name);
            var dynamicMethod = new DynamicMethod(name, typeof(void), parameters, GetType(), true);
            GenerateNative(dynamicMethod.GetILGenerator(), methodInfo);
            invoke = (NativeInvoke)dynamicMethod.CreateDelegate(typeof(NativeInvoke));
        }
        private bool CheckReturnTypes(System.Type type, Type[] returns)
        {
            if (type == typeof(void)) return returns.Length == 0;
            if (returns.Length != 1) return false;
            if (type == typeof(bool)) return returns[0] == KERNEL_TYPE.BOOL;
            else if (type == typeof(long)) return returns[0] == KERNEL_TYPE.INTEGER;
            else if (type == typeof(real)) return returns[0] == KERNEL_TYPE.REAL;
            else if (type == typeof(Real2)) return returns[0] == KERNEL_TYPE.REAL2;
            else if (type == typeof(Real3)) return returns[0] == KERNEL_TYPE.REAL3;
            else if (type == typeof(Real4)) return returns[0] == KERNEL_TYPE.REAL4;
            else if (type == typeof(string)) return returns[0] == KERNEL_TYPE.STRING;
            else if (type == typeof(object)) return returns[0] == KERNEL_TYPE.ENTITY;
            return false;
        }
        private static bool TryGetType(Type type, out System.Type result)
        {
            if (type.dimension == 0 && type.definition.library == LIBRARY.KERNEL)
            {
                switch (type.definition.code)
                {
                    case TypeCode.Invalid:
                        break;
                    case TypeCode.Bool:
                        result = typeof(bool);
                        return true;
                    case TypeCode.Integer:
                        result = typeof(long);
                        return true;
                    case TypeCode.Real:
                        result = typeof(real);
                        return true;
                    case TypeCode.Real2:
                        result = typeof(Real2);
                        return true;
                    case TypeCode.Real3:
                        result = typeof(Real3);
                        return true;
                    case TypeCode.Real4:
                        result = typeof(Real4);
                        return true;
                    case TypeCode.String:
                        result = typeof(string);
                        return true;
                    case TypeCode.Handle:
                    case TypeCode.Interface:
                    case TypeCode.Function:
                    case TypeCode.Coroutine:
                        break;
                    case TypeCode.Entity:
                        result = typeof(object);
                        return true;
                    default:
                        break;
                }
            }
            result = null;
            return false;
        }
        private static void GenerateNative(ILGenerator generator, System.Reflection.MethodInfo method)
        {
            int pidx = Frame.SIZE;
            var topPoint = generator.DeclareLocal(typeof(byte*));
            generator.Emit(Ldarg_2);
            generator.Emit(Ldarg_3);
            generator.Emit(Conv_U);
            generator.Emit(Add);
            generator.Emit(Stloc, topPoint);//stack+top
            var retType = method.ReturnType;
            LocalBuilder returnPoint = null;
            if (retType != typeof(void))
            {
                pidx += 4;
                returnPoint = generator.DeclareLocal(typeof(byte*));
                generator.Emit(Ldarg_2);
                generator.Emit(Ldarg_2);
                generator.Emit(Ldarg_3);
                generator.Emit(Conv_U);
                generator.Emit(Add);
                generator.Emit(Ldc_I4_S, Frame.SIZE);
                generator.Emit(Add);
                generator.Emit(Ldind_U4);
                generator.Emit(Conv_U);
                generator.Emit(Add);
                generator.Emit(Stloc, returnPoint);//stack+*(uint*)(stack+top+Frame.SIZE)
                generator.Emit(Ldloc, returnPoint);
                if (retType == typeof(string))
                {
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_Kernel_stringAgency);
                    generator.Emit(Ldloc, returnPoint);
                    generator.Emit(Ldind_U4);
                    generator.Emit(Callvirt, method_StringAgency_Release);
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_Kernel_stringAgency);
                }
                else if (retType == typeof(object))
                {
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_kernel_manipulator);
                    generator.Emit(Ldloc, returnPoint);
                    generator.Emit(Ldobj, typeof(Entity));
                    generator.Emit(Callvirt, method_EntityManipulator_Release);
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_kernel_manipulator);
                }
            }
            generator.Emit(Ldarg_1);
            generator.Emit(Castclass, method.DeclaringType);
            foreach (var parameter in method.GetParameters())
            {
                var type = parameter.ParameterType;
                if (type == typeof(bool))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldind_U1);
                    pidx += (int)TypeCode.Bool.FieldSize();
                }
                else if (type == typeof(long))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldind_I8);
                    pidx += (int)TypeCode.Integer.FieldSize();
                }
                else if (type == typeof(real))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
#if FIXED
                    generator.Emit(Ldobj, typeof(real));
#else
                    generator.Emit(Ldind_R8);
#endif
                    pidx += (int)TypeCode.Real.FieldSize();
                }
                else if (type == typeof(Real2))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldobj, type);
                    pidx += (int)TypeCode.Real2.FieldSize();
                }
                else if (type == typeof(Real3))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldobj, type);
                    pidx += (int)TypeCode.Real3.FieldSize();
                }
                else if (type == typeof(Real4))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldobj, type);
                    pidx += (int)TypeCode.Real4.FieldSize();
                }
                else if (type == typeof(string))
                {
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_Kernel_stringAgency);
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldind_U4);
                    generator.Emit(Callvirt, method_StringAgency_Get);

                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_Kernel_stringAgency);
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldind_U4);
                    generator.Emit(Callvirt, method_StringAgency_Release);
                    pidx += (int)TypeCode.String.FieldSize();
                }
                else if (type == typeof(object))
                {
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_kernel_manipulator);
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldobj, typeof(Entity));
                    generator.Emit(Callvirt, method_EntityManipulator_Get);

                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_kernel_manipulator);
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldobj, typeof(Entity));
                    generator.Emit(Callvirt, method_EntityManipulator_Release);
                    pidx += (int)TypeCode.Entity.FieldSize();
                }
                else throw ExceptionGeneratorVM.CommunicationNotSupportedType(method.Name, type.Name);
            }
            generator.Emit(Callvirt, method);
            if (retType == typeof(bool)) generator.Emit(Stind_I1);
            else if (retType == typeof(long)) generator.Emit(Stind_I8);
            else if (retType == typeof(real))
#if FIXED 
                generator.Emit(Stobj, typeof(real));
#else
                generator.Emit(Stind_R8);
#endif
            else if (retType == typeof(Real2) || retType == typeof(Real3) || retType == typeof(Real4)) generator.Emit(Stobj, retType);
            else if (retType == typeof(string))
            {
                generator.Emit(Callvirt, method_StringAgency_Add);
                generator.Emit(Stind_I4);

                generator.Emit(Ldarg_0);
                generator.Emit(Ldfld, field_Kernel_stringAgency);
                generator.Emit(Ldloc, returnPoint);
                generator.Emit(Ldind_U4);
                generator.Emit(Callvirt, method_StringAgency_Reference);
            }
            else if (retType == typeof(object))
            {
                generator.Emit(Callvirt, method_EntityManipulator_Add);
                generator.Emit(Stobj, typeof(Entity));

                generator.Emit(Ldarg_0);
                generator.Emit(Ldfld, field_kernel_manipulator);
                generator.Emit(Ldloc, returnPoint);
                generator.Emit(Ldobj, typeof(Entity));
                generator.Emit(Callvirt, method_EntityManipulator_Reference);
            }
            else if (retType != typeof(void)) throw ExceptionGeneratorVM.CommunicationNotSupportedType(method.Name, retType.Name);
            generator.Emit(Ret);
        }
        private static void GenerateNative2(ILGenerator generator, System.Reflection.MethodInfo method)
        {
            int pidx = Frame.SIZE;
            var topPoint = generator.DeclareLocal(typeof(byte*));
            generator.Emit(Ldarg_2);
            generator.Emit(Ldarg_3);
            generator.Emit(Conv_U);
            generator.Emit(Add);
            generator.Emit(Stloc, topPoint);//stack+top
            var retType = method.ReturnType;
            LocalBuilder returnPoint = null;
            if (retType != typeof(void))
            {
                pidx += 4;
                returnPoint = generator.DeclareLocal(typeof(byte*));
                generator.Emit(Ldarg_2);
                generator.Emit(Ldarg_2);
                generator.Emit(Ldarg_3);
                generator.Emit(Conv_U);
                generator.Emit(Add);
                generator.Emit(Ldc_I4_S, Frame.SIZE);
                generator.Emit(Add);
                generator.Emit(Ldind_U4);
                generator.Emit(Conv_U);
                generator.Emit(Add);
                generator.Emit(Stloc, returnPoint);//stack+*(uint*)(stack+top+Frame.SIZE)
                generator.Emit(Ldloc, returnPoint);
                if (retType == typeof(string))
                {
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_Kernel_stringAgency);
                    generator.Emit(Ldloc, returnPoint);
                    generator.Emit(Ldind_U4);
                    generator.Emit(Callvirt, method_StringAgency_Release);
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_Kernel_stringAgency);
                }
                else if (retType == typeof(object))
                {
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_kernel_manipulator);
                    generator.Emit(Ldloc, returnPoint);
                    generator.Emit(Ldobj, typeof(Entity));
                    generator.Emit(Callvirt, method_EntityManipulator_Release);
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_kernel_manipulator);
                }
            }
            generator.Emit(Ldarg_1);
            generator.Emit(Castclass, method.DeclaringType);
            foreach (var parameter in method.GetParameters())
            {
                var type = parameter.ParameterType;
                if (type == typeof(bool))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldind_U1);
                    pidx += (int)TypeCode.Bool.FieldSize();
                }
                else if (type == typeof(long))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldind_I8);
                    pidx += (int)TypeCode.Integer.FieldSize();
                }
                else if (type == typeof(real))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
#if FIXED
                    generator.Emit(Ldobj, typeof(real));
#else
                    generator.Emit(Ldind_R8);
#endif
                    pidx += (int)TypeCode.Real.FieldSize();
                }
                else if (type == typeof(Real2))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldobj, type);
                    pidx += (int)TypeCode.Real2.FieldSize();
                }
                else if (type == typeof(Real3))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldobj, type);
                    pidx += (int)TypeCode.Real3.FieldSize();
                }
                else if (type == typeof(Real4))
                {
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldobj, type);
                    pidx += (int)TypeCode.Real4.FieldSize();
                }
                else if (type == typeof(string))
                {
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_Kernel_stringAgency);
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldind_U4);
                    generator.Emit(Callvirt, method_StringAgency_Get);

                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_Kernel_stringAgency);
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldind_U4);
                    generator.Emit(Callvirt, method_StringAgency_Release);
                    pidx += (int)TypeCode.String.FieldSize();
                }
                else if (type == typeof(object))
                {
                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_kernel_manipulator);
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldobj, typeof(Entity));
                    generator.Emit(Callvirt, method_EntityManipulator_Get);

                    generator.Emit(Ldarg_0);
                    generator.Emit(Ldfld, field_kernel_manipulator);
                    generator.Emit(Ldloc, topPoint);
                    generator.Emit(Ldc_I4, pidx);
                    generator.Emit(Add);
                    generator.Emit(Ldobj, typeof(Entity));
                    generator.Emit(Callvirt, method_EntityManipulator_Release);
                    pidx += (int)TypeCode.Entity.FieldSize();
                }
                else throw ExceptionGeneratorVM.CommunicationNotSupportedType(method.Name, type.Name);
            }
            generator.Emit(Callvirt, method);
            if (retType == typeof(bool)) generator.Emit(Stind_I1);
            else if (retType == typeof(long)) generator.Emit(Stind_I8);
            else if (retType == typeof(real))
#if FIXED 
                generator.Emit(Stobj, typeof(real));
#else
                generator.Emit(Stind_R8);
#endif
            else if (retType == typeof(Real2) || retType == typeof(Real3) || retType == typeof(Real4)) generator.Emit(Stobj, retType);
            else if (retType == typeof(string))
            {
                generator.Emit(Callvirt, method_StringAgency_Add);
                generator.Emit(Stind_I4);

                generator.Emit(Ldarg_0);
                generator.Emit(Ldfld, field_Kernel_stringAgency);
                generator.Emit(Ldloc, returnPoint);
                generator.Emit(Ldind_U4);
                generator.Emit(Callvirt, method_StringAgency_Reference);
            }
            else if (retType == typeof(object))
            {
                generator.Emit(Callvirt, method_EntityManipulator_Add);
                generator.Emit(Stobj, typeof(Entity));

                generator.Emit(Ldarg_0);
                generator.Emit(Ldfld, field_kernel_manipulator);
                generator.Emit(Ldloc, returnPoint);
                generator.Emit(Ldobj, typeof(Entity));
                generator.Emit(Callvirt, method_EntityManipulator_Reference);
            }
            else if (retType != typeof(void)) throw ExceptionGeneratorVM.CommunicationNotSupportedType(method.Name, retType.Name);
            generator.Emit(Ret);
        }
    }
}
