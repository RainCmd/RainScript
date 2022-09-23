using RainScript.Compiler.LogicGenerator;
using System;

namespace RainScript.Compiler
{
    internal class DeclarationManager : IDisposable
    {
        public readonly Compiling.Library library;
        public readonly RelyLibrary[] relies;
        public readonly ScopeList<LambdaFunction> lambdas;

        public readonly ScopeList<ScopeList<Compiling.Space>> relyCompilings;
        public readonly ScopeList<ScopeList<RelySpace>> relyReferences;
        public DeclarationManager(string name, RelyLibrary[] relies, CollectionPool pool)
        {
            library = new Compiling.Library(name);
            this.relies = relies;
            lambdas = pool.GetList<LambdaFunction>();
            relyCompilings = pool.GetList<ScopeList<Compiling.Space>>();
            relyReferences = pool.GetList<ScopeList<RelySpace>>();
        }
        public CompilingDefinition GetParent(CompilingDefinition definition)
        {
            if ((bool)definition)
                if (definition.library == LIBRARY.KERNEL) return RelyKernel.definitions[definition.index].parent;
                else if (definition.code == TypeCode.Interface) return RelyKernel.INTEGER;
                else if (definition.code == TypeCode.Function) return RelyKernel.FUNCTION;
                else if (definition.code == TypeCode.Coroutine) return RelyKernel.COROUTINE;
                else if (definition.library == LIBRARY.SELF)
                {
                    if (definition.code == TypeCode.Handle) return library.definitions[(int)definition.index].parent;
                }
                else return relies[definition.library].definitions[definition.index].parent;
            return CompilingDefinition.INVALID;
        }
        public bool TryGetDefinition(CompilingDefinition definition, out IDefinition result)
        {
            if (definition.code == TypeCode.Handle)
            {
                if (definition.library == LIBRARY.KERNEL) result = RelyKernel.definitions[(int)definition.index];
                else if (definition.library == LIBRARY.SELF) result = library.definitions[(int)definition.index];
                else result = relies[definition.library].definitions[definition.index];
                return true;
            }
            else if (definition.library == LIBRARY.KERNEL || definition.code == TypeCode.Function || definition.code == TypeCode.Coroutine)
            {
                if (RelyKernel.definitions.Length > (int)definition.code)
                {
                    result = RelyKernel.definitions[(int)definition.code];
                    return true;
                }
            }
            result = default;
            return false;
        }
        public bool TryGetInterface(CompilingDefinition definition, out IInterface result)
        {
            if (TryGetDefinition(definition, out var definitionResult))
            {
                result = definitionResult;
                return true;
            }
            else if (definition.library != LIBRARY.KERNEL && definition.code == TypeCode.Interface)
            {
                if (definition.library == LIBRARY.SELF) result = library.interfaces[(int)definition.index];
                else result = relies[definition.library].interfaces[definition.index];
                return true;
            }
            result = default;
            return false;
        }
        private bool TryGetInherit(IInterface index, CompilingDefinition baseDefinition, ref uint depth)
        {
            depth++;
            if (baseDefinition == RelyKernel.INTERFACE) return true;
            foreach (var item in index.Inherits)
                if (item == baseDefinition || (TryGetInterface(item, out var inherit) && TryGetInherit(inherit, baseDefinition, ref depth)))
                    return true;
            depth--;
            return false;
        }
        public bool TryGetInherit(CompilingType baseType, CompilingType subType, out uint depth)
        {
            if (baseType == subType)
            {
                depth = 0;
                return true;
            }
            if (baseType.dimension == 0)
            {
                if (subType.dimension > 0)
                {
                    if (baseType.definition == RelyKernel.ARRAY)
                    {
                        depth = 1;
                        return true;
                    }
                    else if (baseType.definition == RelyKernel.HANDLE)
                    {
                        depth = 2;
                        return true;
                    }
                }
                else if (subType.definition.code == TypeCode.Function)
                {
                    if (baseType.definition == RelyKernel.FUNCTION)
                    {
                        depth = 1;
                        return true;
                    }
                    else if (baseType.definition == RelyKernel.HANDLE)
                    {
                        depth = 2;
                        return true;
                    }
                }
                else if (subType.definition.code == TypeCode.Coroutine)
                {
                    if (baseType.definition == RelyKernel.COROUTINE)
                    {
                        depth = 1;
                        return true;
                    }
                    else if (baseType.definition == RelyKernel.HANDLE)
                    {
                        depth = 2;
                        return true;
                    }
                }
                else if (subType.definition.code == TypeCode.Interface)
                {
                    if (baseType.definition == RelyKernel.INTERFACE)
                    {
                        depth = 1;
                        return true;
                    }
                    else if (baseType.definition == RelyKernel.HANDLE)
                    {
                        depth = 2;
                        return true;
                    }
                    else if (baseType.definition.code == TypeCode.Interface && TryGetInterface(subType.definition, out var index))
                    {
                        depth = 0;
                        return TryGetInherit(index, baseType.definition, ref depth);
                    }
                }
                else
                {
                    var index = subType.definition;
                    depth = 0;
                    while (TryGetDefinition(index, out var definition))
                    {
                        if (baseType.definition.code == TypeCode.Interface)
                            if (TryGetInherit(definition, baseType.definition, ref depth))
                                return true;
                        depth++;
                        index = definition.Parent;
                        if (index == baseType.definition) return true;
                    }
                }
            }
            depth = default;
            return false;
        }
        public ISpace GetSpace(Declaration declaration)
        {
            if (declaration.library == LIBRARY.KERNEL) return RelyKernel.kernel;
            else if (declaration.library == LIBRARY.SELF)
            {
                switch (declaration.code)
                {
                    case DeclarationCode.Invalid: goto default;
                    case DeclarationCode.Definition: return library.definitions[(int)declaration.index].space;
                    case DeclarationCode.MemberVariable:
                    case DeclarationCode.MemberMethod:
                    case DeclarationCode.MemberFunction:
                    case DeclarationCode.Constructor:
                    case DeclarationCode.ConstructorFunction: return library.definitions[(int)declaration.definitionIndex].space;
                    case DeclarationCode.Delegate: return library.delegates[(int)declaration.index].space;
                    case DeclarationCode.Coroutine: return library.coroutines[(int)declaration.index].space;
                    case DeclarationCode.Interface: return library.interfaces[(int)declaration.index].space;
                    case DeclarationCode.InterfaceMethod:
                    case DeclarationCode.InterfaceFunction: return library.delegates[(int)declaration.definitionIndex].space;
                    case DeclarationCode.GlobalVariable: return library.variables[(int)declaration.index].space;
                    case DeclarationCode.GlobalMethod:
                    case DeclarationCode.GlobalFunction: return library.methods[(int)declaration.index].space;
                    case DeclarationCode.NativeMethod:
                    case DeclarationCode.NativeFunction: return library.natives[(int)declaration.index].space;
                    case DeclarationCode.Lambda: return library.methods[(int)declaration.index].space;
                    case DeclarationCode.LambdaClosureValue: return library.definitions[(int)declaration.definitionIndex].space;
                    case DeclarationCode.LocalVariable:
                    default: return null;
                }
            }
            else
            {
                var rely = relies[declaration.library];
                switch (declaration.code)
                {
                    case DeclarationCode.Invalid: goto default;
                    case DeclarationCode.Definition: return rely.definitions[declaration.index].space;
                    case DeclarationCode.MemberVariable:
                    case DeclarationCode.MemberMethod:
                    case DeclarationCode.MemberFunction:
                    case DeclarationCode.Constructor:
                    case DeclarationCode.ConstructorFunction: return rely.definitions[declaration.definitionIndex].space;
                    case DeclarationCode.Delegate: return rely.delegates[declaration.index].space;
                    case DeclarationCode.Coroutine: return rely.coroutines[declaration.index].space;
                    case DeclarationCode.Interface: return rely.interfaces[declaration.index].space;
                    case DeclarationCode.InterfaceMethod:
                    case DeclarationCode.InterfaceFunction: return rely.interfaces[declaration.definitionIndex].space;
                    case DeclarationCode.GlobalVariable: return rely.variables[declaration.index].space;
                    case DeclarationCode.GlobalMethod:
                    case DeclarationCode.GlobalFunction: return rely.methods[declaration.index].space;
                    case DeclarationCode.NativeMethod:
                    case DeclarationCode.NativeFunction: return rely.natives[declaration.index].space;
                    case DeclarationCode.Lambda: return rely.methods[(int)declaration.index].space;
                    case DeclarationCode.LambdaClosureValue: return rely.definitions[(int)declaration.definitionIndex].space;
                    case DeclarationCode.LocalVariable:
                    default: return null;
                }
            }
        }
        public IMethod GetMethod(Declaration declaration)
        {
            if (declaration.code == DeclarationCode.Constructor && declaration.index == LIBRARY.METHOD_INVALID) return null;
            if (declaration.library == LIBRARY.KERNEL)
            {
                if (declaration.code == DeclarationCode.Constructor) return RelyKernel.methods[declaration.index];
                else if (declaration.code == DeclarationCode.MemberMethod) return RelyKernel.methods[RelyKernel.definitions[declaration.definitionIndex].methods[declaration.index]];
                else if (declaration.code == DeclarationCode.GlobalMethod) return RelyKernel.methods[declaration.index];
            }
            else if (declaration.library == LIBRARY.SELF)
            {
                if (declaration.code == DeclarationCode.Constructor) return library.methods[(int)declaration.index];
                else if (declaration.code == DeclarationCode.MemberMethod) return library.methods[(int)library.definitions[(int)declaration.definitionIndex].methods[declaration.index]];
                else if (declaration.code == DeclarationCode.GlobalMethod) return library.methods[(int)declaration.index];
                else if (declaration.code == DeclarationCode.InterfaceMethod) return library.interfaces[(int)declaration.definitionIndex].methods[(int)declaration.index];
                else if (declaration.code == DeclarationCode.NativeMethod) return library.natives[(int)declaration.index];
            }
            else
            {
                var rely = relies[declaration.library];
                if (declaration.code == DeclarationCode.Constructor) return rely.methods[declaration.index];
                else if (declaration.code == DeclarationCode.MemberMethod) return rely.methods[rely.definitions[declaration.definitionIndex].methods[declaration.index]];
                else if (declaration.code == DeclarationCode.GlobalMethod) return rely.methods[declaration.index];
                else if (declaration.code == DeclarationCode.InterfaceMethod) return rely.interfaces[declaration.definitionIndex].methods[declaration.index];
                else if (declaration.code == DeclarationCode.NativeMethod) return rely.natives[declaration.index];
            }
            return null;
        }
        public IMethod GetOverrideMethod(IMethod method)
        {
            if (method.Declaration.code == DeclarationCode.MemberMethod)
            {
                var declaration = new Declaration(method.Declaration.library, Visibility.Public, DeclarationCode.Definition, method.Declaration.definitionIndex, 0, 0);
                while (TryGetDefinition(GetParent(new CompilingDefinition(declaration)), out var parent))
                {
                    for (int i = 0; i < parent.MethodCount; i++)
                        if (parent.GetMethod(i).Name == method.Name)
                            return parent.GetMethod(i);
                    declaration = parent.Declaration;
                }
            }
            else if (method.Declaration.code == DeclarationCode.InterfaceMethod)
            {
                var declaration = new Declaration(method.Declaration.library, Visibility.Public, DeclarationCode.Interface, method.Declaration.definitionIndex, 0, 0);
                if (TryGetInterface(new CompilingDefinition(declaration), out var result))
                    foreach (var inherit in result.Inherits)
                        if (TryGetOverrideMethod(new CompilingDefinition(inherit.Declaration), method.Name, out method))
                            return method;
            }
            return default;
        }
        private bool TryGetOverrideMethod(CompilingDefinition definition, string name, out IMethod method)
        {
            if (TryGetInterface(definition, out var result))
            {
                for (int i = 0; i < result.MethodCount; i++)
                    if (result.GetMethod(i).Name == name)
                    {
                        method = result.GetMethod(i);
                        return true;
                    }
                foreach (var item in result.Inherits)
                    if (TryGetOverrideMethod(new CompilingDefinition(item.Declaration), name, out method))
                        return true;
            }
            method = default;
            return false;
        }
        public bool TryGetConstructor(CompilingType type, out IMethod method)
        {
            if (type.definition.library == LIBRARY.KERNEL || type.dimension > 0)
            {
                method = default;
                return false;
            }
            else if (type.definition.library == LIBRARY.SELF)
            {
                if (type.definition.code == TypeCode.Handle)
                {
                    var constructors = library.definitions[(int)type.definition.index].constructors;
                    if (constructors != LIBRARY.METHOD_INVALID)
                    {
                        method = library.methods[(int)constructors];
                        return true;
                    }
                }
            }
            else
            {
                var rely = relies[type.definition.library];
                if (type.definition.code == TypeCode.Handle)
                {
                    var constructors = rely.definitions[type.definition.index].constructors;
                    if (constructors != LIBRARY.METHOD_INVALID)
                    {
                        method = rely.methods[constructors];
                        return true;
                    }
                }
            }
            method = default;
            return false;
        }
        public bool TryGetParameters(Declaration declaration, out CompilingType[] types)
        {
            if (declaration.library == LIBRARY.KERNEL)
            {
                if (declaration.code == DeclarationCode.MemberFunction)
                {
                    types = RelyKernel.methods[RelyKernel.definitions[declaration.definitionIndex].methods[declaration.index]].functions[declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.ConstructorFunction)
                {
                    types = RelyKernel.methods[declaration.index].functions[declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.GlobalFunction)
                {
                    types = RelyKernel.methods[declaration.index].functions[declaration.overloadIndex].parameters;
                    return true;
                }
            }
            else if (declaration.library == LIBRARY.SELF)
            {
                if (declaration.code == DeclarationCode.MemberFunction)
                {
                    types = library.methods[(int)library.definitions[(int)declaration.definitionIndex].methods[declaration.index]][(int)declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.ConstructorFunction)
                {
                    types = library.methods[(int)declaration.index][(int)declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.Delegate)
                {
                    types = library.delegates[(int)declaration.index].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.InterfaceFunction)
                {
                    types = library.interfaces[(int)declaration.definitionIndex].methods[declaration.index].functions[declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.GlobalFunction)
                {
                    types = library.methods[(int)declaration.index][(int)declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.NativeFunction)
                {
                    types = library.natives[(int)declaration.index][(int)declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.Lambda)
                {
                    types = library.methods[(int)declaration.index][0].parameters;
                    return true;
                }
            }
            else
            {
                var rely = relies[declaration.library];
                if (declaration.code == DeclarationCode.MemberFunction)
                {
                    types = rely.methods[rely.definitions[declaration.definitionIndex].methods[declaration.index]].functions[(int)declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.ConstructorFunction)
                {
                    types = rely.methods[declaration.index].functions[declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.Delegate)
                {
                    types = rely.delegates[declaration.index].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.InterfaceFunction)
                {
                    types = rely.interfaces[declaration.definitionIndex].methods[declaration.index].functions[declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.GlobalFunction)
                {
                    types = rely.methods[declaration.index].functions[declaration.overloadIndex].parameters;
                    return true;
                }
                else if (declaration.code == DeclarationCode.NativeFunction)
                {
                    types = rely.natives[declaration.index].functions[declaration.overloadIndex].parameters;
                    return true;
                }
            }
            types = default;
            return false;
        }
        public bool TryGetReturns(Declaration declaration, out CompilingType[] types)
        {
            if (declaration.library == LIBRARY.KERNEL)
            {
                if (declaration.code == DeclarationCode.MemberFunction)
                {
                    types = RelyKernel.methods[RelyKernel.definitions[declaration.definitionIndex].methods[declaration.index]].functions[declaration.overloadIndex].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.ConstructorFunction)
                {
                    types = new CompilingType[] { new CompilingType(LIBRARY.KERNEL, Visibility.Public, (TypeCode)declaration.definitionIndex, declaration.definitionIndex, 0) };
                    return true;
                }
                else if (declaration.code == DeclarationCode.GlobalFunction)
                {
                    types = RelyKernel.methods[declaration.index].functions[declaration.overloadIndex].returns;
                    return true;
                }
            }
            else if (declaration.library == LIBRARY.SELF)
            {
                if (declaration.code == DeclarationCode.MemberFunction)
                {
                    types = library.methods[(int)library.definitions[(int)declaration.definitionIndex].methods[declaration.index]][(int)declaration.overloadIndex].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.ConstructorFunction)
                {
                    types = new CompilingType[] { new CompilingType(new CompilingDefinition(library.definitions[(int)declaration.definitionIndex].declaration), 0) };
                    return true;
                }
                else if (declaration.code == DeclarationCode.Delegate)
                {
                    types = library.delegates[(int)declaration.index].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.Coroutine)
                {
                    types = library.coroutines[(int)declaration.index].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.InterfaceFunction)
                {
                    types = library.interfaces[(int)declaration.definitionIndex].methods[declaration.index].functions[declaration.overloadIndex].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.GlobalFunction)
                {
                    types = library.methods[(int)declaration.index][(int)declaration.overloadIndex].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.NativeFunction)
                {
                    types = library.natives[(int)declaration.index][(int)declaration.overloadIndex].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.Lambda)
                {
                    types = library.methods[(int)declaration.index][0].returns;
                    return true;
                }
            }
            else
            {
                var rely = relies[declaration.library];
                if (declaration.code == DeclarationCode.MemberFunction)
                {
                    types = rely.methods[rely.definitions[declaration.definitionIndex].methods[declaration.index]].functions[(int)declaration.overloadIndex].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.ConstructorFunction)
                {
                    types = new CompilingType[] { new CompilingType(new CompilingDefinition(rely.definitions[(int)declaration.definitionIndex].declaration), 0) };
                    return true;
                }
                else if (declaration.code == DeclarationCode.Delegate)
                {
                    types = rely.delegates[declaration.index].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.Coroutine)
                {
                    types = rely.coroutines[declaration.index].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.InterfaceFunction)
                {
                    types = rely.interfaces[declaration.definitionIndex].methods[declaration.index].functions[declaration.overloadIndex].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.GlobalFunction)
                {
                    types = rely.methods[declaration.index].functions[declaration.overloadIndex].returns;
                    return true;
                }
                else if (declaration.code == DeclarationCode.NativeFunction)
                {
                    types = rely.natives[declaration.index].functions[declaration.overloadIndex].returns;
                    return true;
                }
            }
            types = default;
            return false;
        }
        public bool TryGetFunction(Declaration methodDeclaration, CompilingType[] parameters, CompilingType[] returns, out Declaration functionDeclaration)
        {
            if (methodDeclaration.library == LIBRARY.KERNEL)
            {
                if (methodDeclaration.code == DeclarationCode.MemberMethod)
                {
                    foreach (var function in RelyKernel.methods[RelyKernel.definitions[methodDeclaration.definitionIndex].methods[methodDeclaration.index]].functions)
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return CompilingType.IsEquals(returns, function.returns);
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.Constructor)
                {
                    foreach (var function in RelyKernel.methods[methodDeclaration.index].functions)
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return returns.Length == 1 && returns[0] == new CompilingType(LIBRARY.KERNEL, Visibility.Public, (TypeCode)function.declaration.definitionIndex, function.declaration.definitionIndex, 0);
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.GlobalMethod)
                {
                    foreach (var function in RelyKernel.methods[methodDeclaration.index].functions)
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return CompilingType.IsEquals(returns, function.returns);
                        }
                }
            }
            else if (methodDeclaration.library == LIBRARY.SELF)
            {
                if (methodDeclaration.code == DeclarationCode.MemberMethod)
                {
                    foreach (var function in library.methods[(int)library.definitions[(int)methodDeclaration.definitionIndex].methods[methodDeclaration.index]])
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return CompilingType.IsEquals(returns, function.returns);
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.Constructor)
                {
                    foreach (var function in library.methods[(int)methodDeclaration.index])
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return returns.Length == 1 && returns[0] == new CompilingType(LIBRARY.SELF, Visibility.Public, (TypeCode)function.declaration.definitionIndex, function.declaration.definitionIndex, 0);
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.InterfaceMethod)
                {
                    foreach (var function in library.interfaces[(int)methodDeclaration.definitionIndex].methods[methodDeclaration.index].functions)
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return CompilingType.IsEquals(returns, function.returns);
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.GlobalMethod)
                {
                    foreach (var function in library.methods[(int)methodDeclaration.index])
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return CompilingType.IsEquals(returns, function.returns);
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.NativeMethod)
                {
                    foreach (var function in library.natives[(int)methodDeclaration.index])
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return CompilingType.IsEquals(returns, function.returns);
                        }
                }
            }
            else
            {
                var relyLibrary = relies[methodDeclaration.library];
                if (methodDeclaration.code == DeclarationCode.MemberMethod)
                {
                    foreach (var function in relyLibrary.methods[relyLibrary.definitions[(int)methodDeclaration.definitionIndex].methods[methodDeclaration.index]].functions)
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return CompilingType.IsEquals(returns, function.returns);
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.Constructor)
                {
                    foreach (var function in relyLibrary.methods[(int)methodDeclaration.index].functions)
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return returns.Length == 1 && returns[0] == new CompilingType(methodDeclaration.library, Visibility.Public, (TypeCode)function.declaration.definitionIndex, function.declaration.definitionIndex, 0);
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.InterfaceMethod)
                {
                    foreach (var function in relyLibrary.interfaces[(int)methodDeclaration.definitionIndex].methods[methodDeclaration.index].functions)
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return CompilingType.IsEquals(returns, function.returns);
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.GlobalMethod)
                {
                    foreach (var function in relyLibrary.methods[(int)methodDeclaration.index].functions)
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return CompilingType.IsEquals(returns, function.returns);
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.NativeMethod)
                {
                    foreach (var function in relyLibrary.natives[(int)methodDeclaration.index].functions)
                        if (CompilingType.IsEquals(parameters, function.parameters))
                        {
                            functionDeclaration = function.declaration;
                            return CompilingType.IsEquals(returns, function.returns);
                        }
                }
            }
            functionDeclaration = default;
            return false;
        }
        public bool TryGetFunction(Declaration methodDeclaration, CompilingType[] parameters, out Declaration functionDeclaration)
        {
            functionDeclaration = default;
            var measure = uint.MaxValue;
            if (methodDeclaration.library == LIBRARY.KERNEL)
            {
                if (methodDeclaration.code == DeclarationCode.MemberMethod)
                {
                    foreach (var function in RelyKernel.methods[RelyKernel.definitions[methodDeclaration.definitionIndex].methods[methodDeclaration.index]].functions)
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.Constructor)
                {
                    foreach (var function in RelyKernel.methods[methodDeclaration.index].functions)
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.GlobalMethod)
                {
                    foreach (var function in RelyKernel.methods[methodDeclaration.index].functions)
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
            }
            else if (methodDeclaration.library == LIBRARY.SELF)
            {
                if (methodDeclaration.code == DeclarationCode.MemberMethod)
                {
                    foreach (var function in library.methods[(int)library.definitions[(int)methodDeclaration.definitionIndex].methods[methodDeclaration.index]])
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.Constructor)
                {
                    foreach (var function in library.methods[(int)methodDeclaration.index])
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.InterfaceMethod)
                {
                    foreach (var function in library.interfaces[(int)methodDeclaration.definitionIndex].methods[methodDeclaration.index].functions)
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.GlobalMethod)
                {
                    foreach (var function in library.methods[(int)methodDeclaration.index])
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.NativeMethod)
                {
                    foreach (var function in library.natives[(int)methodDeclaration.index])
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
            }
            else
            {
                var relyLibrary = relies[methodDeclaration.library];
                if (methodDeclaration.code == DeclarationCode.MemberMethod)
                {
                    foreach (var function in relyLibrary.methods[relyLibrary.definitions[(int)methodDeclaration.definitionIndex].methods[methodDeclaration.index]].functions)
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.Constructor)
                {
                    foreach (var function in relyLibrary.methods[(int)methodDeclaration.index].functions)
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.InterfaceMethod)
                {
                    foreach (var function in relyLibrary.interfaces[(int)methodDeclaration.definitionIndex].methods[methodDeclaration.index].functions)
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.GlobalMethod)
                {
                    foreach (var function in relyLibrary.methods[(int)methodDeclaration.index].functions)
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
                else if (methodDeclaration.code == DeclarationCode.NativeMethod)
                {
                    foreach (var function in relyLibrary.natives[(int)methodDeclaration.index].functions)
                        if (TryGetMeasure(function.parameters, parameters, out var result) && result < measure)
                        {
                            functionDeclaration = function.declaration;
                            measure = result;
                        }
                }
            }
            return measure < uint.MaxValue;
        }
        public bool TryGetMeasure(CompilingType[] baseTypes, CompilingType[] subTypes, out uint measure)
        {
            measure = 0;
            if (baseTypes.Length != subTypes.Length) return false;
            for (int i = 0; i < baseTypes.Length; i++)
                if (TryGetMeasure(baseTypes[i], subTypes[i], out var result)) measure += result;
                else return false;
            return true;
        }
        public bool TryGetMeasure(CompilingType baseType, CompilingType subType, out uint measure)
        {
            measure = 0;
            if (subType == baseType) return true;
            if (subType == RelyKernel.NULL_TYPE) return baseType.dimension > 0 || baseType.definition.code == TypeCode.Handle || baseType.definition.code == TypeCode.Interface || baseType.definition.code == TypeCode.Function || baseType.definition.code == TypeCode.Coroutine || baseType.definition.code == TypeCode.Entity;
            else if (baseType.dimension > 0) return false;
            else if (subType.dimension > 0)
            {
                if (baseType == new CompilingType(RelyKernel.HANDLE, 0))
                {
                    measure = 2;
                    return true;
                }
                else if (baseType == new CompilingType(RelyKernel.ARRAY, 0))
                {
                    measure = 1;
                    return true;
                }
                return false;
            }
            if (subType.definition == RelyKernel.INTEGER)
            {
                if (baseType.definition == RelyKernel.REAL)
                {
                    measure += 0xff;
                    return true;
                }
            }
            else if (subType.definition == RelyKernel.REAL2)
            {
                if (baseType.definition == RelyKernel.REAL3)
                {
                    measure += 0xfff;
                    return true;
                }
                else if (baseType.definition == RelyKernel.REAL4)
                {
                    measure += 0xffff;
                    return true;
                }
            }
            else if (subType.definition == RelyKernel.REAL3)
            {
                if (baseType.definition == RelyKernel.REAL4)
                {
                    measure += 0xfff;
                    return true;
                }
            }
            else return TryGetInherit(baseType, subType, out measure);
            return false;
        }
        public string GetDeclarationFullName(Declaration declaration)
        {
            if (declaration)
            {
                if (declaration.library == LIBRARY.KERNEL)
                {
                    switch (declaration.code)
                    {
                        case DeclarationCode.Invalid: break;
                        case DeclarationCode.Definition:
                            {
                                var definition = RelyKernel.definitions[declaration.index];
                                return definition.space.GetFullName() + "." + definition.name;
                            }
                        case DeclarationCode.MemberVariable:
                            {
                                var definition = RelyKernel.definitions[declaration.definitionIndex];
                                var vaibale = definition.variables[declaration.index];
                                return definition.space.GetFullName() + "." + definition.name + "." + vaibale.name;
                            }
                        case DeclarationCode.MemberMethod:
                            {
                                var definition = RelyKernel.definitions[declaration.definitionIndex];
                                var method = RelyKernel.methods[definition.methods[declaration.index]];
                                return definition.space.GetFullName() + "." + definition.name + "." + method.name;
                            }
                        case DeclarationCode.MemberFunction:
                            {
                                var definition = RelyKernel.definitions[declaration.definitionIndex];
                                var method = RelyKernel.methods[definition.methods[declaration.index]];
                                var function = method.functions[declaration.overloadIndex];
                                var name = definition.space.GetFullName() + "." + definition.name + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.Constructor:
                            {
                                var definition = RelyKernel.definitions[declaration.definitionIndex];
                                if (definition.constructors == LIBRARY.METHOD_INVALID) break;
                                var method = RelyKernel.methods[definition.constructors];
                                return definition.space.GetFullName() + "." + definition.name + "." + method.name;
                            }
                        case DeclarationCode.ConstructorFunction:
                            {
                                var definition = RelyKernel.definitions[declaration.definitionIndex];
                                if (definition.constructors == LIBRARY.METHOD_INVALID) break;
                                var method = RelyKernel.methods[definition.constructors];
                                var function = method.functions[declaration.overloadIndex];
                                var name = definition.space.GetFullName() + "." + definition.name + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.Delegate:
                        case DeclarationCode.Coroutine:
                        case DeclarationCode.Interface:
                        case DeclarationCode.InterfaceMethod:
                        case DeclarationCode.InterfaceFunction: break;
                        case DeclarationCode.GlobalVariable:
                            {
                                var variable = RelyKernel.variables[declaration.index];
                                return variable.space.GetFullName() + "." + variable.name;
                            }
                        case DeclarationCode.GlobalMethod:
                            {
                                var method = RelyKernel.methods[declaration.index];
                                return method.space.GetFullName() + "." + method.name;
                            }
                        case DeclarationCode.GlobalFunction:
                            {
                                var method = RelyKernel.methods[declaration.index];
                                var function = method.functions[declaration.overloadIndex];
                                var name = method.space.GetFullName() + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.NativeMethod:
                        case DeclarationCode.NativeFunction:
                        case DeclarationCode.Lambda:
                        case DeclarationCode.LambdaClosureValue:
                        case DeclarationCode.LocalVariable: break;
                    }
                }
                else if (declaration.library == LIBRARY.SELF)
                {
                    switch (declaration.code)
                    {
                        case DeclarationCode.Invalid: break;
                        case DeclarationCode.Definition:
                            {
                                var definition = library.definitions[(int)declaration.index];
                                return definition.space.GetFullName() + "." + definition.name.Segment;
                            }
                        case DeclarationCode.MemberVariable:
                            {
                                var definition = library.definitions[(int)declaration.definitionIndex];
                                var vaibale = definition.variables[declaration.index];
                                return definition.space.GetFullName() + "." + definition.name.Segment + "." + vaibale.name;
                            }
                        case DeclarationCode.MemberMethod:
                            {
                                var definition = library.definitions[(int)declaration.definitionIndex];
                                var method = library.methods[(int)definition.methods[declaration.index]];
                                return definition.space.GetFullName() + "." + definition.name.Segment + "." + method.name;
                            }
                        case DeclarationCode.MemberFunction:
                            {
                                var definition = library.definitions[(int)declaration.definitionIndex];
                                var method = library.methods[(int)definition.methods[declaration.index]];
                                var function = method[(int)declaration.overloadIndex];
                                var name = definition.space.GetFullName() + "." + definition.name + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.Constructor:
                            {
                                var definition = library.definitions[(int)declaration.definitionIndex];
                                if (definition.constructors == LIBRARY.METHOD_INVALID) break;
                                var method = library.methods[(int)definition.constructors];
                                return definition.space.GetFullName() + "." + definition.name.Segment + "." + method.name;
                            }
                        case DeclarationCode.ConstructorFunction:
                            {
                                var definition = library.definitions[(int)declaration.definitionIndex];
                                if (definition.constructors == LIBRARY.METHOD_INVALID) break;
                                var method = library.methods[(int)definition.constructors];
                                var function = method[(int)declaration.overloadIndex];
                                var name = definition.space.GetFullName() + "." + definition.name + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.Delegate:
                            {
                                var function = library.delegates[(int)declaration.index];
                                return function.space.GetFullName() + "." + function.name.Segment;
                            }
                        case DeclarationCode.Coroutine:
                            {
                                var coroutine = library.coroutines[(int)declaration.index];
                                return coroutine.space.GetFullName() + "." + coroutine.name.Segment;
                            }
                        case DeclarationCode.Interface:
                            {
                                var definition = library.interfaces[(int)declaration.index];
                                return definition.space.GetFullName() + "." + definition.name.Segment;
                            }
                        case DeclarationCode.InterfaceMethod:
                            {
                                var definition = library.interfaces[(int)declaration.definitionIndex];
                                var method = definition.methods[(int)declaration.index];
                                return definition.space.GetFullName() + "." + definition.name.Segment + "." + method.name;
                            }
                        case DeclarationCode.InterfaceFunction:
                            {
                                var definition = library.interfaces[(int)declaration.definitionIndex];
                                var method = definition.methods[(int)declaration.index];
                                var function = method.functions[(int)declaration.overloadIndex];
                                var name = definition.space.GetFullName() + "." + definition.name + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.GlobalVariable:
                            {
                                var variable = library.variables[(int)declaration.index];
                                return variable.space.GetFullName() + "." + variable.name.Segment;
                            }
                        case DeclarationCode.GlobalMethod:
                            {
                                var method = library.methods[(int)declaration.index];
                                return method.space.GetFullName() + "." + method.name;
                            }
                        case DeclarationCode.GlobalFunction:
                            {
                                var method = library.methods[(int)declaration.index];
                                var function = method[(int)declaration.overloadIndex];
                                var name = method.space.GetFullName() + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.NativeMethod:
                        case DeclarationCode.NativeFunction:
                            {
                                var method = library.natives[(int)declaration.index];
                                var function = method[(int)declaration.overloadIndex];
                                var name = method.space.GetFullName() + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.Lambda:
                        case DeclarationCode.LambdaClosureValue:
                        case DeclarationCode.LocalVariable: break;
                    }
                }
                else
                {
                    var rely = relies[declaration.library];
                    switch (declaration.code)
                    {
                        case DeclarationCode.Invalid: break;
                        case DeclarationCode.Definition:
                            {
                                var definition = rely.definitions[(int)declaration.index];
                                return definition.space.GetFullName() + "." + definition.name;
                            }
                        case DeclarationCode.MemberVariable:
                            {
                                var definition = rely.definitions[(int)declaration.definitionIndex];
                                var vaibale = definition.variables[declaration.index];
                                return definition.space.GetFullName() + "." + definition.name + "." + vaibale.name;
                            }
                        case DeclarationCode.MemberMethod:
                            {
                                var definition = rely.definitions[(int)declaration.definitionIndex];
                                var method = rely.methods[(int)definition.methods[declaration.index]];
                                return definition.space.GetFullName() + "." + definition.name + "." + method.name;
                            }
                        case DeclarationCode.MemberFunction:
                            {
                                var definition = rely.definitions[(int)declaration.definitionIndex];
                                var method = rely.methods[(int)definition.methods[declaration.index]];
                                var function = method.functions[declaration.overloadIndex];
                                var name = definition.space.GetFullName() + "." + definition.name + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.Constructor:
                            {
                                var definition = rely.definitions[(int)declaration.definitionIndex];
                                if (definition.constructors == LIBRARY.METHOD_INVALID) break;
                                var method = rely.methods[(int)definition.constructors];
                                return definition.space.GetFullName() + "." + definition.name + "." + method.name;
                            }
                        case DeclarationCode.ConstructorFunction:
                            {
                                var definition = rely.definitions[(int)declaration.definitionIndex];
                                if (definition.constructors == LIBRARY.METHOD_INVALID) break;
                                var method = rely.methods[(int)definition.constructors];
                                var function = method.functions[declaration.overloadIndex];
                                var name = definition.space.GetFullName() + "." + definition.name + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.Delegate:
                            {
                                var function = rely.delegates[(int)declaration.index];
                                return function.space.GetFullName() + "." + function.name;
                            }
                        case DeclarationCode.Coroutine:
                            {
                                var coroutine = rely.coroutines[(int)declaration.index];
                                return coroutine.space.GetFullName() + "." + coroutine.name;
                            }
                        case DeclarationCode.Interface:
                            {
                                var definition = rely.interfaces[(int)declaration.index];
                                return definition.space.GetFullName() + "." + definition.name;
                            }
                        case DeclarationCode.InterfaceMethod:
                            {
                                var definition = rely.interfaces[(int)declaration.definitionIndex];
                                var method = definition.methods[(int)declaration.index];
                                return definition.space.GetFullName() + "." + definition.name + "." + method.name;
                            }
                        case DeclarationCode.InterfaceFunction:
                            {
                                var definition = rely.interfaces[(int)declaration.definitionIndex];
                                var method = definition.methods[(int)declaration.index];
                                var function = method.functions[declaration.overloadIndex];
                                var name = definition.space.GetFullName() + "." + definition.name + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.GlobalVariable:
                            {
                                var variable = rely.variables[(int)declaration.index];
                                return variable.space.GetFullName() + "." + variable.name;
                            }
                        case DeclarationCode.GlobalMethod:
                            {
                                var method = rely.methods[(int)declaration.index];
                                return method.space.GetFullName() + "." + method.name;
                                var function = method.functions[declaration.overloadIndex];
                            }
                        case DeclarationCode.GlobalFunction:
                            {
                                var method = rely.methods[(int)declaration.index];
                                var function = method.functions[declaration.overloadIndex];
                                var name = method.space.GetFullName() + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.NativeMethod:
                            {
                                var method = rely.natives[(int)declaration.index];
                                return method.space.GetFullName() + "." + method.name;
                            }
                        case DeclarationCode.NativeFunction:
                            {
                                var method = rely.natives[(int)declaration.index];
                                var function = method.functions[declaration.overloadIndex];
                                var name = method.space.GetFullName() + "." + method.name + "(";
                                for (int i = 0; i < function.parameters.Length; i++)
                                {
                                    if (i > 0) name += ",";
                                    name += GetDeclarationFullName(function.parameters[i].definition.Declaration);
                                    for (int index = 0; index < function.parameters[i].dimension; index++) name += "[]";
                                }
                                return name + ")";
                            }
                        case DeclarationCode.Lambda:
                        case DeclarationCode.LambdaClosureValue:
                        case DeclarationCode.LocalVariable: break;
                    }
                }
            }
            return declaration.ToString();
        }
        public void Dispose()
        {
            lambdas.Dispose();
            foreach (var item in relyCompilings) item.Dispose();
            relyCompilings.Dispose();
            foreach (var item in relyReferences) item.Dispose();
            relyReferences.Dispose();
        }
    }
}
