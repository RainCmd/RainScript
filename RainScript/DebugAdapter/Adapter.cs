using RainScript.VirtualMachine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using RainScript.Vector;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript.DebugAdapter
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
        SetExceptionFilter,
        ClearBreakpoint,
        GetCoroutines,
        SetVariable,
        GetVariable,
        GetHeap,
        GetStack,
        GetHover,
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
        private bool _disposed, _continue;
        private readonly int selfToken, remoteToken;
        private readonly Kernel kernel;
        private readonly DebugTable debug;
        private readonly Socket socket;
        private readonly IPEndPoint remote;
        private readonly byte[] sendBuffer, recvBuffer;
        private readonly RuntimeLibraryInfo library;
        private readonly Dictionary<int, Breakpoint> breakpoints = new Dictionary<int, Breakpoint>();
        private readonly SymbolTable symbol;
        private long exceptionFilter;
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
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            catch (Exception)
            {
                port++;
                goto rebind;
            }
            this.remote = remote;
            this.sendBuffer = sendBuffer;
            this.recvBuffer = recvBuffer;
            library = kernel.libraryAgency.libraries.Find(value => value.name == debug.name);
            if (library == null) throw new DllNotFoundException(string.Format("程序集 {0} 没有在已加载的程序集列表中", debug.name));
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
                        kernel.step = false;
                        _continue = true;
                    }
                    break;
                case RECVInstruction.Next:
                    {
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        Send(writer);
                        kernel.step = true;
                        _continue = true;
                    }
                    break;
                case RECVInstruction.Pause:
                    {
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        Send(writer);
                        kernel.step = true;
                    }
                    break;
                case RECVInstruction.SetExceptionFilter:
                    {
                        reader.ReadInt32();//数据长度
                        long.TryParse(reader.ReadString(), out exceptionFilter);
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
                        var code = kernel.libraryAgency[library.index].code;
                        foreach (var line in lines)
                        {
                            var verified = debug.TryGetBreakpoint(fileName, line, out var point);
                            if (verified && *(code + point) == (byte)CommandMacro.BREAKPOINT) *(bool*)(code + point + 1) = true;
                            else verified = false;
                            var breakpoint = new Breakpoint(breakpointIndex++, library.index, point, verified);
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
                        foreach (var item in kernel.coroutineAgency.GetCoroutines()) list.Add((int)item.instanceID);
                        if (kernel.coroutineAgency.invoking != null)
                        {
                            var invokingID = (int)kernel.coroutineAgency.invoking.instanceID;
                            if (!list.Contains(invokingID)) list.Add(invokingID);
                        }
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
                        var index = reader.ReadInt32();
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        if (index == 0x7fff_ffff)//局部变量
                        {
                            writer.Write(0);

                            var point = writer.Size;
                            writer.Write(0);
                            var count = 0;
                            var invoking = kernel.coroutineAgency.invoking;
                            foreach (var variable in debug.GetVariables(invoking.point))
                            {
                                writer.Write(variable.name);
                                WriteVariable(ref writer, variable.type, invoking.stack + invoking.bottom + variable.address);
                                count++;
                            }
                            writer.Write(count, point);
                        }
                        else if (index == 0x7fff_fffe)//程序集
                        {
                            var point = writer.Size;
                            writer.Write(0);
                            var count = 0;
                            foreach (var space in debug.spaces)
                            {
                                writer.Write(space.name);
                                writer.Write(space.index);
                                count++;
                            }
                            writer.Write(count, point);

                            writer.Write(0);
                        }
                        else if (index >= DebugTable.SPACE_INDEX)
                        {
                            if (debug.TryGetSpace(index, out var space))
                            {
                                var point = writer.Size;
                                writer.Write(0);
                                var count = 0;
                                foreach (var item in space.spaces)
                                {
                                    writer.Write(item.name);
                                    writer.Write(item.index);
                                    count++;
                                }
                                writer.Write(count, point);

                                point = writer.Size;
                                writer.Write(0);
                                count = 0;
                                foreach (var variableIndex in space.variables)
                                {
                                    var variable = debug.globalVariables[variableIndex];
                                    writer.Write(variable.name);
                                    library.LocalToGlobal(variable.library, variable.index, out var globalLibrary, out var globalIndex);
                                    var target = kernel.libraryAgency[globalLibrary];
                                    WriteVariable(ref writer, variable.type, target.data + target.variables[globalIndex]);
                                    count++;
                                }
                                writer.Write(count, point);
                            }
                            else
                            {
                                writer.Write(0);
                                writer.Write(0);
                            }
                        }
                        else
                        {
                            writer.Write(0);

                            if (kernel.heapAgency.TryGetType((uint)index, out var type) == ExitCode.None)
                            {
                                if (type.dimension > 0)
                                {
                                    if (kernel.heapAgency.TryGetArrayLength((uint)index, out var length) == ExitCode.None)
                                    {
                                        type = new Type(type.definition, type.dimension - 1);
                                        writer.Write((int)length);
                                        for (int i = 0; i < length; i++)
                                        {
                                            writer.Write("[{0}]".Format(i));
                                            WriteVariable(ref writer, type, kernel.heapAgency.GetArrayPoint((uint)index, i));
                                        }
                                    }
                                    else writer.Write(0);
                                }
                                else
                                {
                                    var point = writer.Size;
                                    writer.Write(0);
                                    var count = WriteDefinition(ref writer, kernel.heapAgency.GetPoint((uint)index), type.definition);
                                    writer.Write(count, point);
                                }
                            }
                            else writer.Write(0);
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
                        var frames = kernel.coroutineAgency.invoking.GetStackFrames();
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
                case RECVInstruction.GetHover:
                    {
                        reader.ReadInt32();//数据长度
                        var path = reader.ReadString();
                        var line = reader.ReadInt32();
                        var col = reader.ReadInt32();
                        var writer = GetWriter(RainSocketHead.message);
                        writer.Write((int)SENDInstruction.Reply);
                        writer.Write(reqID);
                        if (debug.TryGetVariable(path, line, col, out var variable))
                        {
                            writer.Write(true);
                            var invoking = kernel.coroutineAgency.invoking;
                            WriteVariable(ref writer, variable.type, invoking.stack + invoking.bottom + variable.address);
                        }
                        else if (debug.TryGetGlobalVariable(path, line, col, out var globalVariable))
                        {
                            writer.Write(true);
                            library.LocalToGlobal(globalVariable.library, globalVariable.index, out var globalLibrary, out var globalIndex);
                            var target = kernel.libraryAgency[globalLibrary];
                            WriteVariable(ref writer, globalVariable.type, target.data + target.variables[globalIndex]);
                        }
                        else writer.Write(false);
                        Send(writer);
                    }
                    break;
            }
        }
        private int WriteDefinition(ref BufferWriter writer, byte* address, TypeDefinition definition)
        {
            var targetLibrary = kernel.libraryAgency[definition.library];
            switch (definition.code)
            {
                case TypeCode.Handle:
                    {
                        var info = targetLibrary.definitions[definition.index];
                        var count = info.variables.Length;
                        if (info.parent != TypeDefinition.INVALID) count += WriteDefinition(ref writer, address, info.parent);
                        for (int i = 0; i < info.variables.Length; i++)
                        {
                            writer.Write("[{0}]".Format(i));
                            var variable = info.variables[i];
                            WriteVariable(ref writer, variable.type, address + info.baseOffset + variable.offset);
                        }
                        return count;
                    }
                case TypeCode.Function:
                    {
                        var info = (RuntimeDelegateInfo*)address;
                        writer.Write("函数");
                        writer.Write(0);
                        writer.Write("");
                        writer.Write("{0},{1},{2}".Format(info->library, info->function.method, info->function.index));
                        if (info->type == FunctionType.Member || info->type == FunctionType.Virtual)
                        {
                            if (kernel.heapAgency.TryGetType(info->target, out var type) == ExitCode.None)
                            {
                                writer.Write("目标对象");
                                writer.Write((int)info->target);
                                writer.Write("HandleID: " + info->target.ToString());
                                writer.Write(GetTypeName(type));
                                return 2;
                            }
                        }
                        return 1;
                    }
                case TypeCode.Coroutine:
                    {
                        var id = *(ulong*)address;
                        writer.Write("协程 id");
                        writer.Write(0);
                        writer.Write("");
                        writer.Write(((int)id).ToString());
                        return 1;
                    }
            }
            return 0;
        }
        private void WriteVariable(ref BufferWriter writer, Type type, byte* address)
        {
            if (type.dimension > 0 || type.definition.code == TypeCode.Handle || type.definition.code == TypeCode.Interface || type.definition.code == TypeCode.Function || type.definition.code == TypeCode.Coroutine)
            {
                writer.Write(*(int*)address);
                writer.Write("HandleID: " + ((uint*)address)->ToString());
                var result = kernel.heapAgency.TryGetType(*(uint*)address, out type);
                if (result == ExitCode.None)
                {
                    if (kernel.heapAgency.TryGetArrayLength(*(uint*)address, out var length) == ExitCode.None)
                    {
                        var postfix = "";
                        for (int i = 1; i < type.dimension; i++) postfix += "[]";
                        writer.Write("{0}[{1}]{2}".Format(GetDefinitionName(type.definition), length, postfix));
                    }
                    else writer.Write(GetTypeName(type));
                }
                else writer.Write(result.ToString());
            }
            else
            {
                writer.Write(0);
                writer.Write(GetDefinitionName(type.definition));
                if (type == KERNEL_TYPE.BOOL) writer.Write(((bool*)address)->ToString());
                else if (type == KERNEL_TYPE.INTEGER) writer.Write(((long*)address)->ToString());
                else if (type == KERNEL_TYPE.REAL) writer.Write(((real*)address)->ToString());
                else if (type == KERNEL_TYPE.REAL2) writer.Write(((Real2*)address)->ToString());
                else if (type == KERNEL_TYPE.REAL3) writer.Write(((Real3*)address)->ToString());
                else if (type == KERNEL_TYPE.REAL4) writer.Write(((Real4*)address)->ToString());
                else if (type == KERNEL_TYPE.STRING) writer.Write(kernel.stringAgency.Get(*(uint*)address));
                else if (type == KERNEL_TYPE.ENTITY) writer.Write(((Entity*)address)->ToString());
                else writer.Write("未知");
            }
        }
        private string GetTypeName(Type type)
        {
            var postfix = "";
            for (int i = 0; i < type.dimension; i++) postfix += "[]";
            return GetDefinitionName(type.definition) + postfix;
        }
        private string GetDefinitionName(TypeDefinition definition)
        {
            if (definition.library == LIBRARY.KERNEL)
            {
                switch (definition.code)
                {
                    case TypeCode.Bool: return KeyWorld.BOOL;
                    case TypeCode.Integer: return KeyWorld.INTEGER;
                    case TypeCode.Real: return KeyWorld.REAL;
                    case TypeCode.Real2: return KeyWorld.REAL2;
                    case TypeCode.Real3: return KeyWorld.REAL3;
                    case TypeCode.Real4: return KeyWorld.REAL4;
                    case TypeCode.String: return KeyWorld.STRING;
                    case TypeCode.Handle: return KeyWorld.HANDLE;
                    case TypeCode.Interface: return KeyWorld.INTERFACE;
                    case TypeCode.Function: return KeyWorld.FUNCTION;
                    case TypeCode.Coroutine: return KeyWorld.COROUTINE;
                    case TypeCode.Entity: return KeyWorld.ENTITY;
                }
            }
            else if (definition.library == library.index)
            {
                switch (definition.code)
                {
                    case TypeCode.Handle: return debug.definitions[(int)definition.index];
                    case TypeCode.Function: return debug.functions[(int)definition.index];
                }
            }
            return definition.ToString();
        }
        private void Send(BufferWriter writer)
        {
            socket.SendTo(sendBuffer, writer.Size, SocketFlags.None, remote);
        }

        internal void OnException(StackFrame[] stacks, long code)
        {
            if (code == exceptionFilter || exceptionFilter == 0)
            {
                var writer = GetWriter(RainSocketHead.message);
                writer.Write((int)SENDInstruction.Exception);
                writer.Write((int)kernel.coroutineAgency.invoking.instanceID);
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
                _continue = false;
                while (!_continue && !_disposed) Thread.Sleep(10);
            }
        }
        internal void OnHit()
        {
            var writer = GetWriter(RainSocketHead.message);
            writer.Write((int)SENDInstruction.HitBreakpoint);
            writer.Write((int)kernel.coroutineAgency.invoking.instanceID);
            writer.Write("命中断点");
            Send(writer);
            _continue = false;
            while (!_continue && !_disposed) Thread.Sleep(10);
        }

        private void ClearBreakpoint()
        {
            var la = kernel.libraryAgency;
            foreach (var item in breakpoints)
            {
                var point = la[item.Value.library].code + item.Value.point;
                if (*point == (byte)CommandMacro.BREAKPOINT) *(bool*)(point + 1) = false;
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
