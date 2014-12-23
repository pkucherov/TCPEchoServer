using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Common;
using System.Diagnostics;
using System.Threading.Tasks;


namespace TCPEchoClient
{
    static class SocketExtensions
    {
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }
    }

    class EchoClient : AsyncSendReceiveBase
    {
        private Socket _internalSocket;
        private readonly object _socketLocker = new object();
        private readonly ManualResetEvent _exitEvent = new ManualResetEvent(false);
        private const int ConnectionCheckingTime = 2000;
        private List<IPEndPoint> _endPoints;

        public ManualResetEvent ExitEvent
        {
            get { return _exitEvent; }
        }

        private Socket _socket
        {
            get
            {
                lock (_socketLocker)
                {
                    return _internalSocket;    
                }
            }
            set
            {
                lock (_socketLocker)
                {
                    _internalSocket = value;
                }
            }
        }

        public EchoClient(List<IPEndPoint> endPoints)
        {
            _endPoints = endPoints;
        }

        public void Connect(IPEndPoint ipe)
        {
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.RemoteEndPoint = ipe;
            connectArgs.Completed += connectArgs_Completed;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.ConnectAsync(connectArgs);            
        }

        public void SendData(string strData)
        {            
            SocketAsyncEventArgs sendArgs;
            if (!_sendArgsStack.TryPop(out sendArgs))
            { 
            }
            Debug.Assert(sendArgs.UserToken.GetType() == typeof(SendUserToken));

            sendArgs.AcceptSocket = _socket;
            startSend(sendArgs, strData);
        }

        private void checkServerConection()
        {
            Task.Factory.StartNew(check);               
        }

        private void check()
        {
            try
            {
                bool bRet;
                do
                {
                    bRet = _socket.IsConnected();
                }
                while (bRet && !_exitEvent.WaitOne(ConnectionCheckingTime));
            }
            catch (SocketException) { }
            Console.WriteLine("connection lost");
            IPEndPoint ipe = null;
            //Connect(ipe);
        }

        protected override void sendCompleted(SocketAsyncEventArgs sendArgs)
        {
            sendDataPacket(sendArgs);
        }

        private void connectArgs_Completed(object sender, SocketAsyncEventArgs connectArgs)
        {
            Console.WriteLine("connectArgs_Completed");
            if (connectArgs.SocketError != SocketError.Success)
            {
                Console.WriteLine("SocketError");
                return;
            }

            checkServerConection();            

            SocketAsyncEventArgs receiveArgs;
            if (!_receiveArgsStack.TryPop(out receiveArgs))
            {
            }
            Debug.Assert(receiveArgs.UserToken.GetType() == typeof(ReceiveUserToken));
            receiveArgs.AcceptSocket = connectArgs.ConnectSocket;

            startReceive(receiveArgs);
        }
       
        private void startSend(SocketAsyncEventArgs sendArgs, string strData)
        {
            DataPacket dp = new DataPacket();
            dp.Data = strData;              
            
            SendUserToken token = (SendUserToken)sendArgs.UserToken;
            token.DataPacket = dp;

            sendDataPacket(sendArgs);           
        }

        protected override void onDataPacketReaded(SocketAsyncEventArgs args, DataPacket dp)
        {
            Console.WriteLine(dp.Data);
        }
    }
}
