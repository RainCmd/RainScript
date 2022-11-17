namespace RainScript.Compiler.LogicGenerator
{
    enum TokenType
    {
        LogicAnd,               // &&
        LogicOr,                // ||

        Casting,                // 类型转换

        Less,                   // <
        Greater,                // >
        LessEquals,             // <=
        GreaterEquals,          // >=
        Equals,                 // ==
        NotEquals,              // !=

        BitAnd,                 // &
        BitOr,                  // |
        BitXor,                 // ^
        ShiftLeft,              // <<
        ShiftRight,             // >>

        Plus,                   // +
        Minus,                  // -

        Mul,                    // *
        Div,                    // /
        Mod,                    // %

        Not,                    // !
        Inverse,                // ~
        Positive,               // + 正号
        Negative,               // - 负号
        IncrementLeft,          // ++ 左自增(++X)
        DecrementLeft,          // -- 左自减(--X)
    }
    internal enum TokenPriority : byte
    {
        None,                       // 无
        LogicOperation,             // 逻辑运算
        Compare,                    // 比较运算
        BitOperation,               // 位运算
        ElementaryOperation,        // 初级运算
        IntermediateOperation,      // 中级运算
        AdvancedOperation,          // 高级运算
        Casting,                    // 类型转换
        SymbolicOperation,          // 符号运算
    }
    [System.Flags]
    internal enum TokenAttribute : uint
    {
        None = 0x0001,              //无
        Operator = 0x0002,          //运算符
        Value = 0x004,              //值
        Constant = 0x000C,          //常量
        Assignable = 0x0010,        //可赋值
        Callable = 0x0020,          //可调用
        Array = 0x0040,             //数组
        Tuple = 0x0080,             //元组
        Coroutine = 0x0100,         //携程
        Type = 0x0200,              //类型
        Method = 0x0400,            //方法
    }
    internal readonly struct Token
    {
        public readonly Lexical lexical;
        public readonly TokenType type;
        public readonly TokenPriority priority;
        public Token(Lexical lexical, TokenType type)
        {
            this.lexical = lexical;
            this.type = type;
            priority = type.Priority();
        }
    }
    internal static class TokenExtension
    {
        public static TokenPriority Priority(this TokenType type)
        {
            switch (type)
            {
                case TokenType.LogicAnd:
                case TokenType.LogicOr: return TokenPriority.LogicOperation;
                case TokenType.Less:
                case TokenType.Greater:
                case TokenType.LessEquals:
                case TokenType.GreaterEquals:
                case TokenType.Equals:
                case TokenType.NotEquals: return TokenPriority.Compare;
                case TokenType.BitAnd:
                case TokenType.BitOr:
                case TokenType.BitXor:
                case TokenType.ShiftLeft:
                case TokenType.ShiftRight: return TokenPriority.BitOperation;
                case TokenType.Plus:
                case TokenType.Minus: return TokenPriority.ElementaryOperation;
                case TokenType.Mul:
                case TokenType.Div:
                case TokenType.Mod: return TokenPriority.IntermediateOperation;
                case TokenType.Not:
                case TokenType.Inverse:
                case TokenType.Positive:
                case TokenType.Negative:
                case TokenType.IncrementLeft:
                case TokenType.DecrementLeft:
                case TokenType.Casting: return TokenPriority.SymbolicOperation;
                default: return TokenPriority.None;
            }
        }
        public static TokenAttribute Precondition(this TokenType type)
        {
            switch (type)
            {
                case TokenType.LogicAnd:
                case TokenType.LogicOr: return TokenAttribute.Value;
                case TokenType.Less:
                case TokenType.Greater:
                case TokenType.LessEquals:
                case TokenType.GreaterEquals:
                case TokenType.Equals:
                case TokenType.NotEquals:
                case TokenType.BitAnd:
                case TokenType.BitOr:
                case TokenType.BitXor:
                case TokenType.ShiftLeft:
                case TokenType.ShiftRight:
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Mul:
                case TokenType.Div:
                case TokenType.Mod: return TokenAttribute.Value;
                case TokenType.Not:
                case TokenType.Inverse:
                case TokenType.Positive:
                case TokenType.Negative:
                case TokenType.IncrementLeft:
                case TokenType.DecrementLeft: return TokenAttribute.None | TokenAttribute.Operator;
                case TokenType.Casting: return TokenAttribute.Type;
                default: return 0;
            }
        }
        public static bool ContainAny(this TokenAttribute attribute, TokenAttribute other)
        {
            return (attribute & other) > 0;
        }
        public static bool ContainAll(this TokenAttribute attribute, TokenAttribute other)
        {
            return (attribute & other) == other;
        }
        public static TokenAttribute AddTypeAttribute(this TokenAttribute attribute, CompilingType type)
        {
            if (type.dimension > 0 || type == RelyKernel.STRING_TYPE || type == RelyKernel.ARRAY_TYPE) attribute |= TokenAttribute.Array;
            else if (type.definition.code == TypeCode.Function) attribute |= TokenAttribute.Callable;
            else if (type.definition.code == TypeCode.Coroutine) attribute |= TokenAttribute.Coroutine;
            return attribute;
        }
    }
}
