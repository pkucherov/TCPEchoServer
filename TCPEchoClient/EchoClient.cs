﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Common;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;


namespace TCPEchoClient
{
    static class SocketExtensions
    {
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                bool bStatus = socket.Poll(1, SelectMode.SelectRead);
                return (!(bStatus && socket.Available == 0)) && socket.Connected;
            }
            catch (SocketException) { return false; }
        }
    }

    class ClientSocket : Socket
    {        
        private IPEndPoint _endPoint;

        public ClientSocket(IPEndPoint ep)
            : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            _endPoint = ep;
        }

        public IPEndPoint ServerEndPoint
        {
            get
            {
                return _endPoint;
            }
        }
    }

    class EchoClient : AsyncSendReceiveBase
    {
        private ClientSocket _internalSocket;
        private readonly object _socketLocker = new object();
        private readonly ManualResetEvent _exitEvent = new ManualResetEvent(false);
        private readonly AutoResetEvent _errorEvent = new AutoResetEvent(false);
        private readonly WaitHandle[] _events;
        private const int ConnectionCheckingTime = 2000;
        private List<IPEndPoint> _endPoints;

        public ManualResetEvent ExitEvent
        {
            get { return _exitEvent; }
        }

        private ClientSocket _socket
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
            _events = new WaitHandle[] { _exitEvent, _errorEvent };
        }

        public void Connect(IPEndPoint ipe)
        {
            Console.WriteLine("Connecting IP {0} port {1} selected", ipe.Address, ipe.Port);

            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.RemoteEndPoint = ipe;
            connectArgs.Completed += connectArgs_Completed;
            _socket = new ClientSocket(ipe);
            _socket.ConnectAsync(connectArgs);
        }

        public void Close()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
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

        private void checkServerConnection()
        {
            Task.Factory.StartNew(check);
        }

        private void check()
        {
            bool bConnected = false;
            int nWaitIndex = WaitHandle.WaitTimeout;
            do
            {
                bConnected = _socket.IsConnected();                
            }
            while (bConnected && ((nWaitIndex = WaitHandle.WaitAny(_events, ConnectionCheckingTime)) == WaitHandle.WaitTimeout));

            if (nWaitIndex == 1 || !bConnected)// connection error
            {
                Debug.WriteLine("Connection lost");
                IPEndPoint iep = _socket.ServerEndPoint;

                _endPoints.Remove(iep);
                if (_endPoints.Count > 0)
                {
                    IPEndPoint nextEndPoint = _endPoints[0];
                    Connect(nextEndPoint);
                }
                else
                {
                    ExitEvent.Set();
                }
            }
        }

        protected override void sendCompleted(SocketAsyncEventArgs sendArgs)
        {
            sendDataPacket(sendArgs);
        }

        private void connectArgs_Completed(object sender, SocketAsyncEventArgs connectArgs)
        {
            checkServerConnection();

            Debug.WriteLine("connectArgs_Completed");
            if (connectArgs.SocketError != SocketError.Success)
            {
                Console.WriteLine("Connection error");
                Debug.WriteLine("SocketError");
                return;
            }
            Console.WriteLine("Connected");
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

        protected override void onReceiveError(SocketAsyncEventArgs receiveArgs)
        {
            _errorEvent.Set();
        }

        protected override void onSendError(SocketAsyncEventArgs sendArgs)
        {
            _errorEvent.Set();
        }
    }
}
