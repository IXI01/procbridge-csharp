using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Json;
using static ProcBridge_CSharp.Test.TestServer;

namespace ProcBridge_CSharp.Test
{
    public class ServerClientTest
    {
        private static Server _server;
        private Client _client;

        [OneTimeSetUp]
        public void SetUpClass()
        {
            try {
                _server = new TestServer();
                _server.Start();
            }
            catch (ServerException) {
                Console.WriteLine($"use existing server on port {PORT}");
                _server = null;
            }
        }

        [OneTimeTearDown]
        public static void TearDownClass()
        {
            if (_server != null) {
                _server.Stop();
                _server = null;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _client = new Client("127.0.0.1", PORT);
        }

        [TearDown]
        public void TearDown()
        {
            _client = null;
        }

        [Test]
        public void TestNone()
        {
            object reply = _client.Request(null, null);
            Assert.IsNull(reply);
            reply = _client.Request("Echo", null);
            Assert.IsNull(reply);
            reply = _client.Request(null, "Hello");
            Assert.IsNull(reply);
        }

        [Test]
        public void TestEcho()
        {
            JsonValue reply = _client.Request("Echo", 123);
            Assert.AreEqual(123, (int)reply);
            reply = _client.Request("Echo", 3.14);
            Assert.AreEqual(3.14, (double)reply);
            reply = _client.Request("Echo", "hello");
            Assert.AreEqual("hello", (string)reply);
            reply = _client.Request("Echo", new JsonArray {1, 2, 3});
            Assert.NotNull(reply);
            List<int> replyList = new List<int>();
            foreach (JsonValue item in reply) {
                replyList.Add(item);
            }
            Assert.AreEqual(new List<int> {1, 2, 3}, replyList);
            JsonObject obj = new JsonObject {{"key", "value"}};
            reply = _client.Request("Echo", obj);
            Assert.NotNull(reply);
            ((JsonObject) reply).TryGetValue("key", out JsonValue key);
            string keyString = key == null ? "" : (string)key;

            Assert.AreEqual("value", keyString);
        }

        [Test]
        public void TestSum()
        {
            JsonValue reply = _client.Request("Sum", new JsonArray {1, 2, 3, 4});
            Assert.AreEqual(10, (int)reply);
        }


        [Test]
        public void TestError()
        {
            ServerException exception = null;
            try {
                _client.Request("Err", null);
            }
            catch (ServerException ex) {
                exception = ex;
            }

            Assert.NotNull(exception);
            Assert.AreEqual("generated error", exception.Message);
        }

        [Test]
        public void TestBigPayload()
        {
            string text =
                "Lorem ipsum dolor sit amet, ligula suspendisse nulla pretium, rhoncus tempor fermentum, enim integer ad vestibulum volutpat. Nisl rhoncus turpis est, vel elit, congue wisi enim nunc ultricies sit, magna tincidunt. Maecenas aliquam maecenas ligula nostra, accumsan taciti. Sociis mauris in integer, a dolor netus non dui aliquet, sagittis felis sodales, dolor sociis mauris, vel eu libero cras. Faucibus at. Arcu habitasse elementum est, ipsum purus pede porttitor class, ut adipiscing, aliquet sed auctor, imperdiet arcu per diam dapibus libero duis. Enim eros in vel, volutpat nec pellentesque leo, temporibus scelerisque nec.\r\nAc dolor ac adipiscing amet bibendum nullam, lacus molestie ut libero nec, diam et, pharetra sodales, feugiat ullamcorper id tempor id vitae. Mauris pretium aliquet, lectus tincidunt. Porttitor mollis imperdiet libero senectus pulvinar. Etiam molestie mauris ligula laoreet, vehicula eleifend. Repellat orci erat et, sem cum, ultricies sollicitudin amet eleifend dolor nullam erat, malesuada est leo ac. Varius natoque turpis elementum est. Duis montes, tellus lobortis lacus amet arcu et. In vitae vel, wisi at, id praesent bibendum libero faucibus porta egestas, quisque praesent ipsum fermentum tempor. Curabitur auctor, erat mollis sed, turpis vivamus a dictumst congue magnis. Aliquam amet ullamcorper dignissim molestie, mollis. Tortor vitae tortor eros wisi facilisis.\r\nConsectetuer arcu ipsum ornare pellentesque vehicula, in vehicula diam, ornare magna erat felis wisi a risus. Justo fermentum id. Malesuada eleifend, tortor molestie, a a vel et. Mauris at suspendisse, neque aliquam faucibus adipiscing, vivamus in. Wisi mattis leo suscipit nec amet, nisl fermentum tempor ac a, augue in eleifend in venenatis, cras sit id in vestibulum felis in, sed ligula. In sodales suspendisse mauris quam etiam erat, quia tellus convallis eros rhoncus diam orci, porta lectus esse adipiscing posuere et, nisl arcu vitae laoreet. Morbi integer molestie, amet suspendisse morbi, amet maecenas, a maecenas mauris neque proin nisl mollis. Suscipit nec ligula ipsum orci nulla, in posuere ut quis ultrices, lectus primis vehicula velit hasellus lectus, vestibulum orci laoreet inceptos vitae, at consectetuer amet et consectetuer. Congue porta scelerisque praesent at, lacus vestibulum et at dignissim cras urna, ante convallis turpis duis lectus sed aliquet, at et ultricies. Eros sociis nec hamenaeos dignissimos imperdiet, luctus ac eros sed vestibulum, lobortis adipiscing praesent. Nec eros eu ridiculus libero felis.\r\nDonec arcu risus diam amet sit. Congue tortor risus vestibulum commodo nisl, luctus augue amet quis aenean maecenas sit, donec velit iusto, morbi felis elit et nibh. Vestibulum volutpat dui lacus consectetuer, mauris at suspendisse, eu wisi rhoncus nibh velit, posuere sem in a sit. Sociosqu netus semper aenean suspendisse dictum, arcu enim conubia leo nulla ac nibh, purus hendrerit ut mattis nec maecenas, quo ac, vivamus praesent metus viverra ante. Natoque sed sit hendrerit, dapibus velit molestiae leo a, ut lorem sit et lacus aliquam. Sodales nulla ante auctor excepturi wisi, dolor lacinia dignissim eros condimentum dis pellentesque, sodales lacus nunc, feugiat at. In orci ligula suscipit luctus, sed dolor eleifend aliquam dui, ut diam mauris, sollicitudin sed nisl lacus.";
            JsonValue reply = _client.Request("Echo", text);
            Assert.AreEqual(text, (string)reply);
        }
    }
}