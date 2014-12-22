﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using Common;
using System.Runtime.InteropServices;

namespace TCPEchoServer
{
    abstract class BaseServer : AsyncSendReceiveBase
    {
        Socket _listener;
        ConcurrentStack<SocketAsyncEventArgs> _acceptArgsStack;
      
        const int nMaxAccept = 100;

        BlockingCollection<Socket> _clientCollection = new BlockingCollection<Socket>();

        public BaseServer()
        {            
            _acceptArgsStack = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < nMaxAccept; i++)
            {
                SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(acceptArgs_Completed);

                _acceptArgsStack.Push(acceptArgs);
            }          
        }

        public void Start(IPEndPoint ipe)
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(ipe);
            int backlog = 100;
            _listener.Listen(backlog);

            bool bRet = startAccept();

            if (!bRet)
            {

            }

        }

        private bool startAccept()
        {
            SocketAsyncEventArgs acceptArgs;
            if (!_acceptArgsStack.TryPop(out acceptArgs))
            {
                //acceptArgs = new SocketAsyncEventArgs();
                //acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(acceptArgs_Completed);
            }

            bool bRet = _listener.AcceptAsync(acceptArgs);
            return bRet;
        }

        private void acceptArgs_Completed(object sender, SocketAsyncEventArgs args)
        {
            startAccept();

            if (args.SocketError != SocketError.Success)
            {

            }

            System.Console.WriteLine("acceptArgs_Completed");

            _clientCollection.Add(args.AcceptSocket);
            startReceive(args);

            args.AcceptSocket = null;
            _acceptArgsStack.Push(args);
        }       
                
        protected override void receiveCompleted(SocketAsyncEventArgs receiveArgs)
        {
            processPacket(receiveArgs);
            startReceive(receiveArgs);
        }        

        protected override void onDataPacketReaded(SocketAsyncEventArgs args, DataPacket dp)
        {
            startSend(args, dp);
        }

        private void startSend(SocketAsyncEventArgs args, DataPacket dpForSend)
        {
            foreach (Socket client in _clientCollection)
            {
                SocketAsyncEventArgs sendArgs;
                if (!_sendArgsStack.TryPop(out sendArgs))
                {
                    //sendArgs = new SocketAsyncEventArgs();
                    //var segment = _bufferManager.GetBuffer();
                    //sendArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
                    //sendArgs.Completed += sendArgs_Completed;
                }
                sendArgs.AcceptSocket = client;
                //if (args.UserToken != null)
                //{
                //    sendArgs.UserToken = args.UserToken;
                //}
                SendUserToken token = (SendUserToken)sendArgs.UserToken;
                token.DataPacket = dpForSend;
                sendDataPacket(sendArgs);
            }
        }
        
        protected override void sendCompleted(SocketAsyncEventArgs sendArgs)
        {
            sendDataPacket(sendArgs);
        }
    }
}
