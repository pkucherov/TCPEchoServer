using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

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

        private void connectArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            
        }
    }
}
