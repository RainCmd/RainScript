namespace RainScript.Compiler.LogicGenerator
{
    internal readonly struct Local
    {
        public readonly Anchor anchor;
        public readonly uint index;
        public readonly CompilingType type;
        public Declaration Declaration
        {
            get
            {
                return new Declaration(LIBRARY.SELF, Visibility.Public, DeclarationCode.LocalVariable, index, 0, 0);
            }
        }
        public Local(Anchor anchor, uint index, CompilingType type)
        {
            this.anchor = anchor;
            this.index = index;
            this.type = type;
        }
    }
    internal class LocalContext : System.IDisposable
    {
        private readonly ScopeList<ScopeDictionary<string, Local>> localDeclarations;
        private uint index = 0;
        public LocalContext(CollectionPool pool)
        {
            localDeclarations = pool.GetList<ScopeDictionary<string, Local>>();
        }
        public void PushBlock(CollectionPool pool)
        {
            localDeclarations.Add(pool.GetDictionary<string, Local>());
        }
        public void PopBlock()
        {
            localDeclarations[-1].Dispose();
            localDeclarations.RemoveAt(-1);
        }
        public Local AddLocal(Anchor anchor, CompilingType type)
        {
            var result = new Local(anchor, index++, type);
            if ((bool)anchor) return localDeclarations[-1][anchor.Segment] = result;
            return result;
        }
        public Local AddLocal(string name, Anchor anchor, CompilingType type)
        {
            return localDeclarations[-1][name] = new Local(anchor, index++, type);
        }
        public bool TryGetLocal(string name, out Local local)
        {
            var index = localDeclarations.Count;
            while (index-- > 0)
                if (localDeclarations[index].TryGetValue(name, out local))
                    return true;
            local = default;
            return false;
        }
        public bool TryGetLocal(Declaration declaration, out Local local)
        {
            foreach (var locals in localDeclarations)
                foreach (var item in locals)
                    if (item.Value.index == declaration.index)
                    {
                        local = item.Value;
                        return true;
                    }
            local = default;
            return false;
        }
        public void Reset()
        {
            foreach (var item in localDeclarations) item.Dispose();
            localDeclarations.Clear();
            index = 0;
        }
        public void Dispose()
        {
            foreach (var item in localDeclarations) item.Dispose();
            localDeclarations.Dispose();
        }
    }
    internal class LambdaClosure : System.IDisposable
    {
        public readonly ExpressionParser environment;
        private readonly ScopeDictionary<Declaration, Declaration> map;
        private readonly ScopeList<Compiling.Definition.MemberVariableInfo> variables;
        private Compiling.Definition definition;
        public Compiling.Definition Closure { get { return definition; } }
        public LambdaClosure(ExpressionParser environment)
        {
            this.environment = environment;
            map = environment.pool.GetDictionary<Declaration, Declaration>();
            variables = environment.pool.GetList<Compiling.Definition.MemberVariableInfo>();
        }
        private Compiling.Definition GetDefinition()
        {
            if (definition == null)
            {
                var declaration = new Declaration(LIBRARY.SELF, Visibility.Space, DeclarationCode.Definition, (uint)environment.manager.library.definitions.Count, 0, 0);
                definition = new Compiling.Definition(default, declaration, environment.context.space, LIBRARY.METHOD_INVALID, null, null, null, default);
                environment.manager.library.definitions.Add(definition);
            }
            return definition;
        }
        public bool TryGetClosureVariables(uint methodIndex, out Declaration definition, out Declaration[] sourceVariables, out CompilingType[] types)
        {
            if (map.Count > 0)
            {
                definition = this.definition.declaration;
                this.definition = new Compiling.Definition(default, definition, this.definition.space, LIBRARY.METHOD_INVALID, null, variables.ToArray(), new uint[] { methodIndex }, default);
                this.definition.parent = RelyKernel.HANDLE;
                environment.manager.library.definitions[(int)definition.index] = this.definition;
                sourceVariables = new Declaration[map.Count];
                types = new CompilingType[map.Count];
                foreach (var item in map)
                {
                    var index = (int)item.Value.index;
                    sourceVariables[index] = item.Key;
                    types[index] = variables[index].type;
                }
                return true;
            }
            else
            {
                definition = default;
                sourceVariables = default;
                types = default;
                return false;
            }
        }
        public bool TryGetVariableType(Declaration declaration, out CompilingType type)
        {
            if (declaration.code == DeclarationCode.LambdaClosureValue)
            {
                type = variables[(int)declaration.index].type;
                return true;
            }
            type = default;
            return false;
        }
        private Declaration Convert(Anchor name, Declaration declaration)
        {
            if (!map.TryGetValue(declaration, out var result))
            {
                var definition = GetDefinition();
                result = new Declaration(LIBRARY.SELF, Visibility.Public, DeclarationCode.LambdaClosureValue, (uint)variables.Count, 0, definition.declaration.index);
                var variable = new Compiling.Definition.MemberVariableInfo(name, result, environment.context.space, default);
                variable.type = environment.GetVariableType(declaration);
                variables.Add(variable);
                map[declaration] = result;
            }
            return result;
        }
        public bool TryFindDeclaration(Anchor name, out Declaration declaration)
        {
            if (environment.TryFindDeclaration(name, out declaration))
            {
                if (declaration.code == DeclarationCode.LocalVariable || declaration.code == DeclarationCode.LambdaClosureValue)
                    declaration = Convert(name, declaration);
                return true;
            }
            declaration = default;
            return false;
        }
        public bool TryGetThisValueDeclaration(out Declaration declaration)
        {
            if (environment.TryGetThisValueDeclaration(out declaration))
            {
                declaration = Convert(default, declaration);
                return true;
            }
            return false;
        }
        public void Dispose()
        {
            map.Dispose();
            variables.Dispose();
        }
    }
}
