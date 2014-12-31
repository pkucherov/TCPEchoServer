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
    public class EchoUnitTestMultiTask
    {
        //[TestMethod]
        public void SingleClient()
        {
            ManualResetEvent exitEvent = new ManualResetEvent(false);
            ManualResetEvent serverStarted = new ManualResetEvent(false);
            Task taskServer = Task.Run(() =>
            {
                EchoServer echoServer = new EchoServer();
                echoServer.Start(new IPEndPoint(IPAddress.Any, 2030));
                serverStarted.Set();
                exitEvent.WaitOne();
            });

            serverStarted.WaitOne();
            Task taskClient = Task.Run(() =>
            {
                List<IPEndPoint> endPoints = new List<IPEndPoint>();
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2030);
                endPoints.Add(ep);
                EchoTestClient echoClient = new EchoTestClient(endPoints);

                echoClient.Connect(endPoints[0]);
                string strTestData = "1234567890AaBbCcDdEeFfGgHhJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";
                echoClient.SendData(strTestData);
                echoClient.DataReady.WaitOne();
                Assert.IsTrue(string.Equals(strTestData, echoClient.ReceivedData));
                string strTestData2 = "JjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890AaBbCcDdEeFfGgHhJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890AaBbCcDdEeFfGgHh";
                echoClient.SendData(strTestData2);
                echoClient.DataReady.WaitOne();
                Assert.IsTrue(string.Equals(strTestData2, echoClient.ReceivedData));
                exitEvent.Set();
            });
            taskServer.Wait();
            taskClient.Wait();            
        }
    }
    [TestClass]
    public class EchoUnitTest
    {
     

        [TestMethod]
        public void SingleThreadSingleClient()
        {
            EchoServer echoServer = new EchoServer();
            echoServer.Start(new IPEndPoint(IPAddress.Any, 2031));

            List<IPEndPoint> endPoints = new List<IPEndPoint>();
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2031);
            endPoints.Add(ep);
            EchoTestClient echoClient = new EchoTestClient(endPoints);

            echoClient.Connect(endPoints[0]);
            string strTestData = "1234567890AaBbCcDdEeFfGgHhJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";
            echoClient.SendData(strTestData);
            echoClient.DataReady.WaitOne();
            Assert.IsTrue(string.Equals(strTestData, echoClient.ReceivedData));
            string strTestData2 = "JjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890AaBbCcDdEeFfGgHhJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890AaBbCcDdEeFfGgHh";
            echoClient.SendData(strTestData2);
            echoClient.DataReady.WaitOne();
            Assert.IsTrue(string.Equals(strTestData2, echoClient.ReceivedData));
        }

        // [TestMethod]
        public void MultipleClients()
        {
            ManualResetEvent exitEvent = new ManualResetEvent(false);
            ManualResetEvent serverStarted = new ManualResetEvent(false);
            Task taskServer = Task.Run(() =>
            {
                EchoServer echoServer = new EchoServer();
                echoServer.Start(new IPEndPoint(IPAddress.Any, 2032));
                serverStarted.Set();
                exitEvent.WaitOne();
            });

            serverStarted.WaitOne();

            List<IPEndPoint> endPoints = new List<IPEndPoint>();
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2032);
            endPoints.Add(ep);
            List<EchoTestClient> aClients = new List<EchoTestClient>();
            for (int i = 0; i < 10; i++)
            {
                EchoTestClient echoClient = new EchoTestClient(endPoints);
                echoClient.Connect(endPoints[0]);
                aClients.Add(echoClient);
            }

            Task taskClient = Task.Run(() =>
            {
                EchoTestClient echoClient = aClients[0];
                string strTestData = "1234567890AaBbCcDdEeFfGgHhJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";
                echoClient.SendData(strTestData);
                echoClient.DataReady.WaitOne();
                Assert.IsTrue(string.Equals(strTestData, echoClient.ReceivedData));
                string strTestData2 = "JjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890AaBbCcDdEeFfGgHhJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890AaBbCcDdEeFfGgHh";
                echoClient.SendData(strTestData2);
                echoClient.DataReady.WaitOne();
                Assert.IsTrue(string.Equals(strTestData2, echoClient.ReceivedData));
                exitEvent.Set();
            });

        }
        [TestMethod]
        public void MultipleClientsSingleThread()
        {
            EchoServer echoServer = new EchoServer();
            echoServer.Start(new IPEndPoint(IPAddress.Any, 2032));

            List<IPEndPoint> endPoints = new List<IPEndPoint>();
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2032);
            endPoints.Add(ep);
            List<EchoTestClient> aClients = new List<EchoTestClient>();
            for (int i = 0; i < 10; i++)
            {
                EchoTestClient echoClient = new EchoTestClient(endPoints);
                echoClient.Connect(endPoints[0]);
                aClients.Add(echoClient);
            }

            EchoTestClient clientMaster = aClients[0];
            string strTestData2 = "JjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890AaBbCcDdEeFfGgHhJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890AaBbCcDdEeFfGgHh";
            clientMaster.SendData(strTestData2);

            for (int i = 1; i < 10; i++)
            {
                EchoTestClient echoClient = aClients[i];
                echoClient.DataReady.WaitOne();
                Assert.IsTrue(string.Equals(strTestData2, echoClient.ReceivedData));
            }
        }
        [TestMethod]
        public void MultipleClientsSingleThread2()
        {
            EchoServer echoServer = new EchoServer();
            echoServer.Start(new IPEndPoint(IPAddress.Any, 2033));

            List<IPEndPoint> endPoints = new List<IPEndPoint>();
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2033);
            endPoints.Add(ep);
            List<EchoTestClient> aClients = new List<EchoTestClient>();
            for (int i = 0; i < 10; i++)
            {
                EchoTestClient echoClient = new EchoTestClient(endPoints);
                echoClient.Connect(endPoints[0]);
                aClients.Add(echoClient);
            }
           
            string strTestData2 = "JjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890AaBbCcDdEeFfGgHhJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890AaBbCcDdEeFfGgHh";
            for (int j = 0; j < 10; j++)
            {
                EchoTestClient clientMaster = aClients[j];
                clientMaster.SendData(strTestData2);
                for (int i = 0; i < 10; i++)
                {
                    EchoTestClient echoClient = aClients[i];
                    echoClient.DataReady.WaitOne();
                    Assert.IsTrue(string.Equals(strTestData2, echoClient.ReceivedData));
                }
            }
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
            ReceivedData = ((DataPacket)dp).Data;
            DataReady.Set();
        }

    }
}
