using System;
using System.Net;

namespace TCPEchoClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ParametersParser parser = new ParametersParser(args);
            parser.GetEndPoints();
            

            EchoClient echoClient = new EchoClient();
            int nPort = 2030;
            echoClient.Connect(new IPEndPoint(IPAddress.Parse("192.168.0.155"), nPort));
            for (; ; )
            {
                string strData = Console.ReadLine();
                echoClient.SendData(strData);
            }
        }
    }

    class ParametersParser
    {
        public ParametersParser(string[] args)
        {
            foreach (string serverAddress in args)
            {
                parseAddress(serverAddress);
            }
        }

        private void parseAddress(string serverAddress)
        {
            
        }

        public IPEndPoint[] GetEndPoints()
        {
            return null;
        }
    }
}
