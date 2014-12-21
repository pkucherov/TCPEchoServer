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

        public void SendData(string strData)
        {
            //SocketAsyncEventArgs receiveSendEventArgs = new SocketAsyncEventArgs();
            //receiveSendEventArgs.AcceptSocket = connectArgs.ConnectSocket;
            //receiveSendEventArgs.Completed += receiveSendEventArgs_Completed;
            //startSend(receiveSendEventArgs);
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

            startReceive(receiveSendEventArgs);
        }

        private void startReceive(SocketAsyncEventArgs args)
        {
            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += receiveArgs_Completed;
            receiveArgs.AcceptSocket = args.AcceptSocket;
            receiveArgs.UserToken = "abb";
            //args.AcceptSocket = null;
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

            Console.WriteLine(dp.Data);
        }
    }
}
