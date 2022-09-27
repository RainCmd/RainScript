using RainScript;
using RainScript.VirtualMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RainScriptDebugger
{
    enum SENDInstruction : int
    {
        Reply = 0xffff,

        HitBreakpoint = 0x2001,
        Exception,
        Terminated,
    }
    enum RECVInstruction
    {
        Continue = 0x1001,
        Next,
        Pause,
        SetBreakpoint,
        ClearBreakpoint,
        GetCoroutines,
        SetVariable,
        GetVariable,
        GetHeap,
        GetStack,
    }
    internal struct Breakpoint
    {
        public readonly int id;
        public readonly uint library;
        public readonly uint point;
        public readonly bool verified;
        public Breakpoint(int id, uint library, uint point, bool verified)
        {
            this.id = id;
            this.library = library;
            this.point = point;
            this.verified = verified;
        }
        public override bool Equals(object obj)
        {
            return obj is Breakpoint breakpoint && id == breakpoint.id;
        }
        public override int GetHashCode()
        {
            return id;
        }
    }
    internal unsafe class Adapter : IDisposable
    {
        private const byte BASE_Stackzero = 5;
        private bool _disposed, _continue;
        private readonly int selfToken, remoteToken;
        private readonly Kernel kernel;
        private readonly DebugTable debug;
        private readonly Socket socket;
        private readonly IPEndPoint remote;
        private readonly byte[] sendBuffer, recvBuffer;
        private readonly uint library;
        private readonly Dictionary<int, Breakpoint> breakpoints = new Dictionary<int, Breakpoint>();
        private readonly SymbolTable symbol;
        private DateTime lastbeat;
        private int breakpointIndex = 1;
        private BufferWriter GetWriter(RainSocketHead head)
        {
            var writer = new BufferWriter(sendBuffer);
            writer.Write((byte)head);
            writer.Write(selfToken);
            return writer;
        }
        public Adapter(Kernel kernel, DebugTable debug, SymbolTable symbol, IPEndPoint remote, byte[] sendBuffer, byte[] recvBuffer, int token, BufferReader reader)
        {
            this.kernel = kernel;
            this.debug = debug;
            this.symbol = symbol;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var port = 14576;
        rebind:
            try
            {
                socket.Bind(new IPEndPoint(remote.Address, port));
            }
            catch (Exception)
            {
                port++;
                goto rebind;
            }
            this.remote = remote;
            this.sendBuffer = sendBuffer;
            this.recvBuffer = recvBuffer;
            var rla = new RKernenl(kernel).libraryAgency;
            while (library < rla.Count)
            {
                if (rla[library].name == debug.name) break;
                library++;
            }
            if (library == rla.Count) throw new DllNotFoundException(string.Format("程序集 {0} 没有在已加载的程序集列表中", debug.name));
            selfToken = token;
            remoteToken = reader.ReadInt32();
            this.remote.Port = reader.ReadInt32();
            var writer = new BufferWriter(sendBuffer);
            writer.Write((byte)RainSocketHead.convention);
            writer.Write(remoteToken);
            writer.Write(selfToken);
            writer.Write(port);
            Send(writer);
            OnHeartbeat();
            new Thread(Heartbeat).Start();
        }
        private void Heartbeat()
        {
            while (!_disposed)
            {
                Thread.Sleep(1000);
                if (DateTime.Now - lastbeat > new TimeSpan(0, 0, 3)) Dispose();
                else lock (sendBuffer)
                    {
                        var writer = GetWriter(RainSocketHead.heartbeat);
                        Send(writer);
                    }
            }
        }
        private void OnHeartbeat()
        {
            lastbeat = DateTime.Now;
        }
        public void Recv()
        {
            while (!_disposed)
            {
                EndPoint point = remote;
                socket.ReceiveFrom(recvBuffer, ref point);
                var reader = new BufferReader(recvBuffer);
                switch ((RainSocketHead)reader.ReadInt8())
                {
                    case RainSocketHead.heartbeat:
                        if (reader.ReadInt32() == remoteToken) OnHeartbeat();
                        break;
                    case RainSocketHead.message:
                        if (reader.ReadInt32() == remoteToken)
                            lock (sendBuffer)
                            {
                                try
                                {
                                    OnRecv(reader);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                        break;
                }
            }
        }
        private void OnRecv(BufferReader reader)
        {
            reader.ReadInt32();//指令总长度
            var recv = (RECVInstruction)reader.ReadInt32();
            var reqID = reader.ReadInt32();
            switch (recv)
            {
                case RECVInstruction.Continue:
                    {
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        Send(writer);
                        new RKernenl(kernel).Step(false);
                        _continue = true;
                    }
                    break;
                case RECVInstruction.Next:
                    {
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        Send(writer);
                        new RKernenl(kernel).Step(true);
                        _continue = true;
                    }
                    break;
                case RECVInstruction.Pause:
                    {
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        Send(writer);
                        new RKernenl(kernel).Step(true);
                    }
                    break;
                case RECVInstruction.SetBreakpoint://插件里还没用到，可能需要补充
                    {
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        Send(writer);
                    }
                    break;
                case RECVInstruction.ClearBreakpoint:
                    {
                        reader.ReadInt32();//数据长度
                        ClearBreakpoint();
                        var fileName = reader.ReadString();
                        var lines = new int[reader.ReadInt32()];
                        for (int i = 0; i < lines.Length; i++) lines[i] = reader.ReadInt32() - 1;

                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);

                        writer.Write(lines.Length);
                        var code = new RKernenl(kernel).libraryAgency[library].code;
                        foreach (var line in lines)
                        {
                            var verified = debug.TryGetBreakpoint(fileName, line, out var point);
                            if (verified && *(code + point) == BASE_Stackzero)
                            {
                                var address = (int*)(code + point + 5);
                                *address = -Math.Abs(*address) - 1;
                            }
                            else verified = false;
                            var breakpoint = new Breakpoint(breakpointIndex++, library, point, verified);
                            breakpoints.Add(breakpoint.id, breakpoint);

                            writer.Write(breakpoint.id);
                            writer.Write(line + 1);
                            writer.Write(breakpoint.verified);
                        }

                        Send(writer);
                    }
                    break;
                case RECVInstruction.GetCoroutines:
                    {
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        var list = new List<int>();
                        foreach (var item in new RKernenl(kernel).coroutineAgency.Activities) list.Add((int)item.id);
                        var invokingID = (int)new RKernenl(kernel).coroutineAgency.invoking.id;
                        if (!list.Contains(invokingID)) list.Add(invokingID);
                        writer.Write(list.Count);
                        foreach (var item in list) writer.Write(item);

                        Send(writer);
                    }
                    break;
                case RECVInstruction.SetVariable://插件里还没用到，可能需要补充
                    {
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        Send(writer);
                    }
                    break;
                case RECVInstruction.GetVariable:
                    {
                        reader.ReadInt32();//指令数据长度
                        var type = reader.ReadInt32();
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);

                        if (type == 1)//局部变量
                        {
                            var point = writer.Size;
                            writer.Write(0);
                            var count = 0;
                            var invoking = new RKernenl(kernel).coroutineAgency.invoking;
                            var address = invoking.point;
                            var stack = invoking.stack;
                            foreach (var variable in debug.GetVariables(address))
                            {
                                writer.Write(variable.name);
                                writer.Write(DebugTable.Evaluate(kernel, variable, stack));
                                count++;
                            }
                            writer.Write(count, point);
                        }
                        else if (type == 2)//全局变量
                        {
                            writer.Write(debug.globalVariables.Count);
                            var address = new RKernenl(kernel).libraryAgency[library].data;
                            foreach (var variable in debug.globalVariables)
                            {
                                writer.Write(variable.name);
                                writer.Write(DebugTable.Evaluate(kernel, variable, address));
                            }
                        }

                        Send(writer);
                    }
                    break;
                case RECVInstruction.GetHeap://插件里还没用到，可能需要补充
                    {
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        Send(writer);
                    }
                    break;
                case RECVInstruction.GetStack:
                    {
                        reader.ReadInt32();//数据长度
                        var id = reader.ReadInt32();
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        var frames = new RKernenl(kernel).coroutineAgency.invoking.GetStackFrames();
                        writer.Write(frames.Length);
                        for (int i = 0; i < frames.Length; i++)
                        {
                            if (symbol != null)
                            {
                                symbol.GetInfo(frames[i], out var file, out var func, out var line);
                                writer.Write(func);
                                writer.Write((int)line + 1);
                                writer.Write(file);
                            }
                            else if (debug.TryGetFunctionInfo(frames[i].address, out var path, out var fn, out var line))
                            {
                                writer.Write(fn);
                                writer.Write((int)line + 1);
                                writer.Write(path);
                            }
                            else
                            {
                                writer.Write(frames[i].ToString());
                                writer.Write(0);
                                writer.Write("");
                            }
                        }
                        Send(writer);
                    }
                    break;
                default:
                    break;
            }
        }
        private void Send(BufferWriter writer)
        {
            socket.SendTo(sendBuffer, writer.Size, SocketFlags.None, remote);
        }

        internal void OnException(StackFrame[] stacks, long code)
        {
            var writer = GetWriter(RainSocketHead.message);
            writer.Write((int)SENDInstruction.Exception);
            writer.Write((int)new RKernenl(kernel).coroutineAgency.invoking.id);
            writer.Write(code);
            writer.Write(((ExitCode)code).ToString());
            var stackMsg = "";
            foreach (var stack in stacks)
            {
                if (!string.IsNullOrEmpty(stackMsg)) stackMsg += "\r\n";
                stackMsg += stack.ToString();
            }
            writer.Write(stackMsg);
            Send(writer);
        }
        internal void OnHit()
        {
            var writer = GetWriter(RainSocketHead.message);
            writer.Write((int)SENDInstruction.HitBreakpoint);
            writer.Write((int)new RKernenl(kernel).coroutineAgency.invoking.id);
            writer.Write("命中断点");
            Send(writer);
            _continue = false;
            while (!_continue && !_disposed) Thread.Sleep(10);
        }

        private void ClearBreakpoint()
        {
            var la = new RKernenl(kernel).libraryAgency;
            foreach (var item in breakpoints)
            {
                var point = la[item.Value.library].code + item.Value.point;
                if (*point == BASE_Stackzero)
                {
                    var p = (int*)(point + 5);
                    *p = Math.Abs(*p);
                }
            }
            breakpoints.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ClearBreakpoint();
            socket.Close();
        }
    }
}
