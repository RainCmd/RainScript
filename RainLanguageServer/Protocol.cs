using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace RainLanguageServer
{
    public static class MarkupKind
    {
        public const string PlainText = "plaintext";
        public const string Markdown = "markdown";
    }
    [Serializable]
    public sealed class MarkupContent
    {
        public string kind;
        public string value;

        public static implicit operator MarkupContent(string text) => new MarkupContent { kind = MarkupKind.PlainText, value = text };
    }
    [Serializable]
    public struct Range
    {
        public Position start, end;

        public static implicit operator SourceSpan(Range r) => new SourceSpan(r.start, r.end);
        public static implicit operator Range(SourceSpan span) => new Range { start = span.Start, end = span.End };

        public override string ToString() => $"{start} - {end}";
    }
    [Serializable]
    public sealed class Hover
    {
        public MarkupContent contents;
        public Range? range;

        /// <summary>
        /// range所适用的文档版本。
        /// </summary>
        public int? _version;
    }
    [Serializable]
    public sealed class TextDocumentIdentifier
    {
        public Uri uri;

        public static implicit operator TextDocumentIdentifier(Uri uri) => new TextDocumentIdentifier { uri = uri };
    }

    [Serializable]
    public sealed class TextDocumentContentChangedEvent
    {
        public Range range;
        public int? rangeLength;
        public string text;
    }

    [Serializable]
    public sealed class VersionedTextDocumentIdentifier
    {
        public Uri uri;
        public int? version;
    }
    [Serializable]
    public sealed class DidChangeTextDocumentParams
    {
        public VersionedTextDocumentIdentifier textDocument;
        public TextDocumentContentChangedEvent[] contentChanges;
    }

    [Serializable]
    public sealed class TextDocumentPositionParams
    {
        public TextDocumentIdentifier textDocument;
        public Position position;
    }
    [Serializable]
    public sealed class DocumentLinkOptions
    {
        public bool resolveProvider;
    }

    [Serializable]
    public sealed class CodeLensOptions
    {
        public bool resolveProvider = true;
    }
    public enum TextDocumentSyncKind
    {
        None = 0,
        Full = 1,
        Incremental = 2
    }
    [Serializable]
    public sealed class TextDocumentSyncOptions
    {
        /// <summary>
        /// 打开和关闭通知被发送到服务器。
        /// </summary>
        public bool openClose = true;
        public TextDocumentSyncKind change = TextDocumentSyncKind.Incremental;
        public bool willSave;
        public bool willSaveWaitUntil;
        public SaveOptions save;
    }
    [Serializable]
    public sealed class CompletionOptions
    {
        /// <summary>
        /// 服务器提供对解决完成项的附加信息的支持。
        /// </summary>
        public bool resolveProvider = true;
        /// <summary>
        /// 自动触发补全的字符。
        /// </summary>
        public string[] triggerCharacters = { "." };
    }
    [Serializable]
    public sealed class SignatureHelpOptions
    {
        /// <summary>
        /// 触发签名的字符自动帮助。
        /// </summary>
        public string[] triggerCharacters = { "(", ",", ")" };
    }
    [Serializable]
    public sealed class CodeActionOptions
    {
        // 
        // 此服务器可能返回的codeaction类型。
        // 
        // 类型列表可以是泛型的，例如`CodeActionKind. js `。Refactor`，或者服务器可能会列出他们提供的每种特定类型。
        // 
        public string[] codeActionKinds = { "" };
    }
    [Serializable]
    public sealed class DocumentOnTypeFormattingOptions
    {
        public string firstTriggerCharacter = "\n";
        public string[] moreTriggerCharacter = { };
    }

    [Serializable]
    public sealed class ExecuteCommandOptions
    {
        public string[] commands = { };
    }

    [Serializable]
    public sealed class ServerCapabilities
    {
        public TextDocumentSyncOptions textDocumentSync = new TextDocumentSyncOptions();
        public bool hoverProvider = true;
        public CompletionOptions completionProvider = new CompletionOptions();
        public SignatureHelpOptions signatureHelpProvider = new SignatureHelpOptions();
        public bool definitionProvider = true;
        public bool referencesProvider = true;
        public bool documentHighlightProvider = true;
        public bool documentSymbolProvider = true;
        public bool workspaceSymbolProvider = true;
        public CodeActionOptions codeActionProvider = new CodeActionOptions();
        public CodeLensOptions codeLensProvider = new CodeLensOptions();
        public bool documentFormattingProvider = true;
        public bool documentRangeFormattingProvider = true;
        public DocumentOnTypeFormattingOptions documentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions();
        public bool renameProvider = true;
        public DocumentLinkOptions documentLinkProvider;
        public bool declarationProvider = false; // 3.14.0+
        public ExecuteCommandOptions executeCommandProvider = new ExecuteCommandOptions();
        public object experimental;
    }
    [Serializable]
    public struct InitializeResult
    {
        public ServerCapabilities capabilities;
        public static InitializeResult result = new InitializeResult() { capabilities = new ServerCapabilities() };
    }
    [Serializable]
    public sealed class InitializeParams
    {
        public string rootPath;
        public Uri rootUri;
        public TraceLevel trace;
    }
}
