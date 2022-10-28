using System.Collections.Generic;

namespace RainLanguageServer
{
    internal enum Visibility
    {
        None = 0,
        Public = 0x1,
        Internal = 0x2,
        Space = 0x4,
        Protected = 0x8,
        Private = 0x10,
    }
    internal class Declaration
    {
        public Visibility visibility;
        public Space space;
        public DocumentDeclrartion document;
        public readonly List<DocumentToken> references = new List<DocumentToken>();
    }
    internal class VariableDeclaration : Declaration
    {
        public DocumentToken type;
    }
    internal class Variable : VariableDeclaration
    {
        //todo 变量初始化表达式
    }
    internal class FunctionDeclaration : Declaration
    {
        public readonly List<VariableDeclaration> parameters = new List<VariableDeclaration>();
        public readonly List<DocumentToken> returns = new List<DocumentToken>();
    }
    internal class Function : FunctionDeclaration
    {
        //todo 函数体
    }
    internal class Method
    {
        public readonly List<Function> functions = new List<Function>();
    }
    internal class Definition : Declaration
    {
        public readonly List<DocumentToken> inherts = new List<DocumentToken>();
        public readonly List<VariableDeclaration> variables = new List<VariableDeclaration>();
        public readonly List<Method> methods = new List<Method>();
        public Method constructor;
        public Function destructor;
    }
    internal class Coroutine : Declaration
    {
        public readonly List<DocumentToken> results = new List<DocumentToken>();
    }
    internal class InterfaceMethod
    {
        public readonly List<FunctionDeclaration> functions = new List<FunctionDeclaration>();
    }
    internal class Interface : Declaration
    {
        public readonly List<DocumentToken> inherts = new List<DocumentToken>();
        public readonly List<InterfaceMethod> methods = new List<InterfaceMethod>();
    }
    internal class Space
    {
        public string name;
        public readonly Space parent;
        public readonly List<Space> children = new List<Space>();
        public readonly List<DocumentSpace> documentSpaces = new List<DocumentSpace>();
        public readonly List<Definition> definitions = new List<Definition>();
        public readonly List<Variable> variables = new List<Variable>();
        public readonly List<FunctionDeclaration> delegates = new List<FunctionDeclaration>();
        public readonly List<Coroutine> coroutines = new List<Coroutine>();
        public readonly List<Method> methods = new List<Method>();
        public readonly List<Interface> interfaces = new List<Interface>();
        public readonly List<InterfaceMethod> natives = new List<InterfaceMethod>();
        public void Remove(DocumentSpace space)
        {
            if (documentSpaces.Remove(space) && documentSpaces.Count == 0)
            {
                parent.children.Remove(this);
            }
        }
    }
    internal class Library : Space
    {

    }
}
