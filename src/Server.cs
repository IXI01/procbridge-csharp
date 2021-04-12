using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProcBridge_CSharp
{
    public class Server
    {
        private readonly int _port;
        private readonly IDelegate _d;

        private Socket _serverSocket;
        private bool _started;

        private TextWriter _logger;

        public Server(int port, IDelegate d)
        {
            _port = port;
            _d = d;

            _started = false;
            _serverSocket = null;
            _logger = Console.Error;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool IsStarted()
        {
            return _started;
        }

        public int GetPort()
        {
            return _port;
        }

        public TextWriter GetLogger()
        {
            return _logger;
        }

        public void SetLogger(TextWriter logger)
        {
            _logger = logger;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start()
        {
            if (_started) {
                throw new InvalidOperationException("server already started");
            }

            Socket serverSocket;
            try {
                serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port));
                serverSocket.Listen(50);
            }
            catch (SocketException e) {
                throw new ServerException(e);
            }

            _serverSocket = serverSocket;

            ThreadPool.QueueUserWorkItem(delegate
            {
                while (true) {
                    try {
                        Socket socket = serverSocket.Accept();
                        Connection conn = new Connection(socket, _d, _logger);
                        lock (this) {
                            if (!_started) {
                                return; // finish listener
                            }

                            ThreadPool.QueueUserWorkItem(conn.Run);
                        }
                    }
                    catch (SocketException) {
                        return; // finish listener
                    }
                }
            });

            _started = true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            if (!_started) {
                throw new InvalidOperationException("server does not started");
            }

            try {
                _serverSocket.Close();
            }
            catch (SocketException) {
            }

            _serverSocket = null;

            _started = false;
        }

        class Connection
        {
            private readonly Socket _socket;
            private readonly IDelegate _d;
            private readonly TextWriter _logger;

            public Connection(Socket socket, IDelegate d, TextWriter logger)
            {
                _socket = socket;
                _d = d;
                _logger = logger;
            }

            public void Run(object state)
            {
                try {
                    using NetworkStream outstream = new NetworkStream(_socket);
                    using NetworkStream instream = new NetworkStream(_socket);

                    KeyValuePair<string, JsonValue> req = Protocol.ReadRequest(instream);
                    string method = req.Key;
                    JsonValue payload = req.Value;

                    JsonValue result = null;
                    Exception exception = null;
                    try {
                        result = _d.HandleRequest(method, payload);
                    }
                    catch (Exception ex) {
                        exception = ex;
                    }

                    if (exception != null) {
                        Protocol.WriteBadResponse(outstream, exception.Message);
                    }
                    else {
                        Protocol.WriteGoodResponse(outstream, result);
                    }
                }
                catch (Exception ex) {
                    _logger?.WriteLine(ex.ToString());
                }
            }
        }
    }
}