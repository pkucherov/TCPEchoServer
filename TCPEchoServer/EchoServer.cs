using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using Common;

namespace TCPEchoServer
{
    class EchoServer
    {
        Socket _listener;
        ConcurrentStack<SocketAsyncEventArgs> _acceptArgsStack;
        ConcurrentStack<SocketAsyncEventArgs> _receiveArgsStack;

        public EchoServer()
        {
            _acceptArgsStack = new ConcurrentStack<SocketAsyncEventArgs>();
            _receiveArgsStack = new ConcurrentStack<SocketAsyncEventArgs>();
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

            System.Console.WriteLine("acceptArgs_Completed");


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

        private void receiveArgs_Completed(object sender, SocketAsyncEventArgs receiveSendEventArgs)
        {
            if (receiveSendEventArgs.SocketError != SocketError.Success)
            {
                return;
            }

            System.Console.WriteLine("receiveArgs_Completed");
            if (receiveSendEventArgs.BytesTransferred == 0)
            {
                return;
            }

            System.Console.WriteLine("transferred = {0}", receiveSendEventArgs.BytesTransferred);
            processPacket(receiveSendEventArgs);
            startReceive(receiveSendEventArgs);
        }
        private void processPacket(SocketAsyncEventArgs args)
        {
            DataPacket dp = new DataPacket();
            dp.MagicKey = 0xFFAC;
            dp.Version = 0x01;
            dp.StringSize = 10;
            dp.Data = "123456";

            if (args.Buffer != null)
            {
                dp.Deserialize(args.Buffer);
            }
        }
    }
}
