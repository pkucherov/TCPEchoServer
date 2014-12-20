﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TCPEchoServer
{
    class EchoServer
    {
        Socket _listener;

        public EchoServer()
        {

        }

        public void Start(IPEndPoint ipe)
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(ipe);
            int backlog = 100;
            _listener.Listen(backlog);

            // Start an asynchronous socket to listen for connections.
            Console.WriteLine("Waiting for a connection...");
            bool bRet = startAccept();

            if (!bRet)
            {

            }

        }

        private bool startAccept()
        {
            SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(acceptArgs_Completed);
            acceptArgs.UserToken = "hghghg";
            bool bRet = _listener.AcceptAsync(acceptArgs);
            return bRet;
        }

        private void acceptArgs_Completed(object sender, SocketAsyncEventArgs args)
        {
            startAccept();

            if (args.SocketError != SocketError.Success)
            {

            }

            System.Console.WriteLine("acceptCompleted");


            startReceive(args);

        }

        private void startReceive(SocketAsyncEventArgs args)
        {
            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += receiveArgs_Completed;
            receiveArgs.AcceptSocket = args.AcceptSocket;
            receiveArgs.UserToken = "abb";
            args.AcceptSocket = null;
            byte[] a = new byte[100];
            receiveArgs.SetBuffer(a, 0, 100);
            bool willRaiseEvent = receiveArgs.AcceptSocket.ReceiveAsync(receiveArgs);
        }

        void receiveArgs_Completed(object sender, SocketAsyncEventArgs receiveSendEventArgs)
        {
            if (receiveSendEventArgs.SocketError != SocketError.Success)
            {
                return;
            }

            System.Console.WriteLine("receiveSendEventArgs_Completed");
            if (receiveSendEventArgs.BytesTransferred == 0)
            {
                return;
            }

            System.Console.WriteLine("transferred = {0}", receiveSendEventArgs.BytesTransferred);

            startReceive(receiveSendEventArgs);
        }
    }
}
