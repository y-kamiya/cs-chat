using NUnit.Framework;
using System.Net.Sockets;
using System;
using System.Collections.Generic;

using TCPServer;

namespace ChatTest
{
    [TestFixture]
    public class MessageTest
    {
        [TestCase("aaa", "bbb")]
        public void TestCreate(string body, string from)
        {
            Message message = Message.Create(body, from);
            Assert.AreEqual(body, message.GetBody());
            Assert.AreEqual(from, message.GetFrom());
            Assert.AreEqual(Message.Type.Command, message.GetMessageType());
        }

        [TestCase("aaa", Message.Type.Broadcast)]
        [TestCase("aaa bbb ccc", Message.Type.Broadcast)]
        [TestCase("/quit", Message.Type.Quit)]
        [TestCase("/aaa", Message.Type.Unknown)]
        [TestCase("quit aaa", Message.Type.Broadcast)]
        public void TestParse(string body, Message.Type expected)
        {
            Message message = Message.Create(body, "hoge");
            Assert.AreEqual(expected, message.Parse().GetMessageType());
        }
    }

    [TestFixture]
    public class ClientStateTest
    {
        [TestCase("aaa", "bbb")]
        public void TestReadWriteChan(string body, string from)
        {
            TcpClient client = new TcpClient();
            ClientState clientState = ClientState.Create("hoge", client);
            Message message = Message.Create("aaa", "bbb");
            clientState.WriteChan(message);
            Message msg = clientState.ReadChan();
            Assert.AreSame(message, msg);
            client.Close();
        }
    }

    [TestFixture]
    public class ServerStateTest
    {
        [TestCase()]
        public void TestAddClient()
        {
            TcpClient client = new TcpClient();
            ClientState clientState1 = ClientState.Create("name1", client);
            ClientState clientState2 = ClientState.Create("name2", client);
            ServerState serverState = ServerState.Create();

            bool isName1 = serverState.AddClient("name1", clientState1);
            bool isName2 = serverState.AddClient("name2", clientState2);
            Assert.IsTrue(isName1);
            Assert.IsTrue(isName2);

            isName1 = serverState.AddClient("name1", clientState1);
            Assert.IsFalse(isName1);

            Dictionary<string, ClientState> map = serverState.GetClientMap();
            Assert.IsTrue(map.ContainsKey("name1"));
            Assert.IsTrue(map.ContainsKey("name2"));
            
            serverState.RemoveClient("name1");
            Assert.IsFalse(map.ContainsKey("name1"));

            isName1 = serverState.AddClient("name1", clientState1);
            Assert.IsTrue(isName1);

            client.Close();
        }
    }

}
