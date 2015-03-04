using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace TCPServer
{
    class ServerState
    {
        private int maxConnections;
        private int currentFactor;
        private List<TcpClient> clientList;
        // private ReaderWriterLock rwLockClientList = new ReaderWriterLock();

        private ServerState(int maxConnections, int currentFactor)
        {
            this.maxConnections = maxConnections;
            this.currentFactor = currentFactor;
            this.clientList = new List<TcpClient>();
        }

        public static ServerState GetServerState(int maxConnections, int currentFactor)
        {
            return new ServerState(maxConnections, currentFactor);
        }

        public int GetCurrentFactor()
        {
            return this.currentFactor;
        }

        public void SetCurrentFactor(int factor)
        {
            this.currentFactor = factor;
        }

        public List<TcpClient> GetClientList()
        {
            return this.clientList;
        }

        public bool AddClient(TcpClient client)
        {
            int count = this.clientList.Count;
            if (this.maxConnections <= count)
            {
                return false;
            }
            this.clientList.Add(client);
            return true;
        }

        public void removeClient(TcpClient client)
        {
            this.clientList.Remove(client);
        }
    }

    class Server
    {
        delegate void TalkThreadDelegate(TcpClient client);

        private ServerState serverState;
        
        private Server(int maxConnections, int currentFactor)
        {
            this.serverState = ServerState.GetServerState(maxConnections, currentFactor);
        }

        public static Server GetServer(int maxConnections, int currentFactor)
        {
            return new Server(maxConnections, currentFactor);
        }

        private ServerState getServerState()
        {
            return this.serverState;
        }

        public void Accept(TcpListener listener)
        {
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("client connected");

                var talkThreadDelegate = new TalkThreadDelegate(this.talk);
                talkThreadDelegate.BeginInvoke(client, new AsyncCallback(this.onTalkThreadFinished), client); 

                Thread.Sleep(100);
            }
        }

        private void onTalkThreadFinished(IAsyncResult ar)
        {
            TcpClient client = (TcpClient) ar.AsyncState;
            client.Close();
            Console.WriteLine("talk thread is finished");
        }

        private void talk(TcpClient client)
        {
            ServerState serverState = this.getServerState();
            if (client.Connected)
            {
                serverState.AddClient(client);
                NetworkStream netStream = client.GetStream();
                StreamReader sReader = new StreamReader(netStream, Encoding.UTF8);

                string str = String.Empty;

                do
                {
                    str = sReader.ReadLine();
                    if (str == null)
                    {
                        break;
                    }
                    Console.WriteLine(str);

                    int num;
                    byte[] sendBytes;
                    bool isInt = int.TryParse(str, out num);
                    if (isInt)
                    {
                        int sendNum = num * serverState.GetCurrentFactor();
                        sendBytes = Encoding.UTF8.GetBytes(sendNum.ToString() + "\n");
                    } else
                    {
                        string msg = "you should enter numbers\n";
                        sendBytes = Encoding.UTF8.GetBytes(msg.ToString());
                    }
                    netStream.Write(sendBytes, 0, sendBytes.Length);
                } while (!str.Equals("quit"));

                serverState.removeClient(client);
                sReader.Close();
            }
        }

    }

    class EntryPoint
    {
        delegate void AcceptThreadDelegate(TcpListener listener);

        public static void Main(string[] args)
        {
            IPEndPoint ipAdd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            TcpListener listener = new TcpListener(ipAdd);
            listener.Start(0);
            Console.WriteLine("start listening at port 8888");

            Server server = Server.GetServer(2, 2);
            var acceptThreadDelegate = new AcceptThreadDelegate(server.Accept);
            acceptThreadDelegate.BeginInvoke(listener, null, null);

            Console.WriteLine("press a key to finish");
            Console.ReadLine();
            listener.Stop();
        }
    }
}
