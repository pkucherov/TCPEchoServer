using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common;
using System.Diagnostics;

namespace TCPEchoServer
{
    abstract class BaseServer : AsyncSendReceiveBase
    {
        Socket _listener;
        ConcurrentStack<SocketAsyncEventArgs> _acceptArgsStack;

        const int nMaxAccept = 100;

        List<Socket> _clientCollection = new List<Socket>();

        public BaseServer()
            : base(new DataProcessor())
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
                Debug.WriteLine("acceptArgs_Completed SocketError");
            }

            Debug.WriteLine("acceptArgs_Completed");
           // Task.Factory.StartNew(() =>
            {
                lock (((ICollection) _clientCollection).SyncRoot)
                {
                    _clientCollection.Add(args.AcceptSocket);
                }
            }//);

            startReceive(args);

            args.AcceptSocket = null;
            _acceptArgsStack.Push(args);
        }

        protected override void onDataPacketReaded(SocketAsyncEventArgs args, IDataPacket dp)
        {
            Debug.WriteLine("server received = {0}", ((DataPacket)dp).Data);
            startSend(dp);
        }

        private void startSend(IDataPacket dpForSend)
        {
           // Task.Factory.StartNew(() =>
            {
                lock (((ICollection) _clientCollection).SyncRoot)
                {
                    List<Socket> aSocketsToDelete = new List<Socket>();
                    foreach (Socket client in _clientCollection)
                    {
                        if (client.Connected)
                        {
                            SocketAsyncEventArgs sendArgs;
                            if (!_sendArgsStack.TryPop(out sendArgs))
                            {
                                //sendArgs = new SocketAsyncEventArgs();
                                //var segment = _bufferManager.GetBuffer();
                                //sendArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
                                //sendArgs.Completed += sendArgs_Completed;
                            }

                            Debug.Assert(sendArgs.UserToken.GetType() == typeof (SendUserToken));
                            sendArgs.AcceptSocket = client;

                            SendUserToken token = (SendUserToken) sendArgs.UserToken;
                            token.DataPacket = dpForSend;
                            sendDataPacket(sendArgs);
                        }
                        else
                        {
                            aSocketsToDelete.Add(client);
                        }
                    }
                    for (int i = 0; i < aSocketsToDelete.Count; i++)
                    {
                        Socket socket = aSocketsToDelete[i];
                        _clientCollection.Remove(socket);
                    }
                }
            }//);
        }

        protected override void sendCompleted(SocketAsyncEventArgs sendArgs)
        {
            sendDataPacket(sendArgs);
        }
    }
}
