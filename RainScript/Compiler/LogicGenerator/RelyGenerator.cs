using System;
using System.Linq;

namespace RainScript.Compiler.LogicGenerator
{
    internal class ReliedDeclaration
    {
        public readonly ReliedSpace space;
        public readonly string name;

        public ReliedDeclaration(ReliedSpace space, string name)
        {
            this.space = space;
            this.name = name;
        }
    }
    internal class ReliedMethod : ReliedDeclaration, IDisposable
    {
        public readonly ScopeList<Declaration> functions;
        public ReliedMethod(ReliedSpace space, string name, CollectionPool pool) : base(space, name)
        {
            functions = pool.GetList<Declaration>();
        }

        public void Dispose()
        {
            functions.Dispose();
        }
    }
    internal class ReliedDefinitioin : ReliedDeclaration, IDisposable
    {
        public readonly ScopeList<Declaration> variables;
        public readonly ScopeList<ReliedMethod> methods;
        public ReliedDefinitioin(ReliedSpace space, string name, CollectionPool pool) : base(space, name)
        {
            variables = pool.GetList<Declaration>();
            methods = pool.GetList<ReliedMethod>();
        }
        public void Dispose()
        {
            variables.Dispose();
            foreach (var method in methods) method.Dispose();
            methods.Dispose();
        }
    }
    internal class ReliedInterface : ReliedDeclaration, IDisposable
    {
        public readonly ScopeList<ReliedMethod> methods;
        public ReliedInterface(ReliedSpace space, string name, CollectionPool pool) : base(space, name)
        {
            methods = pool.GetList<ReliedMethod>();
        }
        public void Dispose()
        {
            methods.Dispose();
        }
    }
    internal class ReliedSpace : IDisposable
    {
        public readonly ImportSpaceInfo space;
        public readonly ScopeList<ReliedSpace> children;
        public ReliedSpace(ImportSpaceInfo space, CollectionPool pool)
        {
            this.space = space;
            children = pool.GetList<ReliedSpace>();
        }
        public ReliedSpace GetChild(string name, CollectionPool pool)
        {
            foreach (var child in children) if (child.space.name == name) return child;
            var result = new ReliedSpace(new ImportSpaceInfo(space, name), pool);
            children.Add(result);
            return result;
        }
        public virtual void Dispose()
        {
            foreach (var child in children) child.Dispose();
            children.Dispose();
        }
    }
    internal class ReliedLibrary : ReliedSpace
    {
        public readonly uint library;
        //todo 引用里用到的类型需要明确下，运行时验证用
        public readonly ScopeList<ReliedDefinitioin> definitioins;
        public readonly ScopeList<ReliedDeclaration> variables;
        public readonly ScopeList<ReliedDeclaration> delegates;
        public readonly ScopeList<ReliedDeclaration> coroutines;
        public readonly ScopeList<ReliedMethod> methods;
        public readonly ScopeList<ReliedInterface> interfaces;
        public readonly ScopeList<ReliedMethod> natives;
        public ReliedLibrary(uint library, string name, CollectionPool pool) : base(new ImportSpaceInfo(null, name), pool)
        {
            this.library = library;
            definitioins = pool.GetList<ReliedDefinitioin>();
            variables = pool.GetList<ReliedDeclaration>();
            delegates = pool.GetList<ReliedDeclaration>();
            coroutines = pool.GetList<ReliedDeclaration>();
            methods = pool.GetList<ReliedMethod>();
            interfaces = pool.GetList<ReliedInterface>();
            natives = pool.GetList<ReliedMethod>();
        }
        public ReliedSpace GetSpace(RelySpace space, CollectionPool pool)
        {
            if (space.parent == null) return this;
            else return GetSpace(space.parent, pool).GetChild(space.name, pool);
        }
        public override void Dispose()
        {
            base.Dispose();
            foreach (var definitioin in definitioins) definitioin.Dispose();
            definitioins.Dispose();
            variables.Dispose();
            delegates.Dispose();
            coroutines.Dispose();
            foreach (var method in methods) method.Dispose();
            methods.Dispose();
            interfaces.Dispose();
            foreach (var native in natives) native.Dispose();
            natives.Dispose();
        }
    }
    internal class ReliedGenerator : IDisposable
    {
        private readonly DeclarationManager manager;
        private readonly CollectionPool pool;
        private readonly ScopeDictionary<Declaration, Declaration> declarationMap;
        private readonly ScopeList<ReliedLibrary> libraries;
        public ReliedGenerator(DeclarationManager manager, CollectionPool pool)
        {
            this.manager = manager;
            this.pool = pool;
            declarationMap = pool.GetDictionary<Declaration, Declaration>();
            libraries = pool.GetList<ReliedLibrary>();
        }
        public Declaration Convert(Declaration declaration)
        {
            if (declaration.code == DeclarationCode.LocalVariable) return declaration;
            if (declaration.library == LIBRARY.SELF || declaration.library == LIBRARY.KERNEL) return declaration;
            if (!declarationMap.TryGetValue(declaration, out var result))
            {
                var rely = manager.relies[declaration.library];
                var index = libraries.FindIndex(value => value.library == declaration.library);
                if (index < 0)
                {
                    index = libraries.Count;
                    libraries.Add(new ReliedLibrary(declaration.library, rely.name, pool));
                }
                var relied = libraries[index];
                switch (declaration.code)
                {
                    case DeclarationCode.Invalid: goto default;
                    case DeclarationCode.Definition:
                        {
                            var source = rely.definitions[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.definitioins.Count, 0, 0);
                            relied.definitioins.Add(new ReliedDefinitioin(relied.GetSpace(source.space, pool), source.name, pool));
                        }
                        break;
                    case DeclarationCode.MemberVariable:
                        {
                            var sourceDefinition = rely.definitions[declaration.definitionIndex];
                            var definitionDeclaration = Convert(sourceDefinition.declaration);
                            var definition = libraries[(int)definitionDeclaration.library].definitioins[(int)definitionDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)definition.variables.Count, 0, definitionDeclaration.index);
                            definition.variables.Add(declaration);
                        }
                        break;
                    case DeclarationCode.MemberMethod:
                        {
                            var sourceDefinition = rely.definitions[declaration.definitionIndex];
                            var definitionDeclaration = Convert(sourceDefinition.declaration);
                            var definition = libraries[(int)definitionDeclaration.library].definitioins[(int)definitionDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)definition.methods.Count, 0, definitionDeclaration.index);
                            definition.methods.Add(new ReliedMethod(relied.GetSpace(sourceDefinition.space, pool), rely.methods[sourceDefinition.methods[declaration.index]].name, pool));
                        }
                        break;
                    case DeclarationCode.MemberFunction:
                        {
                            var sourceMethod = rely.methods[rely.definitions[declaration.definitionIndex].methods[declaration.index]];
                            var methodDeclaration = Convert(sourceMethod.declaration);
                            var method = libraries[(int)methodDeclaration.library].definitioins[(int)methodDeclaration.definitionIndex].methods[(int)declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, methodDeclaration.index, (uint)method.functions.Count, methodDeclaration.definitionIndex);
                            method.functions.Add(declaration);
                        }
                        break;
                    case DeclarationCode.Constructor:
                        {
                            var sourceDefinition = rely.definitions[declaration.definitionIndex];
                            var definitionDeclaration = Convert(sourceDefinition.declaration);
                            var definition = libraries[(int)definitionDeclaration.library].definitioins[(int)definitionDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)definition.methods.Count, 0, definitionDeclaration.index);
                            definition.methods.Add(new ReliedMethod(relied.GetSpace(sourceDefinition.space, pool), definition.name, pool));
                        }
                        break;
                    case DeclarationCode.ConstructorFunction:
                        {
                            var methodDeclaration = Convert(rely.methods[declaration.index].declaration);
                            var method = libraries[(int)methodDeclaration.library].definitioins[(int)methodDeclaration.definitionIndex].methods[(int)declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, methodDeclaration.index, (uint)method.functions.Count, methodDeclaration.definitionIndex);
                            method.functions.Add(declaration);
                        }
                        break;
                    case DeclarationCode.Delegate:
                        {
                            var source = rely.delegates[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.delegates.Count, 0, 0);
                            relied.delegates.Add(new ReliedDeclaration(relied.GetSpace(source.space, pool), source.name));
                        }
                        break;
                    case DeclarationCode.Coroutine:
                        {
                            var source = rely.coroutines[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.coroutines.Count, 0, 0);
                            relied.coroutines.Add(new ReliedDeclaration(relied.GetSpace(source.space, pool), source.name));
                        }
                        break;
                    case DeclarationCode.Interface:
                        {
                            var source = rely.interfaces[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.interfaces.Count, 0, 0);
                            relied.interfaces.Add(new ReliedInterface(relied.GetSpace(source.space, pool), source.name, pool));
                        }
                        break;
                    case DeclarationCode.InterfaceMethod:
                        {
                            var sourceInterface = rely.interfaces[declaration.index];
                            var interfaceDeclaration = Convert(sourceInterface.declaration);
                            var definition = libraries[(int)interfaceDeclaration.library].interfaces[(int)interfaceDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)definition.methods.Count, 0, interfaceDeclaration.index);
                            definition.methods.Add(new ReliedMethod(relied.GetSpace(sourceInterface.space, pool), definition.name, pool));
                        }
                        break;
                    case DeclarationCode.InterfaceFunction:
                        {
                            var sourceMethod = rely.interfaces[declaration.definitionIndex].methods[declaration.index];
                            var methodDeclaration = Convert(sourceMethod.declaration);
                            var method = libraries[(int)methodDeclaration.library].interfaces[(int)methodDeclaration.definitionIndex].methods[(int)declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, methodDeclaration.index, (uint)method.functions.Count, methodDeclaration.definitionIndex);
                            method.functions.Add(declaration);
                        }
                        break;
                    case DeclarationCode.GlobalVariable:
                        {
                            var source = rely.variables[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.variables.Count, 0, 0);
                            relied.variables.Add(new ReliedDeclaration(relied.GetSpace(source.space, pool), source.name));
                        }
                        break;
                    case DeclarationCode.GlobalMethod:
                        {
                            var source = rely.methods[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.methods.Count, 0, 0);
                            relied.methods.Add(new ReliedMethod(relied.GetSpace(source.space, pool), source.name, pool));
                        }
                        break;
                    case DeclarationCode.GlobalFunction:
                        {
                            var methodDeclaration = Convert(rely.methods[declaration.index].declaration);
                            var method = libraries[(int)methodDeclaration.library].methods[(int)methodDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, methodDeclaration.index, (uint)method.functions.Count, 0);
                            method.functions.Add(declaration);
                        }
                        break;
                    case DeclarationCode.NativeMethod:
                        {
                            var source = rely.natives[declaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, (uint)relied.natives.Count, 0, 0);
                            relied.natives.Add(new ReliedMethod(relied.GetSpace(source.space, pool), source.name, pool));
                        }
                        break;
                    case DeclarationCode.NativeFunction:
                        {
                            var methodDeclaration = Convert(rely.natives[declaration.index].declaration);
                            var native = libraries[(int)methodDeclaration.library].natives[(int)methodDeclaration.index];
                            result = new Declaration((uint)index, declaration.visibility, declaration.code, methodDeclaration.index, (uint)native.functions.Count, 0);
                            native.functions.Add(declaration);
                        }
                        break;
                    case DeclarationCode.Lambda:
                    case DeclarationCode.LocalVariable:
                    default: throw ExceptionGeneratorCompiler.Unknow();
                }
                declarationMap.Add(declaration, result);
            }
            return result;
        }
        public void Dispose()
        {
            declarationMap.Dispose();
            libraries.Dispose();
        }
    }
}
