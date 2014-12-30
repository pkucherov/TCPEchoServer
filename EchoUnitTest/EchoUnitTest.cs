using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCPEchoClient;
using TCPEchoServer;

namespace EchoUnitTest
{
    [TestClass]
    public class EchoUnitTest
    {
        [TestMethod]
        public void SendReceiveEcho()
        {
            ManualResetEvent exitEvent = new ManualResetEvent(false);
            Task taskServer = Task.Run(() =>
            {
                EchoServer echoServer = new EchoServer();
                echoServer.Start(new IPEndPoint(IPAddress.Any, 2030));
                exitEvent.WaitOne();
            });

            taskServer.Wait(2000);
            Task taskClient = Task.Run(() =>
            {
                List<IPEndPoint> endPoints = new List<IPEndPoint>();
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"),2030);
                endPoints.Add(ep);
                EchoTestClient echoClient = new EchoTestClient(endPoints);

                echoClient.Connect(endPoints[0]);
                string strTestData = "1234567890AaBbCcDdEeFfGgHhJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";
                echoClient.SendData(strTestData);
                echoClient.DataReady.WaitOne();
                Assert.IsTrue(string.Equals(strTestData, echoClient.ReceivedData));
                exitEvent.Set();
            });
            taskServer.Wait();
            taskClient.Wait();
            
        }
    }

    class EchoTestClient : EchoClient
    {
        public AutoResetEvent DataReady = new AutoResetEvent(false);
        public string ReceivedData { get; set; }

        public EchoTestClient(List<IPEndPoint> endPoints)
            : base(endPoints)
        {
            
        }
        protected override void onDataPacketReaded(SocketAsyncEventArgs args, IDataPacket dp)
        {
            ReceivedData = ((DataPacket) dp).Data;
            DataReady.Set();
        }
        
    }
}
