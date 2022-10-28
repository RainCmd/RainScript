using System;
using System.Collections.Generic;
using System.IO;

namespace RainLanguageServer
{
    class DocumentElement : IDisposable
    {
        public int line;
        public virtual void OnChanged(int start, int end, int afterEnd)
        {
            if (line > end) line += afterEnd - end;
        }
        public virtual void Dispose() { }
        protected static void OnChanged<T>(List<T> declarations, int start, int end, int afterEnd) where T : DocumentElement
        {
            for (int i = 0; i < declarations.Count; i++)
            {
                var declaration = declarations[i];
                if (declaration.line < start) continue;
                else if (declaration.line > end) declaration.OnChanged(start, end, afterEnd);
                else
                {
                    declarations.RemoveAt(i--);
                    declaration.Dispose();
                }
            }
        }
    }
    class DocumentToken : DocumentElement
    {
        public Anchor anchor;
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            anchor.ApplyChanged(end, afterEnd);
        }
        public override void Dispose()
        {
            base.Dispose();
            //todo 清理映射
        }
    }
    class DocumentStatement : DocumentElement
    {
        public bool dirty;
        public readonly List<DocumentToken> tokens = new List<DocumentToken>();
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            foreach (var item in tokens) item.OnChanged(start, end, afterEnd);
        }
        public override void Dispose()
        {
            foreach (var item in tokens) item.Dispose();
            base.Dispose();
        }
    }
    class DocumentDeclrartion : DocumentElement
    {
        public Anchor modify;
        public Anchor name;
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            modify.ApplyChanged(end, afterEnd);
            name.ApplyChanged(end, afterEnd);
        }
        public override void Dispose()
        {
            //todo 清理映射
        }
    }
    class DocumentBlock : DocumentElement
    {
        public bool dirty = true;
        public int endLine;
        public int indent;
        public readonly DocumentBlock parent;
        public readonly List<DocumentBlock> children = new List<DocumentBlock>();
        public DocumentBlock(DocumentBlock parent)
        {
            this.parent = parent;
        }
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            if (start > endLine) return;
            else if (line <= end)
            {
                dirty = true;
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child.endLine < start) continue;
                    else if (child.line > end) continue;
                    else if (child.line < start) dirty = true;
                    else
                    {
                        children.RemoveAt(i--);
                        child.Dispose();
                        if (child.line == start && i >= 0) children[i].dirty = true;
                    }
                }
            }
            foreach (var item in children)
                item.OnChanged(start, end, afterEnd);
        }
        protected int ParseBlock(Document document, int start)
        {
            indent = -1;
            endLine = start;
            while (++start < document.lines.Count)
            {
                if (!string.IsNullOrEmpty(document.lines[start]))
                {
                    endLine = start;

                }
            }
            return start;
        }
        public override void Dispose()
        {
            foreach (var item in children) item.Dispose();
        }
        public static bool TryGetBlock(Document document, int baseIndent, int start, out int indent, out int end)
        {
            var index = start;
            indent = -1;
            end = start;
            while (++index < document.lines.Count)
            {
                var line = document.lines[index];
                if (TryGetIndent(line, out var lineIndent))
                {
                    if (lineIndent <= baseIndent) break;
                    else if (indent < 0) indent = lineIndent;
                    else;//todo ERR:对齐问题
                }
            }
            return end > start;
        }
        public static bool TryGetIndent(string value, out int indent)
        {
            indent = 0;
            if (string.IsNullOrEmpty(value)) return false;
            for (int i = 0; i < value.Length; i++)
                if (value[i] == ' ') indent++;
                else if (value[i] == '\t') indent += 4;
                else return true;
            return false;
        }
    }
    class DocumentVariable : DocumentDeclrartion
    {
        public DocumentToken type;
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            type.OnChanged(start, end, afterEnd);
        }
    }
    class DocumentFunctionDeclaration : DocumentDeclrartion
    {
        public readonly List<DocumentVariable> parameters = new List<DocumentVariable>();
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            foreach (var item in parameters)
                item.OnChanged(start, end, afterEnd);
        }
        public override void Dispose()
        {
            base.Dispose();
            foreach (var item in parameters) item.Dispose();
        }
    }
    class DocumentLogicBlock : DocumentBlock
    {
        public DocumentLogicBlock(DocumentBlock parent) : base(parent) { }
    }
    class DocumentFunction : DocumentBlock
    {
        public DocumentFunctionDeclaration declrartion;
        public DocumentFunction(DocumentBlock parent) : base(parent) { }
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            declrartion.OnChanged(start, end, afterEnd);
        }
        public override void Dispose()
        {
            base.Dispose();
            declrartion.Dispose();
        }
    }
    class DocumentDefinition : DocumentBlock
    {
        public DocumentDeclrartion declrartion;
        public readonly List<DocumentToken> inherts = new List<DocumentToken>();
        public readonly List<DocumentVariable> variables = new List<DocumentVariable>();
        public readonly List<DocumentFunction> functions = new List<DocumentFunction>();
        //todo 析构函数
        public DocumentDefinition(DocumentSpace parent) : base(parent)
        {
        }
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            declrartion.OnChanged(start, end, afterEnd);
            OnChanged(inherts, start, end, afterEnd);
            OnChanged(variables, start, end, afterEnd);
        }
        public override void Dispose()
        {
            base.Dispose();
            declrartion.Dispose();
            foreach (var item in variables) item.Dispose();
        }
    }
    class DocumentCoroutine : DocumentDeclrartion
    {
        public readonly List<DocumentToken> results = new List<DocumentToken>();
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            OnChanged(results, start, end, afterEnd);
        }
    }
    class DocumentInterface : DocumentBlock
    {
        public DocumentDeclrartion declrartion;
        public readonly List<DocumentToken> inherts = new List<DocumentToken>();
        public readonly List<DocumentFunctionDeclaration> functions = new List<DocumentFunctionDeclaration>();
        public DocumentInterface(DocumentBlock parent) : base(parent) { }
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            declrartion.OnChanged(start, end, afterEnd);
            OnChanged(inherts, start, end, afterEnd);
            OnChanged(functions, start, end, afterEnd);
        }
    }
    class DocumentSpace : DocumentBlock
    {
        public readonly Document document;
        public Anchor name;
        public Space space;
        public readonly List<DocumentToken> imports = new List<DocumentToken>();
        public readonly List<DocumentSpace> childSpaces = new List<DocumentSpace>();//block
        public readonly List<DocumentDefinition> definitions = new List<DocumentDefinition>();//block
        public readonly List<DocumentVariable> variables = new List<DocumentVariable>();
        public readonly List<DocumentFunctionDeclaration> delegates = new List<DocumentFunctionDeclaration>();
        public readonly List<DocumentCoroutine> coroutines = new List<DocumentCoroutine>();
        public readonly List<DocumentFunction> functions = new List<DocumentFunction>();//block
        public readonly List<DocumentInterface> interfaces = new List<DocumentInterface>();//block
        public readonly List<DocumentFunctionDeclaration> natives = new List<DocumentFunctionDeclaration>();
        public DocumentSpace(Document document, DocumentSpace parent) : base(parent)
        {
            this.document = document;
        }
        public void Parse()
        {
            for (var i = 0; i < document.lines.Count; i++)
            {
                var line = document.lines[i];
                if (TryGetIndent(line, out var lineIndex))
                {
                    Parse(lineIndex, 0, document.lines.Count - 1);
                    return;
                }
            }
        }
        private void Parse(int indent, int start, int end)
        {
            this.indent = indent;
            line = start;
            endLine = end;
            for (int i = start + 1; i <= end; i++)
            {
                var line = document.lines[i];

            }
        }
        public override void OnChanged(int start, int end, int afterEnd)
        {
            base.OnChanged(start, end, afterEnd);
            if ((bool)name) name.ApplyChanged(end, afterEnd);
            OnChanged(imports, start, end, afterEnd);
            OnChanged(variables, start, end, afterEnd);
            OnChanged(delegates, start, end, afterEnd);
            OnChanged(coroutines, start, end, afterEnd);
            OnChanged(natives, start, end, afterEnd);
        }
        public override void Dispose()
        {
            base.Dispose();
            foreach (var item in imports) item.Dispose();
            foreach (var item in variables) item.Dispose();
            foreach (var item in delegates) item.Dispose();
            foreach (var item in coroutines) item.Dispose();
            foreach (var item in natives) item.Dispose();
            space.documentSpaces.Remove(this);
        }
    }
    class Document : IDisposable
    {
        public readonly List<string> lines = new List<string>();
        private readonly Library library;
        private readonly DocumentSpace root;
        public Document(Library library, string path)
        {
            this.library = library;
            root = new DocumentSpace(this, null)
            {
                name = new Anchor(this, 0, 0, 0),
                space = library
            };
            using (var sr = File.OpenText(path))
                while (!sr.EndOfStream)
                    lines.Add(sr.ReadLine());
        }
        public void Analyse()
        {
            root.Parse();
        }
        public void OnChanged(TextDocumentContentChangedEvent[] changeds)
        {
            for (int x = 0; x < changeds.Length - 1; x++)
            {
                var i = x;
                for (int y = x + 1; y < changeds.Length; y++)
                    if (changeds[y].range.start > changeds[i].range.start)
                        i = y;
                if (i != x)
                    (changeds[i], changeds[x]) = (changeds[x], changeds[i]);
            }
            foreach (var changed in changeds)
            {
                var start = changed.range.start; var end = changed.range.end;
                var text = this.lines[start.line].Substring(start.character) + changed.text + this.lines[end.line].Substring(end.character);
                var lines = text.Replace("\r", "").Split('\n');
                this.lines.RemoveRange(start.line, end.line - start.line + 1);
                foreach (var line in lines)
                    this.lines.Insert(start.line, line);
                root.OnChanged(start.line, end.line, end.line * 2 - start.line + 1 - lines.Length);
            }
        }
        public void Dispose()
        {
            root.Dispose();
        }
    }
}
