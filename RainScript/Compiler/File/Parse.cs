using System.Collections.Generic;

namespace RainScript.Compiler.File
{
    internal class Type : System.IDisposable
    {
        public readonly ScopeList<Lexical> name;
        public readonly uint dimension;
        public Type(ScopeList<Lexical> name, uint dimension)
        {
            this.name = name;
            this.dimension = dimension;
        }
        public void Dispose()
        {
            name.Dispose();
        }
    }
    internal class Declaration
    {
        public readonly Anchor name;
        public readonly Visibility visibility;
        public readonly Space space;
        public Declaration(Anchor name, Visibility visibility, Space space)
        {
            this.name = name;
            this.visibility = visibility;
            this.space = space;
        }
    }
    internal class Definition : Declaration, System.IDisposable
    {
        public class Variable : System.IDisposable
        {
            public Compiling.Definition.MemberVariableInfo compiling;
            public readonly Anchor name;
            public readonly Visibility visibility;
            public readonly Type type;
            public readonly Anchor expression;
            public Variable(Anchor name, Visibility visibility, Type type, Anchor expression)
            {
                this.name = name;
                this.visibility = visibility;
                this.type = type;
                this.expression = expression;
            }
            public void Dispose()
            {
                type.Dispose();
            }
        }
        internal class Constructor : System.IDisposable
        {
            public Compiling.Function compiling;
            public readonly Anchor name;
            public readonly Visibility visibility;
            public readonly ScopeList<Parameter> parameters;
            public readonly TextSegment body;
            public readonly Anchor invokerExpression;
            public Constructor(Anchor name, Visibility visibility, ScopeList<Parameter> parameters, TextSegment body, Anchor invokerExpression)
            {
                this.name = name;
                this.visibility = visibility;
                this.parameters = parameters;
                this.body = body;
                this.invokerExpression = invokerExpression;
            }
            public void Dispose()
            {
                foreach (var item in parameters) item.Dispose();
                parameters.Dispose();
            }
        }
        public Compiling.Definition compiling;
        public readonly ScopeList<ScopeList<Lexical>> inherits;
        public readonly ScopeList<Variable> variables;
        public readonly ScopeList<Constructor> constructors;
        public readonly ScopeList<Function> functions;
        public readonly Function destructor;
        public Definition(Anchor name, Visibility visibility, Space space, ScopeList<ScopeList<Lexical>> interfaces, ScopeList<Variable> variables, ScopeList<Constructor> constructors, ScopeList<Function> functions, Function destructor) : base(name, visibility, space)
        {
            this.inherits = interfaces;
            this.variables = variables;
            this.constructors = constructors;
            this.functions = functions;
            this.destructor = destructor;
        }

        public void Dispose()
        {
            foreach (var item in inherits) item.Dispose();
            inherits.Dispose();
            foreach (var item in variables) item.Dispose();
            variables.Dispose();
            foreach (var item in constructors) item.Dispose();
            constructors.Dispose();
            foreach (var item in functions) item.Dispose();
            functions.Dispose();
            if (destructor != null) destructor.Dispose();
        }
    }
    internal class Variable : Declaration, System.IDisposable
    {
        public Compiling.Variable compiling;
        public readonly Type type;
        public readonly bool constant;
        public readonly Anchor expression;

        public Variable(Anchor name, Visibility visibility, Space space, Type type, bool constant, Anchor expression) : base(name, visibility, space)
        {
            this.type = type;
            this.constant = constant;
            this.expression = expression;
        }
        public void Dispose()
        {
            type.Dispose();
        }
    }
    internal class Parameter : System.IDisposable
    {
        public readonly Anchor name;
        public readonly Type type;
        public Parameter(Anchor name, Type type)
        {
            this.name = name;
            this.type = type;
        }
        public void Dispose()
        {
            type.Dispose();
        }
    }
    internal class FunctionDeclaration : Declaration, System.IDisposable
    {
        public Compiling.Delegate compiling;
        public readonly ScopeList<Parameter> parameters;
        public readonly ScopeList<Type> returns;
        public FunctionDeclaration(Anchor name, Visibility visibility, Space space, ScopeList<Parameter> parameters, ScopeList<Type> returns) : base(name, visibility, space)
        {
            this.parameters = parameters;
            this.returns = returns;
        }
        public void Dispose()
        {
            foreach (var item in parameters) item.Dispose();
            parameters.Dispose();
            foreach (var item in returns) item.Dispose();
            returns.Dispose();
        }
    }
    internal class Function : Declaration, System.IDisposable
    {
        public Compiling.Function compiling;
        public readonly ScopeList<Parameter> parameters;
        public readonly ScopeList<Type> returns;
        public readonly TextSegment body;
        public Function(Anchor name, Visibility visibility, Space space, ScopeList<Parameter> parameters, ScopeList<Type> returns, TextSegment body) : base(name, visibility, space)
        {
            this.parameters = parameters;
            this.returns = returns;
            this.body = body;
        }
        public void Dispose()
        {
            foreach (var item in parameters) item.Dispose();
            parameters.Dispose();
            foreach (var item in returns) item.Dispose();
            returns.Dispose();
        }
    }
    internal class Interface : Declaration, System.IDisposable
    {
        internal class Function : System.IDisposable
        {
            public Compiling.Delegate compiling;
            public readonly Anchor name;
            public readonly ScopeList<Parameter> parameters;
            public readonly ScopeList<Type> returns;
            public Function(Anchor name, ScopeList<Parameter> parameters, ScopeList<Type> returns)
            {
                this.name = name;
                this.parameters = parameters;
                this.returns = returns;
            }
            public void Dispose()
            {
                foreach (var item in parameters) item.Dispose();
                parameters.Dispose();
                foreach (var item in returns) item.Dispose();
                returns.Dispose();
            }
        }
        public Compiling.Interface compiling;
        public readonly ScopeList<ScopeList<Lexical>> inherits;
        public readonly ScopeList<Function> functions;
        public Interface(Anchor name, Visibility visibility, Space space, ScopeList<ScopeList<Lexical>> inherits, ScopeList<Function> functions) : base(name, visibility, space)
        {
            this.inherits = inherits;
            this.functions = functions;
        }
        public void Dispose()
        {
            foreach (var item in functions) item.Dispose();
            functions.Dispose();
        }
    }
    internal class Coroutine : Declaration, System.IDisposable
    {
        public Compiling.Coroutine compiling;
        public readonly ScopeList<Type> returns;
        public Coroutine(Anchor name, Visibility visibility, Space space, ScopeList<Type> returns) : base(name, visibility, space)
        {
            this.returns = returns;
        }

        public void Dispose()
        {
            foreach (var item in returns) item.Dispose();
            returns.Dispose();
        }
    }
    internal partial class Space : System.IDisposable
    {
        public readonly Space parent;
        public readonly Compiling.Space compiling;
        public readonly TextSegment segment;
        public readonly ScopeList<Space> children;
        public readonly ScopeList<ScopeList<Lexical>> imports;

        public readonly ScopeList<Definition> definitions;
        public readonly ScopeList<Variable> variables;
        public readonly ScopeList<FunctionDeclaration> delegates;
        public readonly ScopeList<Coroutine> coroutines;
        public readonly ScopeList<Function> functions;
        public readonly ScopeList<Interface> interfaces;
        public readonly ScopeList<FunctionDeclaration> natives;
        public Space(Compiling.Library library, TextInfo text, CollectionPool pool, ExceptionCollector exceptions) : this(null, library, text, 0, 0, pool, exceptions) { }
        private Space(Space parent, Compiling.Space space, TextInfo text, int startLineIndex, int parentIndent, CollectionPool pool, ExceptionCollector exceptions)
        {
            this.parent = parent;
            compiling = space;
            children = pool.GetList<Space>();
            imports = pool.GetList<ScopeList<Lexical>>();

            relyCompilings = pool.GetList<Compiling.Space>();
            relyReferences = pool.GetList<RelySpace>();

            definitions = pool.GetList<Definition>();
            variables = pool.GetList<Variable>();
            delegates = pool.GetList<FunctionDeclaration>();
            coroutines = pool.GetList<Coroutine>();
            functions = pool.GetList<Function>();
            interfaces = pool.GetList<Interface>();
            natives = pool.GetList<FunctionDeclaration>();

            var indent = -1;
            for (int index = startLineIndex; index < text.LineCount; index++)
            {
                var line = text[index];
                if (Lexical.TryAnalysisFirst(text, line.segment, 0, out var lexical, exceptions))
                {
                    if (lexical.type == LexicalType.Word)
                    {
                        if (indent < 0)
                        {
                            indent = line.indent;
                            if (indent <= parentIndent) exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_INDENT);
                            segment = new TextSegment(text, startLineIndex, index - 1);
                            return;
                        }
                        else if (line.indent != indent)
                        {
                            segment = new TextSegment(text, startLineIndex, index - 1);
                            if (parent == null || line.indent > parentIndent) exceptions.Add(new Anchor(text, line.segment), CompilingExceptionCode.SYNTAX_INDENT);
                            return;
                        }
                        if (lexical.anchor == KeyWorld.IMPORT) ParseImport(text, indent, pool, exceptions);
                        else if (lexical.anchor == KeyWorld.NAMESPACE) indent = ParseChild(text, indent, pool, exceptions);
                        else index = ParseDeclaration(text, index, pool, exceptions);
                    }
                    else if (lexical.type != LexicalType.Annotation) exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                    continue;
                }
                exceptions.Add(new Anchor(text, line.segment), CompilingExceptionCode.LEXICAL_UNKNOWN);
            }
            segment = new TextSegment(text, startLineIndex, text.LineCount - 1);
        }
        private int ParseChild(TextInfo text, int line, CollectionPool pool, ExceptionCollector exceptions)
        {
            using (var lexicals = pool.GetList<Lexical>())
                if (Lexical.TryAnalysis(lexicals, text, text[line].segment, exceptions))
                    if (Lexical.TryExtractName(lexicals, 1, out var index, out var names, pool))
                    {
                        if (index < lexicals.Count) exceptions.Add(lexicals[index].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                        var space = this.compiling;
                        foreach (var name in names) space = space.GetChild(name.anchor.Segment);
                        names.Dispose();
                        var child = new Space(this, space, text, line + 1, text[line].indent, pool, exceptions);
                        children.Add(child);
                        return child.segment.end;
                    }
                    else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_MISSING_NAME);
            return line;
        }
        private void ParseImport(TextInfo text, int line, CollectionPool pool, ExceptionCollector exceptions)
        {
            using (var lexicals = pool.GetList<Lexical>())
                if (Lexical.TryAnalysis(lexicals, text, text[line].segment, exceptions))
                    if (Lexical.TryExtractName(lexicals, 1, out var index, out var name, pool))
                    {
                        if (index < lexicals.Count) exceptions.Add(lexicals[index].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                        imports.Add(name);
                    }
                    else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_MISSING_NAME);
        }
        private bool TryParseVisibility(IList<Lexical> lexicals, out int index, out Visibility visibility, ExceptionCollector exceptions)
        {
            visibility = Visibility.None;
            for (index = 0; index < lexicals.Count; index++)
            {
                var lexical = lexicals[index];
                if (lexical.anchor == KeyWorld.PUBLIC)
                {
                    if (visibility.Clash(Visibility.Public)) exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_INVALID_VISIBILITY);
                    visibility |= Visibility.Public;
                }
                else if (lexical.anchor == KeyWorld.INTERNAL)
                {
                    if (visibility.Clash(Visibility.Internal)) exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_INVALID_VISIBILITY);
                    visibility |= Visibility.Internal;
                }
                else if (lexical.anchor == KeyWorld.SPACE)
                {
                    if (visibility.Clash(Visibility.Space)) exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_INVALID_VISIBILITY);
                    visibility |= Visibility.Space;
                }
                else if (lexical.anchor == KeyWorld.PROTECTED)
                {
                    if (visibility.Clash(Visibility.Protected)) exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_INVALID_VISIBILITY);
                    visibility |= Visibility.Protected;
                }
                else if (lexical.anchor == KeyWorld.PRIVATE)
                {
                    if (visibility.Clash(Visibility.Private)) exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_INVALID_VISIBILITY);
                    visibility |= Visibility.Private;
                }
                else return true;
                index++;
            }
            exceptions.Add(lexicals[-1].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LINE_END);
            return false;
        }
        private int ParseDeclaration(TextInfo text, int line, CollectionPool pool, ExceptionCollector exceptions)
        {
            using (var lexicals = pool.GetList<Lexical>())
                if (Lexical.TryAnalysis(lexicals, text, text[line].segment, exceptions) && TryParseVisibility(lexicals, out var index, out var visibility, exceptions))
                {
                    if (visibility == Visibility.None) visibility = Visibility.Space;
                    var lexical = lexicals[index];
                    if (lexical.anchor == KeyWorld.CONST)
                    {
                        if (TryParseVariable(lexicals, index + 1, out var name, out var type, out var expression, pool))
                        {
                            if (expression) variables.Add(new Variable(name, visibility, this, type, true, expression));
                            else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_CONSTANT_NOT_ASSIGNMENT);
                        }
                        else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                    }
                    else if (lexical.anchor == KeyWorld.CLASS)
                    {
                        if (TryParseDefinition(text, ref line, lexicals, index + 1, visibility, out var definition, pool, exceptions)) definitions.Add(definition);
                    }
                    else if (lexical.anchor == KeyWorld.INTERFACE)
                    {
                        if (TryParseInterface(text, ref line, lexicals, index + 1, visibility, out var definition, pool, exceptions)) interfaces.Add(definition);
                    }
                    else if (lexical.anchor == KeyWorld.NATIVE)
                    {
                        if (TryParseFunction(lexicals, index + 1, out var end, out var name, out var parameters, out var returns, pool))
                        {
                            if (end < lexicals.Count) exceptions.Add(lexicals[end].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                            natives.Add(new FunctionDeclaration(name, visibility, this, parameters, returns));
                        }
                        else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_UNKNOW);
                    }
                    else if (lexical.anchor == KeyWorld.FUNCTION)
                    {
                        if (TryParseFunction(lexicals, index + 1, out var end, out var name, out var parameters, out var returns, pool))
                        {
                            if (end < lexicals.Count) exceptions.Add(lexicals[end].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                            delegates.Add(new FunctionDeclaration(name, visibility, this, parameters, returns));
                        }
                        else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_UNKNOW);
                    }
                    else if (lexical.anchor == KeyWorld.COROUTINE)
                    {
                        if (TryParseCoroutine(lexicals, index + 1, visibility, out var coroutine, pool, exceptions)) coroutines.Add(coroutine);
                    }
                    else if (TryParseVariable(lexicals, index + 1, out var variableName, out var variableType, out var variableExpression, pool))
                        variables.Add(new Variable(variableName, visibility, this, variableType, false, variableExpression));
                    else if (TryParseFunction(lexicals, index + 1, out var functionEnd, out var functionName, out var functionParameters, out var functionReturns, pool))
                    {
                        if (functionEnd < lexicals.Count) exceptions.Add(lexicals[functionEnd].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                        functionEnd = SubBlock(text, line + 1, text[line].indent, exceptions);
                        functions.Add(new Function(functionName, visibility, this, functionParameters, functionReturns, new TextSegment(text, line + 1, functionEnd)));
                        line = functionEnd;
                    }
                    else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_UNKNOW);
                }
            return line;
        }
        private bool TryParseCoroutine(IList<Lexical> lexicals, int index, Visibility visibility, out Coroutine coroutine, CollectionPool pool, ExceptionCollector exceptions)
        {
            if (index < lexicals.Count)
                if (lexicals[index].type == LexicalType.Less)
                {
                    if (++index < lexicals.Count)
                    {
                        if (lexicals[index].type == LexicalType.Greater)
                        {
                            if (++index < lexicals.Count)
                            {
                                if (lexicals[index].type == LexicalType.Word)
                                {
                                    if (index + 1 < lexicals.Count) exceptions.Add(lexicals[index + 1].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                    coroutine = new Coroutine(lexicals[index].anchor, visibility, this, pool.GetList<Type>());
                                    return true;
                                }
                                else exceptions.Add(lexicals[index].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                            }
                            else exceptions.Add(lexicals, CompilingExceptionCode.SYNTAX_MISSING_NAME);
                        }
                        else
                        {
                            var returns = pool.GetList<Type>();
                            while (index < lexicals.Count)
                            {
                                if (Lexical.TryExtractName(lexicals, index, out var end, out var typeName, pool))
                                {
                                    index = end;
                                    returns.Add(new Type(typeName, Lexical.ExtractDimension(lexicals, ref index)));
                                    if (index < lexicals.Count)
                                    {
                                        if (lexicals[index].type == LexicalType.Greater)
                                        {
                                            index++;
                                            if (index < lexicals.Count && lexicals[index].type == LexicalType.Word)
                                            {
                                                if (index + 1 < lexicals.Count) exceptions.Add(lexicals, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                                coroutine = new Coroutine(lexicals[index].anchor, visibility, this, returns);
                                                return true;
                                            }
                                            else
                                            {
                                                exceptions.Add(lexicals, CompilingExceptionCode.SYNTAX_MISSING_NAME);
                                                break;
                                            }
                                        }
                                        else if (lexicals[index].type != LexicalType.Comma)
                                        {
                                            exceptions.Add(lexicals, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                            break;
                                        }
                                        else index++;
                                    }
                                    else
                                    {
                                        exceptions.Add(lexicals, CompilingExceptionCode.SYNTAX_MISSING_NAME);
                                        break;
                                    }
                                }
                                else
                                {
                                    exceptions.Add(lexicals[index].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                    break;
                                }
                            }
                            foreach (var item in returns) item.Dispose();
                            returns.Dispose();
                        }
                    }
                    else exceptions.Add(lexicals, CompilingExceptionCode.SYNTAX_MISSING_NAME);

                }
                else if (lexicals[index].type == LexicalType.Word)
                {
                    if (index + 1 < lexicals.Count) exceptions.Add(lexicals[index + 1].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                    coroutine = new Coroutine(lexicals[index].anchor, visibility, this, pool.GetList<Type>());
                    return true;
                }
                else exceptions.Add(lexicals[index].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
            coroutine = default;
            return false;
        }
        private bool TryParseInterface(TextInfo text, ref int line, IList<Lexical> lexicals, int index, Visibility visibility, out Interface definition, CollectionPool pool, ExceptionCollector exceptions)
        {
            if (index < lexicals.Count)
            {
                if (lexicals[index].type == LexicalType.Word)
                {
                    var name = lexicals[index++].anchor;
                    var interfaces = pool.GetList<ScopeList<Lexical>>();
                    while (Lexical.TryExtractName(lexicals, index, out var endIndex, out var interfaceName, pool))
                    {
                        interfaces.Add(interfaceName);
                        index = endIndex;
                    }
                    if (index < lexicals.Count) exceptions.Add(lexicals[index].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                    var functions = pool.GetList<Interface.Function>();
                    var definitionIndent = text[line].indent;
                    var indent = -1;
                    var lineIndex = line;
                    while (++lineIndex < text.LineCount)
                    {
                        if (indent < 0)
                        {
                            indent = text[lineIndex].indent;
                            if (indent <= definitionIndent) break;
                        }
                        else if (text[lineIndex].indent <= definitionIndent) break;
                        else if (text[lineIndex].indent != indent) exceptions.Add(text, lineIndex, CompilingExceptionCode.SYNTAX_INDENT);
                        using (var lineLexicals = pool.GetList<Lexical>())
                            if (Lexical.TryAnalysis(lineLexicals, text, text[lineIndex].segment, exceptions))
                                if (TryParseFunction(lineLexicals, indent, out var functionEnd, out var functionName, out var parameters, out var returns, pool))
                                {
                                    if (functionEnd < lineLexicals.Count) exceptions.Add(lineLexicals[functionEnd].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                    functions.Add(new Interface.Function(functionName, parameters, returns));
                                }
                                else exceptions.Add(text, lineIndex, CompilingExceptionCode.SYNTAX_UNKNOW);
                    }
                    definition = new Interface(name, visibility, this, interfaces, functions);
                    line = lineIndex - 1;
                    return true;
                }
                else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
            }
            else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_MISSING_NAME);
            definition = null;
            return false;
        }
        private bool TryParseDefinition(TextInfo text, ref int line, IList<Lexical> lexicals, int index, Visibility visibility, out Definition definition, CollectionPool pool, ExceptionCollector exceptions)
        {
            if (index < lexicals.Count)
            {
                if (lexicals[index].type == LexicalType.Word)
                {
                    var name = lexicals[index++].anchor;
                    var inherits = pool.GetList<ScopeList<Lexical>>();
                    while (Lexical.TryExtractName(lexicals, index, out var endIndex, out var inheritName, pool))
                    {
                        inherits.Add(inheritName);
                        index = endIndex;
                    }
                    if (index < lexicals.Count) exceptions.Add(lexicals[index].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                    var variables = pool.GetList<Definition.Variable>();
                    var constructors = pool.GetList<Definition.Constructor>();
                    var functions = pool.GetList<Function>();
                    Function destructor = null;
                    var definitionIndent = text[line].indent;
                    var indent = -1;
                    var lineIndex = line;
                    while (++lineIndex < text.LineCount)
                    {
                        if (indent < 0)
                        {
                            indent = text[lineIndex].indent;
                            if (indent <= definitionIndent) break;
                        }
                        else if (text[lineIndex].indent <= definitionIndent) break;
                        else if (text[lineIndex].indent != indent) exceptions.Add(text, lineIndex, CompilingExceptionCode.SYNTAX_INDENT);
                        using (var lineLexicals = pool.GetList<Lexical>())
                            if (Lexical.TryAnalysis(lineLexicals, text, text[lineIndex].segment, exceptions) && TryParseVisibility(lineLexicals, out index, out var memberVisibility, exceptions))
                                if (lineLexicals[index].type == LexicalType.Negate)//析构函数
                                {
                                    if (memberVisibility != Visibility.None) exceptions.Add(text, lineIndex, CompilingExceptionCode.SYNTAX_INVALID_VISIBILITY);
                                    if (index + 1 < lineLexicals.Count) exceptions.Add(lineLexicals[index + 1].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                    var body = new TextSegment(text, lineIndex + 1, SubBlock(text, lineIndex + 1, indent, exceptions));
                                    destructor = new Function(lineLexicals[index].anchor, Visibility.None, this, pool.GetList<Parameter>(), pool.GetList<Type>(), body);
                                    lineIndex = body.end;
                                }
                                else if (TryParseVariable(lineLexicals, index, out var variableName, out var variableType, out var varibaleExpression, pool))
                                {
                                    if (memberVisibility == Visibility.None) memberVisibility = Visibility.Private;
                                    variables.Add(new Definition.Variable(variableName, memberVisibility, variableType, varibaleExpression));
                                }
                                else if (TryParseFunction(lineLexicals, index, out var functionEnd, out var functionName, out var parameters, out var returns, pool))
                                {
                                    if (memberVisibility == Visibility.None) memberVisibility = Visibility.Private;
                                    var body = new TextSegment(text, lineIndex + 1, SubBlock(text, lineIndex + 1, indent, exceptions));
                                    lineIndex = body.end;
                                    if (functionName.Segment == name.Segment)
                                    {
                                        if (returns.Count > 0) exceptions.Add(functionName, CompilingExceptionCode.SYNTAX_CONSTRUCTOR_NO_RETURN_VALUE);
                                        returns.Dispose();
                                        if (functionEnd < lineLexicals.Count) constructors.Add(new Definition.Constructor(functionName, memberVisibility, parameters, body, new Anchor(text, lineLexicals[functionEnd].anchor.start, lineLexicals[-1].anchor.end)));
                                        else constructors.Add(new Definition.Constructor(functionName, memberVisibility, parameters, body, default));
                                    }
                                    else
                                    {
                                        if (functionEnd < lineLexicals.Count) exceptions.Add(lineLexicals[functionEnd].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                        functions.Add(new Function(functionName, memberVisibility, this, parameters, returns, body));
                                    }
                                }
                                else exceptions.Add(text, lineIndex, CompilingExceptionCode.SYNTAX_UNKNOW);
                    }
                    definition = new Definition(name, visibility, this, inherits, variables, constructors, functions, destructor);
                    line = lineIndex - 1;
                    return true;
                }
                else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
            }
            else exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_MISSING_NAME);
            definition = null;
            return false;
        }
        private bool TryParseVariable(IList<Lexical> lexicals, int index, out Anchor name, out Type type, out Anchor expression, CollectionPool pool)
        {
            if (Lexical.TryExtractName(lexicals, index, out var nameEnd, out var typeName, pool))
            {
                index = nameEnd;
                type = new Type(typeName, Lexical.ExtractDimension(lexicals, ref index));
                if (index < lexicals.Count && lexicals[index].type == LexicalType.Word)
                {
                    name = lexicals[index].anchor;
                    index++;
                    if (index >= lexicals.Count)
                    {
                        expression = default;
                        return true;
                    }
                    else if (lexicals[index].type == LexicalType.Assignment)
                    {
                        index++;
                        if (index < lexicals.Count) expression = new Anchor(lexicals[index].anchor.textInfo, lexicals[index].anchor.start, lexicals[index].anchor.end);
                        else expression = default;
                        return true;
                    }
                }
            }

            name = default;
            type = default;
            expression = default;
            return false;
        }
        private bool TryParseFunction(IList<Lexical> lexicals, int start, out int index, out Anchor name, out ScopeList<Parameter> paraemters, out ScopeList<Type> returns, CollectionPool pool)
        {
            paraemters = null;
            returns = pool.GetList<Type>();
            if (Lexical.TryExtractName(lexicals, start, out index, out var nameList, pool))
            {
                if (index < lexicals.Count)
                {
                    if (lexicals[index].type == LexicalType.BracketLeft0)
                    {
                        start = index;
                        if (nameList.Count == 1 && TryParseParameters(lexicals, start, out index, out paraemters, pool))
                        {
                            name = nameList[0].anchor;
                            return true;
                        }
                        else nameList.Dispose();
                    }
                    else if (lexicals[index].type == LexicalType.Word)
                    {
                        name = lexicals[index].anchor;
                        start = index + 1;
                        if (TryParseParameters(lexicals, start, out index, out paraemters, pool))
                        {
                            returns.Add(new Type(nameList, 0));
                            return true;
                        }
                        else nameList.Dispose();
                    }
                    else
                    {
                        returns.Add(new Type(nameList, Lexical.ExtractDimension(lexicals, ref index)));
                        while (index < lexicals.Count)
                        {
                            if (lexicals[index].type == LexicalType.Comma)
                            {
                                start = index + 1;
                                if (Lexical.TryExtractName(lexicals, start, out index, out nameList, pool))
                                    returns.Add(new Type(nameList, Lexical.ExtractDimension(lexicals, ref index)));
                                else break;
                            }
                            else if (lexicals[index].type == LexicalType.Word)
                            {
                                name = lexicals[index].anchor;
                                start = index + 1;
                                if (TryParseParameters(lexicals, start, out index, out paraemters, pool)) return true;
                                else break;
                            }
                            else break;
                        }
                        foreach (var item in returns) item.Dispose();
                    }
                }
            }
            index = default;
            name = default;
            if (paraemters != null) paraemters.Dispose();
            returns.Dispose();
            return false;
        }
        private bool TryParseParameters(IList<Lexical> lexicals, int start, out int index, out ScopeList<Parameter> parameters, CollectionPool pool)
        {
            index = start;
            if (lexicals[index++].type == LexicalType.BracketLeft0)
            {
                parameters = pool.GetList<Parameter>();
                while (index < lexicals.Count)
                {
                    if (Lexical.TryExtractName(lexicals, index, out var nameEnd, out var typeName, pool))
                    {
                        index = nameEnd;
                        var dimension = Lexical.ExtractDimension(lexicals, ref index);
                        if (index < lexicals.Count)
                        {
                            var lexical = lexicals[index++];
                            if (lexical.type == LexicalType.Word)
                            {
                                parameters.Add(new Parameter(lexical.anchor, new Type(typeName, dimension)));
                                if (index < lexicals.Count)
                                {
                                    lexical = lexicals[index++];
                                    if (lexical.type == LexicalType.BracketRight0) return true;
                                    else if (lexical.type != LexicalType.Comma) break;
                                }
                            }
                            else if (lexical.type == LexicalType.Comma)
                                parameters.Add(new Parameter(default, new Type(typeName, dimension)));
                            else if (lexical.type == LexicalType.BracketRight0)
                            {
                                parameters.Add(new Parameter(default, new Type(typeName, dimension)));
                                return true;
                            }
                            else break;
                        }
                        else typeName.Dispose();
                    }
                    else break;
                }
                foreach (var item in parameters) item.Dispose();
                parameters.Dispose();
            }
            parameters = default;
            return false;
        }
        private int SubBlock(TextInfo text, int line, int parentIndent, ExceptionCollector exceptions)
        {
            var indent = -1;
            while (line < text.LineCount)
            {
                if (indent < 0) indent = text[line].indent;
                if (text[line].indent <= parentIndent) return line - 1;
                else if (text[line].indent < indent) exceptions.Add(text, line, CompilingExceptionCode.SYNTAX_INDENT);
                line++;
            }
            return line - 1;
        }
        public void Dispose()
        {
            foreach (var item in children) item.Dispose();
            children.Dispose();
            foreach (var item in imports) item.Dispose();
            imports.Dispose();

            relyCompilings.Dispose();
            relyReferences.Dispose();

            foreach (var item in definitions) item.Dispose();
            definitions.Dispose();
            foreach (var item in variables) item.Dispose();
            variables.Dispose();
            foreach (var item in delegates) item.Dispose();
            delegates.Dispose();
            foreach (var item in coroutines) item.Dispose();
            coroutines.Dispose();
            foreach (var item in functions) item.Dispose();
            functions.Dispose();
            foreach (var item in interfaces) item.Dispose();
            interfaces.Dispose();
            foreach (var item in natives) item.Dispose();
            natives.Dispose();
        }
    }
}
