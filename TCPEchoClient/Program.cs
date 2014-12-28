using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPEchoClient
{
    class Program
    {
        private const string strExit = "EXIT";

        static void Main(string[] args)
        {
            ParametersParser parser = new ParametersParser(args);
            List<IPEndPoint> endPoints = parser.GetEndPoints();
            if (endPoints.Count == 0)
            {
                printUsage();
            }

            IPEndPoint ip = getSelectedServerIP(endPoints);

            if (ip != null)
            {

                Task taskA = Task.Run(() =>
                {
                    Console.WriteLine("Server IP {0} port {1} selected", ip.Address, ip.Port);
                    EchoClient echoClient = new EchoClient(endPoints);

                    echoClient.Connect(ip);
                    while (!echoClient.ExitEvent.WaitOne(0))
                    {
                        string strData = Console.ReadLine();
                        if (string.Compare(strData, strExit, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            echoClient.ExitEvent.Set();
                            echoClient.Close();                            
                            return;
                        }
                        if (echoClient.ExitEvent.WaitOne(0))
                        {
                            return;
                        }

                        echoClient.SendData(strData);
                    }
                });

                taskA.Wait();
                Console.WriteLine("All finished press ENTER to exit");
                Console.ReadLine();
            }
        }

        private static void printUsage()
        {
            Console.WriteLine("No servers listed in command line");
            Console.WriteLine("Please use following command line syntax:");
            Console.WriteLine("TCPEchoClient <Server IP: Port> [Server IP: Port]");
        }

        private static IPEndPoint getSelectedServerIP(List<IPEndPoint> endPoints)
        {
            IPEndPoint endPoint = null;
            if (endPoints.Count > 1)
            {
                Console.WriteLine("Following servers are available:");

                int i = 1;
                foreach (IPEndPoint ipEndPoint in endPoints)
                {
                    Console.WriteLine("{0}) {1}:{2} ", i++, ipEndPoint.Address, ipEndPoint.Port);
                }
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Please enter 1...{0} to select the server", endPoints.Count);
                Console.WriteLine(sb.ToString());
                string strServerKey = Console.ReadLine();
                int nServerKey;
                if (int.TryParse(strServerKey, out nServerKey))
                {
                    if (nServerKey > 0 && nServerKey <= endPoints.Count)
                    {
                        endPoint = endPoints[nServerKey - 1];
                    }
                }
            }
            else
            {
                endPoint = endPoints[0];
            }
            return endPoint;
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
