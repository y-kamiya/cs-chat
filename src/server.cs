using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace TCPServer
{
    class Server
    {
        private static TcpClient client;

        public static void Main(string[] args)
        {
            IPEndPoint ipAdd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            TcpListener listener = new TcpListener(ipAdd);
            listener.Start(0);
            Console.WriteLine("start listening at port 8888");

            TcpClient c = listener.AcceptTcpClient();
            TCPServer.Server.client = c;
            Console.WriteLine("client connected");

            Thread thread = new Thread(new ThreadStart(TCPServer.Server.talk));
            thread.Start();

            Thread.Sleep(30000);
            Console.WriteLine("press a key to finish");
            Console.ReadLine();
            listener.Stop();
        }

        public static void talk()
        {
            TcpClient client = TCPServer.Server.client;
            if (client.Connected)
            {
                Console.WriteLine("bbbbbbb");
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
                        int sendNum = num * 2;
                        sendBytes = Encoding.UTF8.GetBytes(sendNum.ToString());
                    } else
                    {
                        string msg = "you should enter numbers";
                        sendBytes = Encoding.UTF8.GetBytes(msg.ToString());
                    }
                    netStream.Write(sendBytes, 0, sendBytes.Length);
                } while (!str.Equals("quit"));

                sReader.Close();
                client.Close();
            }
        }
    }
}
