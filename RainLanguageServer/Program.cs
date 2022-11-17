using Newtonsoft.Json;
using StreamJsonRpc;
using System;
using System.IO;
using System.Threading;

namespace RainLanguageServer
{
    internal class Program
    {
        public static readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private static StreamWriter writer;
        public static void Log(string msg)
        {
            writer?.WriteLine(msg);
        }
        static void Main(string[] args)
        {
            writer = File.CreateText(Environment.CurrentDirectory + "/log.txt");
            var jsonMessageFormatter = new JsonMessageFormatter();
            jsonMessageFormatter.JsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            jsonMessageFormatter.JsonSerializer.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
            jsonMessageFormatter.JsonSerializer.Converters.Add(new UriConverter());
            var server = new Server();
            using (var cin = Console.OpenStandardInput())
            using (var cout = Console.OpenStandardOutput())
            using (var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(cout, cin, jsonMessageFormatter), server))
            {
                server.SetJsonRpc(rpc);
                rpc.TraceSource.Switch.Level = System.Diagnostics.SourceLevels.Error;
                rpc.StartListening();
                cancellation.Token.WaitHandle.WaitOne();
            }
            writer.Close();
        }
    }
}
