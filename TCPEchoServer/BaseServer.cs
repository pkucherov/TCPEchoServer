using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using Common;
using System.Runtime.InteropServices;

namespace TCPEchoServer
{   
    abstract class BaseServer
    {
        Socket _listener;
        ConcurrentStack<SocketAsyncEventArgs> _acceptArgsStack;
        ConcurrentStack<SocketAsyncEventArgs> _receiveArgsStack;

        BlockingCollection<Socket> _clientCollection = new BlockingCollection<Socket>();

        public BaseServer()
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

            _clientCollection.Add(args.AcceptSocket);

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
            DataPacketHeader dph = new DataPacketHeader();
            DataPacket dp = new DataPacket();

            if (args.Buffer != null)
            {
                dph.Deserialize(args.Buffer);

                byte[] buffer = new byte[dph.StringSize];
                Buffer.BlockCopy(args.Buffer, Marshal.SizeOf(typeof(DataPacketHeader)), buffer, 0, dph.StringSize);
                dp.Deserialize(buffer);
            }
            startSend(args);
        }

        private void startSend(SocketAsyncEventArgs args)
        {
            foreach (Socket client in _clientCollection)
            {
                SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += sendArgs_Completed;
                sendArgs.AcceptSocket = client;
                sendArgs.UserToken = "abb";
                //args.AcceptSocket = null;       

                DataPacket dp = new DataPacket();
                dp.Data = "asdfghjkljhjhghgfgfdfgTEST TEST2";
                byte[] dataBuffer = dp.Serialize();

                DataPacketHeader dph = new DataPacketHeader();

                dph.MagicKey = 0xFFACBBFA;
                dph.Version = 0x0100;
                dph.StringSize = dataBuffer.Length;

                byte[] headerBuffer = dph.Serialize();
                int nSize = dataBuffer.Length + headerBuffer.Length;
                byte[] messageBuffer = new byte[nSize];

                Buffer.BlockCopy(headerBuffer, 0, messageBuffer, 0, headerBuffer.Length);
                Buffer.BlockCopy(dataBuffer, 0, messageBuffer, headerBuffer.Length, dataBuffer.Length);
                sendArgs.SetBuffer(messageBuffer, 0, nSize);

                sendArgs.AcceptSocket.SendAsync(sendArgs);
            }
        }
        private void sendArgs_Completed(object sender, SocketAsyncEventArgs sendEventArgs)
        {

        }
    }
}
