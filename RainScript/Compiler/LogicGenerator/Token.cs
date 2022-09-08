namespace RainScript.Compiler.LogicGenerator
{
    enum TokenType
    {
        Less,                   // <
        Greater,                // >
        LessEquals,             // <=
        GreaterEquals,          // >=
        Equals,                 // ==
        NotEquals,              // !=

        LogicAnd,               // &&
        LogicOr,                // ||

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
        IncrementRight,         // ++ 右自增(X++)
        DecrementLeft,          // -- 左自减(--X)
        DecrementRight,         // -- 右自减(X--)
    }
    internal enum TokenPriority : byte
    {
        None,                       // 无
        Compare,                    // 比较运算
        LogicOperation,             // 逻辑运算
        BitOperation,               // 位运算
        ElementaryOperation,        // 初级运算
        IntermediateOperation,      // 中级运算
        AdvancedOperation,          // 高级运算
        SymbolicOperation,          // 符号运算
    }
    [System.Flags]
    internal enum TokenAttribute : uint
    {
        None = 0x0001,              //无
        Operator = 0x0002,          //运算符
        Temporary = 0x004,          //临时对象
        Constant = 0x000C,          //常量
        Variable = 0x0014,          //变量
        Function = 0x0020,          //函数地址
        Array = 0x0040,             //数组
        Tuple = 0x0080,             //元组
        Coroutine = 0x0100,         //携程
        Cast = 0x0200,              //类型转换
        Type = 0x0400,              //类型
        Method = 0x0800,            //方法
    }
    internal struct Token
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
                case TokenType.Less:
                case TokenType.Greater:
                case TokenType.LessEquals:
                case TokenType.GreaterEquals:
                case TokenType.Equals:
                case TokenType.NotEquals: return TokenPriority.Compare;
                case TokenType.LogicAnd:
                case TokenType.LogicOr: return TokenPriority.LogicOperation;
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
                case TokenType.IncrementRight:
                case TokenType.DecrementLeft:
                case TokenType.DecrementRight: return TokenPriority.SymbolicOperation;
                default: return TokenPriority.None;
            }
        }
        public static int ParameterCount(this TokenType type)
        {
            switch (type)
            {
                case TokenType.Less:
                case TokenType.Greater:
                case TokenType.LessEquals:
                case TokenType.GreaterEquals:
                case TokenType.Equals:
                case TokenType.NotEquals:
                case TokenType.LogicAnd:
                case TokenType.LogicOr:
                case TokenType.BitAnd:
                case TokenType.BitOr:
                case TokenType.BitXor:
                case TokenType.ShiftLeft:
                case TokenType.ShiftRight:
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Mul:
                case TokenType.Div:
                case TokenType.Mod: return 2;
                case TokenType.Not:
                case TokenType.Inverse:
                case TokenType.Positive:
                case TokenType.Negative:
                case TokenType.IncrementLeft:
                case TokenType.IncrementRight:
                case TokenType.DecrementLeft:
                case TokenType.DecrementRight: return 1;
            }
            return -1;
        }
        public static TokenAttribute Precondition(this TokenType type)
        {
            const TokenAttribute LEFT_VALUE = TokenAttribute.Variable;
            const TokenAttribute RIGHT_VALUE = TokenAttribute.Constant | TokenAttribute.Variable | TokenAttribute.Temporary;
            const TokenAttribute NOT_VALUE = TokenAttribute.None | TokenAttribute.Operator;
            switch (type)
            {
                case TokenType.Less:
                case TokenType.Greater:
                case TokenType.LessEquals:
                case TokenType.GreaterEquals:
                case TokenType.Equals:
                case TokenType.NotEquals:
                case TokenType.LogicAnd:
                case TokenType.LogicOr:
                case TokenType.BitAnd:
                case TokenType.BitOr:
                case TokenType.BitXor:
                case TokenType.ShiftLeft:
                case TokenType.ShiftRight:
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Mul:
                case TokenType.Div:
                case TokenType.Mod: return RIGHT_VALUE;
                case TokenType.Not:
                case TokenType.Inverse:
                case TokenType.Positive:
                case TokenType.Negative:
                case TokenType.IncrementLeft: return NOT_VALUE;
                case TokenType.IncrementRight: return LEFT_VALUE;
                case TokenType.DecrementLeft: return NOT_VALUE;
                case TokenType.DecrementRight: return LEFT_VALUE;
                default: return 0;
            }
        }
        public static bool ContainAny(this TokenAttribute attribute, TokenAttribute other)
        {
            return (attribute & other) > 0;
        }
        public static TokenAttribute AddTypeAttribute(this TokenAttribute attribute, CompilingType type)
        {
            if (type.dimension > 0 || type == RelyKernel.STRING_TYPE || type == RelyKernel.ARRAY_TYPE) attribute |= TokenAttribute.Array;
            else if (type.definition.code == TypeCode.Function) attribute |= TokenAttribute.Function;
            else if (type.definition.code == TypeCode.Coroutine) attribute |= TokenAttribute.Coroutine;
            return attribute;
        }
    }
}
