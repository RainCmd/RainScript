namespace RainScript
{
    internal struct StringSegment
    {
        public readonly string value;
        public readonly int start, end;//[,]
        public char this[int index]
        {
            get
            {
                if (index < 0) index += Length;
                if (index < 0 || index >= Length) throw ExceptionGenerator.CharIndexOutOfRangeException();
                if (start < end) return value[start + index];
                else return value[start - index];
            }
        }
        public StringSegment this[int start, int end]
        {
            get
            {
                var length = Length;
                if (start < 0) start += length;
                if (end < 0) end += length;
                if (start < 0 || start >= length) throw ExceptionGenerator.CharIndexOutOfRangeException();
                if (end < 0 || end >= length) throw ExceptionGenerator.CharIndexOutOfRangeException();
                if (this.start < this.end)
                {
                    start += this.start;
                    end += this.start;
                }
                else
                {
                    start = this.start - start;
                    end = this.start - end;
                }
                return new StringSegment(value, start, end);
            }
        }
        public int Length { get { return System.Math.Abs(end - start) + 1; } }
        public StringSegment(string value) : this(value, 0, -1) { }
        public StringSegment(string value, int index) : this(value, index, index) { }
        public StringSegment(string value, int start, int end)
        {
            this.value = value;
            if (start < 0) start += value.Length;
            if (end < 0) end += value.Length;
            if (start < 0 || start >= value.Length) throw ExceptionGenerator.CharIndexOutOfRangeException();
            if (end < 0 || end >= value.Length) throw ExceptionGenerator.CharIndexOutOfRangeException();
            this.start = start;
            this.end = end;
        }
        public bool Equals(char c, int index)
        {
            if (index < 0 || index >= Length) return false;
            else return c == this[index];
        }
        public override bool Equals(object obj)
        {
            if (obj is StringSegment ss) return ss == this;
            else return false;
        }
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        public override string ToString()
        {
            return value.Substring(start, Length);
        }
        public static bool operator ==(StringSegment left, StringSegment right)
        {
            if (left.Length != right.Length) return false;
            var length = left.Length;
            for (int i = 0; i < length; i++) if (left[i] != right[i]) return false;
            return true;
        }
        public static bool operator !=(StringSegment left, StringSegment right)
        {
            return !(left == right);
        }
        public static bool operator ==(StringSegment left, string right)
        {
            if (left.Length != right.Length) return false;
            for (int i = 0; i < right.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }
        public static bool operator !=(StringSegment left, string right)
        {
            return !(left == right);
        }
        public static implicit operator string(StringSegment segment)
        {
            return segment.ToString();
        }
        public static implicit operator StringSegment(string value)
        {
            return new StringSegment(value);
        }
        public static implicit operator bool(StringSegment segment)
        {
            return string.IsNullOrEmpty(segment.value);
        }
    }
    internal static class KeyWorld
    {
        public const string NAMESPACE = "namespace";
        public const string IMPORT = "import";//导入库
        public const string NATIVE = "native";//IPerformer实现的函数
        public const string PUBLIC = "public";
        public const string INTERNAL = "internal";
        public const string SPACE = "space";
        public const string PROTECTED = "protected";
        public const string PRIVATE = "private";
        public const string STRUCT = "struct";//栈对象暂未实现，保留关键字
        public const string CLASS = "class";
        public const string INTERFACE = "interface";
        public const string NEW = "new";//堆对象和栈对象用class和struct来区别，就不用new了，但是暂时保留
        public const string CONST = "const";

        public const string KERNEL = "kernel";
        public const string GLOBAL = "global";
        public const string BASE = "base";
        public const string THIS = "this";
        public const string TRUE = "true";
        public const string FALSE = "false";
        public const string NULL = "null";
        public const string VAR = "var";
        public const string BOOL = "bool";
        public const string INTEGER = "int";
        public const string REAL = "real";
        public const string REAL2 = "real2";
        public const string REAL3 = "real3";
        public const string REAL4 = "real4";
        public const string STRING = "string";
        public const string HANDLE = "handle";
        public const string ENTITY = "entity";
        public const string FUNCTION = "function";
        public const string COROUTINE = "coroutine";
        public const string ARRAY = "array";

        public const string IF = "if";
        public const string ELIF = "elif";
        public const string ELSE = "else";
        public const string WHILE = "while";
        public const string FOR = "for";
        public const string BREAK = "break";
        public const string CONTINUE = "continue";
        public const string RETURN = "return";
        public const string IS = "is";
        public const string AS = "as";
        public const string START = "start";//开启新的协程
        public const string WAIT = "wait";
        public const string EXIT = "exit";
        //携程+ExitCode可以实现该功能，所以这个先不实现，但保留关键字
        public const string TRY = "try";
        public const string CATCH = "catch";
        public const string FINALLY = "finally";

        public static bool IsKeyWorld(StringSegment segment)
        {
            if (segment == NAMESPACE) return true;
            if (segment == IMPORT) return true;
            if (segment == NATIVE) return true;
            if (segment == PUBLIC) return true;
            if (segment == INTERNAL) return true;
            if (segment == SPACE) return true;
            if (segment == PROTECTED) return true;
            if (segment == PRIVATE) return true;
            if (segment == STRUCT) return true;
            if (segment == CLASS) return true;
            if (segment == INTERFACE) return true;
            if (segment == NEW) return true;
            if (segment == CONST) return true;

            if (segment == KERNEL) return true;
            if (segment == GLOBAL) return true;
            if (segment == BASE) return true;
            if (segment == THIS) return true;
            if (segment == TRUE) return true;
            if (segment == FALSE) return true;
            if (segment == NULL) return true;
            if (segment == VAR) return true;
            if (segment == BOOL) return true;
            if (segment == INTEGER) return true;
            if (segment == REAL) return true;
            if (segment == REAL2) return true;
            if (segment == REAL3) return true;
            if (segment == REAL4) return true;
            if (segment == STRING) return true;
            if (segment == HANDLE) return true;
            if (segment == ENTITY) return true;
            if (segment == FUNCTION) return true;
            if (segment == COROUTINE) return true;
            if (segment == ARRAY) return true;

            if (segment == IF) return true;
            if (segment == ELIF) return true;
            if (segment == ELSE) return true;
            if (segment == WHILE) return true;
            if (segment == FOR) return true;
            if (segment == BREAK) return true;
            if (segment == CONTINUE) return true;
            if (segment == RETURN) return true;
            if (segment == IS) return true;
            if (segment == AS) return true;
            if (segment == START) return true;
            if (segment == WAIT) return true;
            if (segment == EXIT) return true;
            if (segment == TRY) return true;
            if (segment == CATCH) return true;
            if (segment == FINALLY) return true;

            return false;
        }
    }
}
