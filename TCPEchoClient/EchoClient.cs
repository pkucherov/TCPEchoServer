using System;
using System.Net;
using System.Net.Sockets;
using Common;
using System.Runtime.InteropServices;


namespace TCPEchoClient
{
    class EchoClient
    {
        public EchoClient()
        {

        }

        public void Connect(IPEndPoint ipe)
        {
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.RemoteEndPoint = ipe;
            connectArgs.Completed += connectArgs_Completed;
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ConnectAsync(connectArgs);

        }

        private void connectArgs_Completed(object sender, SocketAsyncEventArgs connectArgs)
        {
            Console.WriteLine("connectArgs_Completed");
            if (connectArgs.SocketError != SocketError.Success)
            {
                return;
            }

            SocketAsyncEventArgs receiveSendEventArgs = new SocketAsyncEventArgs();
            receiveSendEventArgs.AcceptSocket = connectArgs.ConnectSocket;
            receiveSendEventArgs.Completed += receiveSendEventArgs_Completed;
            startSend(receiveSendEventArgs);
        }

        void receiveSendEventArgs_Completed(object sender, SocketAsyncEventArgs receiveSendEventArgs)
        {
            if (receiveSendEventArgs.SocketError != SocketError.Success)
            {
                return;
            }
            Console.WriteLine("receiveSendEventArgs_Completed");
        }

        private void startSend(SocketAsyncEventArgs receiveSendEventArgs)
        {
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

            // BlockCopy(Array src, int srcOffset, Array dst, int dstOffset, int count);
            Buffer.BlockCopy(headerBuffer, 0, messageBuffer, 0, headerBuffer.Length);
            Buffer.BlockCopy(dataBuffer, 0, messageBuffer, headerBuffer.Length, dataBuffer.Length);
            receiveSendEventArgs.SetBuffer(messageBuffer, 0, nSize);
                        
            receiveSendEventArgs.AcceptSocket.SendAsync(receiveSendEventArgs);             

        }
    }
}
