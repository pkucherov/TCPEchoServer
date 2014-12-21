using System;
using System.Net;

namespace TCPEchoClient
{
    class Program
    {
        static void Main(string[] args)
        {
            EchoClient echoClient = new EchoClient();
            int nPort = 2030;
            echoClient.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.3"), nPort));
            for (; ; )
            {
                string strData = Console.ReadLine();
                echoClient.SendData(strData);
            }
        }
    }
}
