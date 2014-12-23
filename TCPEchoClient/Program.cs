using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace TCPEchoClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ParametersParser parser = new ParametersParser(args);
            List<IPEndPoint> endPoints = parser.GetEndPoints();
            

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
        private List<IPEndPoint> _endPoints = new List<IPEndPoint>();
        public ParametersParser(string[] args)
        {
            foreach (string serverAddress in args)
            {
                IPEndPoint ep = parseAddress(serverAddress);
                if (ep != null)
                {
                    _endPoints.Add(ep);
                }
            }
        }

        private IPEndPoint parseAddress(string serverAddress)
        {
            string[] sa = serverAddress.Split(':');
            IPEndPoint ep = null;
            
            if (sa.Length == 2)
            {
                IPAddress ip; 
                if (IPAddress.TryParse(sa[0], out ip))
                {
                    int port;
                    if (int.TryParse(sa[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
                    {
                        ep = new IPEndPoint(ip, port); 
                    }
                }
            }
            return ep;
        }

        public List<IPEndPoint> GetEndPoints()
        {
            return _endPoints;
        }
    }
}
