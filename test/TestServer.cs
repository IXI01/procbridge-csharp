using System.Json;
using System;
using System.Linq;

namespace ProcBridge_CSharp.Test
{
    public class TestServer : Server
    {
        public const int PORT = 8000;

        public TestServer() :
            base(PORT, new TestServerDelegate())
        {
        }

        public class TestServerDelegate : Delegate
        {
            [Handler]
            public object Echo(object payload)
            {
                return payload;
            }

            [Handler]
            public int Sum(JsonArray numbers)
            {
                return numbers.Select(x => (int) x).Sum();
            }

            [Handler]
            public void Err()
            {
                throw new Exception("generated error");
            }

            protected override JsonValue HandleUnknownRequest(string method, JsonValue payload)
            {
                return null;
            }
        }
    }
}