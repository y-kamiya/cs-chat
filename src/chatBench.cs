using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TCPClient
{
    class EntryPoint
    {
        public static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("usage: ./chatClient.exe clientCount maxMessageCount");
                return;
            }
            int clientCount = int.Parse(args[0]);
            int maxMessageCount = int.Parse(args[1]);

            WaitHandle[] waitHandles = new WaitHandle[clientCount];
            for (int i = 0; i < clientCount; i++)
            {
                waitHandles[i] = new ManualResetEvent(false);
            }
            Thread[] threads = new Thread[clientCount];

            for (int i = 0; i < clientCount; i++)
            {
                ChatClient client = new ChatClient(i, clientCount, maxMessageCount, waitHandles);
                threads[i] = new Thread(new ThreadStart(client.Connect));
                threads[i].Start();
            }

            for (int i = 0; i < clientCount; i++)
            {
                threads[i].Join();
            }
        }
    }

    class ChatClient
    {
        private int id;
        private string name;
        private int maxMessageCount;
        private int clientCount;
        private WaitHandle[] waitHandles;
        private readonly int waitBetweenSend = 10;

        public ChatClient(int id, int clientCount, int maxMessageCount, WaitHandle[] waitHandles)
        {
            this.id = id;
            this.name = "bench" + id;
            this.clientCount = clientCount;
            this.maxMessageCount = maxMessageCount;
            this.waitHandles = waitHandles;
        }

        public void Connect()
        {
            TcpClient client = new TcpClient("127.0.0.1", 8888);
            NetworkStream netStream = client.GetStream();
            StreamReader sReader = new StreamReader(netStream, Encoding.UTF8);
            ManualResetEvent mre = (ManualResetEvent) this.waitHandles[this.id];

            ThreadStart reader = () => 
            {
                while (true)
                {
                    string str = sReader.ReadLine();
                    if (str == "FINISHED") break;
                    if (str == "hi, " + this.name) { mre.Set(); }
                    if (str == "<" + this.name + ">: i = " + (this.maxMessageCount - 1)) { mre.Set(); }
                    Console.WriteLine("receiver: " + this.name + ", " + str);
                }
            };
            Thread readThread = new Thread(reader);

            try 
            {
                readThread.Start();

                this.sendMessage(this.name, client.GetStream());
                Console.WriteLine("send name: " + this.name);

                // wait all thread are connected
                WaitHandle.WaitAll(this.waitHandles);
                mre.Reset();

                // stop watch
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                for (int i = 0; i < this.maxMessageCount; i++)
                {
                    String msg = "i = " + i;
                    this.sendMessage(msg, netStream);
                    // Console.WriteLine("[debug] send: " + msg);
                    Thread.Sleep(this.waitBetweenSend);
                }

                WaitHandle.WaitAll(this.waitHandles);
                sw.Stop();
                this.sendMessage("/quit", netStream);

                readThread.Join();

                // display result of time
                string format = "[{0} * {1}] {2} time: {3}";
                long elapsedTime = sw.ElapsedMilliseconds;
                Console.WriteLine(format, this.clientCount, this.maxMessageCount, this.name, elapsedTime);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                sReader.Close();
                client.Close();
            }

        }

        private void sendMessage(string msg, NetworkStream netStream)
        {
            byte[] sendBytes = Encoding.UTF8.GetBytes(msg + "\n");
            netStream.Write(sendBytes, 0, sendBytes.Length);
        }
    }
}
