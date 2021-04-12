using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Net.Sockets;
using System.Threading;

namespace ProcBridge_CSharp
{
    public class Client
    {
        private readonly string _host;
        private readonly int _port;
        private readonly long _timeout;
        private readonly IExecutor _executor;

        public static readonly long FOREVER = 0;

        public Client(string host, int port, long timeout, IExecutor executor)
        {
            _host = host;
            _port = port;
            _timeout = timeout;
            _executor = executor;
        }

        public Client(string host, int port)
            : this(host, port, FOREVER, null)
        {
        }

        public string GetHost()
        {
            return _host;
        }

        public int GetPort()
        {
            return _port;
        }

        public long GetTimeout()
        {
            return _timeout;
        }

        public IExecutor GetExecutor()
        {
            return _executor;
        }

        public JsonValue Request(string method, JsonValue payload)
        {
            StatusCode[] respStatusCode = {StatusCode.UNKNOWN};
            JsonValue[] respPayload = {null};
            Exception[] innerException = {null};

            try {
                using Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(_host, _port);
                ThreadStart task = delegate
                {
                    try {
                        using NetworkStream outstream = new NetworkStream(socket);
                        using NetworkStream instream = new NetworkStream(socket);
                        Protocol.WriteRequest(outstream, method, payload);
                        KeyValuePair<StatusCode, JsonValue> entry = Protocol.ReadResponse(instream);
                        respStatusCode[0] = entry.Key;
                        respPayload[0] = entry.Value;
                    }
                    catch (Exception ex) {
                        innerException[0] = ex;
                    }
                };

                if (_timeout <= 0) {
                    task.Invoke();
                }
                else {
                    TimeoutExecutor guard = new TimeoutExecutor(_timeout);
                    guard.Execute(task);
                }
            }
            catch (IOException ex) {
                throw new ClientException(ex);
            }

            if (innerException[0] != null) {
                throw new Exception(innerException[0].Message, innerException[0]);
            }

            if (respStatusCode[0] != StatusCode.GOOD_RESPONSE) {
                throw new ServerException(respPayload[0]);
            }

            return respPayload[0];
        }
    }
}