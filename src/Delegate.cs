using System;
using System.Collections.Generic;
using System.Reflection;
using System.Json;

namespace ProcBridge_CSharp
{
    public abstract class Delegate : IDelegate
    {
        private readonly IDictionary<string, MethodInfo> _handlers;

        protected Delegate()
        {
            _handlers = new Dictionary<string, MethodInfo>();
            Type type = GetType();
            foreach (MethodInfo m in type.GetRuntimeMethods()) {
                if (m.GetCustomAttribute(typeof(Handler)) != null) {
                    string key = m.Name;
                    if (_handlers.ContainsKey(key)) {
                        throw new NotSupportedException("duplicate handler name: " + key);
                    }

                    _handlers.Add(key, m);
                }
            }
        }

        protected void WillHandleRequest(string method, JsonValue payload)
        {
        }

        protected virtual JsonValue HandleUnknownRequest(string method, JsonValue payload)
        {
            throw new ServerException("unknown method: " + method);
        }

        public JsonValue HandleRequest(string method, JsonValue payload)
        {
            WillHandleRequest(method, payload);

            if (!_handlers.ContainsKey(method)) {
                return HandleUnknownRequest(method, payload);
            }

            MethodInfo m = _handlers[method];

            object result;
            try {
                int pcnt = m.GetParameters().Length;
                if (pcnt == 0) {
                    result = m.Invoke(this, null);
                }
                else if (pcnt == 1) {
                    result = m.Invoke(this, new object[] {payload});
                }
                else {
                    // unpack
                    if (!(payload is JsonArray)) {
                        throw new ServerException("payload must be an array");
                    }

                    JsonArray arr = (JsonArray) payload;
                    if (arr.Count != pcnt) {
                        throw new ServerException($"method needs {pcnt} elements");
                    }

                    object[] parameters = new object[arr.Count];
                    int i = 0;
                    foreach (JsonValue value in arr) {
                        parameters[i] = value;
                        i++;
                    }
                    result = m.Invoke(this, parameters);
                }
            }
            catch (TargetInvocationException e) {
                throw new ServerException(e.InnerException);
            }

            return result switch
            {
                int intResult => intResult,
                double doubleResult => doubleResult,
                string stringResult => stringResult,
                _ => (JsonValue) result
            };
        }
    }
}