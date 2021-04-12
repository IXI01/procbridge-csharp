using System.Json;

namespace ProcBridge_CSharp
{
    public interface IDelegate
    {
        /// <summary>
        /// An interface that defines how server handles requests.
        /// </summary>
        /// <param name="method">method the requested method</param>
        /// <param name="payload">payload the requested payload, must be a JSON value</param>
        /// <returns>the result, must be a JSON value</returns>
        JsonValue HandleRequest(string method, JsonValue payload);
    }
}