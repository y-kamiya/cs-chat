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

            for (int i = 0; i < clientCount; i++)
            {
                string name = "bench" + i;
                ChatClient client = new ChatClient(name, clientCount, maxMessageCount);
                Thread thread = new Thread(new ThreadStart(client.Connect));
                thread.Start();
            }
            Console.WriteLine("if you want to finish this client, press any key");
            Console.ReadLine();
        }
    }

    class ChatClient
    {
        private string name;
        private int maxMessageCount;
        private int clientCount;
        private readonly int waitBeforeQuit = 1000;
        private readonly int waitBetweenSend = 10;

        public ChatClient(string name, int clientCount, int maxMessageCount)
        {
            this.name = name;
            this.clientCount = clientCount;
            this.maxMessageCount = maxMessageCount;
        }

        public void Connect()
        {
            TcpClient client = new TcpClient("127.0.0.1", 8888);
            NetworkStream netStream = client.GetStream();
            StreamReader sReader = new StreamReader(netStream, Encoding.UTF8);
            ManualResetEvent mre = new ManualResetEvent(false);

            ThreadStart reader = () => 
            {
                while (true)
                {
                    string str = sReader.ReadLine();
                    if (str == "FINISHED") break;
                    if (str == "hi, " + this.name) { Console.WriteLine("aaaaaaaaaaa"); mre.Set(); }
                    Console.WriteLine("receiver: " + this.name + ", " + str);
                }
            };
            Thread readThread = new Thread(reader);

            try 
            {
                readThread.Start();

                this.sendMessage(this.name, client.GetStream());
                Console.WriteLine("send name: " + this.name);

                mre.WaitOne();
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                for (int i = 0; i < this.maxMessageCount; i++)
                {
                    String msg = "i = " + i;
                    this.sendMessage(msg, netStream);
                    // Console.WriteLine("[debug] send: " + msg);
                    Thread.Sleep(this.waitBetweenSend);
                }

                Thread.Sleep(this.waitBeforeQuit);
                this.sendMessage("/quit", netStream);

                readThread.Join();
                sw.Stop();
                string format = "[{0} * {1}] {2} time: {3}";
                long elapsedTime = sw.ElapsedMilliseconds - this.waitBeforeQuit - this.waitBetweenSend * this.maxMessageCount;
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
