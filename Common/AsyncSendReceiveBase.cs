using System;
using System.Net;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Common
{
    public abstract class AsyncSendReceiveBase
    {
        protected ConcurrentStack<SocketAsyncEventArgs> _receiveArgsStack;
        protected ConcurrentStack<SocketAsyncEventArgs> _sendArgsStack;
        protected BufferManager _bufferManager;

        protected const int nBufferSize = 20;
        protected const int nMaxSendReceive = 50000;

        protected AsyncSendReceiveBase()
        {
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
            //processPacket(receiveArgs);
            //startReceive(receiveArgs);
            receiveCompleted(receiveArgs);

            receiveArgs.AcceptSocket = null;
            _receiveArgsStack.Push(receiveArgs);

        }

        protected abstract void receiveCompleted(SocketAsyncEventArgs receiveArgs);
        

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
                //sendDataPacket(sendArgsNew);
                sendCompleted(sendArgsNew);
            }
            sendArgs.AcceptSocket = null;
            _sendArgsStack.Push(sendArgs);

        }

        protected abstract void sendCompleted(SocketAsyncEventArgs sendArgs);
       
    }
}
