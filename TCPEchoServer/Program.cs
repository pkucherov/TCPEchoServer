using System;
using System.Net;

namespace TCPEchoServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int nPort = 2030;
            if (args.Length >0)
            {
                int nNewPort;
                if (int.TryParse(args[0], out nNewPort))
                {
                    nPort = nNewPort;
                }
            }
            else
            {
                Console.WriteLine("Usage: TCPEchoServer.exe [port]");
            }
            EchoServer echoServer = new EchoServer();
            Console.WriteLine("Using port = {0}", nPort);
            echoServer.Start(new IPEndPoint(IPAddress.Any, nPort));
            Console.WriteLine("Waiting for a connection .... Please press ENTER to exit.");
            Console.ReadLine();
        }
    }
}
