using System;

namespace ProcBridge_CSharp
{
    public class ClientException : Exception
    {
        public ClientException(Exception cause)
            : base(cause.Message, cause)
        {
        }
    }
}