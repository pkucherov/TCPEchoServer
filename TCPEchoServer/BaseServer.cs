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
        ConcurrentStack<SocketAsyncEventArgs> _sendArgsStack;
        BufferManager _bufferManager;

        const int nBufferSize = 100;
        const int nMaxAccept = 100;
        const int nMaxSendReceive = 100;

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
            
            _bufferManager = new BufferManager(2 * nMaxSendReceive, nBufferSize);

            _receiveArgsStack = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < nMaxSendReceive; i++)
            {                
                SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
                receiveArgs.Completed += receiveArgs_Completed;
                //byte[] buf = new byte[100];
                //receiveArgs.SetBuffer(buf, 0, 100);
                var segment = _bufferManager.GetBuffer(); 
                receiveArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
                _receiveArgsStack.Push(receiveArgs);
            }

            _sendArgsStack = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < nMaxSendReceive; i++)
            {               
                SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += sendArgs_Completed;
                //byte[] buf = new byte[100];
                //sendArgs.SetBuffer(buf, 0, 100);
                var segment = _bufferManager.GetBuffer();
                sendArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
                _sendArgsStack.Push(sendArgs);
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
                acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(acceptArgs_Completed);
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

        private void startReceive(SocketAsyncEventArgs args)
        {
            SocketAsyncEventArgs receiveArgs;
            if (!_receiveArgsStack.TryPop(out receiveArgs))
            {
                receiveArgs = new SocketAsyncEventArgs();
                receiveArgs.Completed += receiveArgs_Completed;                                
                
                var segment = _bufferManager.GetBuffer();
                receiveArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
            }

            receiveArgs.AcceptSocket = args.AcceptSocket;
            bool willRaiseEvent = receiveArgs.AcceptSocket.ReceiveAsync(receiveArgs);
        }

        private void receiveArgs_Completed(object sender, SocketAsyncEventArgs receiveArgs)
        {
            if (receiveArgs.SocketError != SocketError.Success)
            {
                return;
            }

            System.Console.WriteLine("receiveArgs_Completed");
            if (receiveArgs.BytesTransferred == 0)
            {
                return;
            }

            System.Console.WriteLine("transferred = {0}", receiveArgs.BytesTransferred);
            processPacket(receiveArgs);
            startReceive(receiveArgs);

            receiveArgs.AcceptSocket = null;
            _receiveArgsStack.Push(receiveArgs);
        }
        private void processPacket(SocketAsyncEventArgs args)
        {
            DataPacketHeader dph = new DataPacketHeader();
            DataPacket dp = new DataPacket();

            if (args.Buffer != null)
            {
                dph.Deserialize(args.Buffer, args.Offset, args.Count);

                byte[] buffer = new byte[dph.StringSize];
                Buffer.BlockCopy(args.Buffer, args.Offset + Marshal.SizeOf(typeof(DataPacketHeader)), buffer, 0, dph.StringSize);
                dp.Deserialize(buffer);
            }
            startSend(args, dp);
        }

        private void startSend(SocketAsyncEventArgs args, DataPacket dpForSend)
        {
            foreach (Socket client in _clientCollection)
            {
                SocketAsyncEventArgs sendArgs;
                if (!_sendArgsStack.TryPop(out sendArgs))
                {
                    sendArgs = new SocketAsyncEventArgs();
                    var segment = _bufferManager.GetBuffer();
                    sendArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
                    sendArgs.Completed += sendArgs_Completed;                                                       
                }
                sendArgs.AcceptSocket = client;

                DataPacket dp = dpForSend;           
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
                Buffer.BlockCopy(messageBuffer, 0, sendArgs.Buffer, sendArgs.Offset, messageBuffer.Length);
                
                sendArgs.AcceptSocket.SendAsync(sendArgs);
            }
        }
        private void sendArgs_Completed(object sender, SocketAsyncEventArgs sendEventArgs)
        {
            sendEventArgs.AcceptSocket = null;
            _sendArgsStack.Push(sendEventArgs);
        }     
    }
}
