using System.Collections.Generic;

namespace RainScript.Compiler
{
    using Compiling;
    internal struct Context
    {
        public readonly Space space;
        public readonly Definition definition;
        public readonly IList<Space> relyCompilings;
        public readonly IList<RelySpace> relyReferences;
        public Context(Space space, Definition definition, IList<Space> relyCompilings, IList<RelySpace> relyReferences)
        {
            this.space = space;
            this.definition = definition;
            this.relyCompilings = relyCompilings;
            this.relyReferences = relyReferences;
        }
        public bool IsVisible(DeclarationManager manager, Declaration declaration)
        {
            if (declaration.visibility.ContainAny(Visibility.Public)) return true;
            else if (declaration.library == LIBRARY.KERNEL) return true;
            else if (declaration.library == LIBRARY.SELF)
            {
                if (declaration.visibility.ContainAny(Visibility.Internal)) return true;
                switch (declaration.code)
                {
                    case DeclarationCode.Invalid: return false;
                    case DeclarationCode.Definition:
                        return manager.library.definitions[(int)declaration.index].space.Contain(space);
                    case DeclarationCode.MemberVariable:
                    case DeclarationCode.MemberMethod:
                    case DeclarationCode.MemberFunction:
                    case DeclarationCode.Constructor:
                    case DeclarationCode.ConstructorFunction:
                        if (manager.library.definitions[(int)declaration.definitionIndex].space.Contain(space))
                            if (definition != null && definition.declaration == declaration) return true;
                            else if (declaration.visibility.ContainAny(Visibility.Protected)) return manager.TryGetInherit(new CompilingType(declaration.library, declaration.visibility, TypeCode.Handle, declaration.definitionIndex, 0), new CompilingType(LIBRARY.SELF, definition.declaration.visibility, TypeCode.Handle, definition.declaration.index, 0), out _);
                            else if (declaration.visibility.ContainAny(Visibility.Public | Visibility.Internal | Visibility.Space)) return true;
                        break;
                    case DeclarationCode.Delegate:
                        return manager.library.delegates[(int)declaration.index].space.Contain(space);
                    case DeclarationCode.Coroutine:
                        return manager.library.coroutines[(int)declaration.index].space.Contain(space);
                    case DeclarationCode.Interface:
                        return manager.library.interfaces[(int)declaration.index].space.Contain(space);
                    case DeclarationCode.InterfaceMethod:
                    case DeclarationCode.InterfaceFunction:
                        return manager.library.interfaces[(int)declaration.definitionIndex].space.Contain(space);
                    case DeclarationCode.GlobalVariable:
                        return manager.library.variables[(int)declaration.index].space.Contain(space);
                    case DeclarationCode.GlobalMethod:
                    case DeclarationCode.GlobalFunction:
                        return manager.library.methods[(int)declaration.index].space.Contain(space);
                    case DeclarationCode.NativeMethod:
                    case DeclarationCode.NativeFunction:
                        return manager.library.natives[(int)declaration.index].space.Contain(space);
                    case DeclarationCode.LocalVariable:
                        return true;
                }
            }
            else return true;
            return false;
        }
        public bool TryFindSpace(DeclarationManager manager, Anchor name, out ISpace space, CollectionPool pool, ExceptionCollector exceptions)
        {
            if (this.space.TryFindSpace(name.Segment, out space)) return true;
            using (var spaces = pool.GetList<ISpace>())
            {
                foreach (var item in relyCompilings)
                    if (item.TryFindChild(name.Segment, out var result))
                        spaces.Add(result);
                foreach (var item in relyReferences)
                    if (item.TryFindChild(name.Segment, out var result))
                        spaces.Add(result);
                foreach (var item in manager.relies)
                    if (item.name == name.Segment)
                        spaces.Add(item);
                if (spaces.Count == 0) return false;
                else
                {
                    space = spaces[0];
                    if (spaces.Count > 1)
                        foreach (var item in spaces)
                            exceptions.Add(name, CompilingExceptionCode.COMPILING_EQUIVOCAL, item.GetFullName());
                    return true;
                }
            }
        }
        public bool TryFindDeclaration(DeclarationManager manager, Anchor name, out Declaration result, CollectionPool pool, ExceptionCollector exceptions)
        {
            if (definition != null)
            {
                for (var index = new CompilingDefinition(definition.declaration); index != CompilingDefinition.INVALID; index = manager.GetParent(index))
                {
                    if (index.library == LIBRARY.KERNEL)
                    {
                        var target = RelyKernel.definitions[index.index];
                        foreach (var item in target.variables)
                            if (name.Segment == item.name)
                            {
                                result = item.declaration;
                                return true;
                            }
                        foreach (var item in target.methods)
                            if (name == RelyKernel.methods[item].name)
                            {
                                result = RelyKernel.methods[item].declaration;
                                return true;
                            }
                    }
                    else if (index.code == TypeCode.Handle)
                    {
                        if (index.library == LIBRARY.SELF)
                        {
                            var target = manager.library.definitions[(int)index.index];
                            foreach (var item in target.variables)
                                if (item.name == name)
                                    if (target == definition || item.declaration.visibility.Access(target.space.Contain(space), true))
                                    {
                                        result = item.declaration;
                                        return true;
                                    }
                                    else goto memberExit;
                            foreach (var item in target.methods)
                            {
                                var method = manager.library.methods[(int)item];
                                if (name == method.name)
                                {
                                    if (target == definition || method.Declaration.visibility.Access(target.space.Contain(space), true))
                                    {
                                        result = method.Declaration;
                                        return true;
                                    }
                                    else goto memberExit;
                                }
                            }
                        }
                        else
                        {
                            var target = manager.relies[index.library].definitions[index.index];
                            foreach (var item in target.variables)
                                if (name == item.name)
                                {
                                    result = item.declaration;
                                    return true;
                                }
                            var methods = manager.relies[index.library].methods;
                            foreach (var item in target.methods)
                                if (name == methods[item].name)
                                {
                                    result = methods[item].declaration;
                                    return true;
                                }
                        }
                    }
                }
            memberExit:;
            }
            for (var index = space; index != null; index = index.parent)
                if (index.TryFindDeclaration(name.Segment, out var declaration))
                {
                    result = declaration;
                    return true;
                }
            using (var declarations = pool.GetList<Declaration>())
            {
                foreach (var item in relyCompilings)
                    if (item.TryFindDeclaration(name.Segment, out var declaration))
                        declarations.Add(declaration);
                foreach (var item in relyReferences)
                    if (item.TryFindDeclaration(name.Segment, out var declaration))
                        declarations.Add(declaration);

                var space = this.space;
                declarations.RemoveAll(declaration =>
                {
                    if (declaration.library == LIBRARY.SELF) return !declaration.visibility.Access(manager.GetSpace(declaration).Contain(space), false);
                    else return (declaration.visibility & Visibility.Public) == 0;
                });
                if (declarations.Count == 0)
                {
                    result = default;
                    return false;
                }
                else
                {
                    result = declarations[0];
                    if (declarations.Count > 1)
                        foreach (var item in declarations)
                            exceptions.Add(name, CompilingExceptionCode.COMPILING_EQUIVOCAL, manager.GetDeclarationFullName(item));
                    return true;
                }
            }
        }
        public bool TryFindMemberDeclarartion(DeclarationManager manager, Anchor name, CompilingDefinition definition, out Declaration result, CollectionPool pool)
        {
            if (definition.code == TypeCode.Interface)
            {
                if (manager.TryGetInterface(definition, out var index))
                {
                    using (var stack = pool.GetStack<IInterface>())
                    {
                        stack.Push(index);
                        while (stack.Count > 0)
                        {
                            index = stack.Pop();
                            for (int i = 0; i < index.MethodCount; i++)
                            {
                                var method = index.GetMethod(i);
                                if (method.Name == name.Segment)
                                {
                                    result = method.Declaration;
                                    return true;
                                }
                            }
                            foreach (var item in index.Inherits)
                                if (manager.TryGetInterface(item, out var inherit))
                                    stack.Push(inherit);
                        }
                    }
                }
            }
            else if (manager.TryGetDefinition(definition, out var index))
            {
                while (index != null)
                {
                    for (int i = 0; i < index.MemberVaribaleCount; i++)
                    {
                        var variable = index.GetMemberVariable(i);
                        if (variable.Name == name.Segment)
                        {
                            result = variable.Declaration;
                            return true;
                        }
                    }
                    for (int i = 0; i < index.MethodCount; i++)
                    {
                        var method = index.GetMethod(i);
                        if (method.Name == name.Segment)
                        {
                            result = method.Declaration;
                            return true;
                        }
                    }
                    //todo 接口必须实现，所以直接在父类中找就可以了
                    foreach (var item in index.Inherits)
                        if (TryFindMemberDeclarartion(manager, name, item, out result, pool)) 
                            return true;
                    if (!manager.TryGetDefinition(index.Parent, out index)) break;
                }
            }
            result = default;
            return false;
        }
        public Declaration FindDeclaration(DeclarationManager manager, IList<Lexical> lexicals, CollectionPool pool, ExceptionCollector exceptions)
        {
            if (lexicals.Count == 1)
            {
                var name = lexicals[0].anchor.Segment.ToString();
                if (definition != null)
                {
                    for (var index = new CompilingDefinition(definition.declaration); index != CompilingDefinition.INVALID; index = manager.GetParent(index))
                    {
                        if (index.library == LIBRARY.KERNEL)
                        {
                            var target = RelyKernel.definitions[index.index];
                            foreach (var item in target.variables)
                                if (name == item.name)
                                    return item.declaration;
                            foreach (var item in target.methods)
                                if (name == RelyKernel.methods[item].name)
                                    return RelyKernel.methods[item].declaration;
                        }
                        else if (index.code == TypeCode.Handle)
                        {
                            if (index.library == LIBRARY.SELF)
                            {
                                var target = manager.library.definitions[(int)index.index];
                                foreach (var item in target.variables)
                                    if (item.name == name)
                                        if (target == definition || item.declaration.visibility.Access(target.space.Contain(space), true)) return item.declaration;
                                        else goto memberExit;
                                foreach (var item in target.methods)
                                {
                                    var method = manager.library.methods[(int)item];
                                    if (name == method.name)
                                    {
                                        if (target == definition || method.Declaration.visibility.Access(target.space.Contain(space), true)) return method.Declaration;
                                        else goto memberExit;
                                    }
                                }
                            }
                            else
                            {
                                var target = manager.relies[index.library].definitions[index.index];
                                foreach (var item in target.variables)
                                    if (name == item.name)
                                        return item.declaration;
                                var methods = manager.relies[index.library].methods;
                                foreach (var item in target.methods)
                                    if (name == methods[item].name)
                                        return methods[item].declaration;
                            }
                        }
                    }
                memberExit:;
                }
                for (var index = space; index != null; index = index.parent)
                    if (index.TryFindDeclaration(name, out var declaration))
                        return declaration;
                using (var declarations = pool.GetList<Declaration>())
                {
                    foreach (var item in relyCompilings)
                        if (item.TryFindDeclaration(name, out var result))
                            declarations.Add(result);
                    foreach (var item in relyReferences)
                        if (item.TryFindDeclaration(name, out var result))
                            declarations.Add(result);

                    if (declarations.Count > 0)
                    {
                        var space = this.space;
                        declarations.RemoveAll(declaration =>
                        {
                            if (declaration.library == LIBRARY.SELF) return !declaration.visibility.Access(manager.GetSpace(declaration).Contain(space), false);
                            else return (declaration.visibility & Visibility.Public) == 0;
                        });
                        if (declarations.Count == 0) exceptions.Add(lexicals, CompilingExceptionCode.COMPILING_DECLARATION_NOT_VISIBLE);
                        else
                        {
                            if (declarations.Count > 1) exceptions.Add(lexicals, CompilingExceptionCode.COMPILING_EQUIVOCAL);
                            return declarations[0];
                        }
                    }
                    else exceptions.Add(lexicals, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                }
            }
            else if (lexicals.Count > 1)
            {
                if (TryFindSpace(manager, lexicals[0].anchor, out var index, pool, exceptions))
                {
                    for (int i = 1; i < lexicals.Count - 1; i++)
                        if (index.TryFindChild(lexicals[i].anchor.Segment, out index))
                        {
                            exceptions.Add(lexicals, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                            return Declaration.INVALID;
                        }
                    if (space.TryFindDeclaration(lexicals[-1].anchor.Segment, out var result)) return result;
                }
                else exceptions.Add(lexicals, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
            }
            return Declaration.INVALID;
        }
    }
}
