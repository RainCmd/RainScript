using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RainLanguageServer
{
    internal class Server
    {
        public string RootPath { get; private set; }
        private string rootUri;
        private TextDocumentAnalyse analyse;
        private JsonRpc rpc;
        public void SetJsonRpc(JsonRpc rpc)
        {
            this.rpc = rpc;
        }
        public void Log(MessageType type, string message)
        {
            _ = rpc.NotifyWithParameterObjectAsync("window/logMessage", new LogMessageParams() { type = type, message = message });
        }
        #region 声明周期
        [JsonRpcMethod("initialize")]
        public InitializeResult Initialize(JToken token, CancellationToken cancellationToken)
        {
            var param = token.ToObject<InitializeParams>();
            RootPath = param.rootPath;
            rootUri = param.rootUri.LocalPath;
            return InitializeResult.result;
        }
        [JsonRpcMethod("initialized")]
        public async Task InitializedAsync(JToken token, CancellationToken cancellationToken)
        {
            analyse = new TextDocumentAnalyse(RootPath);
            await Task.Run(analyse.Analyse, cancellationToken);
        }
        [JsonRpcMethod("shutdown")]
        public void Shutdown()
        {
            Program.cancellation.Cancel();
        }
        [JsonRpcMethod("exit")]
        public void Exit()
        {
            Environment.Exit(0);
        }
        #endregion
        #region 操作
        [JsonRpcMethod("textDocument/hover")]
        public async Task<Hover> Hover(JToken token, CancellationToken cancellationToken)
        {
            var param = token.ToObject<TextDocumentPositionParams>();
            return null;
        }
        [JsonRpcMethod("textDocument/codeAction")]
        public void CodeAction(JToken token, CancellationToken cancellationToken)
        {

        }
        [JsonRpcMethod("textDocument/documentSymbol")]
        public void DocumentSymbol(JToken token, CancellationToken cancellationToken)
        {

        }
        [JsonRpcMethod("textDocument/codeLens")]
        public void CodeLens(JToken token, CancellationToken cancellationToken)
        {

        }
        [JsonRpcMethod("textDocument/documentHighlight")]
        public void DocumentHighlight(JToken token, CancellationToken cancellationToken)
        {

        }
        [JsonRpcMethod("textDocument/definition")]
        public void Definition(JToken token, CancellationToken cancellationToken)
        {

        }
        [JsonRpcMethod("textDocument/references")]
        public void References(JToken token, CancellationToken cancellationToken)
        {

        }
        [JsonRpcMethod("textDocument/completion")]
        public void Completion(JToken token, CancellationToken cancellationToken)
        {

        }
        [JsonRpcMethod("textDocument/onTypeFormatting")]
        public void OnTypeFormatting(JToken token, CancellationToken cancellationToken)
        {

        }
        [JsonRpcMethod("textDocument/rename")]
        public void Rename(JToken token, CancellationToken cancellationToken)
        {

        }
        #endregion
        #region 文件变化
        [JsonRpcMethod("textDocument/didOpen")]
        public void DidOpenTextDocument(JToken token, CancellationToken cancellationToken) { }
        [JsonRpcMethod("textDocument/didChange")]
        public async Task DidChangeTextDocumentAsync(JToken token, CancellationToken cancellationToken)
        {
            var param = token.ToObject<DidChangeTextDocumentParams>();
            await Task.Run(() => analyse.OnTextDocumentChanged(param.textDocument.uri.LocalPath, param.contentChanges), cancellationToken);
            await Task.Run(analyse.Analyse, cancellationToken);
        }
        [JsonRpcMethod("textDocument/didClose")]
        public async Task DidCloseTextDocumentAsync(JToken token, CancellationToken cancellationToken)
        {
            await Task.Run(analyse.Analyse, cancellationToken);
        }
        #endregion
    }
}
