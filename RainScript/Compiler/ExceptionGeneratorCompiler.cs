using System;

namespace RainScript.Compiler
{
    internal class ExpectedException : Exception
    {
        public ExpectedException(string message) : base(message) { }
    }

    internal class ExceptionGeneratorCompiler : ExceptionGenerator
    {
        public static Exception InvalidRecycleOperation()
        {
            return new InvalidOperationException("回收和初始化步调不一致");
        }
        public static Exception InvalidCompilingState(CompileState state)
        {
            return new InvalidOperationException("{0}状态不能执行该操作".Format(state));
        }
        public static Exception TextInfoMismatching()
        {
            return new Exception("文本不匹配");
        }
        public static Exception ConstantParseFail(string value)
        {
            return new Exception("常量解析失败:" + value);
        }
        public static Exception InvalidDeclaration(Declaration declaration)
        {
            return new Exception("无效的声明:" + declaration);
        }
        public static Exception InvalidLexicalType(LexicalType type)
        {
            return new Exception("无效的词汇类型：" + type);
        }
        public static Exception InvalidCompilingType(CompilingType type)
        {
            return new Exception("无效的词汇类型：" + type);
        }
        public static Exception Unknown()
        {
            return new Exception("未知的编译错误");
        }
        public static ExpectedException DuplicateLibraryNames()
        {
            return new ExpectedException("有重复的库名");
        }
        public static ExpectedException CircularRely()
        {
            return new ExpectedException("有循环依赖");
        }
        public static ExpectedException UnknownRelyError()
        {
            return new ExpectedException("未知的依赖错误");
        }
        public static ExpectedException RelyInitFail()
        {
            return new ExpectedException("依赖初始化失败");
        }
        public static ExpectedException DeclaractionParseFail()
        {
            return new ExpectedException("声明定义解析失败");
        }
        public static ExpectedException DeclaractionTidyFail()
        {
            return new ExpectedException("声明定义整理失败");
        }
        public static ExpectedException DeclaractionLinkFail()
        {
            return new ExpectedException("声明定义链接失败");
        }
        public static ExpectedException DeclaractionIllegal()
        {
            return new ExpectedException("声明定义不合法");
        }
        public static ExpectedException InterfaceImplements()
        {
            return new ExpectedException("有接口没实现");
        }
        public static ExpectedException ReferenceGeneratorFail()
        {
            return new ExpectedException("引用信息生成失败");
        }
        public static ExpectedException LogicGeneratorFail()
        {
            return new ExpectedException("库生成失败");
        }
        public static ExpectedException LibraryGeneratorFail()
        {
            return new ExpectedException("库生成失败");
        }
    }
}
