using System.Collections.Generic;

namespace RainScript.Compiler
{
    /// <summary>
    /// 编译错误代码
    /// </summary>
    public enum CompilingExceptionCode
    {
        #region 词法
        /// <summary>
        /// 未知的词法解析错误
        /// </summary>
        LEXICAL_UNKNOWN = 0x01_0000,
        /// <summary>
        /// 缺少配对的符号
        /// </summary>
        LEXICAL_MISSING_PAIRED_SYMBOL,
        /// <summary>
        /// 未知的符号
        /// </summary>
        LEXICAL_UNKNOWN_SYMBOL,
        #endregion 词法

        #region 语法
        /// <summary>
        /// 未知的语法错误
        /// </summary>
        SYNTAX_UNKNOW = 0x02_0000,
        /// <summary>
        /// 意外的词条
        /// </summary>
        SYNTAX_UNEXPECTED_LEXCAL,
        /// <summary>
        /// 对齐问题
        /// </summary>
        SYNTAX_INDENT,
        /// <summary>
        /// 名称缺失
        /// </summary>
        SYNTAX_MISSING_NAME,
        /// <summary>
        /// 名称是个关键字
        /// </summary>
        SYNTAX_NAME_IS_KEY_WORLD,
        /// <summary>
        /// 无效的可见性标记
        /// </summary>
        SYNTAX_INVALID_VISIBILITY,
        /// <summary>
        /// 常量未赋值
        /// </summary>
        SYNTAX_CONSTANT_NOT_ASSIGNMENT,
        /// <summary>
        /// 意外的行尾
        /// </summary>
        SYNTAX_UNEXPECTED_LINE_END,
        /// <summary>
        /// 构造函数没有返回值
        /// </summary>
        SYNTAX_CONSTRUCTOR_NO_RETURN_VALUE,
        /// <summary>
        /// 定义不是公开的
        /// </summary>
        SYNTAX_DEFINITION_NOT_PUBLIC,
        /// <summary>
        /// 缺少配对的符号
        /// </summary>
        SYNTAX_MISSING_PAIRED_SYMBOL,
        /// <summary>
        /// 析构函数中申请托管内存
        /// </summary>
        SYNTAX_DESTRUCTOR_ALLOC,
        #endregion 语法

        #region 编译
        /// <summary>
        /// 未知的编译错误
        /// </summary>
        COMPILING_UNKNONW = 0x03_0000,
        /// <summary>
        /// 库名重复
        /// </summary>
        COMPILING_DUPLICATE_LIBRARY_NAMES,
        /// <summary>
        /// 重复的声明
        /// </summary>
        COMPILING_DUPLICATE_DECLARATION_NAMES,
        /// <summary>
        /// 库未找到
        /// </summary>
        COMPILING_LIBRARY_NOT_FOUND,
        /// <summary>
        /// 循环依赖
        /// </summary>
        COMPILING_CIRCULAR_RELY,
        /// <summary>
        /// 循环继承
        /// </summary>
        COMPILING_CIRCULAR_INHERIT,
        /// <summary>
        /// 接口未实现
        /// </summary>
        COMPILING_INTERFACE_NOT_IMPLEMENTS,
        /// <summary>
        /// 声明未找到
        /// </summary>
        COMPILING_DECLARATION_NOT_FOUND,
        /// <summary>
        /// 命名空间未找到
        /// </summary>
        COMPILING_NAMESPACE_NOT_FOUND,
        /// <summary>
        /// 声明不可见
        /// </summary>
        COMPILING_DECLARATION_NOT_VISIBLE,
        /// <summary>
        /// 意义不明确
        /// </summary>
        COMPILING_EQUIVOCAL,
        /// <summary>
        /// 无效的继承
        /// </summary>
        COMPILING_INVALID_INHERIT,
        /// <summary>
        /// 无效的类型定义
        /// </summary>
        COMPILING_INVALID_DEFINITION,
        /// <summary>
        /// 函数重复定义（函数名和参数类型列表相同）
        /// </summary>
        COMPILING_FUNCTION_DUPLICATE_DEFINITION,
        /// <summary>
        /// 元组类型不明确
        /// </summary>
        COMPILING_TUPLE_TYPE_EQUIVOCAL,
        #endregion 编译

        #region 生成
        /// <summary>
        /// 未知的逻辑生成错误
        /// </summary>
        GENERATOR_UNKNONW = 0x04_0000,
        /// <summary>
        /// 类型不匹配
        /// </summary>
        GENERATOR_TYPE_MISMATCH,
        /// <summary>
        /// 缺少必要参数
        /// </summary>
        GENERATOR_REQUIRED_PARAMETER_MISSING,
        /// <summary>
        /// 无效的操作
        /// </summary>
        GENERATOR_INVALID_OPERATION,
        /// <summary>
        /// 缺少返回值
        /// </summary>
        GENERATOR_MISSING_RETURN,
        /// <summary>
        /// 缺少表达式
        /// </summary>
        GENERATOR_MISSING_EXPRESSION,
        /// <summary>
        /// 函数未找到
        /// </summary>
        GENERATOR_FUNCTION_NOT_FOUND,
        /// <summary>
        /// 元组索引不是常量
        /// </summary>
        GENERATOR_TUPLE_INDEX_NOT_CONSTANT,
        /// <summary>
        /// 元组索引越界
        /// </summary>
        GENERATOR_TUPLE_INDEX_OUT_OF_RANGE,
        /// <summary>
        /// 不是成员方法
        /// </summary>
        GENERATOR_NOT_MEMBER_METHOD,
        /// <summary>
        /// 不是句柄类型的成员方法
        /// </summary>
        GENERATOR_NOT_HANDLE_MEMBER_METHOD,
        /// <summary>
        /// 不是个类型
        /// </summary>
        GENERATOR_NOT_TYPE,
        /// <summary>
        /// 常量值计算失败
        /// </summary>
        GENERATOR_CONSTANT_EVALUATION_FAIL,
        /// <summary>
        /// 未实现的功能
        /// </summary>
        GENERATOR_NOT_IMPLEMENTED,
        /// <summary>
        /// 内部函数不能用携程调用
        /// </summary>
        GENERATOR_NATIVE_COROUTINE,
        /// <summary>
        /// 除零
        /// </summary>
        GENERATOR_DIVIDE_BY_ZERO,
        #endregion
    }
    /// <summary>
    /// 编译异常
    /// </summary>
    public class CompilingException
    {
        /// <summary>
        /// 异常所在文件路径
        /// </summary>
        public readonly string path;
        /// <summary>
        /// 异常所在行
        /// </summary>
        public readonly int line;
        /// <summary>
        /// 异常发生片段
        /// </summary>
        public readonly int start, end;
        /// <summary>
        /// 异常代码
        /// </summary>
        public readonly CompilingExceptionCode code;
        /// <summary>
        /// 额外信息
        /// </summary>
        public readonly string message;
        internal CompilingException(string path, int line, int start, int end, CompilingExceptionCode code, string message)
        {
            this.path = path;
            this.line = line;
            this.start = start;
            this.end = end;
            this.code = code;
            this.message = message;
        }
    }
    /// <summary>
    /// 异常收集器
    /// </summary>
    public class ExceptionCollector
    {
        private readonly List<CompilingException> exceptions = new List<CompilingException>();
        /// <summary>
        /// 异常数量
        /// </summary>
        public int Count { get { return exceptions.Count; } }
        /// <summary>
        /// 异常
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public CompilingException this[int index] { get { return exceptions[index]; } }
        /// <summary>
        /// 遍历异常
        /// </summary>
        /// <returns></returns>
        public IEnumerator<CompilingException> GetEnumerator()
        {
            foreach (var exception in exceptions) yield return exception;
        }
        internal void Add(IList<Lexical> lexicals, CompilingExceptionCode code)
        {
            Add(lexicals, code, "");
        }
        internal void Add(IList<Lexical> lexicals, CompilingExceptionCode code, string message)
        {
            if (lexicals.Count > 0)
            {
                var lexical = lexicals[0];
                Add(new Anchor(lexical.anchor.textInfo, lexical.anchor.start, lexicals[-1].anchor.end), code, message);
            }
            else Add(code, message);
        }
        internal void Add(CompilingExceptionCode code, string message)
        {
            Add("", -1, 0, 0, code, message);
        }
        internal void Add(Anchor anchor, CompilingExceptionCode code)
        {
            Add(anchor, code, "");
        }
        internal void Add(Anchor anchor, CompilingExceptionCode code, string message)
        {
            Add(anchor.textInfo.path, anchor.textInfo.TryGetLineInfo(anchor.start, out var line) ? line.number : -1, anchor.Segment, code, message);
        }
        internal void Add(TextInfo text, int line, CompilingExceptionCode code)
        {
            Add(text, line, code, "");
        }
        internal void Add(TextInfo text, int line, CompilingExceptionCode code, string message)
        {
            Add(text.path, line, text[line].segment, code, message);
        }
        internal void Add(string path, int line, StringSegment segment, CompilingExceptionCode code, string message)
        {
            if (string.IsNullOrEmpty(message)) message = segment;
            Add(path, line, segment.start, segment.end, code, message);
        }
        internal void Add(string path, int line, int start, int end, CompilingExceptionCode code, string message)
        {
            exceptions.Add(new CompilingException(path, line, start, end, code, message));
        }
        internal void Clear()
        {
            exceptions.Clear();
        }
    }
}
