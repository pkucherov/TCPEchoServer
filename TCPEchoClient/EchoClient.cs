﻿using System;
using System.Net;
using System.Net.Sockets;
using Common;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace TCPEchoClient
{
    class EchoClient : AsyncSendReceiveBase
    {
        private Socket _socket;
        public EchoClient()
        {

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
        protected override void sendCompleted(SocketAsyncEventArgs sendArgs)
        {
            sendDataPacket(sendArgs);
        }

        private void connectArgs_Completed(object sender, SocketAsyncEventArgs connectArgs)
        {
            Console.WriteLine("connectArgs_Completed");
            if (connectArgs.SocketError != SocketError.Success)
            {
                return;
            }

            //SocketAsyncEventArgs receiveSendEventArgs = new SocketAsyncEventArgs();
            //receiveSendEventArgs.AcceptSocket = connectArgs.ConnectSocket;
            //receiveSendEventArgs.Completed += receiveArgs_Completed;

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
