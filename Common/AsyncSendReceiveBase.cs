using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

namespace Common
{
    public abstract class AsyncSendReceiveBase
    {
        protected ConcurrentStack<SocketAsyncEventArgs> _receiveArgsStack;
        protected ConcurrentStack<SocketAsyncEventArgs> _sendArgsStack;
        protected BufferManager _bufferManager;

        protected const int nBufferSize = 20;
        protected const int nMaxSendReceive = 50000;
        protected readonly int nPacketHeaderSize;

        protected AsyncSendReceiveBase()
        {
            nPacketHeaderSize = Marshal.SizeOf(typeof(DataPacketHeader));

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
        public void Initialize()
        {

        }
        protected void startReceive(SocketAsyncEventArgs args)
        {
            SocketAsyncEventArgs receiveArgs;
            if (!_receiveArgsStack.TryPop(out receiveArgs))
            {

            }
            Debug.Assert(receiveArgs.UserToken.GetType() == typeof(ReceiveUserToken));

            receiveArgs.AcceptSocket = args.AcceptSocket;
            if (args.UserToken != null)
            {
                Debug.Assert(args.UserToken.GetType() == typeof(ReceiveUserToken));
                receiveArgs.UserToken = args.UserToken;
            }

            bool willRaiseEvent = receiveArgs.AcceptSocket.ReceiveAsync(receiveArgs);
        }
        protected virtual void onReceiveError(SocketAsyncEventArgs receiveArgs)
        {

        }

        private void receiveArgs_Completed(object sender, SocketAsyncEventArgs receiveArgs)
        {
            if (receiveArgs.SocketError != SocketError.Success)
            {
                onReceiveError(receiveArgs);
                return;
            }

            Debug.WriteLine("receiveArgs_Completed");
            if (receiveArgs.BytesTransferred == 0)
            {
                return;
            }

            Debug.WriteLine("transferred = {0}", receiveArgs.BytesTransferred);
         
            receiveCompleted(receiveArgs);

            receiveArgs.AcceptSocket = null;
            _receiveArgsStack.Push(receiveArgs);

        }
  
        protected virtual void receiveCompleted(SocketAsyncEventArgs receiveArgs)
        {
            processPacket(receiveArgs);
            startReceive(receiveArgs);
        }        

        protected abstract void sendCompleted(SocketAsyncEventArgs sendArgs);

        protected void processPacket(SocketAsyncEventArgs args)
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

                    int nCount = args.BytesTransferred - nProcessedDataCount;
                    nCount = nCount > (dph.StringSize - token.ReadDataOffset) ? dph.StringSize - token.ReadDataOffset : nCount;
                    Buffer.BlockCopy(args.Buffer, args.Offset + nProcessedDataCount, token.ReadData,
                        token.ReadDataOffset, nCount);

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
                onDataPacketReaded(args, dp);                
            }
        }
        protected virtual void onDataPacketReaded(SocketAsyncEventArgs args, DataPacket dp)
        {            
        }

        protected void sendDataPacket(SocketAsyncEventArgs sendArgs)
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
                //ZeroMemory )) for test purposes
                byte[] testByte = new byte[nBufferSize];
                Buffer.BlockCopy(testByte, 0, sendArgs.Buffer, sendArgs.Offset, nBufferSize);

                Buffer.BlockCopy(token.DataToSend, 0, sendArgs.Buffer, sendArgs.Offset, token.DataToSend.Length);
                token.Reset();
            }
            else
            {// need to separate data packet into several buffers               

                //ZeroMemory )) for test purposes
                byte[] testByte = new byte[nBufferSize];
                Buffer.BlockCopy(testByte, 0, sendArgs.Buffer, sendArgs.Offset, nBufferSize);

                int nLength = token.ProcessedDataRemains <= nBufferSize ? token.ProcessedDataRemains : nBufferSize;
                Buffer.BlockCopy(token.DataToSend, token.SentDataOffset, sendArgs.Buffer,
                    sendArgs.Offset, nLength);

                token.SentDataOffset += nLength;
                token.ProcessedDataRemains -= nLength;
                if (token.ProcessedDataRemains == 0)
                {
                    token.Reset();
                }
            }
            byte[] testByte2 = new byte[nBufferSize];
            Buffer.BlockCopy(sendArgs.Buffer, sendArgs.Offset, testByte2, 0, nBufferSize);
            string strOut = Encoding.UTF8.GetString(testByte2);

            Debug.WriteLine("Send = {0}", strOut);

            sendArgs.AcceptSocket.SendAsync(sendArgs);
        }

        protected virtual void onSendError(SocketAsyncEventArgs sendArgs)
        {

        }
        private void sendArgs_Completed(object sender, SocketAsyncEventArgs sendArgs)
        {
            if (sendArgs.SocketError != SocketError.Success)
            {
                onSendError(sendArgs);
                return;
            }

            SendUserToken token = (SendUserToken)sendArgs.UserToken;

            if (token.ProcessedDataRemains > 0)
            {
                SocketAsyncEventArgs sendArgsNew;
                if (!_sendArgsStack.TryPop(out sendArgsNew))
                {

                }
                Debug.Assert(sendArgsNew.UserToken.GetType() == typeof(SendUserToken));

                sendArgsNew.AcceptSocket = sendArgs.AcceptSocket;
                sendArgsNew.UserToken = token;

                sendCompleted(sendArgsNew);
            }

            sendArgs.AcceptSocket = null;
            sendArgs.UserToken = new SendUserToken();
            _sendArgsStack.Push(sendArgs);
        }
    }
}
