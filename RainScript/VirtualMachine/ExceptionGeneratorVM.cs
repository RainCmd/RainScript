using System;

namespace RainScript.VirtualMachine
{
    internal class ExceptionGeneratorVM : ExceptionGenerator
    {
        internal static Exception LibraryLoadFail(string name)
        {
            return new Exception(name);
        }

        internal static Exception LibraryLoadError(string loadName, string resultName)
        {
            return new Exception("程序集加载失败，需要加载的程序集名：{0}，实际的程序集名：{1}".Format(loadName, resultName));
        }

        internal static Exception MissingDefinition(string name, string target, TypeCode code)
        {
            return new Exception("程序集[{0}]中引用[{1}]的{2}定义未找到".Format(name, target, code));
        }

        internal static Exception InvalidMethodHandle()
        {
            throw new InvalidOperationException("无效的函数句柄");
        }

        internal static Exception CommunicationNotSupportedType(string name, int i)
        {
            return new Exception("内部函数[{0}]的参数{1}的类型不支持跨脚本通信".Format(name, i));
        }

        internal static Exception PerformerMethodNotFound(string name)
        {
            return new Exception("未找到与内部函数[{0}]匹配的接口".Format(name));
        }

        internal static Exception ReturnTypeInconsistency(string name)
        {
            return new Exception("内部函数[{0}]的返回值类型不支持跨脚本通信".Format(name));
        }

        internal static Exception CommunicationNotSupportedType(string name, string type)
        {
            return new Exception("内部函数[{0}]引用到的[{1}]不支持跨脚本通信".Format(name, type));
        }

        internal static Exception CyclicInheritance(string name)
        {
            return new Exception("程序集[{0}]的类型可能存在循环继承".Format(name));
        }

        internal static Exception MissingDefinition(string self, string target, string definition)
        {
            return new Exception("程序集[{0}]引用的程序集[{1}]中的{2}未找到".Format(self, target, definition));
        }

        internal static Exception EntryNotFound(string name, Function function)
        {
            return new Exception("程序集[{0}]的函数[{1},{2}]入口查找失败".Format(name, function.method, function.index));
        }
        internal static Exception EntryNotFound(string name, DefinitionFunction function, Type type)
        {
            return new Exception("程序集[{0}]的函数[{1},{2},{3},{4}]查找失败,目标对象类型:{5}".Format(name, function.definition.code, function.definition.index, function.funtion.method, function.funtion.index, type));
        }

        internal static Exception ObjectDisposed()
        {
            return new ObjectDisposedException("对象已销毁");
        }

        internal static Exception InvalidOperation(InvokerState state)
        {
            return new InvalidOperationException("{0}状态下不能进行改操作".Format(state));
        }

        internal static Exception InvokerTypeCastFail(FunctionHandle handle, TypeCode code1, TypeCode code2)
        {
            return new InvalidCastException("对程序集 {0} 的携程调用时试图将 {0} 转换为 {1}".Format(handle.library.name, code1, code2));
        }

        internal static Exception InvalidCommand(CommandMacro command)
        {
            return new NotSupportedException("不支持的命令：{0}".Format(command));
        }
        internal static Exception InvalidFunctionType(FunctionType functionType)
        {
            return new NotSupportedException("不支持的函数类型：{0}".Format(functionType));
        }
        internal static Exception InvalidAllocOperation()
        {
            return new InvalidOperationException("可能在析构函数中创建了新的对象。");
        }
    }
}
