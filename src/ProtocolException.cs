using System;

namespace ProcBridge_CSharp
{
    public class ProtocolException : Exception
    {
        public const string UNRECOGNIZED_PROTOCOL = "unrecognized protocol";
        public const string INCOMPATIBLE_VERSION = "incompatible protocol version";
        public const string INCOMPLETE_DATA = "incomplete data";
        public const string INVALID_STATUS_CODE = "invalid status code";
        public const string INVALID_BODY = "invalid body";

        public ProtocolException(string message)
            : base(message)
        {
        }
    }
}