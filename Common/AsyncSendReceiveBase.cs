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

        }

        private void sendArgs_Completed(object sender, SocketAsyncEventArgs sendArgs)
        {

        }
    }
}
