using RainScript.Vector;
using RainScript.Compiler.Compiling;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript.Compiler.LogicGenerator
{
    using Expressions;

    internal readonly struct ConstentExpressionPair
    {
        public readonly Compiling.Variable variable;
        public readonly Expression expression;
        public ConstentExpressionPair(Compiling.Variable variable, Expression expression)
        {
            this.variable = variable;
            this.expression = expression;
        }
    }
    internal class LambdaFunction : System.IDisposable
    {
        private readonly Definition definition;
        private readonly Referencable<CodeAddress> entry;
        private uint returnSize;
        private readonly CompilingType[] parameters;
        private readonly Anchor[] parameterNames;
        public readonly ScopeList<Statement> statements;
        private readonly Anchor anchor;
        public LambdaFunction(Anchor anchor, Referencable<CodeAddress> entry, Definition definition, CompilingType[] parameters, Anchor[] parameterNames, CollectionPool pool)
        {
            this.anchor = anchor;
            this.definition = definition;
            this.entry = entry;
            this.parameters = parameters;
            this.parameterNames = parameterNames;
            statements = pool.GetList<Statement>();
        }
        public void SetReturnCount(uint returnCount)
        {
            returnSize = returnCount * 4;
        }
        public void Generate(GeneratorParameter parameter)
        {
            parameter.generator.SetCodeAddress(entry);
            using (var variable = new VariableGenerator(parameter.pool, Frame.SIZE + returnSize))
            {
                var parameterSize = 0u;
                parameter.debug.AddFunction(anchor.textInfo.path, anchor.StartLine, parameter.generator.Point);
                if (definition != null)
                {
                    parameterSize = 4;
                    variable.DecareLocal(0, new CompilingType(new CompilingDefinition(definition.declaration), 0));
                    for (uint i = 0; i < parameters.Length; i++)
                    {
                        var local = variable.DecareLocal(i + 1, parameters[i]);
                        parameter.debug.AddLocalVariable(parameterNames[i], parameter.generator.Point, local.address, parameter.relied.Convert(local.type).RuntimeType);
                        parameterSize = local.address + parameters[i].FieldSize;
                    }
                }
                else for (uint i = 0; i < parameters.Length; i++)
                    {
                        var local = variable.DecareLocal(i, parameters[i]);
                        parameter.debug.AddLocalVariable(parameterNames[i], parameter.generator.Point, local.address, parameter.relied.Convert(local.type).RuntimeType);
                        parameterSize = local.address + parameters[i].FieldSize;
                    }
                var topValue = new Referencable<uint>(parameter.pool);
                parameter.generator.WriteCode(CommandMacro.FUNCTION_Entrance);
                parameter.generator.WriteCode(Frame.SIZE + returnSize + parameterSize);
                parameter.generator.WriteCode(topValue);
                using (var finallyPoint = new Referencable<CodeAddress>(parameter.pool))
                {
                    foreach (var statement in statements) statement.Generator(new StatementGeneratorParameter(parameter, parameter.generator, variable), finallyPoint);
                    parameter.generator.SetCodeAddress(finallyPoint);
                }
                var maxStack = variable.Generator(parameter.generator);
                Tools.MemoryAlignment(ref maxStack);
                topValue.SetValue(parameter.generator, maxStack);
                topValue.Dispose();
            }
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Return);
        }
        public void Dispose()
        {
            foreach (var statement in statements) statement.Dispose();
            statements.Dispose();
        }
    }
    internal class FunctionGenerator : System.IDisposable
    {
        private readonly Definition definition;
        private readonly CompilingType[] parameters;
        private readonly Anchor[] parameterNames;
        private readonly CompilingType[] returns;
        private readonly BlockStatement statements;
        private readonly string file;
        private readonly string fullName;
        private readonly int line;
        public FunctionGenerator(GeneratorParameter parameter)
        {
            file = fullName = "";
            parameters = returns = new CompilingType[0];
            parameterNames = new Anchor[0];
            statements = new BlockStatement(default, parameter.pool);
            using (var pairs = parameter.pool.GetList<ConstentExpressionPair>())
            {
                foreach (var variable in parameter.manager.library.variables)
                    if (variable.constant)
                        using (var lexicals = parameter.pool.GetList<Lexical>())
                            if (Lexical.TryAnalysis(lexicals, variable.expression.exprssion.textInfo, variable.expression.exprssion.Segment, parameter.exceptions))
                            {
                                var context = new Context(variable.space, null, variable.expression.compilings, variable.expression.references);
                                using (var localContext = new LocalContext(parameter.pool))
                                {
                                    var parser = new ExpressionParser(parameter, context, localContext, false);
                                    if (parser.TryParseTuple(lexicals, out var expressions))
                                        if (parser.TryAssignmentConvert(expressions, new CompilingType[] { variable.type }, out var result, out _))
                                            pairs.Add(new ConstentExpressionPair(variable, result));
                                        else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                }
                            }

                while (pairs.Count > 0)
                {
                    var count = pairs.Count;
                    for (int i = 0; i < pairs.Count; i++)
                    {
                        var variable = pairs[i].variable;
                        var expression = pairs[i].expression;
                        if (variable.type == RelyKernel.BOOL_TYPE)
                        {
                            if (expression.TryEvaluation(out bool value, new EvaluationParameter(parameter)))
                            {
                                parameter.generator.WriteData(1, value, variable.address);
                                variable.calculated = true;
                                pairs.FastRemoveAt(i); i--;
                            }
                        }
                        else if (variable.type == RelyKernel.BYTE_TYPE)
                        {
                            if (expression.TryEvaluation(out byte value, new EvaluationParameter(parameter)))
                            {
                                parameter.generator.WriteData(1, value, variable.address);
                                variable.calculated = true;
                                pairs.FastRemoveAt(i); i--;
                            }
                        }
                        else if (variable.type == RelyKernel.INTEGER_TYPE)
                        {
                            if (expression.TryEvaluation(out long value, new EvaluationParameter(parameter)))
                            {
                                parameter.generator.WriteData(8, value, variable.address);
                                variable.calculated = true;
                                pairs.FastRemoveAt(i); i--;
                            }
                        }
                        else if (variable.type == RelyKernel.REAL_TYPE)
                        {
                            if (expression.TryEvaluation(out real value, new EvaluationParameter(parameter)))
                            {
                                parameter.generator.WriteData(8, value, variable.address);
                                variable.calculated = true;
                                pairs.FastRemoveAt(i); i--;
                            }
                        }
                        else if (variable.type == RelyKernel.REAL2_TYPE)
                        {
                            if (expression.TryEvaluation(out Real2 value, new EvaluationParameter(parameter)))
                            {
                                parameter.generator.WriteData(16, value, variable.address);
                                variable.calculated = true;
                                pairs.FastRemoveAt(i); i--;
                            }
                        }
                        else if (variable.type == RelyKernel.REAL3_TYPE)
                        {
                            if (expression.TryEvaluation(out Real3 value, new EvaluationParameter(parameter)))
                            {
                                parameter.generator.WriteData(24, value, variable.address);
                                variable.calculated = true;
                                pairs.FastRemoveAt(i); i--;
                            }
                        }
                        else if (variable.type == RelyKernel.REAL4_TYPE)
                        {
                            if (expression.TryEvaluation(out Real4 value, new EvaluationParameter(parameter)))
                            {
                                parameter.generator.WriteData(32, value, variable.address);
                                variable.calculated = true;
                                pairs.FastRemoveAt(i); i--;
                            }
                        }
                        else if (variable.type == RelyKernel.STRING_TYPE)
                        {
                            if (expression.TryEvaluation(out string value, new EvaluationParameter(parameter)))
                            {
                                parameter.generator.WriteData(value, variable.address, parameter.pool);
                                variable.calculated = true;
                                pairs.FastRemoveAt(i); i--;
                            }
                        }
                        else if (variable.type.IsHandle)
                        {
                            if (expression.TryEvaluationNull())
                            {
                                parameter.generator.WriteData(4, 0u, variable.address);
                                variable.calculated = true;
                                pairs.FastRemoveAt(i); i--;
                            }
                        }
                        else if (variable.type == RelyKernel.ENTITY_TYPE)
                        {
                            if (expression.TryEvaluationNull())
                            {
                                parameter.generator.WriteData(8, Entity.NULL, variable.address);
                                variable.calculated = true;
                                pairs.FastRemoveAt(i); i--;
                            }
                        }
                        else
                        {
                            parameter.exceptions.Add(variable.name, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                            pairs.FastRemoveAt(i); i--;
                        }
                    }
                    if (pairs.Count == count)
                    {
                        foreach (var pair in pairs)
                            parameter.exceptions.Add(pair.variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                        break;
                    }
                }
            }

            foreach (var variable in parameter.manager.library.variables)
                if (!variable.constant && (bool)variable.expression.exprssion)
                    using (var lexicals = parameter.pool.GetList<Lexical>())
                        if (Lexical.TryAnalysis(lexicals, variable.expression.exprssion.textInfo, variable.expression.exprssion.Segment, parameter.exceptions))
                        {
                            var context = new Context(variable.space, null, variable.expression.compilings, variable.expression.references);
                            using (var localContext = new LocalContext(parameter.pool))
                            {
                                var parser = new ExpressionParser(parameter, context, localContext, false);
                                if (parser.TryParseTuple(lexicals, out var expressions))
                                {
                                    if (parser.TryAssignmentConvert(expressions, new CompilingType[] { variable.type }, out var result, out _))
                                        statements.statements.Add(new ExpressionStatement(new VariableAssignmentExpression(variable.name, new VariableGlobalExpression(variable.name, variable.declaration, variable.constant, variable.type), result, variable.type)));
                                    else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                }
                            }
                        }
        }
        public FunctionGenerator(GeneratorParameter parameter, Compiling.Function function)
        {
            file = function.name.textInfo.path;
            fullName = parameter.manager.GetDeclarationFullName(function.declaration);
            line = function.name.StartLine;
            statements = new BlockStatement(default, parameter.pool);
            parameters = function.parameters;
            parameterNames = function.parameterNames;
            returns = function.returns;
            var localContext = new LocalContext(parameter.pool);
            localContext.PushBlock(parameter.pool);
            if (function.declaration.code == DeclarationCode.MemberFunction)
            {
                definition = parameter.manager.library.definitions[(int)function.declaration.definitionIndex];
                localContext.AddLocal(KeyWord.THIS, definition.name, new CompilingType(new CompilingDefinition(definition.declaration), 0));
                for (int i = 0; i < parameters.Length; i++) localContext.AddLocal(function.parameterNames[i], parameters[i]);
            }
            else if (function.declaration.code == DeclarationCode.ConstructorFunction)
            {
                definition = parameter.manager.library.definitions[(int)function.declaration.definitionIndex];
                var type = new CompilingType(new CompilingDefinition(definition.declaration), 0);
                var thisValue = localContext.AddLocal(KeyWord.THIS, definition.name, type);
                var thisExpression = new VariableLocalExpression(definition.name, thisValue.Declaration, TokenAttribute.Value, type);
                for (int i = 0; i < parameters.Length; i++) localContext.AddLocal(function.parameterNames[i], parameters[i]);
                var logic = definition.constructorInvaokerExpressions[function.declaration.overloadIndex];
                if ((bool)logic.exprssion)
                {
                    using (var lexicals = parameter.pool.GetList<Lexical>())
                        if (Lexical.TryAnalysis(lexicals, logic.exprssion.textInfo, logic.exprssion.Segment, parameter.exceptions) && lexicals.Count > 0)
                        {
                            var lexical = lexicals[0];
                            if (lexical.anchor.Segment == KeyWord.THIS) ParseCtorInvoker(parameter, function, lexical.anchor, lexicals, localContext, new Declaration(LIBRARY.SELF, Visibility.Public, DeclarationCode.Constructor, definition.constructors, 0, definition.declaration.index), thisExpression);
                            else if (lexical.anchor.Segment == KeyWord.BASE)
                            {
                                if (!parameter.manager.TryGetDefinition(definition.parent, out var result)) throw ExceptionGeneratorCompiler.Unknown();
                                if (result.Constructor == LIBRARY.METHOD_INVALID) parameter.exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_FUNCTION_NOT_FOUND);
                                else ParseCtorInvoker(parameter, function, lexical.anchor, lexicals, localContext, new Declaration(result.Declaration.library, Visibility.Public, DeclarationCode.Constructor, result.Constructor, 0, result.Declaration.index), thisExpression);
                            }
                            else
                            {
                                parameter.exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                return;
                            }
                        }
                        else if (parameter.manager.TryGetDefinition(definition.parent, out var result) && result.Declaration.library != LIBRARY.KERNEL)
                            ParseCtorInvoker(parameter, function, logic.exprssion, null, localContext, new Declaration(result.Declaration.library, Visibility.Public, DeclarationCode.Constructor, result.Constructor, 0, result.Declaration.index), thisExpression);
                }
                else if (parameter.manager.TryGetDefinition(definition.parent, out var result) && result.Declaration.library != LIBRARY.KERNEL)
                    ParseCtorInvoker(parameter, function, function.name, null, localContext, new Declaration(result.Declaration.library, Visibility.Public, DeclarationCode.Constructor, result.Constructor, 0, result.Declaration.index), thisExpression);
                InitMemberVariable(parameter, thisExpression);
            }
            else for (int i = 0; i < parameters.Length; i++) localContext.AddLocal(function.parameterNames[i], parameters[i]);
            if ((bool)function.body.body) Parse(parameter, function.space, function.body, localContext, false);
            if (returns.Length > 0 && !CheckReturn(statements))
                parameter.exceptions.Add(function.name, CompilingExceptionCode.GENERATOR_MISSING_RETURN);
            localContext.Dispose();
        }
        public FunctionGenerator(GeneratorParameter parameter, Definition definition)
        {
            file = definition.name.textInfo.path;
            fullName = parameter.manager.GetDeclarationFullName(definition.declaration) + ".~";
            line = (bool)definition.destructor.body ? definition.destructor.body.start : definition.name.StartLine;
            statements = new BlockStatement(default, parameter.pool);
            var localContext = new LocalContext(parameter.pool);
            localContext.PushBlock(parameter.pool);
            localContext.AddLocal(KeyWord.THIS, definition.name, new CompilingType(new CompilingDefinition(definition.declaration), 0));
            this.definition = definition;
            parameters = returns = new CompilingType[0];
            parameterNames = new Anchor[0];
            if ((bool)definition.destructor.body) Parse(parameter, definition.space, definition.destructor, localContext, true);
            localContext.Dispose();
        }
        private void InitMemberVariable(GeneratorParameter parameter, Expression thisExpression)
        {
            foreach (var variable in definition.variables)
                if ((bool)variable.expression.exprssion)
                    using (var lexicals = parameter.pool.GetList<Lexical>())
                        if (Lexical.TryAnalysis(lexicals, variable.expression.exprssion.textInfo, variable.expression.exprssion.Segment, parameter.exceptions))
                        {
                            var context = new Context(variable.space, null, variable.expression.compilings, variable.expression.references);
                            using (var localContext = new LocalContext(parameter.pool))
                            {
                                var parser = new ExpressionParser(parameter, context, localContext, false);
                                if (parser.TryParseTuple(lexicals, out var expressions))
                                {
                                    if (parser.TryAssignmentConvert(expressions, new CompilingType[] { variable.type }, out var result, out _))
                                        statements.statements.Add(new ExpressionStatement(new VariableAssignmentExpression(variable.name, new VariableMemberExpression(variable.name, variable.declaration, thisExpression, variable.type), result, variable.type)));
                                    else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                }
                            }
                        }
        }
        private void ParseCtorInvoker(GeneratorParameter parameter, Compiling.Function function, Anchor anchor, ScopeList<Lexical> lexicals, LocalContext localContext, Declaration method, Expression thisExpression)
        {
            var context = new Context(function.space, definition, function.body.compilings, function.body.references);
            var parser = new ExpressionParser(parameter, context, localContext, false);
            var expressions = new Expression[0];
            if (lexicals == null || lexicals.Count < 2 || parser.TryParseTuple(lexicals[1, -1], out expressions))
                if (parser.TryGetFunction(anchor, parser.manager.GetMethod(method), expressions, out var ctor, out var ctorParameter))
                    statements.statements.Add(new ExpressionStatement(new InvokerMemberExpression(anchor, ctor.Declaration, thisExpression, ctorParameter, returns)));
                else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_FUNCTION_NOT_FOUND);
        }
        private void ParseBranchStatement(GeneratorParameter parameter, ScopeList<Statement> statements, ScopeList<Lexical> lexicals, Anchor anchor, Context context, LocalContext localContext, bool destructor)
        {
            if (lexicals.Count > 1)
            {
                var parser = new ExpressionParser(parameter, context, localContext, destructor);
                if (parser.TryParse(lexicals[1, -1], out var condition))
                {
                    if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.BOOL_TYPE)
                        statements.Add(new BranchStatement(anchor, condition, new BlockStatement(anchor, parameter.pool), new BlockStatement(anchor, parameter.pool)));
                    else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                }
            }
            else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
        }
        private void Parse(GeneratorParameter parameter, Compiling.Space space, LogicBody logicBody, LocalContext localContext, bool destructor)
        {
            var context = new Context(space, definition, logicBody.compilings, logicBody.references);
            statements.indent = logicBody.body.text[logicBody.body.start].indent;
            using (var statementStack = parameter.pool.GetStack<BlockStatement>())
            {
                statementStack.Push(statements);
                for (int lineIndex = logicBody.body.start; lineIndex <= logicBody.body.end; lineIndex++)
                {
                    var line = logicBody.body.text[lineIndex];
                    var blockIndent = statementStack.Peek().indent;
                    if (blockIndent < line.indent)
                    {
                        localContext.PushBlock(parameter.pool);
                        if (statementStack.Peek().statements.Count > 0)
                        {
                            var statement = statementStack.Peek().statements[-1];
                            if (statement is BranchStatement branchStatement)
                            {
                                branchStatement.trueBranch.indent = line.indent;
                                statementStack.Push(branchStatement.trueBranch);
                            }
                            else if (statement is LoopStatement loopStatement)
                            {
                                loopStatement.loopBlock.indent = line.indent;
                                statementStack.Push(loopStatement.loopBlock);
                            }
                            else if (statement is ElseStatement elseStatement)
                            {
                                statementStack.Peek().statements.RemoveAt(-1);
                                elseStatement.statements.indent = line.indent;
                                statementStack.Push(elseStatement.statements);
                            }
                            else if (statement is TryStatement tryStatement)
                            {
                                var block = tryStatement.finallyBlock ?? tryStatement.catchBlock ?? tryStatement.tryBlock;
                                block.indent = line.indent;
                                statementStack.Push(block);
                            }
                            else
                            {
                                var block = new BlockStatement(default, parameter.pool);
                                block.indent = line.indent;
                                statementStack.Push(block);
                            }
                        }
                        else
                        {
                            var block = new BlockStatement(default, parameter.pool);
                            block.indent = line.indent;
                            statementStack.Push(block);
                        }
                    }
                    else while (statementStack.Count > 0)
                        {
                            var statement = statementStack.Peek();
                            if (statement.indent > line.indent)
                            {
                                statementStack.Pop();
                                localContext.PopBlock();
                            }
                            else if (statement.indent < line.indent)
                            {
                                parameter.exceptions.Add(new Anchor(logicBody.body.text, line.segment), CompilingExceptionCode.SYNTAX_INDENT);
                                break;
                            }
                            else if (Lexical.TryAnalysisFirst(logicBody.body.text, line.segment, 0, out var lexical, parameter.exceptions) && lexical.anchor.Segment != KeyWord.ELIF && lexical.anchor.Segment != KeyWord.ELSE)
                            {
                                statement = statementStack.Pop();
                                while (statementStack.Count > 0 && statementStack.Peek().indent == line.indent)
                                {
                                    statement = statementStack.Pop();
                                    localContext.PopBlock();
                                }
                                statementStack.Push(statement);
                                break;
                            }
                            else break;
                        }
                    using (var lexicals = parameter.pool.GetList<Lexical>())
                        if (Lexical.TryAnalysis(lexicals, logicBody.body.text, line.segment, parameter.exceptions) && lexicals.Count > 0)
                        {
                            var anchor = lexicals[0].anchor;
                            if (anchor.Segment == KeyWord.IF) ParseBranchStatement(parameter, statementStack.Peek().statements, lexicals, anchor, context, localContext, destructor);
                            else if (anchor.Segment == KeyWord.ELIF)
                            {
                                var statements = statementStack.Peek().statements;
                                if (statements.Count > 0)
                                {
                                    if (statements[-1] is BranchStatement branchStatement)
                                    {
                                        branchStatement.falseBranch.indent = line.indent;
                                        statementStack.Push(branchStatement.falseBranch);
                                        localContext.PushBlock(parameter.pool);
                                        ParseBranchStatement(parameter, branchStatement.falseBranch.statements, lexicals, anchor, context, localContext, destructor);
                                    }
                                    else if (statements[-1] is LoopStatement loopStatement)
                                    {
                                        loopStatement.elseBlock.indent = line.indent;
                                        statementStack.Push(loopStatement.elseBlock);
                                        localContext.PushBlock(parameter.pool);
                                        ParseBranchStatement(parameter, loopStatement.elseBlock.statements, lexicals, anchor, context, localContext, destructor);
                                    }
                                    else parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                }
                                else parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            }
                            else if (anchor.Segment == KeyWord.ELSE)
                            {
                                if (lexicals.Count > 1) parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                var statements = statementStack.Peek().statements;
                                if (statements.Count > 0)
                                {
                                    if (statements[-1] is BranchStatement branchStatement)
                                    {
                                        branchStatement.falseBranch.indent = line.indent;
                                        statementStack.Peek().statements.Add(new ElseStatement(anchor, branchStatement.falseBranch));
                                    }
                                    else if (statements[-1] is WhileStatement loopStatement)
                                    {
                                        loopStatement.elseBlock.indent = line.indent;
                                        statementStack.Peek().statements.Add(new ElseStatement(anchor, loopStatement.elseBlock));
                                    }
                                    else parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                }
                                else parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            }
                            else if (anchor.Segment == KeyWord.WHILE)
                            {
                                if (lexicals.Count > 1)
                                {
                                    var parser = new ExpressionParser(parameter, context, localContext, destructor);
                                    if (parser.TryParse(lexicals[1, -1], out var condition))
                                    {
                                        if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.BOOL_TYPE)
                                            statementStack.Peek().statements.Add(new WhileStatement(anchor, condition, new BlockStatement(anchor, parameter.pool), new BlockStatement(anchor, parameter.pool)));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else statementStack.Peek().statements.Add(new WhileStatement(anchor, null, new BlockStatement(anchor, parameter.pool), new BlockStatement(anchor, parameter.pool)));
                            }
                            else if (anchor.Segment == KeyWord.FOR)
                            {
                                if (lexicals.Count > 1)
                                {
                                    var parser = new ExpressionParser(parameter, context, localContext, destructor);
                                    if (parser.TryParseTuple(lexicals[1, -1], out var expressions) && CheckForExpressions(parameter, anchor, expressions))
                                    {
                                        var backs = new Expression[expressions.Length - 2];
                                        System.Array.Copy(expressions, 2, backs, 0, backs.Length);
                                        statementStack.Peek().statements.Add(new ForStatement(anchor, expressions[0], expressions[1], backs, new BlockStatement(anchor, parameter.pool), new BlockStatement(anchor, parameter.pool)));
                                    }
                                }
                                else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                            }
                            else if (anchor.Segment == KeyWord.BREAK)
                            {
                                if (lexicals.Count > 1)
                                {
                                    var parser = new ExpressionParser(parameter, context, localContext, destructor);
                                    if (parser.TryParse(lexicals[1, -1], out var condition))
                                    {
                                        if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.BOOL_TYPE)
                                            statementStack.Peek().statements.Add(new BreakStatement(anchor, condition));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else statementStack.Peek().statements.Add(new BreakStatement(anchor, null));
                            }
                            else if (anchor.Segment == KeyWord.CONTINUE)
                            {
                                if (lexicals.Count > 1)
                                {
                                    var parser = new ExpressionParser(parameter, context, localContext, destructor);
                                    if (parser.TryParse(lexicals[1, -1], out var condition))
                                    {
                                        if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.BOOL_TYPE)
                                            statementStack.Peek().statements.Add(new ContinueStatement(anchor, condition));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else statementStack.Peek().statements.Add(new ContinueStatement(anchor, null));
                            }
                            else if (anchor.Segment == KeyWord.RETURN)
                            {
                                if (lexicals.Count > 1)
                                {
                                    var parser = new ExpressionParser(parameter, context, localContext, destructor);
                                    if (parser.TryParseTuple(lexicals[1, -1], out var results))
                                    {
                                        if (parser.TryAssignmentConvert(results, returns, out var result, out _))
                                            statementStack.Peek().statements.Add(new ReturnStatement(anchor, result));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else statementStack.Peek().statements.Add(new ReturnStatement(anchor, null));
                            }
                            else if (anchor.Segment == KeyWord.WAIT)
                            {
                                if (lexicals.Count > 1)
                                {
                                    var parser = new ExpressionParser(parameter, context, localContext, destructor);
                                    if (parser.TryParse(lexicals[1, -1], out var condition))
                                    {
                                        if (condition.returns.Length == 1)
                                        {
                                            if (condition.returns[0] == RelyKernel.BYTE_TYPE) condition = new ByteToIntegerExpression(condition.anchor, condition);
                                            if (condition.returns[0] == RelyKernel.BOOL_TYPE || condition.returns[0] == RelyKernel.INTEGER_TYPE || condition.returns[0].definition.code == TypeCode.Coroutine || condition is BlurryCoroutineExpression)
                                                statementStack.Peek().statements.Add(new WaitStatement(anchor, condition));
                                            else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        }
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else statementStack.Peek().statements.Add(new WaitStatement(anchor, null));
                            }
                            else if (anchor.Segment == KeyWord.EXIT)
                            {
                                if (lexicals.Count > 1)
                                {
                                    var parser = new ExpressionParser(parameter, context, localContext, destructor);
                                    if (parser.TryParse(lexicals[1, -1], out var condition))
                                    {
                                        if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.INTEGER_TYPE)
                                            statementStack.Peek().statements.Add(new ExitStatement(anchor, condition));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                            }
                            else if (anchor.Segment == KeyWord.TRY)
                            {
                                if (lexicals.Count > 1) parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                else if (parameter.command.ignoreExit) parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_IGNORE_EXIT_NONSUPPORT_TRY);
                                else statementStack.Peek().statements.Add(new TryStatement(anchor, new BlockStatement(anchor, parameter.pool), localContext));
                            }
                            else if (anchor.Segment == KeyWord.CATCH)
                            {
                                var statements = statementStack.Peek().statements;
                                if (statements.Count > 0 && statements[-1] is TryStatement tryStatement && tryStatement.catchBlock == null && tryStatement.finallyBlock == null)
                                {
                                    tryStatement.catchBlock = new BlockStatement(anchor, parameter.pool);
                                    if (lexicals.Count > 1)
                                    {
                                        var parser = new ExpressionParser(parameter, context, localContext, destructor);
                                        if (parser.TryParse(lexicals[1, -1], out var exitcode))
                                        {
                                            if (exitcode is BlurryVariableDeclarationExpression blurry) exitcode = new VariableLocalExpression(localContext.AddLocal(blurry.anchor, RelyKernel.INTEGER_TYPE), TokenAttribute.Assignable);
                                            if (exitcode.returns.Length == 1 && exitcode.returns[0] == RelyKernel.INTEGER_TYPE) tryStatement.exitcode = exitcode;
                                            else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                        }
                                    }
                                }
                                else parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            }
                            else if (anchor.Segment == KeyWord.FINALLY)
                            {
                                var statements = statementStack.Peek().statements;
                                if (statements.Count > 0 && statements[-1] is TryStatement tryStatement && tryStatement.finallyBlock == null) tryStatement.finallyBlock = new BlockStatement(anchor, parameter.pool);
                                else parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            }
                            else
                            {
                                var parser = new ExpressionParser(parameter, context, localContext, destructor);
                                if (parser.TryParse(lexicals, out var result))
                                    statementStack.Peek().statements.Add(new ExpressionStatement(result));
                            }
                        }
                }
            }
        }
        private bool CheckForExpressions(GeneratorParameter parameter, Anchor anchor, Expression[] expressions)
        {
            if (expressions.Length < 2)
            {
                parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                return false;
            }
            if (expressions[1].returns.Length != 1 || expressions[1].returns[0] != RelyKernel.BOOL_TYPE)
            {
                parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                return false;
            }
            return true;
        }
        private bool CheckReturn(Statement statement)
        {
            if (statement is ExitStatement) return true;
            else if (statement is ReturnStatement) return true;
            else if (statement is BlockStatement blockStatement)
            {
                foreach (var item in blockStatement.statements)
                    if (CheckReturn(item))
                        return true;
            }
            else if (statement is BranchStatement branchStatement) return CheckReturn(branchStatement.trueBranch) && CheckReturn(branchStatement.falseBranch);
            else if (statement is WhileStatement loopStatement)
            {
                var returned = false;
                foreach (var item in loopStatement.loopBlock.statements)
                    if (item is BreakStatement) return false;
                    else if (CheckReturn(item))
                    {
                        returned = true;
                        break;
                    }
                return returned && CheckReturn(loopStatement.elseBlock);
            }
            return false;
        }
        public void Generate(GeneratorParameter parameter)
        {
            parameter.symbol.WriteFunction(parameter.generator.Point, file, fullName);
            var returnSize = (uint)returns.Length * 4;
            using (var variable = new VariableGenerator(parameter.pool, Frame.SIZE + returnSize))
            {
                var parameterSize = 0u;
                parameter.debug.AddFunction(file, line, parameter.generator.Point);
                if (definition != null)
                {
                    parameterSize = TypeCode.Handle.FieldSize();
                    var thisVarliable = variable.DecareLocal(0, new CompilingType(new CompilingDefinition(definition.declaration), 0));
                    parameter.debug.AddThisVariable(definition.name, parameter.generator.Point, thisVarliable.address, parameter.relied.Convert(thisVarliable.type).RuntimeType);
                    for (uint i = 0; i < parameters.Length; i++)
                    {
                        var local = variable.DecareLocal(i + 1, parameters[i]);
                        parameter.debug.AddLocalVariable(parameterNames[i], parameter.generator.Point, local.address, parameter.relied.Convert(local.type).RuntimeType);
                        parameterSize = local.address + parameters[i].FieldSize;
                    }
                }
                else for (uint i = 0; i < parameters.Length; i++)
                    {
                        var local = variable.DecareLocal(i, parameters[i]);
                        parameter.debug.AddLocalVariable(parameterNames[i], parameter.generator.Point, local.address, parameter.relied.Convert(local.type).RuntimeType);
                        parameterSize = local.address + parameters[i].FieldSize;
                    }
                var topValue = new Referencable<uint>(parameter.pool);
                parameter.generator.WriteCode(CommandMacro.FUNCTION_Entrance);
                parameter.generator.WriteCode(Frame.SIZE + returnSize + parameterSize);
                parameter.generator.WriteCode(topValue);
                using (var finallyPoint = new Referencable<CodeAddress>(parameter.pool))
                {
                    statements.Generator(new StatementGeneratorParameter(parameter, parameter.generator, variable), finallyPoint);
                    parameter.generator.SetCodeAddress(finallyPoint);
                }
                var maxStack = variable.Generator(parameter.generator);
                Tools.MemoryAlignment(ref maxStack);
                topValue.SetValue(parameter.generator, maxStack);
                topValue.Dispose();
            }
            parameter.generator.WriteCode(CommandMacro.FUNCTION_Return);
        }
        public void Dispose()
        {
            statements.Dispose();
        }
    }
}
