using RainScript.Compiler.Compiling;

namespace RainScript.Compiler.File
{
    internal class CompilingInterfaceMethod : System.IDisposable
    {
        public readonly string name;
        public Visibility visibility;
        public readonly ScopeList<Delegate> functions;
        public CompilingInterfaceMethod(string name, CollectionPool pool)
        {
            this.name = name;
            visibility = Visibility.None;
            functions = pool.GetList<Delegate>();
        }
        public void Dispose()
        {
            functions.Dispose();
        }
    }
    internal partial class Space
    {
        public readonly ScopeList<Compiling.Space> relyCompilings;
        public readonly ScopeList<RelySpace> relyReferences;
        private void InitRelies(DeclarationManager manager, ExceptionCollector exceptions)
        {
            foreach (var lexicals in imports)
            {
                if (compiling.TryFindSpace(lexicals[0].anchor.Segment, out var space))
                {
                    for (int i = 1; i < lexicals.Count; i++)
                        if (!space.TryFindChild(lexicals[i].anchor.Segment, out space))
                        {
                            exceptions.Add(lexicals[i].anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                            break;
                        }
                    if (space != null) relyCompilings.Add(((Space)space).compiling);
                }
                else
                {
                    var name = lexicals[0].anchor.Segment;
                    foreach (var item in manager.relies)
                        if (item.name == name)
                            for (int i = 1; i < lexicals.Count; i++)
                                if (!space.TryFindChild(lexicals[i].anchor.Segment, out space))
                                {
                                    exceptions.Add(lexicals[i].anchor, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                                    goto next;
                                }
                            next:
                    if (space != null) relyReferences.Add((RelySpace)space);
                }
            }
        }
        public void Tidy(DeclarationManager manager, CollectionPool pool, ExceptionCollector exceptions)
        {
            InitRelies(manager, exceptions);

            foreach (var item in children) item.Tidy(manager, pool, exceptions);

            foreach (var item in definitions)
            {
                if (compiling.declarations.TryGetValue(item.name.Segment, out var declaration))
                {
                    exceptions.Add(item.name, CompilingExceptionCode.COMPILING_DUPLICATE_DECLARATION_NAMES, declaration.ToString());
                    continue;
                }
                declaration = new Compiler.Declaration(LIBRARY.SELF, item.visibility, DeclarationCode.Definition, (uint)manager.library.definitions.Count, 0, 0);
                compiling.declarations.Add(item.name.Segment, declaration);

                var memberVariables = new Compiling.Definition.MemberVariableInfo[item.variables.Count];
                for (int i = 0; i < memberVariables.Length; i++)
                {
                    var variable = item.variables[i];
                    var variableDeclaraction = new Compiler.Declaration(LIBRARY.SELF, variable.visibility, DeclarationCode.MemberVariable, (uint)i, 0, declaration.index);
                    variable.compiling = new Compiling.Definition.MemberVariableInfo(variable.name, variableDeclaraction, compiling, new LogicExpression(relyCompilings, relyReferences, variable.expression));
                    memberVariables[i] = variable.compiling;
                }

                if (item.constructors.Count == 0) item.constructors.Add(new Definition.Constructor(item.name, Visibility.Public, pool.GetList<Parameter>(), default, default));
                var constructorInvokerExpressions = new LogicExpression[item.constructors.Count];
                var constructorMethod = new Method((uint)manager.library.methods.Count, DeclarationCode.Constructor, item.name.Segment, compiling);
                manager.library.methods.Add(constructorMethod);
                for (int i = 0; i < item.constructors.Count; i++)
                {
                    var constructor = item.constructors[i];
                    constructorInvokerExpressions[i] = new LogicExpression(relyCompilings, relyReferences, constructor.invokerExpression);
                    var constructorDeclaration = new Compiler.Declaration(LIBRARY.SELF, constructor.visibility, DeclarationCode.ConstructorFunction, constructorMethod.Declaration.index, (uint)constructorMethod.Count, declaration.index);
                    constructor.compiling = new Compiling.Function(constructor.name, constructorDeclaration, compiling, 0, constructor.parameters.Count, new LogicBody(relyCompilings, relyReferences, constructor.body), pool);
                    constructorMethod.AddFunction(constructor.compiling);
                }

                var memberMethods = pool.GetList<Method>();
                foreach (var memberFunction in item.functions)
                {
                    var index = memberMethods.FindIndex(value => value.name == memberFunction.name.Segment);
                    if (index < 0)
                    {
                        index = manager.library.methods.Count;
                        var memberMethod = new Method((uint)index, DeclarationCode.MemberMethod, memberFunction.name.Segment, compiling);
                        memberMethods.Add(memberMethod);
                        manager.library.methods.Add(memberMethod);
                    }
                    var method = memberMethods[index];
                    var functionDeclaration = new Compiler.Declaration(LIBRARY.SELF, memberFunction.visibility, DeclarationCode.MemberFunction, method.Declaration.index, (uint)method.Count, declaration.index);
                    memberFunction.compiling = new Compiling.Function(memberFunction.name, functionDeclaration, compiling, memberFunction.returns.Count, memberFunction.parameters.Count, new LogicBody(relyCompilings, relyReferences, memberFunction.body), pool);
                    method.AddFunction(memberFunction.compiling);
                }
                var memberMethodIndices = new uint[memberMethods.Count];
                for (int i = 0; i < memberMethodIndices.Length; i++) memberMethodIndices[i] = memberMethods[i].Declaration.index;
                memberMethods.Dispose();

                item.compiling = new Compiling.Definition(item.name, declaration, compiling, constructorMethod.Declaration.index, constructorInvokerExpressions, memberVariables, memberMethodIndices, new LogicBody(relyCompilings, relyReferences, item.destructor.body));
                manager.library.definitions.Add(item.compiling);
            }

            foreach (var item in variables)
            {
                if (compiling.declarations.TryGetValue(item.name.Segment, out var declaration))
                {
                    exceptions.Add(item.name, CompilingExceptionCode.COMPILING_DUPLICATE_DECLARATION_NAMES, declaration.ToString());
                    continue;
                }
                declaration = new Compiler.Declaration(LIBRARY.SELF, item.visibility, DeclarationCode.GlobalVariable, (uint)manager.library.variables.Count, 0, 0);
                compiling.declarations.Add(item.name.Segment, declaration);
                item.compiling = new Compiling.Variable(item.name, declaration, compiling, item.constant, new LogicExpression(relyCompilings, relyReferences, item.expression));
                manager.library.variables.Add(item.compiling);
            }

            foreach (var item in delegates)
            {
                if (compiling.declarations.TryGetValue(item.name.Segment, out var declaration))
                {
                    exceptions.Add(item.name, CompilingExceptionCode.COMPILING_DUPLICATE_DECLARATION_NAMES, declaration.ToString());
                    continue;
                }
                declaration = new Compiler.Declaration(LIBRARY.SELF, item.visibility, DeclarationCode.Delegate, (uint)manager.library.delegates.Count, 0, 0);
                compiling.declarations.Add(item.name.Segment, declaration);
                item.compiling = new Delegate(item.name, declaration, compiling, item.returns.Count, item.parameters.Count);
                manager.library.delegates.Add(item.compiling);
            }

            foreach (var item in coroutines)
            {
                if (compiling.declarations.TryGetValue(item.name.Segment, out var declaration))
                {
                    exceptions.Add(item.name, CompilingExceptionCode.COMPILING_DUPLICATE_DECLARATION_NAMES, declaration.ToString());
                    continue;
                }
                declaration = new Compiler.Declaration(LIBRARY.SELF, item.visibility, DeclarationCode.Coroutine, (uint)manager.library.coroutines.Count, 0, 0);
                compiling.declarations.Add(item.name.Segment, declaration);
                item.compiling = new Compiling.Coroutine(item.name, declaration, compiling, item.returns.Count);
                manager.library.coroutines.Add(item.compiling);
            }

            foreach (var item in functions)
            {
                Method method = null;
                if (compiling.declarations.TryGetValue(item.name.Segment, out var declaration))
                {
                    if (declaration.code == DeclarationCode.GlobalMethod) method = manager.library.methods[(int)declaration.index];
                    else
                    {
                        exceptions.Add(item.name, CompilingExceptionCode.COMPILING_DUPLICATE_DECLARATION_NAMES, declaration.ToString());
                        continue;
                    }
                }
                else
                {
                    method = new Method((uint)manager.library.methods.Count, DeclarationCode.GlobalMethod, item.name.Segment, compiling);
                    manager.library.methods.Add(method);
                }
                declaration = new Compiler.Declaration(LIBRARY.SELF, item.visibility, DeclarationCode.GlobalFunction, method.Declaration.index, (uint)method.Count, 0);
                item.compiling = new Compiling.Function(item.name, declaration, compiling, item.returns.Count, item.parameters.Count, new LogicBody(relyCompilings, relyReferences, item.body), pool);
                method.AddFunction(item.compiling);
                compiling.declarations[method.name] = method.Declaration;
            }

            foreach (var item in interfaces)
            {
                if (compiling.declarations.TryGetValue(item.name.Segment, out var declaration))
                {
                    exceptions.Add(item.name, CompilingExceptionCode.COMPILING_DUPLICATE_DECLARATION_NAMES, declaration.ToString());
                    continue;
                }
                declaration = new Compiler.Declaration(LIBRARY.SELF, item.visibility, DeclarationCode.Interface, (uint)manager.library.interfaces.Count, 0, 0);
                compiling.declarations.Add(item.name.Segment, declaration);

                var interfaceMethods = pool.GetList<CompilingInterfaceMethod>();
                foreach (var interfaceFunction in item.functions)
                {
                    var index = interfaceMethods.FindIndex(value => value.name == interfaceFunction.name.Segment);
                    if (index < 0)
                    {
                        index = interfaceMethods.Count;
                        interfaceMethods.Add(new CompilingInterfaceMethod(interfaceFunction.name.Segment, pool));
                    }
                    var method = interfaceMethods[index];
                    var functionDeclaration = new Compiler.Declaration(LIBRARY.SELF, Visibility.Public, DeclarationCode.InterfaceFunction, (uint)index, (uint)method.functions.Count, declaration.index);
                    interfaceFunction.compiling = new Compiling.Delegate(interfaceFunction.name, functionDeclaration, compiling, interfaceFunction.returns.Count, interfaceFunction.parameters.Count);
                    method.functions.Add(interfaceFunction.compiling);
                }
                var interfaceCompilingMethods = new Compiling.InterfaceMethod[interfaceMethods.Count];
                for (int i = 0; i < interfaceCompilingMethods.Length; i++)
                {
                    var methodDeclaration = new Compiler.Declaration(LIBRARY.SELF, interfaceMethods[i].visibility, DeclarationCode.InterfaceMethod, (uint)i, 0, declaration.index);
                    interfaceCompilingMethods[i] = new Compiling.InterfaceMethod(declaration, compiling, interfaceMethods[i].name, interfaceMethods[i].functions.ToArray());
                    interfaceMethods[i].Dispose();
                }
                interfaceMethods.Dispose();

                item.compiling = new Compiling.Interface(item.name, declaration, compiling, item.inherits.Count, interfaceCompilingMethods);
                manager.library.interfaces.Add(item.compiling);
            }

            foreach (var item in natives)
            {
                Native native = null;
                if (compiling.declarations.TryGetValue(item.name.Segment, out var declaration))
                {
                    if (declaration.code == DeclarationCode.NativeMethod) native = manager.library.natives[(int)declaration.index];
                    else
                    {
                        exceptions.Add(item.name, CompilingExceptionCode.COMPILING_DUPLICATE_DECLARATION_NAMES, declaration.ToString());
                        continue;
                    }
                }
                else
                {
                    native = new Compiling.Native((uint)manager.library.natives.Count, item.name.Segment, compiling);
                    manager.library.natives.Add(native);
                }
                declaration = new Compiler.Declaration(LIBRARY.SELF, item.visibility, DeclarationCode.NativeFunction, native.Declaration.index, (uint)native.Count, 0);
                item.compiling = new Compiling.Delegate(item.name, declaration, compiling, item.returns.Count, item.parameters.Count);
                native.AddFunction(item.compiling);
                compiling.declarations[native.name] = native.Declaration;
            }
        }
    }
}
