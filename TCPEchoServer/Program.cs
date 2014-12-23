using System;
using System.Net;

namespace TCPEchoServer
{
    class Program
    {
        static void Main(string[] args)
        {
            EchoServer echoServer = new EchoServer();
            int nPort = 2030;
            echoServer.Start(new IPEndPoint(IPAddress.Any, nPort));
            Console.WriteLine("Waiting for a connection .... Please press any key to exit.");
            Console.ReadLine();
        }
    }
}
