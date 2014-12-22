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

        const int nBufferSize = 20;
        const int nMaxAccept = 100;
        const int nMaxSendReceive = 50000;
        private readonly int nPacketHeaderSize;

        BlockingCollection<Socket> _clientCollection = new BlockingCollection<Socket>();

        public BaseServer()
        {
            nPacketHeaderSize = Marshal.SizeOf(typeof(DataPacketHeader));
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
                var segment = _bufferManager.GetBuffer();
                receiveArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
                receiveArgs.UserToken = new ReceiveUserToken();
                _receiveArgsStack.Push(receiveArgs);
            }

            _sendArgsStack = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < nMaxSendReceive; i++)
            {
                SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += sendArgs_Completed;
                var segment = _bufferManager.GetBuffer();
                sendArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
                sendArgs.UserToken = new SendUserToken();
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
            if (args.UserToken != null)
            {
                receiveArgs.UserToken = args.UserToken;
            }

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
            ReceiveUserToken token = (ReceiveUserToken)args.UserToken;
            bool bDataPacketReaded = false;
            int nProcessedDataCount = 0;
            DataPacketHeader dph = new DataPacketHeader();
            DataPacket dp = new DataPacket();
            if (!token.IsHeaderReaded)
            {
                if (args.BytesTransferred >= nPacketHeaderSize)
                {
                    if (args.Buffer != null)
                    {
                        dph.Deserialize(args.Buffer, args.Offset, args.Count);
                        token.IsHeaderReaded = true;
                        token.ProcessedDataCount += nPacketHeaderSize;
                        nProcessedDataCount += nPacketHeaderSize;
                        token.DataPacketHeader = dph;
                    }
                }
            }
            else
            {
                dph = token.DataPacketHeader;
            }

            if (args.BytesTransferred >= token.ProcessedDataCount + dph.StringSize)
            {
                if (args.Buffer != null)
                {
                    byte[] buffer = new byte[dph.StringSize];
                    Buffer.BlockCopy(args.Buffer, args.Offset + Marshal.SizeOf(typeof(DataPacketHeader)), buffer, 0, dph.StringSize);

                    dp.Deserialize(buffer);
                    bDataPacketReaded = true;
                }
            }
            else
            {
                if (args.Buffer != null)
                {
                    byte[] buffer = null;
                    if (token.ReadData == null)
                    {
                        buffer = new byte[dph.StringSize];
                        token.ReadData = buffer;
                    }

                    Buffer.BlockCopy(args.Buffer, args.Offset + nProcessedDataCount, token.ReadData,
                        token.ReadDataOffset, args.BytesTransferred - nProcessedDataCount);
                    token.ReadDataOffset += args.BytesTransferred - nProcessedDataCount;
                    token.ProcessedDataCount += args.BytesTransferred - nProcessedDataCount;
                    if (token.ProcessedDataCount >= dph.StringSize + nPacketHeaderSize)
                    {
                        dp.Deserialize(token.ReadData);
                        bDataPacketReaded = true;
                    }
                }
            }

            if (bDataPacketReaded)
            {
                token.Reset();
                startSend(args, dp);
            }
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
                //if (args.UserToken != null)
                //{
                //    sendArgs.UserToken = args.UserToken;
                //}
                SendUserToken token = (SendUserToken)sendArgs.UserToken;
                token.DataPacket = dpForSend;
                sendDataPacket(sendArgs);
            }
        }

        private static void sendDataPacket(SocketAsyncEventArgs sendArgs)
        {
            SendUserToken token = (SendUserToken)sendArgs.UserToken;
            if (token.DataToSend == null)
            {
                DataPacket dp = token.DataPacket;
                byte[] dataBuffer = dp.Serialize();

                DataPacketHeader dph = new DataPacketHeader();

                dph.MagicKey = 0xFFACBBFA;
                dph.Version = 0x0100;
                dph.StringSize = dataBuffer.Length;

                byte[] headerBuffer = dph.Serialize();
                int nSize = dataBuffer.Length + headerBuffer.Length;
                byte[] packetBuffer = new byte[nSize];
                Buffer.BlockCopy(headerBuffer, 0, packetBuffer, 0, headerBuffer.Length);
                Buffer.BlockCopy(dataBuffer, 0, packetBuffer, headerBuffer.Length, dataBuffer.Length);
                token.DataToSend = packetBuffer;
                token.SentDataOffset = 0;
                token.ProcessedDataRemains = packetBuffer.Length;
            }
            if (token.ProcessedDataRemains <= nBufferSize && token.SentDataOffset == 0) //data packet fully fit in send buffer
            {
                Buffer.BlockCopy(token.DataToSend, 0, sendArgs.Buffer, sendArgs.Offset, token.DataToSend.Length);
                token.Reset();
            }
            else
            {// need to separate data packet into several buffers               

                int nLength = token.ProcessedDataRemains <= nBufferSize ? token.ProcessedDataRemains : nBufferSize;
                Buffer.BlockCopy(token.DataToSend, token.SentDataOffset, sendArgs.Buffer,
                    sendArgs.Offset + token.SentDataOffset, nLength);

                token.SentDataOffset += nLength;
                token.ProcessedDataRemains -= nLength;
            }

            sendArgs.AcceptSocket.SendAsync(sendArgs);
        }
        private void sendArgs_Completed(object sender, SocketAsyncEventArgs sendArgs)
        {
            SendUserToken token = (SendUserToken)sendArgs.UserToken;

            if (token.ProcessedDataRemains > 0)
            {
                SocketAsyncEventArgs sendArgsNew;
                if (!_sendArgsStack.TryPop(out sendArgsNew))
                {

                }
                sendArgsNew.AcceptSocket = sendArgs.AcceptSocket;
                sendArgsNew.UserToken = token;
                sendDataPacket(sendArgsNew);                
            }
            sendArgs.AcceptSocket = null;
            _sendArgsStack.Push(sendArgs);
        }
    }
}
