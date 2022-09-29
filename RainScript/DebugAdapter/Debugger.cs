using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using RainScript.VirtualMachine;

namespace RainScript.DebugAdapter
{
    /// <summary>
    /// 调试器
    /// </summary>
    public unsafe class Debugger : IDisposable
    {
        private bool disposed;
        private Socket socket;
        private Adapter adapter;
        private readonly byte[] sendBuffer = new byte[65536], recvBuffer = new byte[65536];
        /// <summary>
        /// 调试名称
        /// </summary>
        public readonly string name;
        internal readonly Kernel kernel;
        internal readonly Func<string, DebugTable> debugTableLoader;
        internal readonly Func<string, SymbolTable> symbolTableLoader;
        /// <summary>
        /// 调试器
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="kernel">核心</param>
        /// <param name="debugTableLoader">调试表加载器</param>
        /// <param name="symbolTableLoader">符号表加载器</param>
        public Debugger(string name, Kernel kernel, Func<string, DebugTable> debugTableLoader, Func<string, SymbolTable> symbolTableLoader)
        {
            disposed = false;
            adapter = null;
            this.name = name;
            this.kernel = kernel;
            this.debugTableLoader = debugTableLoader;
            this.symbolTableLoader = symbolTableLoader;
            kernel.OnHitBreakpoint += OnHitBreakpoint;
            kernel.OnExit += OnExit;
            new Thread(Accept).Start();
        }

        private void OnExit(StackFrame[] stacks, long code)
        {
            adapter?.OnException(stacks, code);
        }

        private void Broadcast(int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                var ip = new IPEndPoint(IPAddress.Broadcast, 38465);
                var portBytes = BitConverter.GetBytes(port);
                var nameBytes = Encoding.UTF8.GetBytes(name);
                var buffer = new byte[portBytes.Length + nameBytes.Length];
                Array.Copy(portBytes, buffer, portBytes.Length);
                Array.Copy(nameBytes, 0, buffer, portBytes.Length, nameBytes.Length);
                while (!disposed && kernel)
                {
                    if (adapter == null) socket.SendTo(buffer, buffer.Length, SocketFlags.None, ip);
                    Thread.Sleep(1000);
                }
            }
            finally
            {
                socket.Close();
                Dispose();
            }
        }
        private void Accept()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                var port = 14576;
            Rebuild:
                var ip = new IPEndPoint(IPAddress.Any, port);
                try
                {
                    socket.Bind(new IPEndPoint(IPAddress.Any, port));
                }
                catch (Exception)
                {
                    port++;
                    goto Rebuild;
                }
                new Thread(() => Broadcast(port)).Start();
                while (!disposed)
                {
                    EndPoint remote = ip;
                    adapter = null;
                    var size = socket.ReceiveFrom(recvBuffer, ref remote);
                    var reader = new BufferReader(recvBuffer);
                    if (reader.ReadInt8() == (int)RainSocketHead.hookup)
                    {
                        try
                        {
                            var libraryName = reader.ReadString();
                            var debug = debugTableLoader(libraryName);
                            var symbol = symbolTableLoader?.Invoke(libraryName);
                            if (debug != null)
                            {
                                adapter = new Adapter(kernel, debug, symbol, (IPEndPoint)remote, sendBuffer, recvBuffer, (int)DateTime.Now.Ticks, reader);
                                adapter.Recv();
                                adapter.Dispose();
                            }
                            else Console.WriteLine(string.Format("程序集调试表 {0} 加载失败", libraryName));
                        }
                        catch (Exception e) { Console.WriteLine(e); }
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e); }
            finally { Dispose(); }
        }
        private void OnHitBreakpoint()
        {
            adapter?.OnHit();
        }
        /// <summary>
        /// 释放调试器
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            socket.Close();
            adapter?.Dispose();
        }
        /// <summary>
        /// 析构
        /// </summary>
        ~Debugger()
        {
            Dispose();
        }
    }
}
