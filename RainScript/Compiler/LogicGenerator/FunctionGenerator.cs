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

    internal class LambdaFunction : System.IDisposable
    {
        private readonly Referencable<CodeAddress> entry;
        private uint returnSize;
        public readonly CompilingType[] parameters;
        public readonly ScopeList<Statement> statements;
        public LambdaFunction(Referencable<CodeAddress> entry, CompilingType[] parameters, CollectionPool pool)
        {
            this.entry = entry;
            this.parameters = parameters;
            statements = pool.GetList<Statement>();
        }
        public void SetReturnCount(uint returnCount)
        {
            returnSize = returnCount * 4;
        }
        public void Generate(GeneratorParameter parameter, Generator generator)
        {
            generator.SetCodeAddress(entry);
            using (var variable = new VariableGenerator(parameter.pool, Frame.SIZE + returnSize))
            {
                var parameterSize = 0u;
                for (uint i = 0; i < parameters.Length; i++)
                {
                    variable.DecareLocal(i, parameters[i]);
                    parameterSize += parameters[i].FieldSize;
                }
                var topValue = new Referencable<uint>(parameter.pool);
                generator.WriteCode(CommandMacro.FUNCTION_Entrance);
                generator.WriteCode(Frame.SIZE + returnSize + parameterSize);
                generator.WriteCode(topValue);
                using (var finallyPoint = new Referencable<CodeAddress>(parameter.pool))
                {
                    foreach (var statement in statements) statement.Generator(new StatementGeneratorParameter(parameter, generator, variable), finallyPoint);
                    generator.SetCodeAddress(finallyPoint);
                }
                topValue.SetValue(generator, variable.Generator(generator));
                topValue.Dispose();
            }
            generator.WriteCode(CommandMacro.FUNCTION_Return);
        }
        public void Dispose()
        {
            foreach (var statement in statements) statement.Dispose();
            statements.Dispose();
        }
    }
    internal class FunctionGenerator : System.IDisposable
    {
        public readonly Definition definition;
        public readonly CompilingType[] parameters;
        public readonly CompilingType[] returns;
        public readonly BlockStatement statements;
        public FunctionGenerator(GeneratorParameter parameter, Generator generator)
        {
            parameters = returns = new CompilingType[0];
            statements = new BlockStatement(default, parameter.pool);
            foreach (var variable in parameter.manager.library.variables)
                if (variable.constant)
                    using (var lexicals = parameter.pool.GetList<Lexical>())
                        if (Lexical.TryAnalysis(lexicals, variable.expression.exprssion.textInfo, variable.expression.exprssion.Segment, parameter.exceptions))
                        {
                            var context = new Context(variable.space, null, variable.expression.compilings, variable.expression.references);
                            using (var localContext = new LocalContext(parameter.pool))
                            {
                                var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                                if (parser.TryParseTuple(lexicals, out var expressions))
                                {
                                    if (parser.TryAssignmentConvert(expressions, new CompilingType[] { variable.type }, out var result, out _))
                                    {
                                        if (variable.type == RelyKernel.BOOL_TYPE)
                                        {
                                            if (result.TryEvaluation(out bool value)) generator.WriteData(1, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.INTEGER_TYPE)
                                        {
                                            if (result.TryEvaluation(out long value)) generator.WriteData(8, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.REAL_TYPE)
                                        {
                                            if (result.TryEvaluation(out real value)) generator.WriteData(8, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.REAL2_TYPE)
                                        {
                                            if (result.TryEvaluation(out Real2 value)) generator.WriteData(16, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.REAL3_TYPE)
                                        {
                                            if (result.TryEvaluation(out Real3 value)) generator.WriteData(24, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.REAL4_TYPE)
                                        {
                                            if (result.TryEvaluation(out Real4 value)) generator.WriteData(32, value, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.STRING_TYPE)
                                        {
                                            if (result.TryEvaluation(out string value)) generator.WriteData(value, variable.address, parameter.pool);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type.IsHandle)
                                        {
                                            if (result.TryEvaluationNull()) generator.WriteData(4, 0u, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else if (variable.type == RelyKernel.ENTITY_TYPE)
                                        {
                                            if (result.TryEvaluationNull()) generator.WriteData(8, Entity.NULL, variable.address);
                                            else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_CONSTANT_EVALUATION_FAIL);
                                        }
                                        else parameter.exceptions.Add(variable.name, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                    }
                                    else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
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
                                var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
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
            statements = new BlockStatement(default, parameter.pool);
            parameters = function.parameters;
            returns = function.returns;
            var localContext = new LocalContext(parameter.pool);
            localContext.PushBlock(parameter.pool);
            if (function.declaration.code == DeclarationCode.MemberFunction)
            {
                definition = parameter.manager.library.definitions[(int)function.declaration.definitionIndex];
                localContext.AddLocal(KeyWorld.THIS, definition.name, new CompilingType(new CompilingDefinition(definition.declaration), 0));
                for (int i = 0; i < parameters.Length; i++) localContext.AddLocal(function.parameterNames[i], parameters[i]);
            }
            else if (function.declaration.code == DeclarationCode.ConstructorFunction)
            {
                definition = parameter.manager.library.definitions[(int)function.declaration.definitionIndex];
                var type = new CompilingType(new CompilingDefinition(definition.declaration), 0);
                var thisValue = localContext.AddLocal(KeyWorld.THIS, definition.name, type);
                var thisExpression = new VariableLocalExpression(definition.name, thisValue.Declaration, TokenAttribute.Value, type);
                for (int i = 0; i < parameters.Length; i++) localContext.AddLocal(function.parameterNames[i], parameters[i]);
                var logic = definition.constructorInvaokerExpressions[function.declaration.overloadIndex];
                if ((bool)logic.exprssion)
                {
                    using (var lexicals = parameter.pool.GetList<Lexical>())
                        if (Lexical.TryAnalysis(lexicals, logic.exprssion.textInfo, logic.exprssion.Segment, parameter.exceptions) && lexicals.Count > 1)
                        {
                            var lexical = lexicals[0];
                            Declaration ctorMethod = default;
                            if (lexical.anchor.Segment == KeyWorld.THIS) ParseCtorInvoker(parameter, function, lexicals, localContext, new Declaration(LIBRARY.SELF, Visibility.Public, DeclarationCode.Constructor, definition.constructors, 0, definition.declaration.index), thisExpression);
                            else if (lexical.anchor.Segment == KeyWorld.BASE)
                            {
                                if (parameter.manager.TryGetDefinition(definition.parent, out var result))
                                {
                                    if (result.Constructor == LIBRARY.ENTRY_INVALID) parameter.exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_FUNCTION_NOT_FOUND);
                                    else ParseCtorInvoker(parameter, function, lexicals, localContext, new Declaration(result.Declaration.library, Visibility.Public, DeclarationCode.Constructor, result.Constructor, 0, result.Declaration.index), thisExpression);
                                    InitMemberVariable(parameter, thisExpression);
                                }
                                else throw ExceptionGeneratorCompiler.Unknown();
                            }
                            else
                            {
                                parameter.exceptions.Add(lexical.anchor, CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                return;
                            }
                            var context = new Context(function.space, definition, function.body.compilings, function.body.references);
                            var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                            if (parser.TryParseTuple(lexicals[1, -1], out var expressions))
                                if (parser.TryGetFunction(parser.manager.GetMethod(ctorMethod), expressions, out var ctor, out var ctorParameter))
                                    statements.statements.Add(new ExpressionStatement(new InvokerMemberExpression(lexical.anchor, ctor.Declaration, thisExpression, ctorParameter, returns)));
                                else parameter.exceptions.Add(lexical.anchor, CompilingExceptionCode.GENERATOR_FUNCTION_NOT_FOUND);
                        }
                }
                else InitMemberVariable(parameter, thisExpression);
            }
            else for (int i = 0; i < parameters.Length; i++) localContext.AddLocal(function.parameterNames[i], parameters[i]);
            if ((bool)function.body.body) Parse(parameter, function.space, function.body, localContext);
            if (returns.Length > 0 && !CheckReturn(statements))
                parameter.exceptions.Add(function.name, CompilingExceptionCode.GENERATOR_MISSING_RETURN);
            localContext.Dispose();
        }
        public FunctionGenerator(GeneratorParameter parameter, Definition definition)
        {
            statements = new BlockStatement(default, parameter.pool);
            var localContext = new LocalContext(parameter.pool);
            localContext.PushBlock(parameter.pool);
            localContext.AddLocal(KeyWorld.THIS, definition.name, new CompilingType(new CompilingDefinition(definition.declaration), 0));
            this.definition = definition;
            parameters = returns = new CompilingType[0];
            if ((bool)definition.destructor.body) Parse(parameter, definition.space, definition.destructor, localContext);
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
                            using (var locals = new LocalContext(parameter.pool))
                            {
                                var parser = new ExpressionParser(parameter.manager, context, locals, parameter.pool, parameter.exceptions);
                                if (parser.TryParseTuple(lexicals, out var expressions))
                                {
                                    if (parser.TryAssignmentConvert(expressions, new CompilingType[] { variable.type }, out var result, out _))
                                        statements.statements.Add(new ExpressionStatement(new VariableAssignmentExpression(variable.name, new VariableMemberExpression(variable.name, variable.declaration, thisExpression, variable.type), result, variable.type)));
                                    else parameter.exceptions.Add(variable.expression.exprssion, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
                                }
                            }
                        }
        }
        private void ParseCtorInvoker(GeneratorParameter parameter, Compiling.Function function, ScopeList<Lexical> lexicals, LocalContext localContext, Declaration method, Expression thisExpression)
        {
            var context = new Context(function.space, definition, function.body.compilings, function.body.references);
            var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
            if (parser.TryParseTuple(lexicals[1, -1], out var expressions))
                if (parser.TryGetFunction(parser.manager.GetMethod(method), expressions, out var ctor, out var ctorParameter))
                    statements.statements.Add(new ExpressionStatement(new InvokerMemberExpression(lexicals[0].anchor, ctor.Declaration, thisExpression, ctorParameter, returns)));
                else parameter.exceptions.Add(lexicals[0].anchor, CompilingExceptionCode.GENERATOR_FUNCTION_NOT_FOUND);
        }
        private void ParseBranchStatement(GeneratorParameter parameter, ScopeList<Statement> statements, ScopeList<Lexical> lexicals, Anchor anchor, Context context, LocalContext localContext)
        {
            if (lexicals.Count > 2)
            {
                var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                if (parser.TryParse(lexicals[1, -1], out var condition))
                {
                    if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.BOOL_TYPE)
                        statements.Add(new BranchStatement(anchor, condition, new BlockStatement(anchor, parameter.pool), new BlockStatement(anchor, parameter.pool)));
                    else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                }
            }
            else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
        }
        private void Parse(GeneratorParameter parameter, Compiling.Space space, LogicBody logicBody, LocalContext localContext)
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
                            else
                            {
                                statement = statementStack.Pop();
                                while (statementStack.Count > 0 && statementStack.Peek().indent == line.indent)
                                    statement = statementStack.Pop();
                                statementStack.Push(statement);
                                break;
                            }
                        }
                    using (var lexicals = parameter.pool.GetList<Lexical>())
                        if (Lexical.TryAnalysis(lexicals, logicBody.body.text, line.segment, parameter.exceptions) && lexicals.Count > 0)
                        {
                            var anchor = lexicals[0].anchor;
                            if (anchor.Segment == KeyWorld.IF) ParseBranchStatement(parameter, statementStack.Peek().statements, lexicals, anchor, context, localContext);
                            else if (anchor.Segment == KeyWorld.ELIF)
                            {
                                var statements = statementStack.Peek().statements;
                                if (statements.Count > 0)
                                {
                                    if (statements[-1] is BranchStatement branchStatement)
                                    {
                                        branchStatement.falseBranch.indent = line.indent;
                                        statementStack.Push(branchStatement.falseBranch);
                                        ParseBranchStatement(parameter, branchStatement.falseBranch.statements, lexicals, anchor, context, localContext);
                                    }
                                    else if (statements[-1] is LoopStatement loopStatement)
                                    {
                                        loopStatement.elseBlock.indent = line.indent;
                                        statementStack.Push(loopStatement.elseBlock);
                                        ParseBranchStatement(parameter, loopStatement.elseBlock.statements, lexicals, anchor, context, localContext);
                                    }
                                    else parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                }
                                else parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            }
                            else if (anchor.Segment == KeyWorld.ELSE)
                            {
                                if (lexicals.Count > 2) parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.SYNTAX_UNEXPECTED_LEXCAL);
                                var statements = statementStack.Peek().statements;
                                if (statements.Count > 0)
                                {
                                    if (statements[-1] is BranchStatement branchStatement)
                                    {
                                        branchStatement.falseBranch.indent = line.indent;
                                        statementStack.Peek().statements.Add(new ElseStatement(anchor, branchStatement.falseBranch));
                                    }
                                    else if (statements[-1] is LoopStatement loopStatement)
                                    {
                                        loopStatement.elseBlock.indent = line.indent;
                                        statementStack.Peek().statements.Add(new ElseStatement(anchor, loopStatement.elseBlock));
                                    }
                                    else parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                                }
                                else parameter.exceptions.Add(anchor, CompilingExceptionCode.SYNTAX_MISSING_PAIRED_SYMBOL);
                            }
                            else if (anchor.Segment == KeyWorld.LOOP)
                            {
                                if (lexicals.Count > 2)
                                {
                                    var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                                    if (parser.TryParse(lexicals[1, -1], out var condition))
                                    {
                                        if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.BOOL_TYPE)
                                            statementStack.Peek().statements.Add(new LoopStatement(anchor, condition, new BlockStatement(anchor, parameter.pool), new BlockStatement(anchor, parameter.pool)));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else statementStack.Peek().statements.Add(new LoopStatement(anchor, null, new BlockStatement(anchor, parameter.pool), new BlockStatement(anchor, parameter.pool)));
                            }
                            else if (anchor.Segment == KeyWorld.FOREACH) parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_NOT_IMPLEMENTED);
                            else if (anchor.Segment == KeyWorld.BREAK)
                            {
                                if (lexicals.Count > 2)
                                {
                                    var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                                    if (parser.TryParse(lexicals[1, -1], out var condition))
                                    {
                                        if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.BOOL_TYPE)
                                            statementStack.Peek().statements.Add(new BreakStatement(anchor, condition));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else statementStack.Peek().statements.Add(new BreakStatement(anchor, null));
                            }
                            else if (anchor.Segment == KeyWorld.CONTINUE)
                            {
                                if (lexicals.Count > 2)
                                {
                                    var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                                    if (parser.TryParse(lexicals[1, -1], out var condition))
                                    {
                                        if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.BOOL_TYPE)
                                            statementStack.Peek().statements.Add(new ContinueStatement(anchor, condition));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else statementStack.Peek().statements.Add(new ContinueStatement(anchor, null));
                            }
                            else if (anchor.Segment == KeyWorld.RETURN)
                            {
                                if (lexicals.Count > 1)
                                {
                                    var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                                    if (parser.TryParseTuple(lexicals[1, -1], out var results))
                                    {
                                        if (parser.TryAssignmentConvert(results, returns, out var result, out _))
                                            statementStack.Peek().statements.Add(new ReturnStatement(anchor, result));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else statementStack.Peek().statements.Add(new ReturnStatement(anchor, null));
                            }
                            else if (anchor.Segment == KeyWorld.WAIT)
                            {
                                if (lexicals.Count > 1)
                                {
                                    var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                                    if (parser.TryParse(lexicals[1, -1], out var condition))
                                    {
                                        if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.INTEGER_TYPE)
                                            statementStack.Peek().statements.Add(new WaitStatement(anchor, condition));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else statementStack.Peek().statements.Add(new WaitStatement(anchor, null));
                            }
                            else if (anchor.Segment == KeyWorld.EXIT)
                            {
                                if (lexicals.Count > 2)
                                {
                                    var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                                    if (parser.TryParse(lexicals[1, -1], out var condition))
                                    {
                                        if (condition.returns.Length == 1 && condition.returns[0] == RelyKernel.INTEGER_TYPE)
                                            statementStack.Peek().statements.Add(new ExitStatement(anchor, condition));
                                        else parameter.exceptions.Add(lexicals[1, -1], CompilingExceptionCode.GENERATOR_TYPE_MISMATCH);
                                    }
                                }
                                else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_MISSING_EXPRESSION);
                            }
                            else if (anchor.Segment == KeyWorld.TRY) parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_NOT_IMPLEMENTED);
                            else if (anchor.Segment == KeyWorld.THROW) parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_NOT_IMPLEMENTED);
                            else if (anchor.Segment == KeyWorld.FINALLY) parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_NOT_IMPLEMENTED);
                            else
                            {
                                var parser = new ExpressionParser(parameter.manager, context, localContext, parameter.pool, parameter.exceptions);
                                if (parser.TryParse(lexicals, out var result))
                                    statementStack.Peek().statements.Add(new ExpressionStatement(result));
                            }
                        }
                }
            }
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
            else if (statement is LoopStatement loopStatement)
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
        public void Generate(GeneratorParameter parameter, Generator generator)
        {
            var returnSize = (uint)returns.Length * 4;
            using (var variable = new VariableGenerator(parameter.pool, Frame.SIZE + returnSize))
            {
                var parameterSize = 0u;
                if (definition != null)
                {
                    variable.DecareLocal(0, new CompilingType(new CompilingDefinition(definition.declaration), 0));
                    for (uint i = 0; i < parameters.Length; i++)
                    {
                        variable.DecareLocal(i + 1, parameters[i]);
                        parameterSize += parameters[i].FieldSize;
                    }
                }
                else for (uint i = 0; i < parameters.Length; i++)
                    {
                        variable.DecareLocal(i, parameters[i]);
                        parameterSize += parameters[i].FieldSize;
                    }
                var topValue = new Referencable<uint>(parameter.pool);
                generator.WriteCode(CommandMacro.FUNCTION_Entrance);
                generator.WriteCode(Frame.SIZE + returnSize + parameterSize);
                generator.WriteCode(topValue);
                using (var finallyPoint = new Referencable<CodeAddress>(parameter.pool))
                {
                    statements.Generator(new StatementGeneratorParameter(parameter, generator, variable), finallyPoint);
                    generator.SetCodeAddress(finallyPoint);
                }
                topValue.SetValue(generator, variable.Generator(generator));
                topValue.Dispose();
            }
            generator.WriteCode(CommandMacro.FUNCTION_Return);
        }
        public void Dispose()
        {
            statements.Dispose();
        }
    }
}
