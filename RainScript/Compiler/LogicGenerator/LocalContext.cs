namespace RainScript.Compiler.LogicGenerator
{
    internal struct Local
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
        private readonly CollectionPool pool;
        private readonly ScopeList<ScopeDictionary<string, Local>> localDeclarations;
        private uint index = 0;
        public LocalContext(CollectionPool pool)
        {
            this.pool = pool;
            localDeclarations = pool.GetList<ScopeDictionary<string, Local>>();
        }
        public void PushBlock()
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
            return localDeclarations[-1][anchor.Segment] = new Local(anchor, index++, type);
        }
        public Local AddLocal(string name, Anchor anchor, CompilingType type)
        {
            return localDeclarations[-1][name] = new Local(anchor, index++, type);
        }
        public Local GetLocal(uint index)
        {
            foreach (var locals in localDeclarations)
                foreach (var item in locals)
                    if (item.Value.index == index)
                        return item.Value;
            return default;
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
        public void Dispose()
        {
            foreach (var item in localDeclarations) item.Dispose();
            localDeclarations.Dispose();
        }
    }
}
