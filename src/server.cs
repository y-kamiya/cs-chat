using System;
using System.Text;
using System.Tuple;
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
                    if (str == null) break;
                    Console.WriteLine(str);

                    Tuple<int,int> result = checkInput(str);
                    if (result.Item1 == 1) {
                        this.respondAnswer(result.Item2, netStream);
                    } else if (result.Item1 == 2) {
                        int newFactor = result.Item2;
                        serverState.SetCurrentFactor(newFactor);
                        this.broadcastFactorChange(newFactor);
                    } else {
                        this.respondUsage(netStream);
                    }
                } while (!str.Equals("quit"));

                serverState.removeClient(client);
                sReader.Close();
            }
        }

        private Tuple<int,int> checkInput(string input)
        {
            int num;
            if (int.TryParse(input, out num)) 
            {
                return Tuple.Create<int,int>(1, num);
            }

            if (input.Length < 2) 
            {
                return Tuple.Create<int,int>(0, 0);
            }

            string str = input.Substring(1);
            if (input[0] == '*' && int.TryParse(str, out num)) 
            {
                return Tuple.Create<int,int>(2, num);
            }

            return Tuple.Create<int,int>(0, 0);
        }

        private void broadcastFactorChange(int newFactor)
        {
            string msg = "new factor is ";
            byte[] sendBytes = Encoding.UTF8.GetBytes(msg + newFactor.ToString() + "\n");
            List<TcpClient> clientList = this.getServerState().GetClientList();
            clientList.ForEach(client => {
                client.GetStream().Write(sendBytes, 0, sendBytes.Length);
            });
        }

        private void respondAnswer(int num, NetworkStream netStream)
        {
            int answer = num * this.getServerState().GetCurrentFactor();
            byte[] sendBytes = Encoding.UTF8.GetBytes(answer.ToString() + "\n");
            netStream.Write(sendBytes, 0, sendBytes.Length);
        }

        private void respondUsage(NetworkStream netStream)
        {
            string msg = "you should enter numbers or *N to change factor\n";
            byte[] sendBytes = Encoding.UTF8.GetBytes(msg.ToString());
            netStream.Write(sendBytes, 0, sendBytes.Length);
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
