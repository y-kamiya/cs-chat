using System;
using System.Text;
using System.Tuple;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace TCPServer
{
    public class Message
    {
        public enum Type { Command, Notice, Tell, Broadcast, Quit, Unknown };

        private Type messageType;
        private string from;
        // private string to;
        private string body;

        private Message(Type type, string body, string from)
        {
            this.messageType = type;
            this.from = from;
            this.body = body;
        }

        public static Message Create(string body, string from)
        {
            return new Message(Type.Command, body, from);
        }

        public Type GetMessageType()
        {
            return this.messageType;
        }

        public string GetFrom()
        {
            return this.from;
        }

        public string GetBody()
        {
            return this.body;
        }

        public Message Parse()
        {
            string body = this.body;
            if (body[0] != '/') 
            {
                return new Message(Type.Broadcast, body, this.from);
            }

            string[] words = body.Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1)
            {
                string command = words[0].Substring(1);
                if (command == "quit")
                {
                    return new Message(Type.Quit, body, this.from);
                }
                return new Message(Type.Unknown, body, this.from);
            }
            return new Message(Type.Unknown, body, this.from);
        }
    }

    class ClientState
    {
        private string name;
        private TcpClient client;
        private Queue<Message> chan;

        private ClientState(string name, TcpClient client)
        {
            this.name = name;
            this.client = client;
            this.chan = new Queue<Message>();
        }

        public static ClientState Create(string name, TcpClient client)
        {
            return new ClientState(name, client);
        }

        public string GetName()
        {
            return this.name;
        }

        public TcpClient GetClient()
        {
            return this.client;
        }

        public void WriteChan(Message message)
        {
            Queue<Message> chan = this.chan;
            lock (((ICollection)chan).SyncRoot)
            {
                chan.Enqueue(message);
                Monitor.Pulse(chan);
            }
        }

        public Message ReadChan()
        {
            Queue<Message> chan = this.chan;
            lock (((ICollection)chan).SyncRoot)
            {
                if (chan.Count == 0)
                {
                    Monitor.Wait(chan);
                }
                return chan.Dequeue();
            }
        }
    }

    class ServerState
    {
        private Dictionary<string, ClientState> clientMap;
        // private ReaderWriterLock rwLockClientList = new ReaderWriterLock();

        private ServerState()
        {
            this.clientMap = new Dictionary<string, ClientState>();
        }

        public static ServerState Create()
        {
            return new ServerState();
        }

        public Dictionary<string, ClientState> GetClientMap()
        {
            return this.clientMap;
        }

        public bool AddClient(string name, ClientState clientState)
        {
            bool isContained = this.GetClientMap().ContainsKey(name);
            if (isContained)
            {
                return false;
            }
            this.GetClientMap().Add(name, clientState);
            return true;
        }

        public void RemoveClient(string name)
        {
            this.clientMap.Remove(name);
        }
    }

    class Server
    {
        delegate void TalkThreadDelegate(TcpClient client);

        private ServerState serverState;
        
        private Server()
        {
            this.serverState = ServerState.Create();
        }

        public static Server GetServer()
        {
            return new Server();
        }

        private ServerState getServerState()
        {
            return this.serverState;
        }

        public void Accept(object data)
        {
            TcpListener listener = (TcpListener)data;
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("client connected");

                Thread thread = new Thread(new ParameterizedThreadStart(this.talk));
                thread.IsBackground = true;
                thread.Start(client);
                // var talkThreadDelegate = new TalkThreadDelegate(this.talk);
                // talkThreadDelegate.BeginInvoke(client, new AsyncCallback(this.onTalkThreadFinished), client); 

                Thread.Sleep(100);
            }
        }

        /*
        private void onTalkThreadFinished(IAsyncResult ar)
        {
            TcpClient client = (TcpClient) ar.AsyncState;
            client.Close();
            Console.WriteLine("talk is finished");
        }
        */

        private ClientState addClient(ServerState serverState, TcpClient client)
        {
            while (true) 
            {
                this.response("input your name!\n", client.GetStream());
                Console.WriteLine("[debug] input your name!");
                StreamReader sReader = new StreamReader(client.GetStream(), Encoding.UTF8);
                string name = sReader.ReadLine();
                ClientState clientState = ClientState.Create(name, client);
                bool ok = serverState.AddClient(name, clientState);
                if (ok)
                {
                    string msg = "hi, " + name + "\n";
                    Console.WriteLine("[debug] " + msg);
                    this.response(msg, client.GetStream());
                    return clientState;
                }
                else 
                {
                    string msg = "can not use this name, choose another one\n";
                    this.response(msg, client.GetStream());
                }
            }
        }

        private void talk(object data)
        {
            TcpClient client = (TcpClient)data;
            ServerState serverState = this.getServerState();
            if (client.Connected)
            {
                ClientState clientState = this.addClient(serverState, client);

                NetworkStream netStream = client.GetStream();
                StreamReader sReader = new StreamReader(netStream, Encoding.UTF8);

                // make server thread and receive thread
                ThreadStart receive = () => 
                {
                    try
                    {
                        while (true)
                        {
                            string str = sReader.ReadLine();
                            Console.WriteLine("*" + clientState.GetName() + "* " + str);
                            if (str.Length == 0) continue;
                            clientState.WriteChan(Message.Create(str, clientState.GetName()));
                        }
                    }
                    catch (Exception) {
                        Console.WriteLine("receive thread is aborted");
                    }
                };

                ThreadStart server = () =>
                {
                    try
                    {
                        while (true)
                        {
                            Message message = clientState.ReadChan();
                            // Console.WriteLine("[debug] <messge> type: " + message.GetMessageType() + ", body: " + message.GetBody());
                            bool isExit = handleMessage(serverState, clientState, message);
                            if (isExit) {
                                break;
                            }
                        }
                    }
                    catch (ThreadAbortException) {
                        Console.WriteLine("server thread is aborted");
                    }
                };

                Thread receiveThread = new Thread(receive);
                Thread serverThread = new Thread(server);
                receiveThread.Start();
                serverThread.Start();

                serverThread.Join();

                serverState.RemoveClient(clientState.GetName());
                sReader.Close();
                client.Close();
                Console.WriteLine("talk is finished");
            }
        }

        /*
        private void tell(ServerState serverState, ClientState clientState, Message message)
        {
            Dictionary<string, ClientState> dict = serverState.GetClientMap();
            dict[message.to].WriteChan(message);
        }
        */

        private void broadcast(ServerState serverState, ClientState clientState, Message message)
        {
            Dictionary<string, ClientState> dict = serverState.GetClientMap();
            foreach (ClientState cState in dict.Values)
            {
                cState.WriteChan(message);
            }
        }

        private bool handleMessage(ServerState serverState, ClientState clientState, Message message)
        {
            if (message.GetMessageType() == Message.Type.Command)
            {
                Message parsedMessage = message.Parse();
                // Console.WriteLine("[debug]<parsedMessage> type: " + parsedMessage.GetMessageType() + ", from: " + parsedMessage.GetFrom() + ", body: " + parsedMessage.GetBody());
                if (parsedMessage.GetMessageType() == Message.Type.Tell)
                {
                    // this.tell(serverState, clientState, parsedMessage);
                    return false;
                }
                else if (parsedMessage.GetMessageType() == Message.Type.Quit)
                {
                    string msg = "FINISHED";
                    this.responseToClient(clientState, msg);
                    return true;
                }
                else if (parsedMessage.GetMessageType() == Message.Type.Unknown)
                {
                    string msg = "unknown command: " + parsedMessage.GetBody();
                    return this.responseToClient(clientState, msg);
                }
                else
                {
                    this.broadcast(serverState, clientState, parsedMessage);
                    return false;
                }
            }
            else if (message.GetMessageType() == Message.Type.Notice)
            {
                string msg = "*** " + message.GetBody();
                return this.responseToClient(clientState, msg);
            }
            else if (message.GetMessageType() == Message.Type.Tell)
            {
                string msg = "*" + message.GetFrom() + "*: " + message.GetBody();
                return this.responseToClient(clientState, msg);
            }
            else if (message.GetMessageType() == Message.Type.Broadcast)
            {
                string msg = "<" + message.GetFrom() + ">: " + message.GetBody();
                return this.responseToClient(clientState, msg);
            }
            else
            {
                string msg = "unknown message type: " + message.GetMessageType();
                return this.responseToClient(clientState, msg);
            }
        }

        private bool responseToClient(ClientState clientState, string msg)
        {
            NetworkStream ns = clientState.GetClient().GetStream();
            return this.response(msg + "\n", ns);
        }

        private bool response(string msg, NetworkStream netStream)
        {
            try 
            {
                byte[] sendBytes = Encoding.UTF8.GetBytes(msg);
                netStream.Write(sendBytes, 0, sendBytes.Length);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("error in response: " + e);
                return true;
            }
        }

    }

    class EntryPoint
    {
        public static void Main(string[] args)
        {
            IPEndPoint ipAdd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            TcpListener listener = new TcpListener(ipAdd);
            listener.Start(0);
            Console.WriteLine("start listening at port 8888");

            Server server = Server.GetServer();
            Thread acceptThread = new Thread(new ParameterizedThreadStart(server.Accept));
            acceptThread.IsBackground = true;
            acceptThread.Start(listener);

            Console.WriteLine("press a key to finish");
            Console.ReadLine();
            listener.Stop();
        }
    }
}
