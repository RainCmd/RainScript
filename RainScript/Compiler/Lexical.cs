namespace RainScript.Compiler
{
    internal enum LexicalType
    {
        Unknow,

        BracketLeft0,           // (
        BracketLeft1,           // [
        BracketLeft2,           // {
        BracketRight0,          // )
        BracketRight1,          // ]
        BracketRight2,          // }
        Comma,                  // ,
        Assignment,             // =
        Equals,                 // ==
        Lambda,                 // =>
        BitAnd,                 // &
        LogicAnd,               // &&
        BitAndAssignment,       // &=
        BitOr,                  // |
        LogicOr,                // ||
        BitOrAssignment,        // |=
        BitXor,                 // ^
        BitXorAssignment,       // ^=
        Less,                   // <
        LessEquals,             // <=
        ShiftLeft,              // <<
        ShiftLeftAssignment,    // <<=
        Greater,                // >
        GreaterEquals,          // >=
        ShiftRight,             // >>
        ShiftRightAssignment,   // >>=
        Plus,                   // +
        Increment,              // ++
        PlusAssignment,         // +=
        Minus,                  // -
        Decrement,              // --
        RealInvoker,            // ->
        MinusAssignment,        // -=
        Mul,                    // *
        MulAssignment,          // *=
        Div,                    // /
        DivAssignment,          // /=
        Annotation,             // 注释
        Mod,                    // %
        ModAssignment,          // %=
        Not,                    // !
        NotEquals,              // !=
        Negate,                 // ~
        Dot,                    // .
        Question,               // ?
        QuestionDot,            // ?.
        QuestionInvoke,         // ?(
        Colon,                  // :
        ConstReal,              // 数字(实数)
        ConstNumber,            // 数字(整数)
        ConstBinary,            // 数字(二进制)
        ConstHexadecimal,       // 数字(十六进制)
        ConstChars,             // 数字(单引号字符串)
        ConstString,            // 字符串
        Word,                   // 单词
        Backslash,              // 反斜杠\
    }
    internal struct Lexical
    {
        public readonly LexicalType type;
        public readonly Anchor anchor;
        public Lexical(LexicalType type, TextInfo text, StringSegment segment) : this(type, new Anchor(text, segment)) { }
        public Lexical(LexicalType type, Anchor anchor)
        {
            this.type = type;
            this.anchor = anchor;
        }
        public override string ToString()
        {
            return "{0}    {1}    ".Format(type.ToString().PadRight(24), anchor.Segment);
        }
        public static bool TryAnalysis(ScopeList<Lexical> list, TextInfo text, StringSegment segment, ExceptionCollector exceptions)
        {
            var exceptionCount = exceptions.Count;
            for (int index = 0; TryAnalysisFirst(text, segment, index, out var lexical, exceptions); index = lexical.anchor.end - segment.start + 1)
                if (lexical.type == LexicalType.Backslash)
                {
                    index = lexical.anchor.end - segment.start + 1;
                    if (!TryAnalysisFirst(text, segment, index, out var nextLexical, exceptions)) break;
                    if (nextLexical.type != LexicalType.Annotation && nextLexical.type != LexicalType.Unknow)
                    {
                        for (int i = index, end = nextLexical.anchor.start - segment.start; i < end; i++)
                            if (segment[i] == '\n') goto next;
                        list.Add(lexical);
                    next:;
                        list.Add(nextLexical);
                    }
                }
                else if (lexical.type != LexicalType.Annotation && lexical.type != LexicalType.Unknow) list.Add(lexical);
            return exceptionCount == exceptions.Count;
        }
        public static bool TryAnalysisFirst(TextInfo text, StringSegment segment, int index, out Lexical lexical, ExceptionCollector exceptions)
        {
            while (index < segment.Length && char.IsWhiteSpace(segment[index])) index++;

            if (index < segment.Length)
                switch (segment[index])
                {
                    case '(':
                        lexical = new Lexical(LexicalType.BracketLeft0, text, segment[index, index]);
                        return true;
                    case '[':
                        lexical = new Lexical(LexicalType.BracketLeft1, text, segment[index, index]);
                        return true;
                    case '{':
                        lexical = new Lexical(LexicalType.BracketLeft2, text, segment[index, index]);
                        return true;
                    case ')':
                        lexical = new Lexical(LexicalType.BracketRight0, text, segment[index, index]);
                        return true;
                    case ']':
                        lexical = new Lexical(LexicalType.BracketRight1, text, segment[index, index]);
                        return true;
                    case '}':
                        lexical = new Lexical(LexicalType.BracketRight2, text, segment[index, index]);
                        return true;
                    case ',':
                        lexical = new Lexical(LexicalType.Comma, text, segment[index, index]);
                        return true;
                    case '=':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.Equals, text, segment[index, index + 1]);
                        else if (segment.Equals('>', index + 1)) lexical = new Lexical(LexicalType.Lambda, text, segment[index, index + 1]);
                        else lexical = new Lexical(LexicalType.Assignment, text, segment[index, index]);
                        return true;
                    case '&':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.BitAndAssignment, text, segment[index, index + 1]);
                        else if (segment.Equals('&', index + 1)) lexical = new Lexical(LexicalType.LogicAnd, text, segment[index, index + 1]);
                        else lexical = new Lexical(LexicalType.BitAnd, text, segment[index, index]);
                        return true;
                    case '|':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.BitOrAssignment, text, segment[index, index + 1]);
                        else if (segment.Equals('|', index + 1)) lexical = new Lexical(LexicalType.LogicOr, text, segment[index, index + 1]);
                        else lexical = new Lexical(LexicalType.BitOr, text, segment[index, index]);
                        return true;
                    case '^':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.BitXorAssignment, text, segment[index, index + 1]);
                        else lexical = new Lexical(LexicalType.BitXor, text, segment[index, index]);
                        return true;
                    case '<':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.LessEquals, text, segment[index, index + 1]);
                        else if (segment.Equals('<', index + 1))
                        {
                            if (segment.Equals('=', index + 2)) lexical = new Lexical(LexicalType.ShiftLeftAssignment, text, segment[index, index + 2]);
                            else lexical = new Lexical(LexicalType.ShiftLeft, text, segment[index, index + 1]);
                        }
                        else lexical = new Lexical(LexicalType.Less, text, segment[index, index]);
                        return true;
                    case '>':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.GreaterEquals, text, segment[index, index + 1]);
                        else if (segment.Equals('>', index + 1))
                        {
                            if (segment.Equals('=', index + 2)) lexical = new Lexical(LexicalType.ShiftRightAssignment, text, segment[index, index + 2]);
                            else lexical = new Lexical(LexicalType.ShiftRight, text, segment[index, index + 1]);
                        }
                        else lexical = new Lexical(LexicalType.Greater, text, segment[index, index]);
                        return true;
                    case '+':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.PlusAssignment, text, segment[index, index + 1]);
                        else if (segment.Equals('+', index + 1)) lexical = new Lexical(LexicalType.Increment, text, segment[index, index + 1]);
                        else lexical = new Lexical(LexicalType.Plus, text, segment[index, index]);
                        return true;
                    case '-':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.MinusAssignment, text, segment[index, index + 1]);
                        else if (segment.Equals('-', index + 1)) lexical = new Lexical(LexicalType.Decrement, text, segment[index, index + 1]);
                        else if (segment.Equals('>', index + 1)) lexical = new Lexical(LexicalType.RealInvoker, text, segment[index, index + 1]);
                        else lexical = new Lexical(LexicalType.Minus, text, segment[index, index]);
                        return true;
                    case '*':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.MulAssignment, text, segment[index, index + 1]);
                        else lexical = new Lexical(LexicalType.Mul, text, segment[index, index]);
                        return true;
                    case '/':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.DivAssignment, text, segment[index, index + 1]);
                        else if (segment.Equals('/', index + 1))
                        {
                            var i = index + 2;
                            while (i < segment.Length && segment[i] != '\n') i++;
                            lexical = new Lexical(LexicalType.Annotation, text, segment[index, index - 1]);
                        }
                        else lexical = new Lexical(LexicalType.Div, text, segment[index, index]);
                        return true;
                    case '%':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.ModAssignment, text, segment[index, index + 1]);
                        else lexical = new Lexical(LexicalType.Mod, text, segment[index, index]);
                        return true;
                    case '!':
                        if (segment.Equals('=', index + 1)) lexical = new Lexical(LexicalType.NotEquals, text, segment[index, index + 1]);
                        else lexical = new Lexical(LexicalType.Not, text, segment[index, index]);
                        return true;
                    case '~':
                        lexical = new Lexical(LexicalType.Negate, text, segment[index, index]);
                        return true;
                    case '.':
                        if (index + 1 < segment.Length && char.IsDigit(segment[index + 1]))
                        {
                            var i = index + 2;
                            while (i < segment.Length && char.IsDigit(segment[i])) i++;
                            lexical = new Lexical(LexicalType.ConstReal, text, segment[index, i - 1]);
                        }
                        else lexical = new Lexical(LexicalType.Dot, text, segment[index, index]);
                        return true;
                    case '?':
                        if (segment.Equals('.', index + 1)) lexical = new Lexical(LexicalType.QuestionDot, text, segment[index, index + 1]);
                        else if (segment.Equals('(', index + 1)) lexical = new Lexical(LexicalType.QuestionInvoke, text, segment[index, index + 1]);
                        else lexical = new Lexical(LexicalType.Question, text, segment[index, index]);
                        return true;
                    case ':':
                        lexical = new Lexical(LexicalType.Colon, text, segment[index, index]);
                        return true;
                    case '\'':
                        {
                            var i = index + 1;
                            while (i < segment.Length)
                            {
                                if (segment[i] == '\'')
                                {
                                    lexical = new Lexical(LexicalType.ConstChars, text, segment[index, i]);
                                    return true;
                                }
                                else if (segment[i] == '\\')
                                {
                                    i++;
                                    if (i >= segment.Length || segment[i] == '\n') break;
                                }
                                else if (segment[i] == '\n') break;
                                i++;
                            }
                            lexical = new Lexical(LexicalType.ConstChars, text, segment[index, i - 1]);
                            exceptions?.Add(lexical.anchor, CompilingExceptionCode.LEXICAL_MISSING_PAIRED_SYMBOL);
                            return true;
                        }
                    case '\"':
                        {
                            var i = index + 1;
                            while (i < segment.Length)
                            {
                                if (segment[i] == '\"')
                                {
                                    lexical = new Lexical(LexicalType.ConstString, text, segment[index, i]);
                                    return true;
                                }
                                else if (segment[i] == '\\')
                                {
                                    i++;
                                    if (i >= segment.Length || segment[i] == '\n') break;
                                }
                                else if (segment[i] == '\n') break;
                                i++;
                            }
                            lexical = new Lexical(LexicalType.ConstString, text, segment[index, i - 1]);
                            exceptions?.Add(lexical.anchor, CompilingExceptionCode.LEXICAL_MISSING_PAIRED_SYMBOL);
                            return true;
                        }
                    case '\\':
                        lexical = new Lexical(LexicalType.Backslash, text, segment[index, index]);
                        return true;
                    default:
                        if (char.IsDigit(segment[index]))
                        {
                            if (segment[index] == '0')
                            {
                                if (segment.Equals('b', index + 1) || segment.Equals('B', index + 1))
                                {
                                    var i = index + 2;
                                    while (i < segment.Length && (segment[i] == '0' || segment[i] == '1' || segment[i] == '_')) i++;
                                    lexical = new Lexical(LexicalType.ConstBinary, text, segment[index, i - 1]);
                                    return true;
                                }
                                else if (segment.Equals('x', index + 1) || segment.Equals('X', index + 1))
                                {
                                    var i = index + 2;
                                    while (i < segment.Length)
                                    {
                                        var c = segment[i];
                                        if (char.IsDigit(c) || c == '_') i++;
                                        else
                                        {
                                            c |= (char)0x20u;
                                            if (c >= 'a' && c <= 'f') i++;
                                            else break;
                                        }
                                    }
                                    lexical = new Lexical(LexicalType.ConstHexadecimal, text, segment[index, i - 1]);
                                    return true;
                                }
                            }
                            var dot = false;
                            var idx = index + 1;
                            while (idx < segment.Length)
                            {
                                if (char.IsDigit(segment[idx]) || segment[idx] == '_') idx++;
                                else if (segment[idx] == '.')
                                {
                                    if (dot)
                                    {
                                        lexical = new Lexical(LexicalType.ConstReal, text, segment[index, idx - 1]);
                                        return true;
                                    }
                                    else if (idx + 1 < segment.Length && char.IsDigit(segment[idx + 1])) dot = true;
                                    else
                                    {
                                        lexical = new Lexical(LexicalType.ConstNumber, text, segment[index, idx - 1]);
                                        return true;
                                    }
                                    idx++;
                                }
                                else break;
                            }
                            lexical = new Lexical(dot ? LexicalType.ConstReal : LexicalType.ConstNumber, text, segment[index, idx - 1]);
                            return true;
                        }
                        else if (char.IsLetter(segment[index]))
                        {
                            var i = index + 1;
                            while (i < segment.Length && (char.IsLetterOrDigit(segment[i]) || segment[i] == '_')) i++;
                            lexical = new Lexical(LexicalType.Word, text, segment[index, i - 1]);
                            return true;
                        }
                        else
                        {
                            var i = index + 1;
                            while (i < segment.Length && !char.IsWhiteSpace(segment[i])) i++;
                            lexical = new Lexical(LexicalType.Unknow, text, segment[index, i - 1]);
                            exceptions?.Add(lexical.anchor, CompilingExceptionCode.LEXICAL_UNKNOWN);
                            return true;
                        }
                }
            lexical = default;
            return false;
        }
        public static bool TryExtractName(ListSegment<Lexical> lexicals, int start, out int index, out ScopeList<Lexical> name, CollectionPool pool)
        {
            name = pool.GetList<Lexical>();
            index = start;
            while (index < lexicals.Count)
            {
                if (lexicals[index].type == LexicalType.Word) name.Add(lexicals[index]);
                else break;
                if (index + 1 >= lexicals.Count) return true;
                else if (lexicals[index + 1].type == LexicalType.Dot) index += 2;
                else return true;
            }
            name.Dispose();
            return false;
        }
        public static uint ExtractDimension(ListSegment<Lexical> lexicals, ref int index)
        {
            var dimension = 0u;
            while (index + 2 < lexicals.Count)
                if (lexicals[index + 1].type == LexicalType.BracketLeft1 && lexicals[index + 2].type == LexicalType.BracketRight1)
                {
                    dimension++;
                    index += 2;
                }
                else break;
            return dimension;
        }
    }
}
