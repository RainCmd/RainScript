using RainScript.Compiler.LogicGenerator.Expressions;
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
        public readonly CollectionPool pool;
        public readonly ExceptionCollector exceptions;
        public ExpressionParser(DeclarationManager manager, Context context, LocalContext localContext, CollectionPool pool, ExceptionCollector exceptions)
        {
            this.manager = manager;
            this.context = context;
            this.localContext = localContext;
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
            else return context.TryFindDeclaration(manager, name, out result, pool, exceptions);
        }
        public CompilingType GetVariableType(Declaration declaration)
        {
            if (declaration.code == DeclarationCode.LocalVariable)
            {
                if (localContext.TryGetLocal(declaration, out var local)) return local.type;
            }
            else if (declaration.code == DeclarationCode.MemberVariable)
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
        public void BuildLambda(CompilingType functionType, CompilingType[] returns, CompilingType[] parameters, Anchor[] parameterNames, out Expression expression, out LambdaFunction lambda)
        {
            var declaration = new Declaration(LIBRARY.SELF, Visibility.Space, DeclarationCode.Lambda, (uint)manager.library.lambdas.Count, 0, 0);
            expression = new DelegateCreateLambdaFunctionExpression(default, declaration, functionType);
            lambda = new LambdaFunction(declaration, context, parameters, pool.GetList<Statement>());
            manager.lambdas.Add(lambda);
            manager.library.lambdas.Add(new Compiling.Function(declaration, context.space, returns, parameters, parameterNames, pool));
        }
        private bool IsDecidedTypes(CompilingType[] types)
        {
            foreach (var type in types)
                if (type == RelyKernel.NULL_TYPE || type == RelyKernel.BLURRY_TYPE) return false;
            return true;
        }
        private bool CanConvert(CompilingType source, CompilingType type, out bool convert)
        {
            if (source == RelyKernel.BLURRY_TYPE)
            {
                convert = default;
                return false;
            }
            else if (type == RelyKernel.BLURRY_TYPE)
            {
                convert = true;
                return source != RelyKernel.BLURRY_TYPE && source != RelyKernel.NULL_TYPE;
            }
            else if (source == RelyKernel.NULL_TYPE)
            {
                if (type == RelyKernel.ENTITY_TYPE)
                {
                    convert = true;
                    return true;
                }
                else if (type.dimension > 0 || type.definition.code == TypeCode.Handle || type.definition.code == TypeCode.Interface || type.definition.code == TypeCode.Function || type.definition.code == TypeCode.Coroutine)
                {
                    convert = true;
                    return true;
                }
            }
            else if (type == source)
            {
                convert = false;
                return true;
            }
            else if (type == RelyKernel.REAL_TYPE)
            {
                if (source == RelyKernel.INTEGER_TYPE)
                {
                    convert = true;
                    return true;
                }
            }
            else if (type == RelyKernel.REAL2_TYPE)
            {
                if (source == RelyKernel.REAL3_TYPE)
                {
                    convert = true;
                    return true;
                }
                else if (source == RelyKernel.REAL4_TYPE)
                {
                    convert = true;
                    return true;
                }
            }
            else if (type == RelyKernel.REAL3_TYPE)
            {
                if (source == RelyKernel.REAL2_TYPE)
                {
                    convert = true;
                    return true;
                }
                else if (source == RelyKernel.REAL4_TYPE)
                {
                    convert = true;
                    return true;
                }
            }
            else if (type == RelyKernel.REAL4_TYPE)
            {
                if (source == RelyKernel.REAL2_TYPE)
                {
                    convert = true;
                    return true;
                }
                else if (source == RelyKernel.REAL3_TYPE)
                {
                    convert = true;
                    return true;
                }
            }
            else if (manager.TryGetInherit(type, source, out _))
            {
                convert = true;
                return true;
            }
            convert = default;
            return false;
        }
        public bool TryAssignmentConvert(Expression[] sources, CompilingType[] types, out Expression result)
        {
            for (int index = 0, typeIndex = 0; index < sources.Length; index++)
            {
                var expression = sources[index];
                if (expression.returns.Length == 1)
                {
                    if (TryAssignmentConvert(expression, types[typeIndex], out expression))
                    {
                        sources[index] = expression;
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
                        if (!CanConvert(expression.returns[i], types[typeIndex++], out _))
                        {
                            result = default;
                            return false;
                        }
            }
            return TryAssignmentConvert(TupleExpression.Combine(sources), types, out result);
        }
        public unsafe bool TryAssignmentConvert(Expression source, CompilingType[] types, out Expression result)
        {
            if (source.returns.Length == types.Length)
            {
                var count = 0;
                var converts = stackalloc int[types.Length];
                for (int i = 0; i < types.Length; i++)
                    if (!CanConvert(source.returns[i], types[i], out var convert))
                    {
                        result = default;
                        return false;
                    }
                    else if (convert) converts[count++] = i;
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
            return false;
        }
        public bool TryAssignmentConvert(Expression source, CompilingType type, out Expression result)
        {
            if (source.returns.Length == 1)
            {
                var st = source.returns[0];
                if (type == RelyKernel.BLURRY_TYPE)
                {
                    if (st != RelyKernel.NULL_TYPE && st != RelyKernel.BLURRY_TYPE)
                    {
                        result = source;
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
                                        return true;
                                    }
                                }
                                else if (source is MethodMemberExpression memberMethod)
                                {
                                    if (manager.TryGetFunction(memberMethod.declaration, parameters, returns, out var functionDeclaration))
                                    {
                                        result = new DelegateCreateMemberFunctionExpression(source.anchor, functionDeclaration, memberMethod.target, type);
                                        return true;
                                    }
                                }
                                else if (source is MethodVirtualExpression virtualMethod)
                                {
                                    if (manager.TryGetFunction(virtualMethod.declaration, parameters, returns, out var functionDeclaration))
                                    {
                                        result = new DelegateCreateVirtualMemberFunctionExpression(source.anchor, functionDeclaration, virtualMethod.target, type);
                                        return true;
                                    }
                                }
                                else if (source is MethodQuestionExpression questionMethod)
                                {
                                    if (manager.TryGetFunction(questionMethod.declaration, parameters, returns, out var functionDeclaration))
                                    {
                                        result = new DelegateCreateQuestionMemberFunctionExpression(source.anchor, functionDeclaration, questionMethod.target, type);
                                        return true;
                                    }
                                }
                                else if (source is BlurryLambdaExpression lambda)
                                {
                                    if (lambda.parameters.Length == parameters.Length)
                                    {
                                        using (var localContext = new LocalContext(pool))
                                        {
                                            localContext.PushBlock();
                                            for (int i = 0; i < parameters.Length; i++)
                                                localContext.AddLocal(lambda.parameters[i], parameters[i]);
                                            var parser = new ExpressionParser(manager, context, localContext, pool, exceptions);
                                            if (parser.TryParseTuple(lambda.body, out var expressions))
                                            {
                                                BuildLambda(type, returns, parameters, lambda.parameters, out result, out var lambdaFunction);
                                                if (parser.TryAssignmentConvert(expressions, returns, out var expression))
                                                {
                                                    lambdaFunction.statements.Add(new ReturnStatement(expression.anchor, expression));
                                                    return true;
                                                }
                                                else if (returns.Length == 0)
                                                {
                                                    foreach (var item in expressions)
                                                        if (!IsDecidedTypes(item.returns))
                                                        {
                                                            result = default;
                                                            exceptions.Add(item.anchor, CompilingExceptionCode.COMPILING_EQUIVOCAL);
                                                            return false;
                                                        }
                                                    foreach (var item in expressions)
                                                        lambdaFunction.statements.Add(new ExpressionStatement(item));
                                                    return true;
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
                                result = new CoroutineCreateExpression(source.anchor, blurryCoroutine.invoker, type);
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
                        return true;
                    }
                    else if (type.dimension > 0 || type.definition.code == TypeCode.Handle || type.definition.code == TypeCode.Interface || type.definition.code == TypeCode.Function || type.definition.code == TypeCode.Coroutine)
                    {
                        result = new ConstantHandleNullExpression(source.anchor, type);
                        return true;
                    }
                }
                else if (st == type)
                {
                    result = source;
                    return true;
                }
                else if (type == RelyKernel.REAL_TYPE)
                {
                    if (st == RelyKernel.INTEGER_TYPE)
                    {
                        result = new IntegerToRealExpression(source.anchor, source);
                        return true;
                    }
                }
                else if (type == RelyKernel.REAL2_TYPE)
                {
                    if (st == RelyKernel.REAL3_TYPE)
                    {
                        result = new Real3ToReal2Expression(source.anchor, source);
                        return true;
                    }
                    else if (st == RelyKernel.REAL4_TYPE)
                    {
                        result = new Real4ToReal2Expression(source.anchor, source);
                        return true;
                    }
                }
                else if (type == RelyKernel.REAL3_TYPE)
                {
                    if (st == RelyKernel.REAL2_TYPE)
                    {
                        result = new Real2ToReal3Expression(source.anchor, source);
                        return true;
                    }
                    else if (st == RelyKernel.REAL4_TYPE)
                    {
                        result = new Real4ToReal3Expression(source.anchor, source);
                        return true;
                    }
                }
                else if (type == RelyKernel.REAL4_TYPE)
                {
                    if (st == RelyKernel.REAL2_TYPE)
                    {
                        result = new Real2ToReal4Expression(source.anchor, source);
                        return true;
                    }
                    else if (st == RelyKernel.REAL3_TYPE)
                    {
                        result = new Real3ToReal4Expression(source.anchor, source);
                        return true;
                    }
                }
                else if (manager.TryGetInherit(type, st, out _))
                {
                    result = source;
                    return true;
                }
            }
            result = default;
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
                                if (breacket.type == LexicalType.BracketLeft0) break;
                                else
                                {
                                    exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    exceptions.Add(breacket.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    return false;
                                }
                            }
                            else if (flag.ContainAny(SplitFlag.Bracket0)) return true;
                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            return false;
                        case LexicalType.BracketRight1:
                            if (stack.Count > 0)
                            {
                                var breacket = stack.Pop();
                                if (breacket.type == LexicalType.BracketLeft1) break;
                                else
                                {
                                    exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    exceptions.Add(breacket.anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                    return false;
                                }
                            }
                            else if (flag.ContainAny(SplitFlag.Bracket1)) return true;
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
                            if (stack.Count == 0 && flag.ContainAny(SplitFlag.Assignment)) return true;
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
                        if (index >= lambdaIndex || lexical.type != LexicalType.Comma)
                        {
                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                            result = default;
                            return false;
                        }
                        index++;
                    }
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
                if (TryParse(lexicals[0, assignmentIndex - 1], out var left) && TryParse(lexicals[assignmentIndex + 1, -1], out var right))
                {
                    if (left.Attribute.ContainAny(TokenAttribute.Variable))
                    {
                        var assignment = lexicals[assignmentIndex];
                        if (left.returns.Length > 1 && left.returns.Length == right.returns.Length)
                        {
                            if (assignment.type == LexicalType.Assignment && TryAssignmentConvert(right, left.returns, out right))
                            {
                                result = new TupleAssignmentExpression(assignment.anchor, new Expression[] { left, right }, left.returns);
                                return true;
                            }
                            else exceptions.Add(lexicals[0, assignmentIndex - 1], CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                        }
                        else if (left.returns.Length == 1 || right.returns.Length == 1)
                        {
                            var lrt = left.returns[0];
                            var rrt = right.returns[0];
                            switch (assignment.type)
                            {
                                case LexicalType.Assignment:
                                    if (TryAssignmentConvert(right, left.returns[0], out right))
                                    {
                                        result = new VariableAssignmentExpression(assignment.anchor, left, right, left.returns[0]);
                                        return true;
                                    }
                                    exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                #region 运算
                                case LexicalType.BitAndAssignment:
                                    if (lrt == RelyKernel.BOOL_TYPE && rrt == RelyKernel.BOOL_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.BOOL_And, left, right, RelyKernel.BOOL_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    else if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.INTEGER_And, left, right, RelyKernel.INTEGER_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                case LexicalType.BitOrAssignment:
                                    if (lrt == RelyKernel.BOOL_TYPE && rrt == RelyKernel.BOOL_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.BOOL_Or, left, right, RelyKernel.BOOL_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    else if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.INTEGER_Or, left, right, RelyKernel.INTEGER_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                case LexicalType.BitXorAssignment:
                                    if (lrt == RelyKernel.BOOL_TYPE && rrt == RelyKernel.BOOL_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.BOOL_Xor, left, right, RelyKernel.BOOL_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    else if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.INTEGER_Xor, left, right, RelyKernel.INTEGER_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                case LexicalType.ShiftLeftAssignment:
                                    if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.INTEGER_LeftShift, left, right, RelyKernel.INTEGER_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                case LexicalType.ShiftRightAssignment:
                                    if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.INTEGER_RightShift, left, right, RelyKernel.INTEGER_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                case LexicalType.PlusAssignment:
                                    if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.INTEGER_Plus, left, right, RelyKernel.INTEGER_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    else if (lrt == RelyKernel.REAL_TYPE)
                                    {
                                        if (rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new IntegerToRealExpression(right.anchor, right);
                                            rrt = RelyKernel.REAL2_TYPE;
                                        }
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL_Plus, left, right, RelyKernel.INTEGER_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL2_Plus, left, right, RelyKernel.REAL2_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL3_Plus, left, right, RelyKernel.REAL3_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL4_Plus, left, right, RelyKernel.REAL4_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    else if (lrt == RelyKernel.STRING_TYPE && rrt == RelyKernel.STRING_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.STRING_Combine, left, right, RelyKernel.STRING_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                case LexicalType.MinusAssignment:
                                    if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.INTEGER_Minus, left, right, RelyKernel.INTEGER_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    else if (lrt == RelyKernel.REAL_TYPE)
                                    {
                                        if (rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new IntegerToRealExpression(right.anchor, right);
                                            rrt = RelyKernel.REAL2_TYPE;
                                        }
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL_Minus, left, right, RelyKernel.INTEGER_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL2_Minus, left, right, RelyKernel.REAL2_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL3_Minus, left, right, RelyKernel.REAL3_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL4_Minus, left, right, RelyKernel.REAL4_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                case LexicalType.MulAssignment:
                                    if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.INTEGER_Multiply, left, right, RelyKernel.INTEGER_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    else if (lrt == RelyKernel.REAL_TYPE)
                                    {
                                        if (rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new IntegerToRealExpression(right.anchor, right);
                                            rrt = RelyKernel.REAL2_TYPE;
                                        }
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL_Multiply, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    else if (lrt == RelyKernel.REAL2_TYPE)
                                    {
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL2_Multiply_vr, left, right, RelyKernel.REAL2_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL2_Multiply_vv, left, right, RelyKernel.REAL2_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    else if (lrt == RelyKernel.REAL3_TYPE)
                                    {
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL3_Multiply_vr, left, right, RelyKernel.REAL2_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL3_Multiply_vv, left, right, RelyKernel.REAL3_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    else if (lrt == RelyKernel.REAL4_TYPE)
                                    {
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL4_Multiply_vr, left, right, RelyKernel.REAL2_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL4_Multiply_vv, left, right, RelyKernel.REAL4_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                case LexicalType.DivAssignment:
                                    if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.INTEGER_Divide, left, right, RelyKernel.INTEGER_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    else if (lrt == RelyKernel.REAL_TYPE)
                                    {
                                        if (rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new IntegerToRealExpression(right.anchor, right);
                                            rrt = RelyKernel.REAL2_TYPE;
                                        }
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL_Divide, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    else if (lrt == RelyKernel.REAL2_TYPE)
                                    {
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL2_Divide_vr, left, right, RelyKernel.REAL2_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL2_Divide_vv, left, right, RelyKernel.REAL2_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    else if (lrt == RelyKernel.REAL3_TYPE)
                                    {
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL3_Divide_vr, left, right, RelyKernel.REAL2_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        else if (rrt == RelyKernel.REAL4_TYPE)
                                        {
                                            right = new Real4ToReal3Expression(right.anchor, right);
                                            rrt = RelyKernel.REAL3_TYPE;
                                        }
                                        if (rrt == RelyKernel.REAL3_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL3_Divide_vv, left, right, RelyKernel.REAL3_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    else if (lrt == RelyKernel.REAL4_TYPE)
                                    {
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL4_Divide_vr, left, right, RelyKernel.REAL2_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        if (rrt == RelyKernel.REAL4_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL4_Divide_vv, left, right, RelyKernel.REAL4_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                case LexicalType.ModAssignment:
                                    if (lrt == RelyKernel.INTEGER_TYPE && rrt == RelyKernel.INTEGER_TYPE)
                                    {
                                        right = new OperationExpression(assignment.anchor, CommandMacro.INTEGER_Mod, left, right, RelyKernel.INTEGER_TYPE);
                                        goto case LexicalType.Assignment;
                                    }
                                    else if (lrt == RelyKernel.REAL_TYPE)
                                    {
                                        if (rrt == RelyKernel.INTEGER_TYPE)
                                        {
                                            right = new IntegerToRealExpression(right.anchor, right);
                                            rrt = RelyKernel.REAL2_TYPE;
                                        }
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL_Mod, left, right, RelyKernel.INTEGER_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    else if (lrt == RelyKernel.REAL2_TYPE)
                                    {
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL2_Mod_vr, left, right, RelyKernel.REAL2_TYPE);
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
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL2_Mod_vv, left, right, RelyKernel.REAL2_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    else if (lrt == RelyKernel.REAL3_TYPE)
                                    {
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL3_Mod_vr, left, right, RelyKernel.REAL2_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        else if (rrt == RelyKernel.REAL4_TYPE)
                                        {
                                            right = new Real4ToReal3Expression(right.anchor, right);
                                            rrt = RelyKernel.REAL3_TYPE;
                                        }
                                        if (rrt == RelyKernel.REAL3_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL3_Mod_vv, left, right, RelyKernel.REAL3_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    else if (lrt == RelyKernel.REAL4_TYPE)
                                    {
                                        if (rrt == RelyKernel.REAL_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL4_Mod_vr, left, right, RelyKernel.REAL2_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                        if (rrt == RelyKernel.REAL4_TYPE)
                                        {
                                            right = new OperationExpression(assignment.anchor, CommandMacro.REAL4_Mod_vv, left, right, RelyKernel.REAL4_TYPE);
                                            goto case LexicalType.Assignment;
                                        }
                                    }
                                    exceptions.Add(assignment.anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    break;
                                #endregion
                                default: throw ExceptionGeneratorCompiler.InvalidLexicalType(assignment.type);
                            }
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
            if (questionIndex == 0 || questionIndex + 4 >= lexicals.Count) exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
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
                if (index + 1 < bracketIndex)
                {
                    if (TryParseTuple(lexicals[index + 1, bracketIndex - 1], out expressions))
                    {
                        index = bracketIndex;
                        return true;
                    }
                }
                else
                {
                    expressions = new Expression[0];
                    index = bracketIndex;
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
        private TokenAttribute PopToken(ScopeStack<Expression> expressionStack, Token token)
        {

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
        private bool TryPushDeclarationExpression(ListSegment<Lexical> lexicals, ref int index, ScopeStack<Expression> expressionStack, Anchor anchor, Declaration declaration, ref TokenAttribute attribute)
        {
            if (declaration.code == DeclarationCode.Definition || declaration.code == DeclarationCode.Delegate || declaration.code == DeclarationCode.Coroutine || declaration.code == DeclarationCode.Interface)
            {
                index++;
                var dimension = Lexical.ExtractDimension(lexicals, ref index);
                var expression = new TypeExpression(anchor, new CompilingType(new CompilingDefinition(declaration), dimension));
                expressionStack.Push(expression);
                attribute = TokenAttribute.Type;
                index--;
                return true;
            }
            else if (declaration.code == DeclarationCode.GlobalVariable)
            {
                var expression = new VariableGlobalExpression(anchor, declaration, IsConstant(declaration), GetVariableType(declaration));
                expressionStack.Push(expression);
                attribute = expression.Attribute;
            }
            else if (declaration.code == DeclarationCode.GlobalMethod)
            {
                var expression = new MethodGlobalExpression(anchor, declaration);
                expressionStack.Push(expression);
                attribute = expression.Attribute;
            }
            else if (declaration.code == DeclarationCode.NativeMethod)
            {
                var expression = new MethodNativeExpression(anchor, declaration);
                expressionStack.Push(expression);
                attribute = expression.Attribute;
            }
            return false;
        }
        public bool TryParse(ListSegment<Lexical> lexicals, out Expression result)
        {
            if (TrySub(lexicals, SplitFlag.Lambda, out var lambdaIndex)) return TryParseLambda(lexicals, lambdaIndex, out result);
            else if (TrySub(lexicals, SplitFlag.Assignment, out var assignmentIndex)) return TryParseAssignment(lexicals, assignmentIndex, out result);
            else if (TrySub(lexicals, SplitFlag.Question, out var questionIndex)) return TryParseQuestion(lexicals, questionIndex, out result);
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
                                    if (expressions.Length == 0) exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                                    else if (attribute.ContainAny(TokenAttribute.Method))
                                    {
                                        var methodExpression = expressionStack.Pop();
                                        if (methodExpression is MethodMemberExpression memberMethod)
                                        {
                                            var method = manager.GetMethod(memberMethod.declaration);
                                            for (int i = 0; i < method.FunctionCount; i++)
                                                if (TryAssignmentConvert(expressions, method.GetFunction(i).Parameters, out var parameter))
                                                {
                                                    var function = method.GetFunction(i);
                                                    if (!context.IsVisible(manager, function.Declaration)) exceptions.Add(methodExpression.anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_VISIBLE);
                                                    var expression = new InvokerMemberExpression(methodExpression.anchor, function.Declaration, memberMethod.target, parameter, function.Returns);
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    goto next_lexical;
                                                }
                                        }
                                        else if (methodExpression is MethodVirtualExpression virtualMethod)
                                        {
                                            var method = manager.GetMethod(virtualMethod.declaration);
                                            for (int i = 0; i < method.FunctionCount; i++)
                                                if (TryAssignmentConvert(expressions, method.GetFunction(i).Parameters, out var parameter))
                                                {
                                                    var function = method.GetFunction(i);
                                                    if (!context.IsVisible(manager, function.Declaration)) exceptions.Add(methodExpression.anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_VISIBLE);
                                                    var expression = new InvokerVirtualMemberExpression(methodExpression.anchor, function.Declaration, virtualMethod.target, parameter, function.Returns);
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    goto next_lexical;
                                                }
                                        }
                                        else if (methodExpression is MethodQuestionExpression questionMethod)
                                        {
                                            var method = manager.GetMethod(questionMethod.declaration);
                                            for (int i = 0; i < method.FunctionCount; i++)
                                                if (TryAssignmentConvert(expressions, method.GetFunction(i).Parameters, out var parameter))
                                                {
                                                    var function = method.GetFunction(i);
                                                    if (!context.IsVisible(manager, function.Declaration)) exceptions.Add(methodExpression.anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_VISIBLE);
                                                    var expression = new InvokerQuestionMemberExpression(methodExpression.anchor, function.Declaration, questionMethod.target, parameter, function.Returns);
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    goto next_lexical;
                                                }
                                        }
                                        else if (methodExpression is MethodGlobalExpression globalMethod)
                                        {
                                            var method = manager.GetMethod(globalMethod.declaration);
                                            for (int i = 0; i < method.FunctionCount; i++)
                                                if (TryAssignmentConvert(expressions, method.GetFunction(i).Parameters, out var parameter))
                                                {
                                                    var function = method.GetFunction(i);
                                                    if (!context.IsVisible(manager, function.Declaration)) exceptions.Add(methodExpression.anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_VISIBLE);
                                                    var expression = new InvokerGlobalExpression(methodExpression.anchor, function.Declaration, parameter, function.Returns);
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    goto next_lexical;
                                                }
                                        }
                                        else if (methodExpression is MethodNativeExpression netiveMethod)
                                        {
                                            var method = manager.GetMethod(netiveMethod.declaration);
                                            for (int i = 0; i < method.FunctionCount; i++)
                                                if (TryAssignmentConvert(expressions, method.GetFunction(i).Parameters, out var parameter))
                                                {
                                                    var function = method.GetFunction(i);
                                                    if (!context.IsVisible(manager, function.Declaration)) exceptions.Add(methodExpression.anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_VISIBLE);
                                                    var expression = new InvokerNativeExpression(methodExpression.anchor, function.Declaration, parameter, function.Returns);
                                                    expressionStack.Push(expression);
                                                    attribute = expression.Attribute;
                                                    goto next_lexical;
                                                }
                                        }
                                        exceptions.Add(lexicals, CompilingExceptionCode.GENERATOR_FUNCTION_NOT_FOUND);
                                        goto parse_fail;
                                    }
                                    else if (attribute.ContainAny(TokenAttribute.Function))
                                    {
                                        var delegateExpression = expressionStack.Pop();
                                        if (delegateExpression.returns.Length != 1 || delegateExpression.returns[0].dimension > 0 || delegateExpression.returns[0].definition.code != TypeCode.Function) throw ExceptionGeneratorCompiler.InvalidCompilingType(delegateExpression.returns[0]);
                                        var delegateType = delegateExpression.returns[0];
                                        var declaration = new Declaration(delegateType.definition.library, delegateType.definition.visibility, DeclarationCode.Delegate, delegateType.definition.index, 0, 0);
                                        if (manager.TryGetReturns(declaration, out var returns) && manager.TryGetParameters(declaration, out var parameters) && TryAssignmentConvert(expressions, parameters, out var parameter))
                                        {
                                            var expression = new InvokerDelegateExpression(delegateExpression.anchor, delegateExpression, parameter, returns);
                                            expressionStack.Push(expression);
                                            attribute = expression.Attribute;
                                            break;
                                        }
                                        exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
                                        goto parse_fail;
                                    }
                                    else if (attribute.ContainAny(TokenAttribute.None | TokenAttribute.Operator))
                                    {
                                        if (TryCombineExpressions(out var expression, expressions))
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
                                                expression = new ArrayEvaluationExpression(lexical.anchor, array, expression, new CompilingType(array.returns[0].definition, array.returns[0].dimension - 1));
                                                break;
                                            }
                                            else if (expression.returns.Length == 2)
                                            {
                                                expression = new ArraySubExpression(lexical.anchor, array, expression);
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
                                                }
                                        }
                                        else
                                        {
                                            exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_UNKNONW);
                                            goto parse_fail;
                                        }
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
                            if (attribute.ContainAny(TokenAttribute.None)) PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Positive), attribute);
                            else PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Plus), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.Increment:
                            if (attribute.ContainAny(TokenAttribute.Variable)) PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.IncrementRight), attribute);
                            else PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.IncrementLeft), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.PlusAssignment: goto default;
                        case LexicalType.Minus:
                            if (attribute.ContainAny(TokenAttribute.None)) PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Negative), attribute);
                            else PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Minus), attribute);
                            attribute = TokenAttribute.Operator;
                            break;
                        case LexicalType.Decrement:
                            if (attribute.ContainAny(TokenAttribute.Variable)) PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.DecrementRight), attribute);
                            else PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.DecrementLeft), attribute);
                            break;
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
                                    if (attribute.ContainAny(TokenAttribute.Constant | TokenAttribute.Variable | TokenAttribute.Temporary | TokenAttribute.Array | TokenAttribute.Coroutine) && expressionStack.Peek().returns.Length == 1)
                                    {
                                        var expression = expressionStack.Peek();
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
                                                expression = new MethodVirtualExpression(lexical.anchor, expression, declaration);
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
                        case LexicalType.Question: goto default;
                        case LexicalType.QuestionDot:
                            if (index + 1 < lexicals.Count)
                            {
                                lexical = lexicals[++index];
                                if (lexical.type == LexicalType.Word)
                                {
                                    if (attribute.ContainAny(TokenAttribute.Constant | TokenAttribute.Variable | TokenAttribute.Temporary | TokenAttribute.Array | TokenAttribute.Coroutine) && expressionStack.Peek().returns.Length == 1)
                                    {
                                        var expression = expressionStack.Peek();
                                        var type = expression.returns[0];
                                        if (type.dimension > 0) type = RelyKernel.ARRAY_TYPE;
                                        if (context.TryFindMemberDeclarartion(manager, lexical.anchor, type.definition, out var declaration, pool))
                                        {
                                            if (declaration.code == DeclarationCode.MemberVariable)
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
                        case LexicalType.Colon: goto default;
                        #region Constants
                        case LexicalType.ConstReal:
                            {
                                if (real.TryParse(lexical.anchor.Segment.ToString().Replace("_", ""), out var value))
                                    expressionStack.Push(new ConstantRealExpression(lexical.anchor, value));
                                else throw ExceptionGeneratorCompiler.ConstantParseFail(lexical.anchor.Segment);
                                attribute = TokenAttribute.Constant;
                            }
                            break;
                        case LexicalType.ConstNumber:
                            {
                                if (long.TryParse(lexical.anchor.Segment.ToString().Replace("_", ""), out var value))
                                    expressionStack.Push(new ConstantIntegerExpression(lexical.anchor, value));
                                else throw ExceptionGeneratorCompiler.ConstantParseFail(lexical.anchor.Segment);
                                attribute = TokenAttribute.Constant;
                            }
                            break;
                        case LexicalType.ConstBinary:
                            {
                                var value = 0;
                                var segment = lexical.anchor.Segment.ToString();
                                for (int i = 0; i < segment.Length; i++)
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
                            }
                            break;
                        case LexicalType.ConstHexadecimal:
                            {
                                var value = 0;
                                var segment = lexical.anchor.Segment.ToString();
                                for (int i = 0; i < segment.Length; i++)
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
                            }
                            break;
                        case LexicalType.ConstChars:
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
                                            if (++index < segment.Length)
                                            {
                                                c = segment[index];
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
                            }
                            break;
                        case LexicalType.ConstString:
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
                                            if (++index < segment.Length)
                                            {
                                                c = segment[index];
                                                if (c == 'a') c = '\a';
                                                else if (c == 'b') c = '\b';
                                                else if (c == 'f') c = '\f';
                                                else if (c == 'n') c = '\n';
                                                else if (c == 'r') c = '\r';
                                                else if (c == 't') c = '\t';
                                                else if (c == 'v') c = '\v';
                                                else if (c == '0') c = '\0';
                                                builder.Append(c);
                                            }
                                        }
                                        else builder.Append(c);
                                    }
                                }
                                expressionStack.Push(new ConstantStringExpression(lexical.anchor, builder.ToString()));
                                attribute = TokenAttribute.Constant;
                            }
                            break;
                        #endregion Constants
                        case LexicalType.Word:
                            {
                                if (lexical.anchor.Segment == KeyWorld.KERNEL)
                                {
                                    if (!TryFindDeclaration(lexicals, ref index, RelyKernel.kernel, out var declaration) || !TryPushDeclarationExpression(lexicals, ref index, expressionStack, lexicals[index].anchor, declaration, ref attribute))
                                        goto parse_fail;
                                }
                                else if (lexical.anchor.Segment == KeyWorld.GLOBAL)
                                {
                                    lexical = lexicals[index];
                                    if (CheckNext(lexicals, ref index, LexicalType.Dot) && CheckNext(lexicals, ref index, LexicalType.Word))
                                    {
                                        ISpace space = null;
                                        if (lexical.anchor.Segment == KeyWorld.KERNEL) space = RelyKernel.kernel;
                                        else if (lexical.anchor.Segment == manager.library.name) space = manager.library;
                                        else foreach (var item in manager.relies)
                                                if (item.name == lexical.anchor.Segment)
                                                {
                                                    space = item;
                                                    break;
                                                }
                                        if (space == null)
                                        {
                                            exceptions.Add(lexical.anchor, CompilingExceptionCode.COMPILING_LIBRARY_NOT_FOUND);
                                            goto parse_fail;
                                        }
                                        else if (!TryFindDeclaration(lexicals, ref index, space, out var declaration) || !TryPushDeclarationExpression(lexicals, ref index, expressionStack, lexicals[index].anchor, declaration, ref attribute))
                                            goto parse_fail;
                                    }
                                }
                                else if (lexical.anchor.Segment == KeyWorld.BASE)
                                {
                                    if (localContext.TryGetLocal(KeyWorld.THIS, out var local))
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
                                                        var expression = new VariableMemberExpression(lexical.anchor, declaration, new VariableLocalExpression(baseAnchor, local.Declaration, local.type), GetVariableType(declaration));
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
                                            else goto default;
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
                            }
                            //todo word
                            break;
                        case LexicalType.Backslash:
                        default:
                            exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                            goto parse_fail;
                    }
                next_lexical:;
                }
            parse_fail:
                result = default;
                return false;
            }
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
    }
}
