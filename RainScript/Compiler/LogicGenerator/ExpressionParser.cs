using RainScript.Compiler.LogicGenerator.Expressions;
using RainScript.Vector;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript.Compiler.LogicGenerator
{
    internal struct ExpressionParser
    {
        public readonly DeclarationManager manager;
        public readonly Context context;
        public readonly LocalContext localContext;
        public readonly LambdaClosure closure;
        public readonly bool destructor;
        public readonly CollectionPool pool;
        public readonly ExceptionCollector exceptions;
        public ExpressionParser(GeneratorParameter parameter, Context context, LocalContext localContext, bool destructor) : this(parameter.manager, context, localContext, null, destructor, parameter.pool, parameter.exceptions) { }
        public ExpressionParser(DeclarationManager manager, Context context, LocalContext localContext, LambdaClosure closure, bool destructor, CollectionPool pool, ExceptionCollector exceptions)
        {
            this.manager = manager;
            this.context = context;
            this.localContext = localContext;
            this.closure = closure;
            this.destructor = destructor;
            this.pool = pool;
            this.exceptions = exceptions;
        }
        public bool TryFindDeclaration(Anchor name, out Declaration result)
        {
            if (localContext.TryGetLocal(name.Segment, out var local))
            {
                result = local.Declaration;
                return true;
            }
            else if (closure != null && closure.TryFindDeclaration(name, out result)) return true;
            else return context.TryFindDeclaration(manager, name, out result, pool, exceptions);
        }
        public bool TryGetThisValueDeclaration(out Declaration declaration)
        {
            if (localContext.TryGetLocal(KeyWorld.THIS, out var local))
            {
                declaration = local.Declaration;
                return true;
            }
            if (closure != null && closure.TryGetThisValueDeclaration(out declaration)) return true;
            declaration = default;
            return false;
        }
        private bool TryGetThisValueExpression(out Expression expression)
        {
            if (TryGetThisValueDeclaration(out var declaration))
            {
                if (declaration.code == DeclarationCode.LocalVariable)
                {
                    expression = new VariableLocalExpression(default, declaration, TokenAttribute.Value, GetVariableType(declaration));
                    return true;
                }
                else if (declaration.code == DeclarationCode.LambdaClosureValue)
                {
                    var local = new Local(default, 0, new CompilingType(LIBRARY.SELF, Visibility.Space, TypeCode.Handle, declaration.definitionIndex, 0));
                    expression = new VariableMemberExpression(default, declaration, new VariableLocalExpression(local, TokenAttribute.Value), GetVariableType(declaration));
                    return true;
                }
            }
            expression = default;
            return false;
        }
        public CompilingType GetVariableType(Declaration declaration)
        {
            if (declaration.code == DeclarationCode.MemberVariable)
            {
                if (declaration.library == LIBRARY.SELF) return manager.library.definitions[(int)declaration.definitionIndex].variables[declaration.index].type;
                else if (declaration.library != LIBRARY.KERNEL) return manager.relies[declaration.library].definitions[declaration.definitionIndex].variables[declaration.index].type;
            }
            else if (declaration.code == DeclarationCode.GlobalVariable)
            {
                if (declaration.library == LIBRARY.KERNEL) return RelyKernel.variables[declaration.index].type;
                else if (declaration.library == LIBRARY.SELF) return manager.library.variables[(int)declaration.index].type;
                else return manager.relies[declaration.library].variables[declaration.index].type;
            }
            else if (declaration.code == DeclarationCode.LambdaClosureValue)
            {
                if (closure != null && closure.TryGetVariableType(declaration, out var type)) return type;
            }
            else if (declaration.code == DeclarationCode.LocalVariable)
            {
                if (localContext.TryGetLocal(declaration, out var local)) return local.type;
            }
            throw ExceptionGeneratorCompiler.InvalidDeclaration(declaration);
        }
        public bool IsConstant(Declaration declaration)
        {
            if (declaration.code == DeclarationCode.GlobalVariable)
            {
                if (declaration.library == LIBRARY.KERNEL) return true;
                else if (declaration.library == LIBRARY.SELF) return manager.library.variables[(int)declaration.index].constant;
                else return manager.relies[declaration.library].variables[declaration.index].constant;
            }
            return false;
        }
        public void BuildLambda(Anchor anchor, LambdaClosure closure, CompilingType functionType, CompilingType[] returns, CompilingType[] parameters, Anchor[] parameterNames, out Expression expression, out LambdaFunction lambda)
        {
            var declaration = new Declaration(LIBRARY.SELF, Visibility.Space, DeclarationCode.Lambda, (uint)manager.library.methods.Count, 0, (uint)manager.lambdas.Count);
            var function = new Compiling.Function(declaration, context.space, returns, parameters, parameterNames, pool);
            lambda = new LambdaFunction(function.entry, parameters, pool);
            manager.lambdas.Add(lambda);
            var method = new Compiling.Method((uint)manager.library.methods.Count, DeclarationCode.Lambda, "", context.space);
            method.AddFunction(function);
            manager.library.methods.Add(method);
            if (closure.TryGetClosureVariables(declaration.index, out var definition, out var sourceVariables, out var sourceTypes)) expression = new DelegateCreateLambdaClosureFunctionExpression(anchor, declaration, definition, sourceVariables, sourceTypes, functionType);
            else expression = new DelegateCreateLambdaFunctionExpression(default, declaration, functionType);
        }
        private bool IsDecidedTypes(CompilingType[] types)
        {
            foreach (var type in types)
                if (type == RelyKernel.NULL_TYPE || type == RelyKernel.BLURRY_TYPE) return false;
            return true;
        }
        private bool CanConvert(CompilingType source, CompilingType type, out bool convert, out uint measure)
        {
            if (source == RelyKernel.BLURRY_TYPE || source == RelyKernel.NULL_TYPE)
            {
                convert = default;
                measure = default;
                return false;
            }
            else if (type == RelyKernel.BLURRY_TYPE)
            {
                convert = false;
                measure = 0;
                return source != RelyKernel.BLURRY_TYPE && source != RelyKernel.NULL_TYPE;
            }
            else if (type == source)
            {
                convert = false;
                measure = 0;
                return true;
            }
            else if (type == RelyKernel.REAL_TYPE)
            {
                if (source == RelyKernel.INTEGER_TYPE)
                {
                    convert = true;
                    measure = 0xff;
                    return true;
                }
            }
            else if (type == RelyKernel.REAL2_TYPE)
            {
                if (source == RelyKernel.REAL3_TYPE)
                {
                    convert = true;
                    measure = 0xfff;
                    return true;
                }
                else if (source == RelyKernel.REAL4_TYPE)
                {
                    convert = true;
                    measure = 0xffff;
                    return true;
                }
            }
            else if (type == RelyKernel.REAL3_TYPE)
            {
                if (source == RelyKernel.REAL2_TYPE)
                {
                    convert = true;
                    measure = 0xfff;
                    return true;
                }
                else if (source == RelyKernel.REAL4_TYPE)
                {
                    convert = true;
                    measure = 0xfff;
                    return true;
                }
            }
            else if (type == RelyKernel.REAL4_TYPE)
            {
                if (source == RelyKernel.REAL2_TYPE)
                {
                    convert = true;
                    measure = 0xffff;
                    return true;
                }
                else if (source == RelyKernel.REAL3_TYPE)
                {
                    convert = true;
                    measure = 0xfff;
                    return true;
                }
            }
            else if (manager.TryGetInherit(type, source, out measure))
            {
                convert = false;
                return true;
            }
            convert = default;
            measure = default;
            return false;
        }
        public bool TryAssignmentConvert(Expression[] sources, CompilingType[] types, out Expression result, out uint measure)
        {
            measure = 0;
            for (int index = 0, typeIndex = 0; index < sources.Length; index++)
            {
                var expression = sources[index];
                if (expression.returns.Length == 1)
                {
                    if (typeIndex < types.Length && TryAssignmentConvert(expression, types[typeIndex], out expression, out var value))
                    {
                        sources[index] = expression;
                        measure += value;
                        typeIndex++;
                    }
                    else
                    {
                        result = default;
                        return false;
                    }
                }
                else if (expression.returns.Length > 1)
                    for (int i = 0; i < expression.returns.Length; i++)
                        if (!(typeIndex < types.Length && CanConvert(expression.returns[i], types[typeIndex++], out _, out _)))
                        {
                            result = default;
                            return false;
                        }
            }
            {
                if (TryAssignmentConvert(TupleExpression.Combine(sources), types, out result, out var value))
                {
                    measure += value;
                    return true;
                }
                else return false;
            }
        }
        public unsafe bool TryAssignmentConvert(Expression source, CompilingType[] types, out Expression result, out uint measure)
        {
            if (source.returns.Length == types.Length)
            {
                measure = 0;
                var count = 0;
                var converts = stackalloc int[types.Length];
                for (int i = 0; i < types.Length; i++)
                    if (CanConvert(source.returns[i], types[i], out var convert, out var value))
                    {
                        measure += value;
                        if (convert) converts[count++] = i;
                    }
                    else
                    {
                        result = default;
                        measure = default;
                        return false;
                    }
                if (count > 0)
                {
                    var indices = new int[count];
                    for (int i = 0; i < count; i++) indices[i] = converts[i];
                    result = new TupleConvertExpression(source.anchor, source, indices, types);
                }
                else result = source;
                return true;
            }
            result = default;
            measure = default;
            return false;
        }
        public bool TryAssignmentConvert(Expression source, CompilingType type, out Expression result, out uint measure)
        {
            if (source.returns.Length == 1)
            {
                var st = source.returns[0];
                if (type == RelyKernel.BLURRY_TYPE)
                {
                    if (st != RelyKernel.NULL_TYPE && st != RelyKernel.BLURRY_TYPE)
                    {
                        result = source;
                        measure = 0;
                        return true;
                    }
                }
                else if (st == RelyKernel.BLURRY_TYPE)
                {
                    if (type.dimension == 0)
                    {
                        if (type.definition.code == TypeCode.Function)
                        {
                            if (manager.TryGetParameters(type.definition.Declaration, out var parameters) && manager.TryGetReturns(type.definition.Declaration, out var returns))
                            {
                                if (source is MethodGlobalExpression globalMethod)
                                {
                                    if (manager.TryGetFunction(globalMethod.declaration, parameters, returns, out var functionDeclaration))
                                    {
                                        result = new DelegateCreateGlobalFunctionExpression(source.anchor, functionDeclaration, type);
                                        measure = 0;
                                        return true;
                                    }
                                }
                                else if (source is MethodNativeExpression nativeMethod)
                                {
                                    if (manager.TryGetFunction(nativeMethod.declaration, parameters, returns, out var functionDeclaration))
                                    {
                                        result = new DelegateCreateNativeFunctionExpression(source.anchor, functionDeclaration, type);
                                        measure = 0;
                                        return true;
                                    }
                                }
                                else if (source is MethodMemberExpression memberMethod)
                                {
                                    if (manager.TryGetFunction(memberMethod.declaration, parameters, returns, out var functionDeclaration))
                                    {
                                        result = new DelegateCreateMemberFunctionExpression(source.anchor, functionDeclaration, memberMethod.target, type);
                                        measure = 0;
                                        return true;
                                    }
                                }
                                else if (source is MethodVirtualExpression virtualMethod)
                                {
                                    if (manager.TryGetFunction(virtualMethod.declaration, parameters, returns, out var functionDeclaration))
                                    {
                                        result = new DelegateCreateVirtualMemberFunctionExpression(source.anchor, functionDeclaration, virtualMethod.target, type);
                                        measure = 0;
                                        return true;
                                    }
                                }
                                else if (source is MethodQuestionExpression questionMethod)
                                {
                                    if (manager.TryGetFunction(questionMethod.declaration, parameters, returns, out var functionDeclaration))
                                    {
                                        result = new DelegateCreateQuestionMemberFunctionExpression(source.anchor, functionDeclaration, questionMethod.target, type);
                                        measure = 0;
                                        return true;
                                    }
                                }
                                else if (source is BlurryLambdaExpression lambda)
                                {
                                    if (lambda.parameters.Length == parameters.Length)
                                    {
                                        using (var localContext = new LocalContext(pool))
                                        {
                                            localContext.PushBlock(pool);
                                            for (int i = 0; i < parameters.Length; i++)
                                                localContext.AddLocal(lambda.parameters[i], parameters[i]);
                                            using (var closure = new LambdaClosure(this))
                                            {
                                                var parser = new ExpressionParser(manager, context, localContext, closure, destructor, pool, exceptions);
                                                if (parser.TryParseTuple(lambda.body, out var expressions))
                                                {
                                                    if (parser.TryAssignmentConvert(expressions, returns, out var expression, out _))
                                                    {
                                                        BuildLambda(lambda.anchor, closure, type, returns, parameters, lambda.parameters, out result, out var lambdaFunction);
                                                        lambdaFunction.statements.Add(new ReturnStatement(expression.anchor, expression));
                                                        lambdaFunction.SetReturnCount((uint)returns.Length);
                                                        measure = 0;
                                                        return true;
                                                    }
                                                    else if (returns.Length == 0)
                                                    {
                                                        foreach (var item in expressions)
                                                            if (!IsDecidedTypes(item.returns))
                                                            {
                                                                result = default;
                                                                exceptions.Add(item.anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
                                                                measure = 0;
                                                                return false;
                                                            }
                                                        BuildLambda(lambda.anchor, closure, type, returns, parameters, lambda.parameters, out result, out var lambdaFunction);
                                                        foreach (var item in expressions)
                                                            lambdaFunction.statements.Add(new ExpressionStatement(item));
                                                        lambdaFunction.SetReturnCount(0);
                                                        measure = 0;
                                                        return true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (type.definition.code == TypeCode.Coroutine && source is BlurryCoroutineExpression blurryCoroutine)
                        {
                            if (manager.TryGetReturns(type.definition.Declaration, out var coroutineReturns) && CompilingType.IsEquals(coroutineReturns, blurryCoroutine.invoker.returns))
                            {
                                result = new CoroutineCreateExpression(source.anchor, blurryCoroutine, type);
                                measure = 0;
                                return true;
                            }
                        }
                    }
                }
                else if (st == RelyKernel.NULL_TYPE)
                {
                    if (type == RelyKernel.ENTITY_TYPE)
                    {
                        result = new ConstantEntityNullExpression(source.anchor);
                        measure = 0;
                        return true;
                    }
                    else if (type.dimension > 0 || type.definition.code == TypeCode.Handle || type.definition.code == TypeCode.Interface || type.definition.code == TypeCode.Function || type.definition.code == TypeCode.Coroutine)
                    {
                        result = new ConstantHandleNullExpression(source.anchor, type);
                        measure = 0;
                        return true;
                    }
                }
                else if (st == type)
                {
                    result = source;
                    measure = 0;
                    return true;
                }
                else if (type == RelyKernel.REAL_TYPE)
                {
                    if (st == RelyKernel.INTEGER_TYPE)
                    {
                        if (source.TryEvaluation(out long value))
                        {
                            result = new ConstantRealExpression(source.anchor, value);
                            measure = 0;
                        }
                        else
                        {
                            result = new IntegerToRealExpression(source.anchor, source);
                            measure = 0xff;
                        }
                        return true;
                    }
                }
                else if (type == RelyKernel.REAL2_TYPE)
                {
                    if (st == RelyKernel.REAL3_TYPE)
                    {
                        result = new Real3ToReal2Expression(source.anchor, source);
                        measure = 0xfff;
                        return true;
                    }
                    else if (st == RelyKernel.REAL4_TYPE)
                    {
                        result = new Real4ToReal2Expression(source.anchor, source);
                        measure = 0xffff;
                        return true;
                    }
                }
                else if (type == RelyKernel.REAL3_TYPE)
                {
                    if (st == RelyKernel.REAL2_TYPE)
                    {
                        result = new Real2ToReal3Expression(source.anchor, source);
                        measure = 0xfff;
                        return true;
                    }
                    else if (st == RelyKernel.REAL4_TYPE)
                    {
                        result = new Real4ToReal3Expression(source.anchor, source);
                        measure = 0xfff;
                        return true;
                    }
                }
                else if (type == RelyKernel.REAL4_TYPE)
                {
                    if (st == RelyKernel.REAL2_TYPE)
                    {
                        result = new Real2ToReal4Expression(source.anchor, source);
                        measure = 0xffff;
                        return true;
                    }
                    else if (st == RelyKernel.REAL3_TYPE)
                    {
                        result = new Real3ToReal4Expression(source.anchor, source);
                        measure = 0xfff;
                        return true;
                    }
                }
                else if (manager.TryGetInherit(type, st, out measure))
                {
                    result = source;
                    return true;
                }
            }
            result = default;
            measure = default;
            return false;
        }
        private bool TrySub(ListSegment<Lexical> lexicals, SplitFlag flag, out int index)
        {
            using (var stack = pool.GetStack<Lexical>())
            {
                for (index = 0; index < lexicals.Count; index++)
                {
                    var lexical = lexicals[index];
                    switch (lexical.type)
                    {
                        case LexicalType.Unknow: goto default;
                        case LexicalType.BracketLeft0:
                        case LexicalType.BracketLeft1:
                        case LexicalType.BracketLeft2:
                            stack.Push(lexical);
                            break;
                        case LexicalType.BracketRight0:
                            if (stack.Count > 0)
                            {
                                var breacket = stack.Pop();
                                if (breacket.type == LexicalType.BracketLeft0 || breacket.type == LexicalType.QuestionInvoke)
                                {
                                    if (flag.ContainAny(SplitFlag.Bracket0) && stack.Count == 0) return true;
                                    break;
                                }
                                else
                                {
                                    exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    exceptions.Add(breacket.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    return false;
                                }
                            }
                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            return false;
                        case LexicalType.BracketRight1:
                            if (stack.Count > 0)
                            {
                                var breacket = stack.Pop();
                                if (breacket.type == LexicalType.BracketLeft1)
                                {
                                    if (flag.ContainAny(SplitFlag.Bracket1) && stack.Count == 0) return true;
                                    break;
                                }
                                else
                                {
                                    exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    exceptions.Add(breacket.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    return false;
                                }
                            }
                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            return false;
                        case LexicalType.BracketRight2:
                            if (stack.Count > 0)
                            {
                                var breacket = stack.Pop();
                                if (breacket.type == LexicalType.BracketLeft2) break;
                                else
                                {
                                    exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    exceptions.Add(breacket.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    return false;
                                }
                            }
                            else if (flag.ContainAny(SplitFlag.Bracket2)) return true;
                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            return false;
                        case LexicalType.Comma:
                            if (stack.Count == 0 && flag.ContainAny(SplitFlag.Comma)) return true;
                            break;
                        case LexicalType.Assignment:
                            if (stack.Count == 0 && flag.ContainAny(SplitFlag.Assignment)) return true;
                            break;
                        case LexicalType.Equals:
                            break;
                        case LexicalType.Lambda:
                            if (stack.Count == 0 && flag.ContainAny(SplitFlag.Lambda)) return true;
                            break;
                        case LexicalType.BitAnd:
                        case LexicalType.LogicAnd:
                            break;
                        case LexicalType.BitAndAssignment: goto case LexicalType.Assignment;
                        case LexicalType.BitOr:
                        case LexicalType.LogicOr:
                            break;
                        case LexicalType.BitOrAssignment: goto case LexicalType.Assignment;
                        case LexicalType.BitXor:
                            break;
                        case LexicalType.BitXorAssignment: goto case LexicalType.Assignment;
                        case LexicalType.Less:
                        case LexicalType.LessEquals:
                        case LexicalType.ShiftLeft:
                            break;
                        case LexicalType.ShiftLeftAssignment: goto case LexicalType.Assignment;
                        case LexicalType.Greater:
                        case LexicalType.GreaterEquals:
                        case LexicalType.ShiftRight:
                            break;
                        case LexicalType.ShiftRightAssignment: goto case LexicalType.Assignment;
                        case LexicalType.Plus:
                        case LexicalType.Increment:
                            break;
                        case LexicalType.PlusAssignment: goto case LexicalType.Assignment;
                        case LexicalType.Minus:
                        case LexicalType.Decrement:
                            break;
                        case LexicalType.MinusAssignment: goto case LexicalType.Assignment;
                        case LexicalType.Mul:
                            break;
                        case LexicalType.MulAssignment: goto case LexicalType.Assignment;
                        case LexicalType.Div:
                            break;
                        case LexicalType.DivAssignment: goto case LexicalType.Assignment;
                        case LexicalType.Annotation: goto default;
                        case LexicalType.Mod:
                            break;
                        case LexicalType.ModAssignment: goto case LexicalType.Assignment;
                        case LexicalType.Not:
                        case LexicalType.NotEquals:
                        case LexicalType.Negate:
                        case LexicalType.Dot:
                            break;
                        case LexicalType.Question:
                            if (stack.Count == 0 && flag.ContainAny(SplitFlag.Question)) return true;
                            else stack.Push(lexical);
                            break;
                        case LexicalType.QuestionDot:
                            break;
                        case LexicalType.QuestionInvoke:
                            stack.Push(lexical);
                            break;
                        case LexicalType.Colon:
                            if (stack.Count > 0)
                            {
                                var question = stack.Pop();
                                if (question.type == LexicalType.Question) break;
                                else
                                {
                                    exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    exceptions.Add(question.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    return false;
                                }
                            }
                            else if (flag.ContainAny(SplitFlag.Colon)) return true;
                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            return false;
                        case LexicalType.ConstReal:
                        case LexicalType.ConstNumber:
                        case LexicalType.ConstBinary:
                        case LexicalType.ConstHexadecimal:
                        case LexicalType.ConstChars:
                        case LexicalType.ConstString:
                        case LexicalType.Word:
                            break;
                        case LexicalType.Backslash:
                        default:
                            exceptions.Add(lexicals[index].anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
                            return false;
                    }
                }
                while (stack.Count > 0)
                    exceptions.Add(stack.Pop().anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                return false;
            }
        }
        private bool TryParseLambda(ListSegment<Lexical> lexicals, int lambdaIndex, out Expression result)
        {
            if (lambdaIndex < lexicals.Count)
            {
                if (destructor) exceptions.Add(lexicals, CompilingExceptionCode.SYNTAX_DESTRUCTOR_ALLOC);
                using (var parameters = pool.GetList<Anchor>())
                {
                    var index = 0;
                    while (index < lambdaIndex)
                    {
                        var lexical = lexicals[index];
                        if (lexical.type == LexicalType.Word)
                        {
                            if (KeyWorld.IsKeyWorld(lexical.anchor.Segment)) exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
                            parameters.Add(lexical.anchor);
                        }
                        index++;
                        if (index >= lambdaIndex) break;
                        else if (lexical.type != LexicalType.Comma)
                        {
                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                            result = default;
                            return false;
                        }
                        index++;
                    }
                    if (lambdaIndex + 1 >= lexicals.Count) exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                    result = new BlurryLambdaExpression(GetAnchor(lexicals), parameters.ToArray(), lexicals[lambdaIndex + 1, -1]);
                    return true;
                }
            }
            else
            {
                result = default;
                return false;
            }
        }
        private bool TryParseAssignment(ListSegment<Lexical> lexicals, int assignmentIndex, out Expression result)
        {
            if (assignmentIndex > 0 && assignmentIndex < lexicals.Count - 1)
            {
                if (TryParseTuple(lexicals[0, assignmentIndex - 1], out var leftTuple) && TryParseTuple(lexicals[assignmentIndex + 1, -1], out var rightTuple))
                {
                    var attribute = TokenAttribute.Assignable;
                    foreach (var item in leftTuple) attribute &= item.Attribute;
                    if (attribute.ContainAny(TokenAttribute.Assignable))
                    {
                        var assignment = lexicals[assignmentIndex];
                        var leftReturnCount = 0;
                        for (int i = 0, ri = 0, rti = 0; i < leftTuple.Length; i++)
                        {
                            if (leftTuple[i] is BlurryVariableDeclarationExpression)
                            {
                                while (ri < rightTuple.Length)
                                    if (rti < rightTuple[ri].returns.Length) break;
                                    else
                                    {
                                        rti = 0;
                                        ri++;
                                    }
                                if (ri < rightTuple.Length)
                                {
                                    var rt = rightTuple[ri].returns[rti];
                                    if (rt != RelyKernel.NULL_TYPE && rt != RelyKernel.BLURRY_TYPE) leftTuple[i] = new VariableLocalExpression(localContext.AddLocal(leftTuple[i].anchor, rt), TokenAttribute.Assignable);
                                    else
                                    {
                                        exceptions.Add(leftTuple[i].anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
                                        result = default;
                                        return false;
                                    }
                                }
                            }
                            leftReturnCount += leftTuple[i].returns.Length;
                            var lrc = leftTuple[i].returns.Length;
                            while (lrc-- > 0)
                            {
                                while (ri < rightTuple.Length)
                                    if (rti < rightTuple[ri].returns.Length)
                                    {
                                        rti++;
                                        break;
                                    }
                                    else
                                    {
                                        rti = 0;
                                        ri++;
                                    }
                                if (ri >= rightTuple.Length)
                                {
                                    exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    result = default;
                                    return false;
                                }
                            }
                        }
                        var rightReturnCount = 0;
                        foreach (var item in rightTuple) rightReturnCount += item.returns.Length;
                        if (leftReturnCount == rightReturnCount)
                        {
                            if (leftReturnCount == 1)
                            {
                                var left = TupleExpression.Combine(leftTuple);
                                var right = TupleExpression.Combine(rightTuple);
                                var lrt = left.returns[0];
                                var rrt = right.returns[0];
                                switch (assignment.type)
                                {
                                    case LexicalType.Assignment:
                                        if (TryAssignmentConvert(right, left.returns[0], out right, out _))
                                        {
                                            if (left is BlurryVariableDeclarationExpression) left = new VariableLocalExpression(localContext.AddLocal(left.anchor, right.returns[0]), TokenAttribute.Assignable);
                                            result = new VariableAssignmentExpression(assignment.anchor, left, right, left.returns[0]);
                                            return true;
                                        }
                                        exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    #region 运算
                                    case LexicalType.BitAndAssignment:
                                        if (lrt == RelyKernel.BOOL_TYPE && rrt == RelyKernel.BOOL_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.BOOL_And, left, right, RelyKernel.BOOL_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        else if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.INTEGER_And, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    case LexicalType.BitOrAssignment:
                                        if (lrt == RelyKernel.BOOL_TYPE && rrt == RelyKernel.BOOL_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.BOOL_Or, left, right, RelyKernel.BOOL_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        else if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.INTEGER_Or, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    case LexicalType.BitXorAssignment:
                                        if (lrt == RelyKernel.BOOL_TYPE && rrt == RelyKernel.BOOL_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.BOOL_Xor, left, right, RelyKernel.BOOL_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        else if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.INTEGER_Xor, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    case LexicalType.ShiftLeftAssignment:
                                        if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.INTEGER_LeftShift, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    case LexicalType.ShiftRightAssignment:
                                        if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.INTEGER_RightShift, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    case LexicalType.PlusAssignment:
                                        if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.INTEGER_Plus, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        else if (lrt == RelyKernel.REAL_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL_Plus, left, right, RelyKernel.REAL_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL2_TYPE)
                                        {
                                            if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new Real3ToReal2Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL2_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new Real4ToReal2Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL2_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL2_Plus, left, right, RelyKernel.REAL2_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL3_TYPE)
                                        {
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new Real2ToReal3Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL3_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new Real4ToReal3Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL3_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL3_Plus, left, right, RelyKernel.REAL3_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL4_TYPE)
                                        {
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new Real2ToReal4Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL4_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new Real3ToReal4Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL4_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL4_Plus, left, right, RelyKernel.REAL4_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.STRING_TYPE && rrt == RelyKernel.STRING_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.STRING_Combine, left, right, RelyKernel.STRING_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    case LexicalType.MinusAssignment:
                                        if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.INTEGER_Minus, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        else if (lrt == RelyKernel.REAL_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL_Minus, left, right, RelyKernel.REAL_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL2_TYPE)
                                        {
                                            if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new Real3ToReal2Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL2_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new Real4ToReal2Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL2_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL2_Minus, left, right, RelyKernel.REAL2_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL3_TYPE)
                                        {
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new Real2ToReal3Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL3_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new Real4ToReal3Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL3_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL3_Minus, left, right, RelyKernel.REAL3_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL4_TYPE)
                                        {
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new Real2ToReal4Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL4_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new Real3ToReal4Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL4_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL4_Minus, left, right, RelyKernel.REAL4_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    case LexicalType.MulAssignment:
                                        if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.INTEGER_Multiply, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        else if (lrt == RelyKernel.REAL_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL_Multiply, left, right, RelyKernel.REAL_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL2_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL2_Multiply_vr, left, right, RelyKernel.REAL2_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                            if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new Real3ToReal2Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL2_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new Real4ToReal2Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL2_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL2_Multiply_vv, left, right, RelyKernel.REAL2_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL3_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL3_Multiply_vr, left, right, RelyKernel.REAL3_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new Real2ToReal3Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL3_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new Real4ToReal3Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL3_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL3_Multiply_vv, left, right, RelyKernel.REAL3_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL4_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL4_Multiply_vr, left, right, RelyKernel.REAL4_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new Real2ToReal4Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL4_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new Real3ToReal4Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL4_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL4_Multiply_vv, left, right, RelyKernel.REAL4_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    case LexicalType.DivAssignment:
                                        if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.INTEGER_Divide, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        else if (lrt == RelyKernel.REAL_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL_Divide, left, right, RelyKernel.REAL_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL2_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL2_Divide_vr, left, right, RelyKernel.REAL2_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                            if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new Real3ToReal2Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL2_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new Real4ToReal2Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL2_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL2_Divide_vv, left, right, RelyKernel.REAL2_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL3_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL3_Divide_vr, left, right, RelyKernel.REAL3_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                            else if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new Real4ToReal3Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL3_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL3_Divide_vv, left, right, RelyKernel.REAL3_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL4_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL4_Divide_vr, left, right, RelyKernel.REAL4_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                            if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL4_Divide_vv, left, right, RelyKernel.REAL4_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    case LexicalType.ModAssignment:
                                        if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new BinaryOperationExpression(assignment.anchor, CommandMacro.INTEGER_Mod, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        else if (lrt == RelyKernel.REAL_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL_Mod, left, right, RelyKernel.REAL_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL2_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL2_Mod_vr, left, right, RelyKernel.REAL2_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                            if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new Real3ToReal2Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL2_TYPE;
                                            }
                                            else if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new Real4ToReal2Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL2_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL2_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL2_Mod_vv, left, right, RelyKernel.REAL2_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL3_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL3_Mod_vr, left, right, RelyKernel.REAL3_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                            else if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new Real4ToReal3Expression(right.anchor, right);
                                                rrt = RelyKernel.REAL3_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL3_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL3_Mod_vv, left, right, RelyKernel.REAL3_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        else if (lrt == RelyKernel.REAL4_TYPE)
                                        {
                                            if (rrt == RelyKernel.INTEGER_TYPE)
                                            {
                                                if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                                                else right = new IntegerToRealExpression(right.anchor, right);
                                                rrt = RelyKernel.REAL_TYPE;
                                            }
                                            if (rrt == RelyKernel.REAL_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL4_Mod_vr, left, right, RelyKernel.REAL4_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                            if (rrt == RelyKernel.REAL4_TYPE)
                                            {
                                                right = new BinaryOperationExpression(assignment.anchor, CommandMacro.REAL4_Mod_vv, left, right, RelyKernel.REAL4_TYPE);
                                                goto case LexicalType.Assignment;
                                            }
                                        }
                                        exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        break;
                                    #endregion
                                    default: throw ExceptionGeneratorCompiler.InvalidLexicalType(assignment.type);
                                }
                            }
                            else if (assignment.type == LexicalType.Assignment)
                            {
                                var leftReturns = new CompilingType[leftReturnCount];
                                var leftReturnIndex = 0;
                                foreach (var expression in leftTuple)
                                    foreach (var returnType in expression.returns)
                                        leftReturns[leftReturnIndex++] = returnType;
                                if (TryAssignmentConvert(rightTuple, leftReturns, out var right, out _))
                                {
                                    result = new TupleAssignmentExpression(assignment.anchor, TupleExpression.Combine(leftTuple), right, leftReturns);
                                    return true;
                                }
                                else exceptions.Add(lexicals[0, assignmentIndex - 1], CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                            }
                            else exceptions.Add(assignment.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                        }
                        else exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                    }
                    else exceptions.Add(lexicals[0, assignmentIndex - 1], CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                }
            }
            else exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
            result = default;
            return false;
        }
        private bool TryParseQuestion(ListSegment<Lexical> lexicals, int questionIndex, out Expression result)
        {
            if (questionIndex == 0 || questionIndex + 3 >= lexicals.Count) exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
            else if (TryParse(lexicals[0, questionIndex - 1], out var condition))
            {
                lexicals = lexicals[questionIndex + 1, -1];
                if (TrySub(lexicals, SplitFlag.Colon, out var colonIndex))
                {
                    if (condition.returns.Length == 1 && condition.returns[0] == new CompilingType(RelyKernel.BOOL, 0))
                    {
                        if (colonIndex > 0 && colonIndex + 1 < lexicals.Count)
                        {
                            if (TryParse(lexicals[0, colonIndex - 1], out var left) && TryParse(lexicals[colonIndex + 1, -1], out var right))
                            {
                                if (manager.TryGetMeasure(left.returns, right.returns, out _))
                                {
                                    result = new QuestionExpression(GetAnchor(lexicals), condition, left, right, left.returns);
                                    return true;
                                }
                                else if (manager.TryGetMeasure(right.returns, left.returns, out _))
                                {
                                    result = new QuestionExpression(GetAnchor(lexicals), condition, left, right, right.returns);
                                    return true;
                                }
                                else exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                            }
                        }
                        else exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                    }
                    else exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                }
            }
            result = default;
            return false;
        }
        private bool TryParseComma(ListSegment<Lexical> lexicals, int commaIndex, out Expression result)
        {
            if (commaIndex > 0)
            {
                if (commaIndex + 1 == lexicals.Count) return TryParse(lexicals[0, commaIndex - 1], out result);
                else if (TryParse(lexicals[0, commaIndex - 1], out var left) && TryParse(lexicals[commaIndex + 1, -1], out var right))
                {
                    if (IsDecidedTypes(left.returns) && IsDecidedTypes(right.returns))
                    {
                        result = TupleExpression.Combine(left, right);
                        return true;
                    }
                }
            }
            else if (commaIndex + 1 < lexicals.Count) return TryParse(lexicals[1, -1], out result);
            result = default;
            return false;
        }
        private bool TryParseBracket(ListSegment<Lexical> lexicals, SplitFlag flag, ref int index, out Expression[] expressions)
        {
            if (TrySub(lexicals[index, -1], flag, out var bracketIndex))
            {
                if (bracketIndex > 1)
                {
                    if (TryParseTuple(lexicals[index + 1, index + bracketIndex - 1], out expressions))
                    {
                        index += bracketIndex;
                        return true;
                    }
                }
                else
                {
                    expressions = new Expression[0];
                    index += bracketIndex;
                    return true;
                }
            }
            expressions = default;
            return false;
        }
        private bool TryCombineExpressions(out Expression result, params Expression[] expressions)
        {
            if (expressions.Length == 1)
            {
                result = expressions[0];
                return true;
            }
            else if (expressions.Length > 1)
            {
                foreach (var expression in expressions)
                    foreach (var item in expression.returns)
                        if (item == RelyKernel.BLURRY_TYPE || item == RelyKernel.NULL_TYPE)
                        {
                            exceptions.Add(expression.anchor, CompilingExceptionCode.COMPILING_TUPLE_TYPE_EQUIVOCAL);
                            result = default;
                            return false;
                        }
                result = TupleExpression.Combine(expressions);
                return true;
            }
            result = default;
            return false;
        }
        private bool TryPopExpression(ScopeStack<Expression> expressionStack, Anchor anchor, out Expression left, out Expression right)
        {
            if (expressionStack.Count >= 2)
            {
                right = expressionStack.Pop();
                left = expressionStack.Pop();
                if (left.returns.Length == 1 && right.returns.Length == 1)
                {
                    var leftType = left.returns[0];
                    var rightType = right.returns[0];
                    if (leftType == rightType) return true;
                    else if (leftType == RelyKernel.INTEGER_TYPE)
                    {
                        if (rightType == RelyKernel.REAL_TYPE || rightType == RelyKernel.REAL2_TYPE || rightType == RelyKernel.REAL3_TYPE || rightType == RelyKernel.REAL4_TYPE)
                        {
                            if (left.TryEvaluation(out long value)) left = new ConstantRealExpression(left.anchor, value);
                            else left = new IntegerToRealExpression(left.anchor, left);
                            return true;
                        }
                    }
                    else if (leftType == RelyKernel.REAL_TYPE)
                    {
                        if (rightType == RelyKernel.INTEGER_TYPE)
                        {
                            if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                            else right = new IntegerToRealExpression(right.anchor, right);
                            return true;
                        }
                        else return rightType == RelyKernel.REAL2_TYPE || rightType == RelyKernel.REAL3_TYPE || rightType == RelyKernel.REAL4_TYPE;
                    }
                    else if (leftType == RelyKernel.REAL2_TYPE)
                    {
                        if (rightType == RelyKernel.INTEGER_TYPE)
                        {
                            if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                            else right = new IntegerToRealExpression(right.anchor, right);
                            return true;
                        }
                        else if (rightType == RelyKernel.REAL_TYPE) return true;
                        else if (rightType == RelyKernel.REAL3_TYPE)
                        {
                            left = new Real2ToReal3Expression(left.anchor, left);
                            return true;
                        }
                        else if (rightType == RelyKernel.REAL4_TYPE)
                        {
                            left = new Real2ToReal4Expression(left.anchor, left);
                            return true;
                        }
                    }
                    else if (leftType == RelyKernel.REAL3_TYPE)
                    {
                        if (rightType == RelyKernel.INTEGER_TYPE)
                        {
                            if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                            else right = new IntegerToRealExpression(right.anchor, right);
                            return true;
                        }
                        else if (rightType == RelyKernel.REAL_TYPE) return true;
                        else if (rightType == RelyKernel.REAL2_TYPE)
                        {
                            right = new Real2ToReal3Expression(right.anchor, right);
                            return true;
                        }
                        else if (rightType == RelyKernel.REAL4_TYPE)
                        {
                            left = new Real3ToReal4Expression(left.anchor, left);
                            return true;
                        }
                    }
                    else if (leftType == RelyKernel.REAL4_TYPE)
                    {
                        if (rightType == RelyKernel.INTEGER_TYPE)
                        {
                            if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, value);
                            else right = new IntegerToRealExpression(right.anchor, right);
                            return true;
                        }
                        else if (rightType == RelyKernel.REAL_TYPE) return true;
                        else if (rightType == RelyKernel.REAL2_TYPE)
                        {
                            right = new Real2ToReal4Expression(right.anchor, right);
                            return true;
                        }
                        else if (rightType == RelyKernel.REAL3_TYPE)
                        {
                            right = new Real3ToReal4Expression(right.anchor, right);
                            return true;
                        }
                    }
                    else if (leftType == RelyKernel.ENTITY_TYPE)
                    {
                        if (right.TryEvaluationNull())
                        {
                            right = new ConstantEntityNullExpression(right.anchor);
                            return true;
                        }
                    }
                    else if (leftType.IsHandle)
                    {
                        if (rightType.IsHandle) return true;
                        else if (right.TryEvaluationNull())
                        {
                            right = new ConstantHandleNullExpression(right.anchor, leftType);
                            return true;
                        }
                    }
                    else if (left.TryEvaluationNull())
                    {
                        if (rightType == RelyKernel.ENTITY_TYPE)
                        {
                            left = new ConstantEntityNullExpression(left.anchor);
                            return true;
                        }
                        else if (rightType.IsHandle)
                        {
                            left = new ConstantHandleNullExpression(left.anchor, rightType);
                            return true;
                        }
                    }
                }
                exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
            }
            else exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
            left = default;
            right = default;
            return false;
        }
        private bool TryPopExpression(ScopeStack<Expression> expressionStack, Anchor anchor, out Expression expression)
        {
            if (expressionStack.Count > 0)
            {
                expression = expressionStack.Pop();
                if (expression.returns.Length == 1) return true;
                else exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
            }
            else exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
            expression = default;
            return false;
        }
        private TokenAttribute PushOperationExpression(ScopeStack<Expression> expressionStack, Anchor anchor, CommandMacro command, Expression left, Expression right, CompilingType type)
        {
            var expression = new BinaryOperationExpression(anchor, command, left, right, type);
            expressionStack.Push(expression);
            return expression.Attribute;
        }
        private TokenAttribute PushOperationExpression(ScopeStack<Expression> expressionStack, Anchor anchor, CommandMacro command, Expression expression)
        {
            expression = new UnaryOperationExpression(anchor, command, expression);
            expressionStack.Push(expression);
            return expression.Attribute;
        }
        private TokenAttribute PopToken(ScopeStack<Expression> expressionStack, Token token)
        {
            var anchor = token.lexical.anchor;
            switch (token.type)
            {
                case TokenType.LogicAnd:
                    if (TryPopExpression(expressionStack, anchor, out var left, out var right))
                    {
                        if (left.returns[0] == RelyKernel.BOOL_TYPE && right.returns[0] == RelyKernel.BOOL_TYPE)
                        {
                            if (left.TryEvaluation(out bool leftValue))
                            {
                                if (!leftValue)
                                {
                                    var constant = new ConstantBooleanExpression(anchor, false);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else
                                {
                                    expressionStack.Push(right);
                                    return right.Attribute;
                                }
                            }
                            else
                            {
                                var logicExpression = new LogicAndExpression(anchor, left, right);
                                expressionStack.Push(logicExpression);
                                return logicExpression.Attribute;
                            }
                        }
                    }
                    break;
                case TokenType.LogicOr:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.BOOL_TYPE && right.returns[0] == RelyKernel.BOOL_TYPE)
                        {
                            if (left.TryEvaluation(out bool leftValue))
                            {
                                if (leftValue)
                                {
                                    var constant = new ConstantBooleanExpression(anchor, true);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else
                                {
                                    expressionStack.Push(right);
                                    return right.Attribute;
                                }
                            }
                            else
                            {
                                var logicExpression = new LogicOrExpression(anchor, left, right);
                                expressionStack.Push(logicExpression);
                                return logicExpression.Attribute;
                            }
                        }
                        else goto default;
                    }
                    break;
                case TokenType.Less:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue < rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Less, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE && right.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue < rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_Less, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.Greater:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue > rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Grater, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE && right.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue > rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_Grater, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.LessEquals:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue <= rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_LessThanOrEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE && right.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue <= rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_LessThanOrEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.GreaterEquals:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue >= rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_GraterThanOrEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE && right.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue >= rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_GraterThanOrEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.Equals:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.BOOL_TYPE && right.returns[0] == RelyKernel.BOOL_TYPE)
                        {
                            if (left.TryEvaluation(out bool leftValue) && right.TryEvaluation(out bool rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue == rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.BOOL_Equals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue == rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Equals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE && right.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue == rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_Equals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL2_TYPE && right.returns[0] == RelyKernel.REAL2_TYPE)
                        {
                            if (left.TryEvaluation(out Real2 leftValue) && right.TryEvaluation(out Real2 rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue == rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Equals, left, right, RelyKernel.BOOL_TYPE);
                        }

                        else if (left.returns[0] == RelyKernel.REAL3_TYPE && right.returns[0] == RelyKernel.REAL3_TYPE)
                        {
                            if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out Real3 rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue == rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Equals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL4_TYPE && right.returns[0] == RelyKernel.REAL4_TYPE)
                        {
                            if (left.TryEvaluation(out Real4 leftValue) && right.TryEvaluation(out Real4 rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue == rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Equals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.STRING_TYPE && right.returns[0] == RelyKernel.STRING_TYPE)
                        {
                            if (left.TryEvaluation(out string leftValue) && right.TryEvaluation(out string rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue == rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.STRING_Equals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.ENTITY_TYPE && right.returns[0] == RelyKernel.ENTITY_TYPE)
                        {
                            if (left.TryEvaluationNull() && right.TryEvaluationNull())
                            {
                                var constant = new ConstantBooleanExpression(anchor, true);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.ENTITY_Equals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0].IsHandle && right.returns[0].IsHandle)
                        {
                            if (left.TryEvaluationNull() && right.TryEvaluationNull())
                            {
                                var constant = new ConstantBooleanExpression(anchor, true);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else if (left.returns[0].dimension == 0 && left.returns[0].definition.code == TypeCode.Function && right.returns[0].dimension == 0 && right.returns[0].definition.code == TypeCode.Function) return PushOperationExpression(expressionStack, anchor, CommandMacro.DELEGATE_Equals, left, right, RelyKernel.BOOL_TYPE);
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.HANDLE_Equals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.NotEquals:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.BOOL_TYPE && right.returns[0] == RelyKernel.BOOL_TYPE)
                        {
                            if (left.TryEvaluation(out bool leftValue) && right.TryEvaluation(out bool rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue != rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.BOOL_NotEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue != rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_NotEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE && right.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue != rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_NotEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL2_TYPE && right.returns[0] == RelyKernel.REAL2_TYPE)
                        {
                            if (left.TryEvaluation(out Real2 leftValue) && right.TryEvaluation(out Real2 rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue != rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_NotEquals, left, right, RelyKernel.BOOL_TYPE);
                        }

                        else if (left.returns[0] == RelyKernel.REAL3_TYPE && right.returns[0] == RelyKernel.REAL3_TYPE)
                        {
                            if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out Real3 rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue != rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_NotEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL4_TYPE && right.returns[0] == RelyKernel.REAL4_TYPE)
                        {
                            if (left.TryEvaluation(out Real4 leftValue) && right.TryEvaluation(out Real4 rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue != rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_NotEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.STRING_TYPE && right.returns[0] == RelyKernel.STRING_TYPE)
                        {
                            if (left.TryEvaluation(out string leftValue) && right.TryEvaluation(out string rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue != rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.STRING_NotEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.ENTITY_TYPE && right.returns[0] == RelyKernel.ENTITY_TYPE)
                        {
                            if (left.TryEvaluationNull() && right.TryEvaluationNull())
                            {
                                var constant = new ConstantBooleanExpression(anchor, false);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.ENTITY_NotEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0].IsHandle && right.returns[0].IsHandle)
                        {
                            if (left.TryEvaluationNull() && right.TryEvaluationNull())
                            {
                                var constant = new ConstantBooleanExpression(anchor, false);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else if (left.returns[0].dimension == 0 && left.returns[0].definition.code == TypeCode.Function && right.returns[0].dimension == 0 && right.returns[0].definition.code == TypeCode.Function) return PushOperationExpression(expressionStack, anchor, CommandMacro.DELEGATE_NotEquals, left, right, RelyKernel.BOOL_TYPE);
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.HANDLE_NotEquals, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.BitAnd:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.BOOL_TYPE && right.returns[0] == RelyKernel.BOOL_TYPE)
                        {
                            if (left.TryEvaluation(out bool leftValue) && right.TryEvaluation(out bool rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue & rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.BOOL_And, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantIntegerExpression(anchor, leftValue & rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_And, left, right, RelyKernel.INTEGER_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.BitOr:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.BOOL_TYPE && right.returns[0] == RelyKernel.BOOL_TYPE)
                        {
                            if (left.TryEvaluation(out bool leftValue) && right.TryEvaluation(out bool rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue | rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.BOOL_Or, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantIntegerExpression(anchor, leftValue | rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Or, left, right, RelyKernel.INTEGER_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.BitXor:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.BOOL_TYPE && right.returns[0] == RelyKernel.BOOL_TYPE)
                        {
                            if (left.TryEvaluation(out bool leftValue) && right.TryEvaluation(out bool rightValue))
                            {
                                var constant = new ConstantBooleanExpression(anchor, leftValue ^ rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.BOOL_Xor, left, right, RelyKernel.BOOL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantIntegerExpression(anchor, leftValue ^ rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Xor, left, right, RelyKernel.INTEGER_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.ShiftLeft:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantIntegerExpression(anchor, leftValue << (int)rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_LeftShift, left, right, RelyKernel.INTEGER_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.ShiftRight:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantIntegerExpression(anchor, leftValue >> (int)rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_RightShift, left, right, RelyKernel.INTEGER_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.Plus:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantIntegerExpression(anchor, leftValue + rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Plus, left, right, RelyKernel.INTEGER_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE && right.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                            {
                                var constant = new ConstantRealExpression(anchor, leftValue + rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_Plus, left, right, RelyKernel.REAL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL2_TYPE && right.returns[0] == RelyKernel.REAL2_TYPE)
                        {
                            if (left.TryEvaluation(out Real2 leftValue) && right.TryEvaluation(out Real2 rightValue))
                            {
                                var constant = new ConstantReal2Expression(anchor, leftValue + rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Plus, left, right, RelyKernel.REAL2_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL3_TYPE && right.returns[0] == RelyKernel.REAL3_TYPE)
                        {
                            if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out Real3 rightValue))
                            {
                                var constant = new ConstantReal3Expression(anchor, leftValue + rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Plus, left, right, RelyKernel.REAL3_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL4_TYPE && right.returns[0] == RelyKernel.REAL4_TYPE)
                        {
                            if (left.TryEvaluation(out Real4 leftValue) && right.TryEvaluation(out Real4 rightValue))
                            {
                                var constant = new ConstantReal4Expression(anchor, leftValue + rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Plus, left, right, RelyKernel.REAL4_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.STRING_TYPE && right.returns[0] == RelyKernel.STRING_TYPE)
                        {
                            if (left.TryEvaluation(out string leftValue) && right.TryEvaluation(out string rightValue))
                            {
                                var constant = new ConstantStringExpression(anchor, leftValue + rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.STRING_Combine, left, right, RelyKernel.STRING_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.Minus:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantIntegerExpression(anchor, leftValue - rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Minus, left, right, RelyKernel.INTEGER_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE && right.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                            {
                                var constant = new ConstantRealExpression(anchor, leftValue - rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_Minus, left, right, RelyKernel.REAL_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL2_TYPE && right.returns[0] == RelyKernel.REAL2_TYPE)
                        {
                            if (left.TryEvaluation(out Real2 leftValue) && right.TryEvaluation(out Real2 rightValue))
                            {
                                var constant = new ConstantReal2Expression(anchor, leftValue - rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Minus, left, right, RelyKernel.REAL2_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL3_TYPE && right.returns[0] == RelyKernel.REAL3_TYPE)
                        {
                            if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out Real3 rightValue))
                            {
                                var constant = new ConstantReal3Expression(anchor, leftValue - rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Minus, left, right, RelyKernel.REAL3_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL4_TYPE && right.returns[0] == RelyKernel.REAL4_TYPE)
                        {
                            if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out Real3 rightValue))
                            {
                                var constant = new ConstantReal3Expression(anchor, leftValue - rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Minus, left, right, RelyKernel.REAL4_TYPE);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.Mul:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                var constant = new ConstantIntegerExpression(anchor, leftValue * rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Multiply, left, right, RelyKernel.INTEGER_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    var constant = new ConstantRealExpression(anchor, leftValue * rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_Multiply, left, right, RelyKernel.REAL_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL2_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out Real2 rightValue))
                                {
                                    var constant = new ConstantReal2Expression(anchor, leftValue * rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Multiply_rv, left, right, RelyKernel.REAL2_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL3_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out Real3 rightValue))
                                {
                                    var constant = new ConstantReal3Expression(anchor, leftValue * rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Multiply_rv, left, right, RelyKernel.REAL3_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL4_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out Real4 rightValue))
                                {
                                    var constant = new ConstantReal4Expression(anchor, leftValue * rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Multiply_rv, left, right, RelyKernel.REAL4_TYPE);
                            }
                        }
                        else if (left.returns[0] == RelyKernel.REAL2_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out Real2 leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    var constant = new ConstantReal2Expression(anchor, leftValue * rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Multiply_vr, left, right, RelyKernel.REAL2_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL2_TYPE)
                            {
                                if (left.TryEvaluation(out Real2 leftValue) && right.TryEvaluation(out Real2 rightValue))
                                {
                                    var constant = new ConstantReal2Expression(anchor, leftValue * rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Multiply_vv, left, right, RelyKernel.REAL2_TYPE);
                            }
                        }
                        else if (left.returns[0] == RelyKernel.REAL3_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    var constant = new ConstantReal3Expression(anchor, leftValue * rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Multiply_vr, left, right, RelyKernel.REAL3_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL3_TYPE)
                            {
                                if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out Real3 rightValue))
                                {
                                    var constant = new ConstantReal3Expression(anchor, leftValue * rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Multiply_vv, left, right, RelyKernel.REAL3_TYPE);
                            }
                        }
                        else if (left.returns[0] == RelyKernel.REAL4_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out Real4 leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    var constant = new ConstantReal4Expression(anchor, leftValue * rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Multiply_vr, left, right, RelyKernel.REAL4_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL4_TYPE)
                            {
                                if (left.TryEvaluation(out Real4 leftValue) && right.TryEvaluation(out Real4 rightValue))
                                {
                                    var constant = new ConstantReal4Expression(anchor, leftValue * rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Multiply_vv, left, right, RelyKernel.REAL4_TYPE);
                            }
                        }
                        goto default;
                    }
                    break;
                case TokenType.Div:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                if (rightValue == 0)
                                {
                                    exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                    expressionStack.Push(left);
                                    return left.Attribute;
                                }
                                var constant = new ConstantIntegerExpression(anchor, leftValue / rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Divide, left, right, RelyKernel.INTEGER_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    if (rightValue == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantRealExpression(anchor, leftValue / rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_Divide, left, right, RelyKernel.REAL_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL2_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out Real2 rightValue))
                                {
                                    if (rightValue.x * rightValue.y == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal2Expression(anchor, leftValue / rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Divide_rv, left, right, RelyKernel.REAL2_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL3_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out Real3 rightValue))
                                {
                                    if (rightValue.x * rightValue.y * rightValue.z == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal3Expression(anchor, leftValue / rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Divide_rv, left, right, RelyKernel.REAL3_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL4_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out Real4 rightValue))
                                {
                                    if (rightValue.x * rightValue.y * rightValue.z * rightValue.w == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal4Expression(anchor, leftValue / rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Divide_rv, left, right, RelyKernel.REAL4_TYPE);
                            }
                        }
                        else if (left.returns[0] == RelyKernel.REAL2_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out Real2 leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    if (rightValue == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal2Expression(anchor, leftValue / rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Divide_vr, left, right, RelyKernel.REAL2_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL2_TYPE)
                            {
                                if (left.TryEvaluation(out Real2 leftValue) && right.TryEvaluation(out Real2 rightValue))
                                {
                                    if (rightValue.x * rightValue.y == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal2Expression(anchor, leftValue / rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Divide_vv, left, right, RelyKernel.REAL2_TYPE);
                            }
                        }
                        else if (left.returns[0] == RelyKernel.REAL3_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    if (rightValue == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal3Expression(anchor, leftValue / rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Divide_vr, left, right, RelyKernel.REAL3_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL3_TYPE)
                            {
                                if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out Real3 rightValue))
                                {
                                    if (rightValue.x * rightValue.y * rightValue.z == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal3Expression(anchor, leftValue / rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Divide_vv, left, right, RelyKernel.REAL3_TYPE);
                            }
                        }
                        else if (left.returns[0] == RelyKernel.REAL4_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out Real4 leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    if (rightValue == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal4Expression(anchor, leftValue / rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Divide_vr, left, right, RelyKernel.REAL4_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL4_TYPE)
                            {
                                if (left.TryEvaluation(out Real4 leftValue) && right.TryEvaluation(out Real4 rightValue))
                                {
                                    if (rightValue.x * rightValue.y * rightValue.z * rightValue.w == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal4Expression(anchor, leftValue / rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Divide_vv, left, right, RelyKernel.REAL4_TYPE);
                            }
                        }
                        goto default;
                    }
                    break;
                case TokenType.Mod:
                    if (TryPopExpression(expressionStack, anchor, out left, out right))
                    {
                        if (left.returns[0] == RelyKernel.INTEGER_TYPE && right.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (left.TryEvaluation(out long leftValue) && right.TryEvaluation(out long rightValue))
                            {
                                if (rightValue == 0)
                                {
                                    exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                    expressionStack.Push(left);
                                    return left.Attribute;
                                }
                                var constant = new ConstantIntegerExpression(anchor, leftValue % rightValue);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Mod, left, right, RelyKernel.INTEGER_TYPE);
                        }
                        else if (left.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    if (rightValue == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantRealExpression(anchor, leftValue % rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_Mod, left, right, RelyKernel.REAL_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL2_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out Real2 rightValue))
                                {
                                    if (rightValue.x * rightValue.y == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal2Expression(anchor, leftValue % rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Mod_rv, left, right, RelyKernel.REAL2_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL3_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out Real3 rightValue))
                                {
                                    if (rightValue.x * rightValue.y * rightValue.z == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal3Expression(anchor, leftValue % rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Mod_rv, left, right, RelyKernel.REAL3_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL4_TYPE)
                            {
                                if (left.TryEvaluation(out real leftValue) && right.TryEvaluation(out Real4 rightValue))
                                {
                                    if (rightValue.x * rightValue.y * rightValue.z * rightValue.w == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal4Expression(anchor, leftValue % rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Mod_rv, left, right, RelyKernel.REAL4_TYPE);
                            }
                        }
                        else if (left.returns[0] == RelyKernel.REAL2_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out Real2 leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    if (rightValue == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal2Expression(anchor, leftValue % rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Mod_vr, left, right, RelyKernel.REAL2_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL2_TYPE)
                            {
                                if (left.TryEvaluation(out Real2 leftValue) && right.TryEvaluation(out Real2 rightValue))
                                {
                                    if (rightValue.x * rightValue.y == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal2Expression(anchor, leftValue % rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Mod_vv, left, right, RelyKernel.REAL2_TYPE);
                            }
                        }
                        else if (left.returns[0] == RelyKernel.REAL3_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    if (rightValue == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal3Expression(anchor, leftValue % rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Mod_vr, left, right, RelyKernel.REAL3_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL3_TYPE)
                            {
                                if (left.TryEvaluation(out Real3 leftValue) && right.TryEvaluation(out Real3 rightValue))
                                {
                                    if (rightValue.x * rightValue.y * rightValue.z == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal3Expression(anchor, leftValue % rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Mod_vv, left, right, RelyKernel.REAL3_TYPE);
                            }
                        }
                        else if (left.returns[0] == RelyKernel.REAL4_TYPE)
                        {
                            if (right.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                if (left.TryEvaluation(out Real4 leftValue) && right.TryEvaluation(out real rightValue))
                                {
                                    if (rightValue == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal4Expression(anchor, leftValue % rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Mod_vr, left, right, RelyKernel.REAL4_TYPE);
                            }
                            else if (right.returns[0] == RelyKernel.REAL4_TYPE)
                            {
                                if (left.TryEvaluation(out Real4 leftValue) && right.TryEvaluation(out Real4 rightValue))
                                {
                                    if (rightValue.x * rightValue.y * rightValue.z * rightValue.w == 0)
                                    {
                                        exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_DIVIDE_BY_ZERO);
                                        expressionStack.Push(left);
                                        return left.Attribute;
                                    }
                                    var constant = new ConstantReal4Expression(anchor, leftValue % rightValue);
                                    expressionStack.Push(constant);
                                    return constant.Attribute;
                                }
                                else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Mod_vv, left, right, RelyKernel.REAL4_TYPE);
                            }
                        }
                        goto default;
                    }
                    break;
                case TokenType.Not:
                    if (TryPopExpression(expressionStack, anchor, out var expression))
                    {
                        if (expression.returns[0] == RelyKernel.BOOL_TYPE)
                        {
                            if (expression.TryEvaluation(out bool value))
                            {
                                var constant = new ConstantBooleanExpression(anchor, !value);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.BOOL_Not, expression);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.Inverse:
                    if (TryPopExpression(expressionStack, anchor, out expression))
                    {
                        if (expression.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (expression.TryEvaluation(out long value))
                            {
                                var constant = new ConstantIntegerExpression(anchor, ~value);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Inverse, expression);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.Positive:
                    if (TryPopExpression(expressionStack, anchor, out expression))
                    {
                        var type = expression.returns[0];
                        if (type == RelyKernel.INTEGER_TYPE || type == RelyKernel.REAL_TYPE || type == RelyKernel.REAL2_TYPE || type == RelyKernel.REAL3_TYPE || type == RelyKernel.REAL4_TYPE)
                        {
                            expressionStack.Push(expression);
                            return expression.Attribute;
                        }
                        else goto default;
                    }
                    break;
                case TokenType.Negative:
                    if (TryPopExpression(expressionStack, anchor, out expression))
                    {
                        if (expression.returns[0] == RelyKernel.INTEGER_TYPE)
                        {
                            if (expression.TryEvaluation(out long value))
                            {
                                var constant = new ConstantIntegerExpression(anchor, -value);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.INTEGER_Negative, expression);
                        }
                        else if (expression.returns[0] == RelyKernel.REAL_TYPE)
                        {
                            if (expression.TryEvaluation(out real value))
                            {
                                var constant = new ConstantRealExpression(anchor, -value);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL_Negative, expression);
                        }
                        else if (expression.returns[0] == RelyKernel.REAL2_TYPE)
                        {
                            if (expression.TryEvaluation(out Real2 value))
                            {
                                var constant = new ConstantReal2Expression(anchor, -value);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL2_Negative, expression);
                        }
                        else if (expression.returns[0] == RelyKernel.REAL3_TYPE)
                        {
                            if (expression.TryEvaluation(out Real3 value))
                            {
                                var constant = new ConstantReal3Expression(anchor, -value);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL3_Negative, expression);
                        }
                        else if (expression.returns[0] == RelyKernel.REAL4_TYPE)
                        {
                            if (expression.TryEvaluation(out Real4 value))
                            {
                                var constant = new ConstantReal4Expression(anchor, -value);
                                expressionStack.Push(constant);
                                return constant.Attribute;
                            }
                            else return PushOperationExpression(expressionStack, anchor, CommandMacro.REAL4_Negative, expression);
                        }
                        else goto default;
                    }
                    break;
                case TokenType.IncrementLeft:
                    if (TryPopExpression(expressionStack, anchor, out expression))
                    {
                        if (expression is VariableExpression variable)
                        {
                            if (expression.returns[0] == RelyKernel.INTEGER_TYPE)
                            {
                                expression = new OperationPrevIncrementExpression(anchor, CommandMacro.INTEGER_Increment, variable);
                                expressionStack.Push(expression);
                                return expression.Attribute;
                            }
                            else if (expression.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                expression = new OperationPrevIncrementExpression(anchor, CommandMacro.REAL_Increment, variable);
                                expressionStack.Push(expression);
                                return expression.Attribute;
                            }
                            else goto default;
                        }
                        else exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                    }
                    break;
                case TokenType.DecrementLeft:
                    if (TryPopExpression(expressionStack, anchor, out expression))
                    {
                        if (expression is VariableExpression variable)
                        {
                            if (expression.returns[0] == RelyKernel.INTEGER_TYPE)
                            {
                                expression = new OperationPrevIncrementExpression(anchor, CommandMacro.INTEGER_Decrement, variable);
                                expressionStack.Push(expression);
                                return expression.Attribute;
                            }
                            else if (expression.returns[0] == RelyKernel.REAL_TYPE)
                            {
                                expression = new OperationPrevIncrementExpression(anchor, CommandMacro.REAL_Decrement, variable);
                                expressionStack.Push(expression);
                                return expression.Attribute;
                            }
                            else goto default;
                        }
                        else exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                    }
                    break;
                case TokenType.Casting:
                    if (expressionStack.Count >= 2)
                    {
                        right = expressionStack.Pop();
                        var typeExpression = (TypeExpression)expressionStack.Pop();
                        if (right.returns.Length == 1)
                        {
                            if (typeExpression.type == right.returns[0])
                            {
                                expressionStack.Push(right);
                                return right.Attribute;
                            }
                            else if (typeExpression.type == RelyKernel.INTEGER_TYPE)
                            {
                                if (right.returns[0] == RelyKernel.REAL_TYPE)
                                {
                                    if (right.TryEvaluation(out real value)) right = new ConstantIntegerExpression(right.anchor, (long)value);
                                    else right = new RealToIntegerExpression(right.anchor, right);
                                    expressionStack.Push(right);
                                    return right.Attribute;
                                }
                            }
                            else if (typeExpression.type == RelyKernel.REAL_TYPE)
                            {
                                if (right.returns[0] == RelyKernel.INTEGER_TYPE)
                                {
                                    if (right.TryEvaluation(out long value)) right = new ConstantRealExpression(right.anchor, (real)value);
                                    else right = new IntegerToRealExpression(right.anchor, right);
                                    expressionStack.Push(right);
                                    return right.Attribute;
                                }
                            }
                            else if (typeExpression.type == RelyKernel.REAL2_TYPE)
                            {
                                if (right.returns[0] == RelyKernel.REAL3_TYPE)
                                {
                                    if (right.TryEvaluation(out Real3 value)) right = new ConstantReal2Expression(right.anchor, (Real2)value);
                                    else right = new Real2ToReal3Expression(right.anchor, right);
                                    expressionStack.Push(right);
                                    return right.Attribute;
                                }
                                else if (right.returns[0] == RelyKernel.REAL4_TYPE)
                                {
                                    if (right.TryEvaluation(out Real4 value)) right = new ConstantReal2Expression(right.anchor, (Real2)value);
                                    else right = new Real2ToReal4Expression(right.anchor, right);
                                    expressionStack.Push(right);
                                    return right.Attribute;
                                }
                            }
                            else if (typeExpression.type == RelyKernel.REAL3_TYPE)
                            {
                                if (right.returns[0] == RelyKernel.REAL2_TYPE)
                                {
                                    if (right.TryEvaluation(out Real2 value)) right = new ConstantReal3Expression(right.anchor, (Real3)value);
                                    else right = new Real3ToReal2Expression(right.anchor, right);
                                    expressionStack.Push(right);
                                    return right.Attribute;
                                }
                                else if (right.returns[0] == RelyKernel.REAL4_TYPE)
                                {
                                    if (right.TryEvaluation(out Real4 value)) right = new ConstantReal3Expression(right.anchor, (Real3)value);
                                    else right = new Real3ToReal4Expression(right.anchor, right);
                                    expressionStack.Push(right);
                                    return right.Attribute;
                                }
                            }
                            else if (typeExpression.type == RelyKernel.REAL4_TYPE)
                            {
                                if (right.returns[0] == RelyKernel.REAL2_TYPE)
                                {
                                    if (right.TryEvaluation(out Real2 value)) right = new ConstantReal4Expression(right.anchor, (Real4)value);
                                    else right = new Real4ToReal2Expression(right.anchor, right);
                                    expressionStack.Push(right);
                                    return right.Attribute;
                                }
                                else if (right.returns[0] == RelyKernel.REAL3_TYPE)
                                {
                                    if (right.TryEvaluation(out Real3 value)) right = new ConstantReal4Expression(right.anchor, (Real4)value);
                                    else right = new Real4ToReal3Expression(right.anchor, right);
                                    expressionStack.Push(right);
                                    return right.Attribute;
                                }
                            }
                            else if (typeExpression.type.IsHandle && right.returns[0].IsHandle)
                            {
                                var castExpression = new CastHandleExpression(anchor, right, typeExpression.type);
                                expressionStack.Push(castExpression);
                                return castExpression.Attribute;
                            }
                        }
                        goto default;
                    }
                    break;
                default:
                    exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                    break;
            }
            return TokenAttribute.None;
        }
        private void PushToken(ScopeStack<Expression> expressionStack, ScopeStack<Token> tokenStack, Token token, TokenAttribute attribute)
        {
            while (tokenStack.Count > 0 && token.priority <= tokenStack.Peek().priority)
                attribute = PopToken(expressionStack, tokenStack.Pop());
            if (token.type.Precondition().ContainAny(attribute)) tokenStack.Push(token);
            else exceptions.Add(token.lexical.anchor, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
        }
        private bool CheckNext(ListSegment<Lexical> lexicals, ref int index, LexicalType type)
        {
            if (index + 1 < lexicals.Count)
            {
                if (lexicals[index + 1].type == type)
                {
                    index++;
                    return true;
                }
                else exceptions.Add(lexicals[index].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
            }
            else exceptions.Add(lexicals[-1].anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LINE_END);
            return false;
        }
        private bool TryFindDeclaration(ListSegment<Lexical> lexicals, ref int index, ISpace space, out Declaration declaration)
        {
            while (CheckNext(lexicals, ref index, LexicalType.Dot) && CheckNext(lexicals, ref index, LexicalType.Word))
            {
                var lexical = lexicals[index];
                if (space.TryFindDeclaration(lexical.anchor.Segment, out declaration)) return true;
                else if (!space.TryFindChild(lexical.anchor.Segment, out space))
                {
                    exceptions.Add(lexical.anchor, CompilingExceptionCode.COMPILING_NAMESPACE_NOT_FOUND);
                    break;
                }
            }
            declaration = default;
            return false;
        }
        private bool TryFindDeclaration(ListSegment<Lexical> lexicals, ref int index, out Declaration declaration)
        {
            if (index < lexicals.Count)
            {
                var lexical = lexicals[index];
                if (lexical.type == LexicalType.Word)
                {
                    if (lexical.anchor.Segment == KeyWorld.GLOBAL)
                    {
                        if (CheckNext(lexicals, ref index, LexicalType.Dot) && CheckNext(lexicals, ref index, LexicalType.Word))
                        {
                            lexical = lexicals[index];
                            if (lexical.anchor.Segment == KeyWorld.KERNEL) return TryFindDeclaration(lexicals, ref index, RelyKernel.kernel, out declaration);
                            else if (lexical.anchor.Segment == manager.library.name) return TryFindDeclaration(lexicals, ref index, manager.library, out declaration);
                            else foreach (var item in manager.relies)
                                    if (lexical.anchor.Segment == item.name)
                                        return TryFindDeclaration(lexicals, ref index, item, out declaration);
                        }
                    }
                    else if (lexical.anchor.Segment == KeyWorld.KERNEL) return TryFindDeclaration(lexicals, ref index, RelyKernel.kernel, out declaration);
                    else if (TryFindDeclaration(lexical.anchor, out declaration)) return true;
                    else if (context.TryFindSpace(manager, lexical.anchor, out var space, pool, exceptions)) return TryFindDeclaration(lexicals, ref index, space, out declaration);
                }
            }
            declaration = default;
            return false;
        }
        private bool TryPushDeclarationExpression(ListSegment<Lexical> lexicals, ref int index, ScopeStack<Expression> expressionStack, ScopeStack<Token> tokenStack, Lexical lexical, Declaration declaration, ref TokenAttribute attribute)
        {
            if (declaration.code == DeclarationCode.Definition || declaration.code == DeclarationCode.Delegate || declaration.code == DeclarationCode.Coroutine || declaration.code == DeclarationCode.Interface)
            {
                if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                {
                    var dimension = Lexical.ExtractDimension(lexicals, ref index);
                    var expression = new TypeExpression(lexical.anchor, new CompilingType(new CompilingDefinition(declaration), dimension));
                    expressionStack.Push(expression);
                    PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                    attribute = expression.Attribute;
                    return true;
                }
                else exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
            }
            else if (declaration.code == DeclarationCode.GlobalVariable)
            {
                if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                {
                    var expression = new VariableGlobalExpression(lexical.anchor, declaration, IsConstant(declaration), GetVariableType(declaration));
                    expressionStack.Push(expression);
                    attribute = expression.Attribute;
                    return true;
                }
                else exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
            }
            else if (declaration.code == DeclarationCode.GlobalMethod)
            {
                if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                {
                    var expression = new MethodGlobalExpression(lexical.anchor, declaration);
                    expressionStack.Push(expression);
                    attribute = expression.Attribute;
                    return true;
                }
                else exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
            }
            else if (declaration.code == DeclarationCode.NativeMethod)
            {
                if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                {
                    var expression = new MethodNativeExpression(lexical.anchor, declaration);
                    expressionStack.Push(expression);
                    attribute = expression.Attribute;
                    return true;
                }
                else exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
            }
            return false;
        }
        private bool TryPushVetcorConstructorExpression(ScopeStack<Expression> expressionStack, Expression[] expressions, Anchor anchor, CompilingType type, int dimension, ref TokenAttribute attribute)
        {
            var count = 0;
            for (int i = 0; i < expressions.Length; i++)
            {
                var expression = expressions[i];
                if (expression.returns.Length == 1)
                {
                    var returnType = expression.returns[0];
                    if (returnType == RelyKernel.INTEGER_TYPE || returnType == RelyKernel.REAL_TYPE) count++;
                    else if (returnType == RelyKernel.REAL2_TYPE)
                    {
                        expressions[i] = new VectorDeconstructionExpression(expression.anchor, expression, RelyKernel.REAL_TYPE, RelyKernel.REAL_TYPE);
                        count += 2;
                    }
                    else if (returnType == RelyKernel.REAL3_TYPE)
                    {
                        expressions[i] = new VectorDeconstructionExpression(expression.anchor, expression, RelyKernel.REAL_TYPE, RelyKernel.REAL_TYPE, RelyKernel.REAL_TYPE);
                        count += 3;
                    }
                    else if (returnType == RelyKernel.REAL4_TYPE)
                    {
                        expressions[i] = new VectorDeconstructionExpression(expression.anchor, expression, RelyKernel.REAL_TYPE, RelyKernel.REAL_TYPE, RelyKernel.REAL_TYPE, RelyKernel.REAL_TYPE);
                        count += 4;
                    }
                    else
                    {
                        exceptions.Add(expression.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                        return false;
                    }
                }
                else foreach (var returnType in expression.returns)
                        if (returnType == RelyKernel.INTEGER_TYPE || returnType == RelyKernel.REAL_TYPE) count++;
                        else
                        {
                            exceptions.Add(expression.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                            return false;
                        }
            }
            if (count == dimension)
            {
                var returns = new CompilingType[dimension];
                for (int i = 0; i < returns.Length; i++) returns[i] = RelyKernel.REAL_TYPE;
                if (TryAssignmentConvert(expressions, returns, out var expression, out _))
                {
                    expression = new VectorCreateExpression(anchor, expression, type);
                    expressionStack.Push(expression);
                    attribute = expression.Attribute;
                    return true;
                }
                else
                {
                    exceptions.Add(expression.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                    return false;
                }
            }
            else
            {
                exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_FUNCTION_NOT_FOUND);
                return false;
            }
        }
        public bool TryGetFunction(IMethod method, Expression[] expressions, out IFunction function, out Expression parameter)
        {
            function = null;
            parameter = null;
            var measure = 0u;
            while (method != null)
            {
                for (int i = 0; i < method.FunctionCount; i++)
                {
                    var index = method.GetFunction(i);
                    if (context.IsVisible(manager, index.Declaration) && TryAssignmentConvert(expressions, index.Parameters, out var indexParameter, out var indexMeasure))
                    {
                        if (function == null || indexMeasure < measure)
                        {
                            function = index;
                            parameter = indexParameter;
                            measure = indexMeasure;
                        }
                    }
                }
                method = manager.GetOverrideMethod(method);
            }
            return function != null;
        }
        private bool TryAddLocal(ScopeStack<Expression> expressionStack, ScopeStack<Token> tokenStack, Lexical lexical, ref TokenAttribute attribute)
        {
            if (attribute.ContainAny(TokenAttribute.Type))
            {
                if (expressionStack.Pop() is TypeExpression typeExpression && tokenStack.Pop().type == TokenType.Casting)
                {
                    var local = localContext.AddLocal(lexical.anchor, typeExpression.type);
                    var expression = new VariableLocalExpression(lexical.anchor, local.Declaration, TokenAttribute.Assignable, typeExpression.type);
                    expressionStack.Push(expression);
                    attribute = expression.Attribute;
                    return true;
                }
                else throw ExceptionGeneratorCompiler.Unknown();
            }
            return false;
        }
        public bool TryParse(ListSegment<Lexical> lexicals, out Expression result)
        {
            if (TrySub(lexicals, SplitFlag.Lambda | SplitFlag.Assignment | SplitFlag.Question, out var splitIndex))
            {
                if (lexicals[splitIndex].type == LexicalType.Lambda) return TryParseLambda(lexicals, splitIndex, out result);
                else if (lexicals[splitIndex].type == LexicalType.Question) return TryParseQuestion(lexicals, splitIndex, out result);
                else return TryParseAssignment(lexicals, splitIndex, out result);
            }
            else if (TrySub(lexicals, SplitFlag.Comma, out var commaIndex)) return TryParseComma(lexicals, commaIndex, out result);
            using (var expressionStack = pool.GetStack<Expression>())
            using (var tokenStack = pool.GetStack<Token>())
            {
                var exceptionCount = exceptions.Count;
                var attribute = TokenAttribute.None;
                for (int index = 0; index < lexicals.Count; index++)
                {
                    var lexical = lexicals[index];
                    switch (lexical.type)
                    {
                        case LexicalType.Unknow: goto default;
                        case LexicalType.BracketLeft0:
                            {
                                if (TryParseBracket(lexicals, SplitFlag.Bracket0, ref index, out var expressions))
                                {
                                    if (attribute.ContainAny(TokenAttribute.Method))
                                    {
                                        var methodExpression = expressionStack.Pop();
                                        if (methodExpression is MethodMemberExpression memberMethod)
                                        {
                                            if (TryGetFunction(manager.GetMethod(memberMethod.declaration), expressions, out var function, out var parameter))
                                            {
                                                var expression = new InvokerMemberExpression(methodExpression.anchor, function.Declaration, memberMethod.target, parameter, function.Returns);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                goto next_lexical;
                                            }
                                        }
                                        else if (methodExpression is MethodVirtualExpression virtualMethod)
                                        {
                                            if (TryGetFunction(manager.GetMethod(virtualMethod.declaration), expressions, out var function, out var parameter))
                                            {
                                                var expression = new InvokerVirtualMemberExpression(methodExpression.anchor, function.Declaration, virtualMethod.target, parameter, function.Returns);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                goto next_lexical;
                                            }
                                        }
                                        else if (methodExpression is MethodQuestionExpression questionMethod)
                                        {
                                            if (TryGetFunction(manager.GetMethod(questionMethod.declaration), expressions, out var function, out var parameter))
                                            {
                                                var expression = new InvokerQuestionMemberExpression(methodExpression.anchor, function.Declaration, questionMethod.target, parameter, function.Returns);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                goto next_lexical;
                                            }
                                        }
                                        else if (methodExpression is MethodGlobalExpression globalMethod)
                                        {
                                            if (TryGetFunction(manager.GetMethod(globalMethod.declaration), expressions, out var function, out var parameter))
                                            {
                                                var expression = new InvokerGlobalExpression(methodExpression.anchor, function.Declaration, parameter, function.Returns);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                goto next_lexical;
                                            }
                                        }
                                        else if (methodExpression is MethodNativeExpression nativeMethod)
                                        {
                                            if (TryGetFunction(manager.GetMethod(nativeMethod.declaration), expressions, out var function, out var parameter))
                                            {
                                                var expression = new InvokerNativeExpression(methodExpression.anchor, function.Declaration, parameter, function.Returns);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                goto next_lexical;
                                            }
                                        }
                                        exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_FUNCTION_NOT_FOUND);
                                        goto parse_fail;
                                    }
                                    else if (attribute.ContainAny(TokenAttribute.Callable))
                                    {
                                        var delegateExpression = expressionStack.Pop();
                                        if (delegateExpression.returns.Length != 1 || delegateExpression.returns[0].dimension > 0 || delegateExpression.returns[0].definition.code != TypeCode.Function) throw ExceptionGeneratorCompiler.InvalidCompilingType(delegateExpression.returns[0]);
                                        var delegateType = delegateExpression.returns[0];
                                        var declaration = new Declaration(delegateType.definition.library, delegateType.definition.visibility, DeclarationCode.Delegate, delegateType.definition.index, 0, 0);
                                        if (manager.TryGetReturns(declaration, out var returns) && manager.TryGetParameters(declaration, out var parameters) && TryAssignmentConvert(expressions, parameters, out var parameter, out _))
                                        {
                                            var expression = new InvokerDelegateExpression(delegateExpression.anchor, delegateExpression, parameter, returns);
                                            expressionStack.Push(expression);
                                            attribute = expression.Attribute;
                                            break;
                                        }
                                        exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
                                        goto parse_fail;
                                    }
                                    else if (attribute.ContainAny(TokenAttribute.Type))
                                    {
                                        if (expressionStack.Pop() is TypeExpression typeExpression && tokenStack.Count > 0 && tokenStack.Peek().type == TokenType.Casting)
                                        {
                                            tokenStack.Pop();
                                            var type = typeExpression.type;
                                            if (type == RelyKernel.REAL2_TYPE)
                                            {
                                                if (TryPushVetcorConstructorExpression(expressionStack, expressions, typeExpression.anchor, type, 2, ref attribute)) break;
                                                else goto parse_fail;
                                            }
                                            else if (type == RelyKernel.REAL3_TYPE)
                                            {
                                                if (TryPushVetcorConstructorExpression(expressionStack, expressions, typeExpression.anchor, type, 3, ref attribute)) break;
                                                else goto parse_fail;
                                            }
                                            else if (type == RelyKernel.REAL4_TYPE)
                                            {
                                                if (TryPushVetcorConstructorExpression(expressionStack, expressions, typeExpression.anchor, type, 4, ref attribute)) break;
                                                else goto parse_fail;
                                            }
                                            else if (manager.TryGetConstructor(type, out var constructor) && TryGetFunction(constructor, expressions, out var constructorFunction, out var constructorParameter))
                                            {
                                                if (destructor) exceptions.Add(typeExpression.anchor, CompilingExceptionCode.SYNTAX_DESTRUCTOR_ALLOC);
                                                var expression = new InvokerConstructorExpression(typeExpression.anchor, constructorFunction.Declaration, constructorParameter, type);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                goto next_lexical;
                                            }
                                            goto default;
                                        }
                                        else throw ExceptionGeneratorCompiler.Unknown();
                                    }
                                    else if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        if (expressions.Length == 0) exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                                        else if (expressions.Length == 1 && expressions[0].Attribute.ContainAny(TokenAttribute.Type))
                                        {
                                            expressionStack.Push(expressions[0]);
                                            attribute = expressions[0].Attribute;
                                            break;
                                        }
                                        else if (TryCombineExpressions(out var expression, expressions))
                                        {
                                            expressionStack.Push(expression);
                                            attribute = expression.Attribute;
                                            break;
                                        }
                                    }
                                }
                                goto default;
                            }
                        case LexicalType.BracketLeft1:
                            {
                                if (TryParseBracket(lexicals, SplitFlag.Bracket1, ref index, out var expressions))
                                {
                                    foreach (var item in expressions)
                                        foreach (var returnType in item.returns)
                                            if (returnType != RelyKernel.INTEGER_TYPE)
                                            {
                                                exceptions.Add(item.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                                goto default;
                                            }
                                    if (attribute.ContainAny(TokenAttribute.Array))
                                    {
                                        if (TryCombineExpressions(out var expression, expressions))
                                        {
                                            var array = expressionStack.Pop();
                                            if (expression.returns.Length == 1)
                                            {
                                                if (array.returns[0].dimension > 0)
                                                {
                                                    expression = new ArrayEvaluationExpression(lexical.anchor, array, expression, new CompilingType(array.returns[0].definition, array.returns[0].dimension - 1));
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    break;
                                                }
                                                else if (array.returns[0] == RelyKernel.STRING_TYPE)
                                                {
                                                    expression = new StringEvaluationExpression(lexical.anchor, array, expression);
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    break;
                                                }
                                            }
                                            else if (expression.returns.Length == 2)
                                            {
                                                expression = new ArraySubExpression(lexical.anchor, array, expression);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                        }
                                    }
                                    else if (attribute.ContainAny(TokenAttribute.Tuple))
                                    {
                                        var tupleExpression = expressionStack.Pop();
                                        if (expressions.Length == 0) exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                                        else using (var elementIndices = pool.GetList<long>())
                                            {
                                                foreach (var item in expressions)
                                                    if (item.TryEvaluation(out long value))
                                                    {
                                                        if (value < 0) value += tupleExpression.returns.Length;
                                                        if (value < 0 || value >= tupleExpression.returns.Length)
                                                        {
                                                            exceptions.Add(item.anchor, CompilingExceptionCode.GENERATOR_TUPLE_INDEX_OUT_OF_RANGE);
                                                            goto default;
                                                        }
                                                        else elementIndices.Add(value);
                                                    }
                                                    else
                                                    {
                                                        exceptions.Add(item.anchor, CompilingExceptionCode.GENERATOR_TUPLE_INDEX_NOT_CONSTANT);
                                                        goto parse_fail;
                                                    }
                                                var returns = new CompilingType[elementIndices.Count];
                                                for (int i = 0; i < returns.Length; i++)
                                                    returns[i] = tupleExpression.returns[elementIndices[i]];
                                                var tuple = new TupleEvaluationExpression(tupleExpression.anchor, tupleExpression, elementIndices.ToArray(), returns);
                                                expressionStack.Push(tuple);
                                                attribute = tuple.Attribute;
                                                break;
                                            }
                                    }
                                    else if (attribute.ContainAny(TokenAttribute.Coroutine))
                                    {
                                        var coroutineExpression = expressionStack.Pop();
                                        if (coroutineExpression.returns.Length != 1 || coroutineExpression.returns[0].dimension > 0 || coroutineExpression.returns[0].definition.code != TypeCode.Coroutine) throw ExceptionGeneratorCompiler.InvalidCompilingType(coroutineExpression.returns[0]);
                                        var coroutineType = coroutineExpression.returns[0];
                                        var declaration = new Declaration(coroutineType.definition.library, Visibility.Public, DeclarationCode.Coroutine, coroutineType.definition.index, 0, 0);
                                        if (manager.TryGetReturns(declaration, out var returns))
                                        {
                                            if (expressions.Length == 0)
                                            {
                                                var indices = new long[returns.Length];
                                                for (int i = 0; i < indices.Length; i++) indices[i] = i;
                                                var coroutine = new CoroutineEvaluationExpression(lexical.anchor, coroutineExpression, indices, returns);
                                                expressionStack.Push(coroutine);
                                                attribute = coroutine.Attribute;
                                                break;
                                            }
                                            else using (var elementIndices = pool.GetList<long>())
                                                {
                                                    foreach (var item in expressions)
                                                        if (item.TryEvaluation(out long value))
                                                        {
                                                            if (value < 0) value += returns.Length;
                                                            if (value < 0 || value >= returns.Length)
                                                            {
                                                                exceptions.Add(item.anchor, CompilingExceptionCode.GENERATOR_TUPLE_INDEX_OUT_OF_RANGE);
                                                                goto default;
                                                            }
                                                            else elementIndices.Add(value);
                                                        }
                                                        else
                                                        {
                                                            exceptions.Add(item.anchor, CompilingExceptionCode.GENERATOR_TUPLE_INDEX_NOT_CONSTANT);
                                                            goto parse_fail;
                                                        }
                                                    var types = new CompilingType[elementIndices.Count];
                                                    for (int i = 0; i < types.Length; i++)
                                                        types[i] = returns[elementIndices[i]];
                                                    var tuple = new CoroutineEvaluationExpression(coroutineExpression.anchor, coroutineExpression, elementIndices.ToArray(), types);
                                                    expressionStack.Push(tuple);
                                                    attribute = tuple.Attribute;
                                                    break;
                                                }
                                        }
                                        else
                                        {
                                            exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
                                            goto parse_fail;
                                        }
                                    }
                                    else if (attribute.ContainAny(TokenAttribute.Type))
                                    {
                                        if (expressionStack.Pop() is TypeExpression typeExpression && tokenStack.Count > 0 && tokenStack.Peek().type == TokenType.Casting)
                                        {
                                            tokenStack.Pop();
                                            if (typeExpression.type.dimension == 0 && TryCombineExpressions(out var expression, expressions) && expression.returns.Length == 1)
                                            {
                                                if (destructor) exceptions.Add(typeExpression.anchor, CompilingExceptionCode.SYNTAX_DESTRUCTOR_ALLOC);
                                                var dimension = Lexical.ExtractDimension(lexicals, ref index);
                                                expression = new ArrayCreateExpression(typeExpression.anchor, expression, new CompilingType(typeExpression.type.definition, dimension + 1));
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                        }
                                        else throw ExceptionGeneratorCompiler.Unknown();
                                    }
                                }
                            }
                            goto default;
                        case LexicalType.BracketLeft2:
                        case LexicalType.BracketRight0:
                        case LexicalType.BracketRight1:
                        case LexicalType.BracketRight2:
                        case LexicalType.Comma:
                        case LexicalType.Assignment: goto default;
                        #region operator
                        case LexicalType.Equals:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Equals), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.Lambda: goto default;
                        case LexicalType.BitAnd:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.BitAnd), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.LogicAnd:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.LogicAnd), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.BitAndAssignment: goto default;
                        case LexicalType.BitOr:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.BitOr), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.LogicOr:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.LogicOr), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.BitOrAssignment: goto default;
                        case LexicalType.BitXor:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.BitXor), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.BitXorAssignment: goto default;
                        case LexicalType.Less:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Less), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.LessEquals:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.LessEquals), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.ShiftLeft:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.ShiftLeft), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.ShiftLeftAssignment: goto default;
                        case LexicalType.Greater:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Greater), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.GreaterEquals:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.GreaterEquals), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.ShiftRight:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.ShiftRight), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.ShiftRightAssignment: goto default;
                        case LexicalType.Plus:
                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator)) PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Positive), attribute);
                            else PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Plus), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.Increment:
                            if (attribute.ContainAll(TokenAttribute.Assignable | TokenAttribute.Value))
                            {
                                if (TryPopExpression(expressionStack, lexical.anchor, out var expression))
                                {
                                    if (expression is VariableExpression variable)
                                    {
                                        if (variable.returns[0] == RelyKernel.INTEGER_TYPE)
                                        {
                                            expression = new OperationPostIncrementExpression(lexical.anchor, CommandMacro.INTEGER_Increment, variable);
                                            expressionStack.Push(expression);
                                            attribute = expression.Attribute;
                                            break;
                                        }
                                        else if (variable.returns[0] == RelyKernel.REAL_TYPE)
                                        {
                                            expression = new OperationPostIncrementExpression(lexical.anchor, CommandMacro.REAL_Increment, variable);
                                            expressionStack.Push(expression);
                                            attribute = expression.Attribute;
                                            break;
                                        }
                                        else
                                        {
                                            exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                            goto parse_fail;
                                        }
                                    }
                                    else
                                    {
                                        exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                        goto parse_fail;
                                    }
                                }
                                else goto parse_fail;
                            }
                            else
                            {
                                PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.IncrementLeft), attribute);
                                attribute = TokenAttribute.Operator;
                                break;
                            }
                        case LexicalType.PlusAssignment: goto default;
                        case LexicalType.Minus:
                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator)) PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Negative), attribute);
                            else PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Minus), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.Decrement:
                            if (attribute.ContainAll(TokenAttribute.Assignable | TokenAttribute.Value))
                            {
                                if (TryPopExpression(expressionStack, lexical.anchor, out var expression))
                                {
                                    if (expression is VariableExpression variable)
                                    {
                                        if (variable.returns[0] == RelyKernel.INTEGER_TYPE)
                                        {
                                            expression = new OperationPostIncrementExpression(lexical.anchor, CommandMacro.INTEGER_Decrement, variable);
                                            expressionStack.Push(expression);
                                            attribute = expression.Attribute;
                                            break;
                                        }
                                        else if (variable.returns[0] == RelyKernel.REAL_TYPE)
                                        {
                                            expression = new OperationPostIncrementExpression(lexical.anchor, CommandMacro.REAL_Decrement, variable);
                                            expressionStack.Push(expression);
                                            attribute = expression.Attribute;
                                            break;
                                        }
                                        else
                                        {
                                            exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                            goto parse_fail;
                                        }
                                    }
                                    else
                                    {
                                        exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                        goto parse_fail;
                                    }
                                }
                                else goto parse_fail;
                            }
                            else
                            {
                                PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.DecrementLeft), attribute);
                                attribute = TokenAttribute.Operator;
                                break;
                            }
                        case LexicalType.MinusAssignment: goto default;
                        case LexicalType.Mul:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Mul), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.MulAssignment: goto default;
                        case LexicalType.Div:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Div), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.DivAssignment:
                        case LexicalType.Annotation: goto default;
                        case LexicalType.Mod:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Mod), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.ModAssignment: goto default;
                        case LexicalType.Not:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Not), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.NotEquals:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.NotEquals), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.Negate:
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Inverse), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        #endregion operator
                        #region dot
                        case LexicalType.Dot:
                            if (index + 1 < lexicals.Count)
                            {
                                lexical = lexicals[++index];
                                if (lexical.type == LexicalType.Word)
                                {
                                    if (attribute.ContainAny(TokenAttribute.Value) && expressionStack.Peek().returns.Length == 1)
                                    {
                                        var expression = expressionStack.Pop();
                                        var type = expression.returns[0];
                                        if (type.dimension > 0) type = RelyKernel.ARRAY_TYPE;
                                        if (context.TryFindMemberDeclarartion(manager, lexical.anchor, type.definition, out var declaration, pool))
                                        {
                                            if (declaration.code == DeclarationCode.MemberVariable)
                                            {
                                                expression = new VariableMemberExpression(lexical.anchor, declaration, expression, GetVariableType(declaration));
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                            else if (declaration.code == DeclarationCode.MemberMethod || declaration.code == DeclarationCode.InterfaceMethod)
                                            {
                                                if (type.IsHandle) expression = new MethodVirtualExpression(lexical.anchor, expression, declaration);
                                                else expression = new MethodMemberExpression(lexical.anchor, expression, declaration);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                        }
                                        else if (VectorMemberExpression.TryCreate(lexical.anchor, expression, out var vectorMember))
                                        {
                                            expressionStack.Push(vectorMember);
                                            attribute = vectorMember.Attribute;
                                            break;
                                        }
                                        else
                                        {
                                            exceptions.Add(lexical.anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                                            goto parse_fail;
                                        }
                                    }
                                }
                                goto default;
                            }
                            else
                            {
                                exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                                goto parse_fail;
                            }
                        case LexicalType.Question: goto default;
                        case LexicalType.QuestionDot:
                            if (index + 1 < lexicals.Count)
                            {
                                lexical = lexicals[++index];
                                if (lexical.type == LexicalType.Word)
                                {
                                    if (attribute.ContainAny(TokenAttribute.Value) && expressionStack.Peek().returns.Length == 1)
                                    {
                                        var expression = expressionStack.Peek();
                                        var type = expression.returns[0];
                                        if (type.dimension > 0) type = RelyKernel.ARRAY_TYPE;
                                        if (context.TryFindMemberDeclarartion(manager, lexical.anchor, type.definition, out var declaration, pool))
                                        {
                                            if (!type.IsHandle)
                                            {
                                                exceptions.Add(expression.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                                goto parse_fail;
                                            }
                                            else if (declaration.code == DeclarationCode.MemberVariable)
                                            {
                                                expression = new VariableQuestionMemberExpression(lexical.anchor, declaration, expression, GetVariableType(declaration));
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                            else if (declaration.code == DeclarationCode.MemberMethod || declaration.code == DeclarationCode.InterfaceMethod)
                                            {
                                                expression = new MethodQuestionExpression(lexical.anchor, expression, declaration);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            exceptions.Add(lexical.anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                                            goto parse_fail;
                                        }
                                    }
                                }
                                goto default;
                            }
                            else
                            {
                                exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                                goto parse_fail;
                            }
                        #endregion dot
                        case LexicalType.QuestionInvoke:
                            if (attribute.ContainAll(TokenAttribute.Callable | TokenAttribute.Value))
                            {
                                if (TryParseBracket(lexicals, SplitFlag.Bracket0, ref index, out var expressions))
                                {
                                    var delegateExpression = expressionStack.Pop();
                                    if (delegateExpression.returns.Length != 1 || delegateExpression.returns[0].dimension > 0 || delegateExpression.returns[0].definition.code != TypeCode.Function) throw ExceptionGeneratorCompiler.InvalidCompilingType(delegateExpression.returns[0]);
                                    var delegateType = delegateExpression.returns[0];
                                    var declaration = new Declaration(delegateType.definition.library, delegateType.definition.visibility, DeclarationCode.Delegate, delegateType.definition.index, 0, 0);
                                    if (manager.TryGetReturns(declaration, out var returns) && manager.TryGetParameters(declaration, out var parameters) && TryAssignmentConvert(expressions, parameters, out var parameter, out _))
                                    {
                                        var expression = new InvokerQuestionDelegateExpression(delegateExpression.anchor, delegateExpression, parameter, returns);
                                        expressionStack.Push(expression);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
                                    goto parse_fail;
                                }
                            }
                            break;
                        case LexicalType.Colon: goto default;
                        #region Constants
                        case LexicalType.ConstReal:
                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                            {
                                if (real.TryParse(lexical.anchor.Segment.ToString().Replace("_", ""), out var value))
                                    expressionStack.Push(new ConstantRealExpression(lexical.anchor, value));
                                else throw ExceptionGeneratorCompiler.ConstantParseFail(lexical.anchor.Segment);
                                attribute = TokenAttribute.Constant;
                                break;
                            }
                            else goto default;
                        case LexicalType.ConstNumber:
                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                            {
                                if (long.TryParse(lexical.anchor.Segment.ToString().Replace("_", ""), out var value))
                                    expressionStack.Push(new ConstantIntegerExpression(lexical.anchor, value));
                                else throw ExceptionGeneratorCompiler.ConstantParseFail(lexical.anchor.Segment);
                                attribute = TokenAttribute.Constant;
                                break;
                            }
                            else goto default;
                        case LexicalType.ConstBinary:
                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                            {
                                var value = 0;
                                var segment = lexical.anchor.Segment.ToString();
                                for (int i = 2; i < segment.Length; i++)
                                {
                                    var c = segment[i];
                                    if (c != '_')
                                    {
                                        value <<= 1;
                                        if (c == '1') value++;
                                    }
                                }
                                expressionStack.Push(new ConstantIntegerExpression(lexical.anchor, value));
                                attribute = TokenAttribute.Constant;
                                break;
                            }
                            else goto default;
                        case LexicalType.ConstHexadecimal:
                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                            {
                                var value = 0;
                                var segment = lexical.anchor.Segment.ToString();
                                for (int i = 2; i < segment.Length; i++)
                                {
                                    var c = segment[i];
                                    if (c != '_')
                                    {
                                        value <<= 4;
                                        if (c >= '0' && c <= '9') value += c - '0';
                                        else value += (c | 0x20) - 'a' + 10;
                                    }
                                }
                                expressionStack.Push(new ConstantIntegerExpression(lexical.anchor, value));
                                attribute = TokenAttribute.Constant;
                                break;
                            }
                            else goto default;
                        case LexicalType.ConstChars:
                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                            {
                                var value = 0;
                                var segment = lexical.anchor.Segment.ToString();
                                for (int i = 0; i < segment.Length; i++)
                                {
                                    var c = segment[i];
                                    if (c != '\'')
                                    {
                                        value <<= 8;
                                        if (c == '\\')
                                        {
                                            if (++i < segment.Length)
                                            {
                                                c = segment[i];
                                                if (c == 'a') c = '\a';
                                                else if (c == 'b') c = '\b';
                                                else if (c == 'f') c = '\f';
                                                else if (c == 'n') c = '\n';
                                                else if (c == 'r') c = '\r';
                                                else if (c == 't') c = '\t';
                                                else if (c == 'v') c = '\v';
                                                else if (c == '0') c = '\0';
                                                value += c & 0xff;
                                            }
                                        }
                                        else value += c & 0xff;
                                    }
                                }
                                expressionStack.Push(new ConstantIntegerExpression(lexical.anchor, value));
                                attribute = TokenAttribute.Constant;
                                break;
                            }
                            else goto default;
                        case LexicalType.ConstString:
                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                            {
                                var builder = new System.Text.StringBuilder();
                                var segment = lexical.anchor.Segment.ToString();
                                for (int i = 0; i < segment.Length; i++)
                                {
                                    var c = segment[i];
                                    if (c != '\"')
                                    {
                                        if (c == '\\')
                                        {
                                            if (++i < segment.Length)
                                            {
                                                c = segment[i];
                                                if (c == 'a') c = '\a';
                                                else if (c == 'b') c = '\b';
                                                else if (c == 'f') c = '\f';
                                                else if (c == 'n') c = '\n';
                                                else if (c == 'r') c = '\r';
                                                else if (c == 't') c = '\t';
                                                else if (c == 'v') c = '\v';
                                                else if (c == '0') c = '\0';
                                                else if (c == 'x')
                                                {
                                                    if (++i < segment.Length && TryGetHexValue(segment[i], out var value))
                                                    {
                                                        if (++i < segment.Length && TryGetHexValue(segment[i], out var value2)) c = (char)(value * 16 + value2);
                                                        else i -= 2;
                                                    }
                                                    else i--;
                                                }
                                                else if (c == 'u')
                                                {
                                                    if (i + 4 < segment.Length)
                                                    {
                                                        var resultChar = 0u;
                                                        var idx = i;
                                                        while (idx - i < 4 && TryGetHexValue(segment[++idx], out var value)) resultChar = (resultChar << 4) + value;
                                                        if (idx == i + 4)
                                                        {
                                                            i = idx;
                                                            c = (char)resultChar;
                                                        }
                                                    }
                                                }
                                                builder.Append(c);
                                            }
                                        }
                                        else builder.Append(c);
                                    }
                                }
                                expressionStack.Push(new ConstantStringExpression(lexical.anchor, builder.ToString()));
                                attribute = TokenAttribute.Constant;
                                break;
                            }
                            else goto default;
                        #endregion Constants
                        case LexicalType.Word:
                            {
                                if (lexical.anchor.Segment == KeyWorld.KERNEL)
                                {
                                    if (!TryFindDeclaration(lexicals, ref index, RelyKernel.kernel, out var declaration) || !TryPushDeclarationExpression(lexicals, ref index, expressionStack, tokenStack, lexicals[index], declaration, ref attribute))
                                        goto parse_fail;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.GLOBAL)
                                {
                                    if (TryFindDeclaration(lexicals, ref index, out var declaration) && TryPushDeclarationExpression(lexicals, ref index, expressionStack, tokenStack, lexicals[index], declaration, ref attribute)) break;
                                    goto parse_fail;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.BASE)
                                {
                                    if (TryGetThisValueExpression(out var thisValueExpression))
                                    {
                                        if (CheckNext(lexicals, ref index, LexicalType.Dot))
                                        {
                                            if (CheckNext(lexicals, ref index, LexicalType.Word))
                                            {
                                                var baseAnchor = lexical.anchor;
                                                lexical = lexicals[index];
                                                if (context.TryFindMemberDeclarartion(manager, lexical.anchor, context.definition.parent, out var declaration, pool))
                                                {
                                                    if (declaration.code == DeclarationCode.MemberVariable)
                                                    {
                                                        if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                                        {
                                                            var expression = new VariableMemberExpression(lexical.anchor, declaration, thisValueExpression, GetVariableType(declaration));
                                                            expressionStack.Push(expression);
                                                            attribute = expression.Attribute;
                                                            break;
                                                        }
                                                        else goto default;
                                                    }
                                                    else if (declaration.code == DeclarationCode.MemberMethod)
                                                    {
                                                        if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                                        {
                                                            var expression = new MethodMemberExpression(lexical.anchor, thisValueExpression, declaration);
                                                            expressionStack.Push(expression);
                                                            attribute = expression.Attribute;
                                                            break;
                                                        }
                                                        else goto default;
                                                    }
                                                }
                                                else
                                                {
                                                    exceptions.Add(lexical.anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                                                    goto parse_fail;
                                                }
                                            }
                                            goto default;
                                        }
                                        else
                                        {
                                            exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                            goto parse_fail;
                                        }
                                    }
                                    else
                                    {
                                        exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_NOT_MEMBER_METHOD);
                                        goto parse_fail;
                                    }
                                }
                                else if (lexical.anchor.Segment == KeyWorld.THIS)
                                {
                                    if (TryGetThisValueExpression(out var thisValueExpression))
                                    {
                                        if (index + 1 < lexicals.Count && lexicals[index + 1].type == LexicalType.Dot)
                                        {
                                            index++;
                                            if (CheckNext(lexicals, ref index, LexicalType.Word))
                                            {
                                                var baseAnchor = lexical.anchor;
                                                lexical = lexicals[index];
                                                if (context.TryFindMemberDeclarartion(manager, lexical.anchor, new CompilingDefinition(context.definition.declaration), out var declaration, pool))
                                                {
                                                    if (declaration.code == DeclarationCode.MemberVariable)
                                                    {
                                                        if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                                        {
                                                            var expression = new VariableMemberExpression(lexical.anchor, declaration, thisValueExpression, GetVariableType(declaration));
                                                            expressionStack.Push(expression);
                                                            attribute = expression.Attribute;
                                                            break;
                                                        }
                                                        else goto default;
                                                    }
                                                    else if (declaration.code == DeclarationCode.MemberMethod)
                                                    {
                                                        if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                                        {
                                                            var expression = new MethodVirtualExpression(lexical.anchor, thisValueExpression, declaration);
                                                            expressionStack.Push(expression);
                                                            attribute = expression.Attribute;
                                                            break;
                                                        }
                                                        else goto default;
                                                    }
                                                }
                                                else
                                                {
                                                    exceptions.Add(lexical.anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                                                    goto parse_fail;
                                                }
                                            }
                                            goto default;
                                        }
                                        else if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                        {
                                            expressionStack.Push(thisValueExpression);
                                            attribute = thisValueExpression.Attribute;
                                            break;
                                        }
                                        else goto default;
                                    }
                                    else
                                    {
                                        exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_NOT_MEMBER_METHOD);
                                        goto parse_fail;
                                    }
                                }
                                else if (lexical.anchor.Segment == KeyWorld.TRUE)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new ConstantBooleanExpression(lexical.anchor, true);
                                        expressionStack.Push(expression);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.FALSE)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new ConstantBooleanExpression(lexical.anchor, false);
                                        expressionStack.Push(expression);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.NULL)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new ConstantNullExpression(lexical.anchor);
                                        expressionStack.Push(expression);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.VAR)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None))
                                    {
                                        if (CheckNext(lexicals, ref index, LexicalType.Word))
                                        {
                                            lexical = lexicals[index];
                                            if (KeyWorld.IsKeyWorld(lexical.anchor.Segment))
                                            {
                                                exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
                                                goto parse_fail;
                                            }
                                            else
                                            {
                                                var expression = new BlurryVariableDeclarationExpression(lexical.anchor);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                        }
                                    }
                                    goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.BOOL)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.BOOL_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.INTEGER)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.INTEGER_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.REAL)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.REAL_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.REAL2)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.REAL2_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.REAL3)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.REAL3_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.REAL4)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.REAL4_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.STRING)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.STRING_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.HANDLE)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.HANDLE_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.ENTITY)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.ENTITY_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.ARRAY)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.ARRAY_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.INTERFACE)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        var expression = new TypeExpression(lexical.anchor, RelyKernel.INTERFACE_TYPE);
                                        expressionStack.Push(expression);
                                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                        attribute = expression.Attribute;
                                        break;
                                    }
                                    else goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.IS)
                                {
                                    if (attribute.ContainAny(TokenAttribute.Value))
                                    {
                                        var expression = expressionStack.Pop();
                                        if (expression.returns.Length == 1 && expression.returns[0].IsHandle)
                                        {
                                            var startIndex = index + 1;
                                            if (TryFindDeclaration(lexicals, ref startIndex, out var declaration))
                                            {
                                                if (declaration.code == DeclarationCode.Definition || declaration.code == DeclarationCode.Delegate || declaration.code == DeclarationCode.Coroutine || declaration.code == DeclarationCode.Interface)
                                                {
                                                    var dimension = Lexical.ExtractDimension(lexicals, ref startIndex);
                                                    var type = new CompilingType(new CompilingDefinition(declaration), dimension);
                                                    VariableLocalExpression localExpression = null;
                                                    if (startIndex + 1 < lexicals.Count && lexicals[startIndex + 1].type == LexicalType.Word)
                                                    {
                                                        startIndex++;
                                                        var local = localContext.AddLocal(lexicals[startIndex].anchor, type);
                                                        if (KeyWorld.IsKeyWorld(local.anchor.Segment)) exceptions.Add(local.anchor, CompilingExceptionCode.SYNTAX_NAME_IS_KEY_WORLD);
                                                        localExpression = new VariableLocalExpression(local.anchor, local.Declaration, TokenAttribute.Assignable, type);
                                                    }
                                                    expression = new IsExpression(lexical.anchor, expression, type, localExpression);
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    index = startIndex;
                                                    break;
                                                }
                                                else
                                                {
                                                    exceptions.Add(lexicals[index + 1, startIndex - 1], CompilingExceptionCode.GENERATOR_NOT_TYPE);
                                                    goto parse_fail;
                                                }
                                            }
                                            else if (startIndex < lexicals.Count) exceptions.Add(lexicals[index + 1, startIndex - 1], CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                                            else exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                                            goto parse_fail;
                                        }
                                        else
                                        {
                                            exceptions.Add(expression.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                            goto parse_fail;
                                        }
                                    }
                                    goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.AS)
                                {
                                    if (attribute.ContainAny(TokenAttribute.Value))
                                    {
                                        var expression = expressionStack.Pop();
                                        if (expression.returns.Length == 1)
                                        {
                                            var startIndex = index + 1;
                                            if (TryFindDeclaration(lexicals, ref startIndex, out var declaration))
                                            {
                                                if (declaration.code == DeclarationCode.Definition || declaration.code == DeclarationCode.Delegate || declaration.code == DeclarationCode.Coroutine || declaration.code == DeclarationCode.Interface)
                                                {
                                                    var dimension = Lexical.ExtractDimension(lexicals, ref startIndex);
                                                    var type = new CompilingType(new CompilingDefinition(declaration), dimension);
                                                    expression = new AsExpression(lexical.anchor, expression, type);
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    index = startIndex;
                                                    break;
                                                }
                                                else
                                                {
                                                    exceptions.Add(lexicals[index + 1, startIndex - 1], CompilingExceptionCode.GENERATOR_NOT_TYPE);
                                                    goto parse_fail;
                                                }
                                            }
                                            else if (startIndex < lexicals.Count) exceptions.Add(lexicals[index + 1, startIndex - 1], CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                                            else exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                                            goto parse_fail;
                                        }
                                    }
                                    goto default;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.START)
                                {
                                    if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        if (index + 1 < lexicals.Count)
                                        {
                                            if (TrySub(lexicals[index + 1, -1], SplitFlag.Bracket0, out var coroutineEnd))
                                            {
                                                coroutineEnd += index + 1;
                                                while (coroutineEnd + 1 < lexicals.Count && lexicals[coroutineEnd + 1].type == LexicalType.Dot)
                                                {
                                                    if (TrySub(lexicals[coroutineEnd + 1, -1], SplitFlag.Bracket0, out var invokerEnd)) coroutineEnd += invokerEnd + 1;
                                                    else break;
                                                }
                                                if (TryParse(lexicals[index + 1, coroutineEnd], out var invoker))
                                                {
                                                    var expression = new BlurryCoroutineExpression(lexical.anchor, invoker);
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    index = coroutineEnd;
                                                    goto next_lexical;
                                                }
                                                else goto parse_fail;
                                            }
                                        }
                                        else
                                        {
                                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LINE_END);
                                            goto parse_fail;
                                        }
                                    }
                                    goto default;
                                }
                                else if (KeyWorld.IsKeyWorld(lexical.anchor.Segment)) goto default;
                                else if (TryFindDeclaration(lexical.anchor, out var declaration))
                                {
                                    switch (declaration.code)
                                    {
                                        case DeclarationCode.Invalid: goto default;
                                        case DeclarationCode.Definition:
                                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                            {
                                                var expression = new TypeExpression(lexical.anchor, new CompilingType(new CompilingDefinition(declaration), Lexical.ExtractDimension(lexicals, ref index)));
                                                expressionStack.Push(expression);
                                                PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                            else goto unexpected_lexical;
                                        case DeclarationCode.MemberVariable:
                                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator | TokenAttribute.Type))
                                            {
                                                if (TryGetThisValueExpression(out var thisValueExpression))
                                                {
                                                    var expression = new VariableMemberExpression(lexical.anchor, declaration, thisValueExpression, GetVariableType(declaration));
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    break;
                                                }
                                            }
                                            goto unexpected_lexical;
                                        case DeclarationCode.MemberMethod:
                                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                            {
                                                if (TryGetThisValueExpression(out var thisValueExpression))
                                                {
                                                    var expression = new MethodVirtualExpression(lexical.anchor, thisValueExpression, declaration);
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    break;
                                                }
                                            }
                                            goto unexpected_lexical;
                                        case DeclarationCode.MemberFunction:
                                        case DeclarationCode.Constructor:
                                        case DeclarationCode.ConstructorFunction: goto default;
                                        case DeclarationCode.Delegate:
                                        case DeclarationCode.Coroutine:
                                        case DeclarationCode.Interface: goto case DeclarationCode.Definition;
                                        case DeclarationCode.InterfaceMethod:
                                        case DeclarationCode.InterfaceFunction: goto default;
                                        case DeclarationCode.GlobalVariable:
                                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                            {
                                                var expression = new VariableGlobalExpression(lexical.anchor, declaration, IsConstant(declaration), GetVariableType(declaration));
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                            else goto unexpected_lexical;
                                        case DeclarationCode.GlobalMethod:
                                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                            {
                                                var expression = new MethodGlobalExpression(lexical.anchor, declaration);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                            else goto unexpected_lexical;
                                        case DeclarationCode.GlobalFunction: goto default;
                                        case DeclarationCode.NativeMethod:
                                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                            {
                                                var expression = new MethodNativeExpression(lexical.anchor, declaration);
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                            else goto unexpected_lexical;
                                        case DeclarationCode.NativeFunction:
                                        case DeclarationCode.Lambda: goto default;
                                        case DeclarationCode.LambdaClosureValue:
                                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator | TokenAttribute.Type))
                                            {
                                                var local = new Local(lexical.anchor, 0, new CompilingType(LIBRARY.SELF, Visibility.Space, TypeCode.Handle, declaration.definitionIndex, 0));
                                                var expression = new VariableMemberExpression(lexical.anchor, declaration, new VariableLocalExpression(local, TokenAttribute.Value), GetVariableType(declaration));
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                            else goto unexpected_lexical;
                                        case DeclarationCode.LocalVariable:
                                            if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator | TokenAttribute.Type))
                                            {
                                                var expression = new VariableLocalExpression(lexical.anchor, declaration, TokenAttribute.Assignable | TokenAttribute.Value, GetVariableType(declaration));
                                                expressionStack.Push(expression);
                                                attribute = expression.Attribute;
                                                break;
                                            }
                                            else goto unexpected_lexical;
                                        default:
                                            throw ExceptionGeneratorCompiler.Unknown();
                                        unexpected_lexical:
                                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                            goto parse_fail;
                                    }
                                }
                                else if (context.TryFindSpace(manager, lexical.anchor, out var space, pool, exceptions))
                                {
                                    if (TryFindDeclaration(lexicals, ref index, space, out declaration))
                                    {
                                        if (!TryPushDeclarationExpression(lexicals, ref index, expressionStack, tokenStack, lexicals[index], declaration, ref attribute))
                                            goto parse_fail;
                                    }
                                    else if (!TryAddLocal(expressionStack, tokenStack, lexical, ref attribute)) goto parse_fail;
                                }
                                else if (TryAddLocal(expressionStack, tokenStack, lexical, ref attribute)) break;
                                else
                                {
                                    exceptions.Add(lexical.anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                                    goto parse_fail;
                                }
                            }
                            break;
                        case LexicalType.Backslash:
                        default:
                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                            goto parse_fail;
                    }
                next_lexical:;
                }
                while (tokenStack.Count > 0) PopToken(expressionStack, tokenStack.Pop());
                if (expressionStack.Count == 1)
                {
                    result = expressionStack.Pop();
                    return true;
                }
                else if (expressionStack.Count > 1)
                {
                    var expressions = new Expression[expressionStack.Count];
                    while (expressionStack.Count > 0) expressions[expressionStack.Count - 1] = expressionStack.Pop();
                    return TryCombineExpressions(out result, expressions);
                }
                exceptions.Add(lexicals, CompilingExceptionCode.SYNTAX_UNKNOW);
            }
        parse_fail:
            result = default;
            return false;
        }
        public bool TryParseTuple(ListSegment<Lexical> lexicals, out Expression[] results)
        {
            if (lexicals.Count > 0)
            {
                using (var expressions = pool.GetList<Expression>())
                {
                    while (TrySub(lexicals, SplitFlag.Comma, out var split))
                    {
                        if (split > 0)
                        {
                            if (TryParse(lexicals[0, split - 1], out var result)) expressions.Add(result);
                            else
                            {
                                results = default;
                                return false;
                            }
                        }
                        if (split + 1 < lexicals.Count) lexicals = lexicals[split + 1, -1];
                        else
                        {
                            results = expressions.ToArray();
                            return true;
                        }
                    }
                    if (TryParse(lexicals, out var expression))
                    {
                        expressions.Add(expression);
                        results = expressions.ToArray();
                        return true;
                    }
                    else
                    {
                        results = default;
                        return false;
                    }
                }
            }
            else
            {
                results = new Expression[0];
                return true;
            }
        }
        private static Anchor GetAnchor(ListSegment<Lexical> list)
        {
            return new Anchor(list[0].anchor.textInfo, list[0].anchor.start, list[-1].anchor.end);
        }
        private static bool TryGetHexValue(char c, out byte value)
        {
            if (c >= '0' && c <= '9')
            {
                value = (byte)(c - '0');
                return true;
            }
            value = (byte)(c | 0x20);
            if (value >= 'a' && value <= 'f')
            {
                value -= (byte)'a' - 10;
                return true;
            }
            return false;
        }
    }
}
