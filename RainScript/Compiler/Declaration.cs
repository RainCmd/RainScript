namespace RainScript.Compiler
{
    internal enum DeclarationCode
    {
        //                    library     index               overrideIndex               definitionIndex   
        Invalid,              //-         -                   -                           -
        Definition,           //库        定义索引            -                           -
        MemberVariable,       //库        成员变量索引        -                           所属定义索引
        MemberMethod,         //库        成员方法索引        -                           所属定义索引
        MemberFunction,       //库        成员方法索引        重载索引                    所属定义索引
        Constructor,          //库        方法列表索引        -                           所属定义索引            
        ConstructorFunction,  //库        方法列表索引        重载索引                    所属定义索引
        Delegate,             //库        委托类型索引        -                           -
        Coroutine,            //库        携程类型索引        -                           -
        Interface,            //库        接口索引            -                           -
        InterfaceMethod,      //库        接口方法索引        -                           所属接口索引            
        InterfaceFunction,    //库        接口方法索引        重载索引                    所属接口索引            
        GlobalVariable,       //库        变量索引            -                           -            
        GlobalMethod,         //库        方法列表索引        -                           -
        GlobalFunction,       //库        方法列表索引        重载索引                    -
        NativeMethod,         //库        内部方法索引        -                           -       
        NativeFunction,       //库        内部方法索引        重载索引                    -       
        Lambda,               //-         方法列表索引        -                           匿名函数索引
        LocalVariable,        //-         局部变量id          -                           -
    }
    internal struct Declaration
    {
        public readonly uint library;
        public readonly Visibility visibility;
        public readonly DeclarationCode code;
        public readonly uint index;
        public readonly uint overrideIndex;
        public readonly uint definitionIndex;

        public Declaration(uint library, Visibility visibility, DeclarationCode code, uint index, uint overrideIndex, uint definitionIndex)
        {
            this.library = library;
            this.visibility = visibility;
            this.code = code;
            this.index = index;
            this.overrideIndex = overrideIndex;
            this.definitionIndex = definitionIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is Declaration declaration &&
                   library == declaration.library &&
                   visibility == declaration.visibility &&
                   code == declaration.code &&
                   index == declaration.index &&
                   overrideIndex == declaration.overrideIndex &&
                   definitionIndex == declaration.definitionIndex;
        }
        public override int GetHashCode()
        {
            int hashCode = 1533665627;
            hashCode = hashCode * -1521134295 + library.GetHashCode();
            hashCode = hashCode * -1521134295 + visibility.GetHashCode();
            hashCode = hashCode * -1521134295 + code.GetHashCode();
            hashCode = hashCode * -1521134295 + index.GetHashCode();
            hashCode = hashCode * -1521134295 + overrideIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + definitionIndex.GetHashCode();
            return hashCode;
        }
        public static bool operator ==(Declaration a, Declaration b)
        {
            return a.library == b.library && a.visibility == b.visibility && a.code == b.code && a.index == b.index && a.overrideIndex == b.overrideIndex && a.definitionIndex == b.definitionIndex;
        }
        public static bool operator !=(Declaration a, Declaration b)
        {
            return !(a == b);
        }
        public static implicit operator bool(Declaration declaration)
        {
            return declaration.library != LIBRARY.INVALID && declaration.code != DeclarationCode.Invalid;
        }
        public static Declaration INVALID = new Declaration(LIBRARY.INVALID, Visibility.None, DeclarationCode.Invalid, 0, 0, 0);
    }
    internal struct CompilingDefinition
    {
        public readonly uint library;
        public readonly Visibility visibility;
        public readonly TypeCode code;
        public readonly uint index;
        public Declaration Declaration
        {
            get
            {
                if (library == LIBRARY.KERNEL) return new Declaration(library, visibility, DeclarationCode.Definition, index, 0, 0);
                switch (code)
                {
                    case TypeCode.Handle: return new Declaration(library, visibility, DeclarationCode.Definition, index, 0, 0);
                    case TypeCode.Function: return new Declaration(library, visibility, DeclarationCode.Delegate, index, 0, 0);
                    case TypeCode.Interface: return new Declaration(library, visibility, DeclarationCode.Interface, index, 0, 0);
                    case TypeCode.Coroutine: return new Declaration(library, visibility, DeclarationCode.Coroutine, index, 0, 0);
                }
                throw ExceptionGeneratorCompiler.Unknown();
            }
        }
        public TypeDefinition RuntimeDefinition
        {
            get
            {
                return new TypeDefinition(library, code, index);
            }
        }
        public CompilingDefinition(Declaration declaration)
        {
            library = declaration.library;
            visibility = declaration.visibility;
            index = declaration.index;
            if (declaration.code == DeclarationCode.Definition)
            {
                if (declaration.library == LIBRARY.KERNEL) code = (TypeCode)declaration.index;
                else code = TypeCode.Handle;
            }
            else if (declaration.code == DeclarationCode.Delegate) code = TypeCode.Function;
            else if (declaration.code == DeclarationCode.Coroutine) code = TypeCode.Coroutine;
            else if (declaration.code == DeclarationCode.Interface) code = TypeCode.Interface;
            else
            {
                library = LIBRARY.INVALID;
                visibility = Visibility.None;
                code = TypeCode.Invalid;
                index = 0;
            }
        }
        public CompilingDefinition(TypeDefinition definition, Visibility visibility) : this(definition.library, visibility, definition.code, definition.index) { }
        public CompilingDefinition(uint library, Visibility visibility, TypeCode code, uint index)
        {
            this.library = library;
            this.visibility = visibility;
            this.code = code;
            this.index = index;
        }
        public override bool Equals(object obj)
        {
            return obj is CompilingDefinition definition &&
                   library == definition.library &&
                   code == definition.code &&
                   index == definition.index;
        }
        public override int GetHashCode()
        {
            int hashCode = 1876850884;
            hashCode = hashCode * -1521134295 + library.GetHashCode();
            hashCode = hashCode * -1521134295 + code.GetHashCode();
            hashCode = hashCode * -1521134295 + index.GetHashCode();
            return hashCode;
        }
        public static bool operator ==(CompilingDefinition a, CompilingDefinition b)
        {
            return a.library == b.library && a.code == b.code && a.index == b.index;
        }
        public static bool operator !=(CompilingDefinition a, CompilingDefinition b)
        {
            return !(a == b);
        }
        public static explicit operator bool(CompilingDefinition definition)
        {
            return definition.library != LIBRARY.INVALID && definition.code != TypeCode.Invalid;
        }
        public static readonly CompilingDefinition INVALID = new CompilingDefinition(LIBRARY.INVALID, Visibility.None, TypeCode.Invalid, 0);
    }
    internal struct CompilingType
    {
        public readonly CompilingDefinition definition;
        public readonly uint dimension;
        public uint FieldSize 
        {
            get
            {
                if (dimension > 0) return TypeCode.Handle.FieldSize();
                else return definition.code.FieldSize();
            } 
        }
        public bool IsHandle
        {
            get
            {
                return dimension > 0 || definition.code == TypeCode.Handle || definition.code == TypeCode.Interface || definition.code == TypeCode.Function || definition.code == TypeCode.Coroutine;
            }
        }
        public Type RuntimeType
        {
            get
            {
                return new Type(definition.library, definition.code, definition.index, dimension);
            }
        }
        public CompilingType(Type type, Visibility visibility) : this(new CompilingDefinition(type.definition, visibility), type.dimension) { }
        public CompilingType(CompilingDefinition definition, uint dimension)
        {
            this.definition = definition;
            this.dimension = dimension;
        }
        public CompilingType(uint library, Visibility visibility, TypeCode code, uint index, uint dimension) : this(new CompilingDefinition(library, visibility, code, index), dimension) { }
        public override bool Equals(object obj)
        {
            return obj is CompilingType type &&
                   definition == type.definition &&
                   dimension == type.dimension;
        }
        public override int GetHashCode()
        {
            int hashCode = -317504682;
            hashCode = hashCode * -1521134295 + definition.GetHashCode();
            hashCode = hashCode * -1521134295 + dimension.GetHashCode();
            return hashCode;
        }
        public static bool operator ==(CompilingType a, CompilingType b)
        {
            return a.definition == b.definition && a.dimension == b.dimension;
        }
        public static bool operator !=(CompilingType a, CompilingType b)
        {
            return !(a == b);
        }
        public static explicit operator bool(CompilingType type)
        {
            return (bool)type.definition;
        }
        public static bool IsEquals(CompilingType[] lhs, CompilingType[]rhs)
        {
            if (lhs.Length != rhs.Length) return false;
            for (int i = 0; i < lhs.Length; i++)
                if (lhs[i] != rhs[i]) return false;
            return true;
        }
        public static readonly CompilingType INVALID = new CompilingType(CompilingDefinition.INVALID, 0);
    }
}
