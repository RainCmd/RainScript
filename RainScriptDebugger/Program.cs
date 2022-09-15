using RainScript;
using RainScript.Compiler;
using RainScript.VirtualMachine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace RainScriptDebugger
{
    internal class Program
    {
        struct Config
        {
            public string name;
            public string entry;
            public HashSet<string> dirs;
            public string pattern;
            public bool symbol;
            public bool ignoreExit;
            public int frame;
            public IEnumerable<IFileInfo> GetFileInfos()
            {
                foreach (var dir in dirs)
                {
                    foreach (var path in Directory.GetFiles(dir, pattern, SearchOption.AllDirectories))
                    {
                        using (var file = File.OpenText(path))
                        {
                            yield return new FileInfo(path, file.ReadToEnd());
                        }
                    }
                }
            }
            public IEnumerable<ReferenceLibrary> GetReferences()
            {
                yield break;
            }
        }
        class FileInfo : IFileInfo
        {
            public FileInfo(string path, string context)
            {
                Path = path;
                Context = context;
            }
            public string Path { get; set; }
            public string Context { get; set; }
        }
        class Performer : IPerformer
        {
            public void Print(string value)
            {
                Console.WriteLine(value);
            }
        }
        static bool IsVaildName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!char.IsLetter(name[0])) return false;
            for (int i = 1; i < name.Length; i++)
                if (!char.IsLetterOrDigit(name[i])) return false;
            return true;
        }
        static void Main(string[] args)
        {
            var path = Environment.CurrentDirectory + "\\generator.config";
            if (!File.Exists(path))
            {
                Console.WriteLine("配置文件未找到：" + path);
                Console.ReadKey();
                return;
            }
            var config = new Config() { dirs = new HashSet<string>() };
            using (var file = File.OpenText(path))
            {
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine().Split('=');
                    if (line.Length == 2)
                    {
                        var item = line[0].Trim().ToLower();
                        var value = line[1].Trim();
                        switch (item)
                        {
                            case "name":
                                config.name = value;
                                break;
                            case "entry":
                                config.entry = value;
                                break;
                            case "path":
                                config.dirs.Add(value);
                                break;
                            case "pattern":
                                config.pattern = value;
                                break;
                            case "symbol":
                                bool.TryParse(value, out config.symbol);
                                break;
                            case "ignoreexit":
                                bool.TryParse(value, out config.ignoreExit);
                                break;
                            case "frame":
                                int.TryParse(value, out config.frame);
                                break;
                        }
                    }
                }
            }
            var index = 0;
            while (index < args.Length)
            {
                var arg = args[index].ToLower();
                switch (arg)
                {
                    case "-n":
                        if (++index < arg.Length) config.name = args[index];
                        break;
                    case "-entry":
                        if (++index < arg.Length) config.entry = args[index];
                        break;
                    case "-path":
                        if (++index < arg.Length) config.dirs.Add(args[index]);
                        break;
                    case "-p":
                        if (++index < arg.Length) config.pattern = args[index];
                        break;
                    case "-s":
                        config.symbol = true;
                        break;
                    case "-ie":
                        config.ignoreExit = true;
                        break;
                    case "-f":
                        if (!(++index < arg.Length && int.TryParse(args[index], out config.frame)))
                        {
                            Console.WriteLine("帧命令解析失败");
                        }
                        break;
                    default:
                        Console.WriteLine("未知的命令：" + args[index]);
                        break;
                }
            }
            if (!IsVaildName(config.name))
            {
                Console.WriteLine("无效的名称：" + config.name);
                return;
            }
            foreach (var dir in config.dirs)
                if (!Directory.Exists(dir))
                {
                    Console.WriteLine("目录不存在：" + dir);
                    return;
                }
            if (config.frame < 0)
            {
                Console.WriteLine("帧间隔不能小于0，当前间隔{0}ms", config.frame);
                return;
            }

            var builder = new Builder(config.name, config.GetFileInfos(), config.GetReferences());
            try
            {
                builder.Compile(new CompilerCommand(config.symbol, config.ignoreExit));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
            var librarys = new Dictionary<string, Library>
            {
                [config.name] = builder.Library
            };
            var performers = new Dictionary<string, Performer>
            {
                [config.name] = new Performer()
            };
            using (var kernel = new Kernel(builder.Library, name => librarys[name], name => performers[name]))
            {
                if (builder.SymbolTable != null)
                {
                    var symbols = new Dictionary<string, SymbolTable>
                    {
                        { config.name, builder.SymbolTable }
                    };
                    kernel.OnExit += (frames, code) =>
                    {
                        Console.WriteLine("携程异常退出，退出代码:" + code.ToString("X"));
                        foreach (var frame in frames)
                        {
                            kernel.GetFrameDetail(frame, name => symbols[name], out var fileName, out var functionName, out var lineNumber);
                            Console.WriteLine("{0} line:{1} func:{2}", fileName, functionName, lineNumber);
                        }
                    };
                }
                var handle = kernel.GetFunctionHandle(config.entry, config.name);
                if (handle == null)
                {
                    Console.WriteLine("入口函数 {0} 未找到");
                }
                var invoker = kernel.Invoker(handle);
                invoker.Start(true, false);
                while (invoker.State == InvokerState.Running)
                {
                    kernel.Update();
                    Thread.Sleep(config.frame);
                }
            }
            Console.WriteLine("携程 {0} 已退出。", config.entry);
        }
    }
}
