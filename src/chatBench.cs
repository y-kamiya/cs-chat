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
                Console.WriteLine("usage: ./chatClient.exe name maxCount");
                return;
            }
            string name = args[0];
            int maxCount = int.Parse(args[1]);

            ChatClient client = new ChatClient(name, maxCount);
            client.Connect();
            Console.WriteLine("if you want to finish this client, press any key");
            Console.ReadLine();
        }
    }

    class ChatClient
    {
        private string name;
        private int maxCount;

        public ChatClient(string name, int maxCount)
        {
            this.name = name;
            this.maxCount = maxCount;
        }

        public void Connect()
        {
            TcpClient client = new TcpClient("127.0.0.1", 8888);
            NetworkStream netStream = client.GetStream();
            StreamReader sReader = new StreamReader(netStream, Encoding.UTF8);

            ThreadStart reader = () => 
            {
                while (true)
                {
                    string str = sReader.ReadLine();
                    if (str == "FINISHED") break;
                    Console.WriteLine(str);
                }
            };
            Thread readThread = new Thread(reader);

            try 
            {
                readThread.Start();

                this.sendMessage(this.name, client.GetStream());

                for (int i = 0; i < this.maxCount; i++)
                {
                    String msg = "i = " + i;
                    this.sendMessage(msg, netStream);
                    Console.WriteLine("send: " + msg);
                }
                this.sendMessage("/quit", netStream);

                readThread.Join();
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
