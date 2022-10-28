using System.Collections.Generic;
using System.IO;

namespace RainLanguageServer
{
    internal class TextDocumentAnalyse
    {
        private readonly string rootPath;
        private readonly Dictionary<string, Document> documents = new Dictionary<string, Document>();
        private readonly List<string> deletes = new List<string>();
        private readonly Library library = new Library();
        public TextDocumentAnalyse(string rootPath)
        {
            this.rootPath = rootPath;
            library.name = Path.GetFileName(rootPath);
        }
        public void Analyse()
        {
            lock (this)
            {
                foreach (var item in documents)
                    if (!File.Exists(item.Key))
                    {
                        deletes.Add(item.Key);
                        item.Value.Dispose();
                    }
                foreach (var item in deletes)
                    documents.Remove(item);
                deletes.Clear();

                foreach (var item in Directory.EnumerateFiles(rootPath, "*.rain", SearchOption.AllDirectories))
                    if (!documents.ContainsKey(item))
                        documents.Add(item, new Document(library, item));

                foreach (var item in documents)
                    item.Value.Analyse();
            }
        }
        public void OnTextDocumentChanged(string path, TextDocumentContentChangedEvent[] changeds)
        {
            lock (this)
            {
                if (documents.TryGetValue(path, out var document))
                    document.OnChanged(changeds);
            }
        }
    }
}
