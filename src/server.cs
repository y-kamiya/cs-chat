using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace TCPServer
{
    class Server
    {
        static void Main(string[] args)
        {
            IPEndPoint ipAdd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            TcpListener listener = new TcpListener(ipAdd);
            listener.Start(0);
            Console.WriteLine("start listening at port 8888");

            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("client connected");

            if (client.Connected)
            {
                listener.Stop();
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
                } while (!str.Equals("quit"));
                sReader.Close();
                client.Close();
            }
            Console.WriteLine("press a key to finish");
            Console.ReadLine();
        }
    }
}
