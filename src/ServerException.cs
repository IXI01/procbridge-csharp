using System;

namespace ProcBridge_CSharp
{
    public class ServerException : Exception
    {
        private const string UNKNOWN_SERVER_ERROR = "unknown server error";

        public ServerException(string message)
            : base(message ?? UNKNOWN_SERVER_ERROR)
        {
        }

        public ServerException(Exception cause)
            : base(cause.Message, cause)
        {
        }
    }
}