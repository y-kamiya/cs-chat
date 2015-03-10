using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TCPClient
{
    class Client
    {
        public static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("usage: ./chatClient.exe name");
                return;
            }
            string name = args[0];
            Connect(name);
            Console.WriteLine("if you want to finish this client, press any key");
            Console.ReadLine();
        }

        private static void Connect(string name)
        {
            TcpClient client = new TcpClient("127.0.0.1", 8888);
            NetworkStream netStream = client.GetStream();
            StreamReader sReader = new StreamReader(netStream, Encoding.UTF8);

            ThreadStart reader = () => 
            {
                while (true)
                {
                    string str = sReader.ReadLine();
                    if (str == null) break;
                    Console.WriteLine(str);
                }
            };
            Thread readThread = new Thread(reader);

            try 
            {
                readThread.Start();

                int count = 0;

                while (true)
                {
                    String msg = Console.ReadLine() + "\n";
                    byte[] sendBytes = Encoding.UTF8.GetBytes(msg);
                    netStream.Write(sendBytes, 0, sendBytes.Length);
                    Console.WriteLine("send: [{0}]Bytes - {1}", sendBytes.Length, msg);

                    count++;
                    if (5 <= count) break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                sReader.Close();
                client.Close();
                readThread.Join();
            }

        }
    }
}
