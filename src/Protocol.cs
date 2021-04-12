using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Json;
using System.Net.Sockets;

namespace ProcBridge_CSharp
{
    public class Protocol
    {
        private static readonly byte[] FLAG = {(byte) 'p', (byte) 'b'};

        private static KeyValuePair<StatusCode, JsonObject> Read(NetworkStream stream)
        {
            int b;

            // 1. FLAG
            b = stream.ReadByte();
            if (b == -1 || b != FLAG[0]) throw new ProtocolException(ProtocolException.UNRECOGNIZED_PROTOCOL);
            b = stream.ReadByte();
            if (b == -1 || b != FLAG[1]) throw new ProtocolException(ProtocolException.UNRECOGNIZED_PROTOCOL);

            // 2. VERSION
            b = stream.ReadByte();
            if (b == -1) throw new ProtocolException(ProtocolException.INCOMPLETE_DATA);
            if (b != Versions.CURRENT[0]) throw new ProtocolException(ProtocolException.INCOMPATIBLE_VERSION);
            b = stream.ReadByte();
            if (b == -1) throw new ProtocolException(ProtocolException.INCOMPLETE_DATA);
            if (b != Versions.CURRENT[1]) throw new ProtocolException(ProtocolException.INCOMPATIBLE_VERSION);

            // 3. STATUS CODE
            b = stream.ReadByte();
            if (b == -1) throw new ProtocolException(ProtocolException.INCOMPLETE_DATA);
            StatusCode statusCode = StatusCodeHelper.FromRawValue(b);
            if (statusCode == StatusCode.UNKNOWN) {
                throw new ProtocolException(ProtocolException.INVALID_STATUS_CODE);
            }

            // 4. RESERVED BYTES (2 bytes)
            b = stream.ReadByte();
            if (b == -1) throw new ProtocolException(ProtocolException.INCOMPLETE_DATA);
            b = stream.ReadByte();
            if (b == -1) throw new ProtocolException(ProtocolException.INCOMPLETE_DATA);

            // 5. LENGTH (little endian)
            int bodyLen;
            b = stream.ReadByte();
            if (b == -1) throw new ProtocolException(ProtocolException.INCOMPLETE_DATA);
            bodyLen = b;
            b = stream.ReadByte();
            if (b == -1) throw new ProtocolException(ProtocolException.INCOMPLETE_DATA);
            bodyLen |= (b << 8);
            b = stream.ReadByte();
            if (b == -1) throw new ProtocolException(ProtocolException.INCOMPLETE_DATA);
            bodyLen |= (b << 16);
            b = stream.ReadByte();
            if (b == -1) throw new ProtocolException(ProtocolException.INCOMPLETE_DATA);
            bodyLen |= (b << 24);

            // 6. JSON OBJECT
            MemoryStream buffer = new MemoryStream();
            int readCount;
            int restCount = bodyLen;
            byte[] buf = new byte[Math.Min(bodyLen, 1024 * 1024)];
            while ((readCount = stream.Read(buf, 0, Math.Min(buf.Length, restCount))) != -1) {
                buffer.Write(buf, 0, readCount);
                restCount -= readCount;
                if (restCount == 0) {
                    break;
                }
            }

            if (buffer.Length != bodyLen) {
                throw new ProtocolException(ProtocolException.INCOMPLETE_DATA);
            }

            buffer.Flush();
            buf = buffer.ToArray();

            try {
                string jsonText = Encoding.UTF8.GetString(buf);
                JsonObject body = (JsonObject) JsonValue.Parse(jsonText);
                return new KeyValuePair<StatusCode, JsonObject>(statusCode, body);
            }
            catch (Exception) {
                throw new ProtocolException(ProtocolException.INVALID_BODY);
            }
        }

        private static void Write(NetworkStream stream, StatusCode statusCode, JsonObject body)
        {
            // 1. FLAG 'p', 'b'
            stream.Write(FLAG);

            // 2. VERSION
            stream.Write(Versions.CURRENT);

            // 3. STATUS CODE
            stream.WriteByte(StatusCodeHelper.ToRawValue(statusCode));

            // 4. RESERVED BYTES (2 bytes)
            stream.WriteByte(0);
            stream.WriteByte(0);

            // make json object
            byte[] buf = Encoding.UTF8.GetBytes(body.ToString());

            // 5. LENGTH (4-byte, little endian)
            int len = buf.Length;
            int b0 = len & 0xff;
            int b1 = (len & 0xff00) >> 8;
            int b2 = (len & 0xff0000) >> 16;
            long b3 = (len & 0xff000000) >> 24;
            stream.WriteByte((byte) b0);
            stream.WriteByte((byte) b1);
            stream.WriteByte((byte) b2);
            stream.WriteByte((byte) b3);

            // 6. JSON OBJECT
            stream.Write(buf);

            stream.Flush();
        }

        public static KeyValuePair<string, JsonValue> ReadRequest(NetworkStream stream)
        {
            KeyValuePair<StatusCode, JsonObject> entry = Read(stream);
            StatusCode statusCode = entry.Key;
            JsonObject body = entry.Value;
            if (statusCode != StatusCode.REQUEST) {
                throw new ProtocolException(ProtocolException.INVALID_STATUS_CODE);
            }

            body.TryGetValue(Keys.METHOD, out JsonValue method);
            string methodString = method == null ? "" : (string)method;
            body.TryGetValue(Keys.PAYLOAD, out JsonValue payload);
            return new KeyValuePair<string, JsonValue>(methodString, payload);
        }

        public static KeyValuePair<StatusCode, JsonValue> ReadResponse(NetworkStream stream)
        {
            KeyValuePair<StatusCode, JsonObject> entry = Read(stream);
            StatusCode statusCode = entry.Key;
            JsonObject body = entry.Value;
            if (statusCode == StatusCode.GOOD_RESPONSE) {
                body.TryGetValue(Keys.PAYLOAD, out JsonValue payload);
                return new KeyValuePair<StatusCode, JsonValue>(StatusCode.GOOD_RESPONSE, payload);
            }
            else if (statusCode == StatusCode.BAD_RESPONSE) {
                body.TryGetValue(Keys.MESSAGE, out JsonValue method);
                string messageString = method == null ? "" : (string)method;
                return new KeyValuePair<StatusCode, JsonValue>(StatusCode.BAD_RESPONSE, messageString);
            }
            else {
                throw new ProtocolException(ProtocolException.INVALID_STATUS_CODE);
            }
        }

        public static void WriteRequest(NetworkStream stream, string method, JsonValue payload)
        {
            JsonObject body = new JsonObject();
            if (method != null) {
                body.Add(Keys.METHOD, method);
            }

            if (payload != null) {
                body.Add(Keys.PAYLOAD, payload);
            }

            Write(stream, StatusCode.REQUEST, body);
        }

        public static void WriteGoodResponse(NetworkStream stream, JsonValue payload)
        {
            JsonObject body = new JsonObject();
            if (payload != null) {
                body.Add(Keys.PAYLOAD, payload);
            }

            Write(stream, StatusCode.GOOD_RESPONSE, body);
        }

        public static void WriteBadResponse(NetworkStream stream, string message)
        {
            JsonObject body = new JsonObject();
            if (message != null) {
                body.Add(Keys.MESSAGE, message);
            }

            Write(stream, StatusCode.BAD_RESPONSE, body);
        }

        private Protocol()
        {
        }
    }
}