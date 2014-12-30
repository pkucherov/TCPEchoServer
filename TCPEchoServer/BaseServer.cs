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
    public abstract class BaseServer : AsyncSendReceiveBase
    {
        private Socket _listener;
        private ConcurrentStack<SocketAsyncEventArgs> _acceptArgsStack;

        private const int nMaxAccept = 100;

        protected List<Socket> _clientCollection = new List<Socket>();

        public BaseServer()
            : base(new DataProcessor())
        {
            _acceptArgsStack = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < nMaxAccept; i++)
            {
                SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += acceptArgs_Completed;

                _acceptArgsStack.Push(acceptArgs);
            }
        }

        public void Start(IPEndPoint ipe)
        {
            try
            {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.Bind(ipe);
                int backlog = 100;
                _listener.Listen(backlog);

                startAccept();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server error: {0}", ex.Message);
            }
        }

        private void startAccept()
        {
            SocketAsyncEventArgs acceptArgs;
            if (!_acceptArgsStack.TryPop(out acceptArgs))
            {
                acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += acceptArgs_Completed;
            }

            if(!_listener.AcceptAsync(acceptArgs))
            {
                acceptArgs_Completed(_listener, acceptArgs);
            }            
        }

        private void acceptArgs_Completed(object sender, SocketAsyncEventArgs args)
        {
            startAccept();

            if (args.SocketError != SocketError.Success)
            {
                Debug.WriteLine("acceptArgs_Completed SocketError");
            }

            Debug.WriteLine("acceptArgs_Completed");
            Socket newClient = args.AcceptSocket;
            Task.Factory.StartNew(() =>
            {
                lock (((ICollection) _clientCollection).SyncRoot)
                {
                    _clientCollection.Add(newClient);
                }
            });

            startReceive(args);

            args.AcceptSocket = null;
            _acceptArgsStack.Push(args);
        }

       
        protected override void sendCompleted(SocketAsyncEventArgs sendArgs)
        {
            sendDataPacket(sendArgs);
        }
    }
}
