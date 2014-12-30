using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using Common;

namespace TCPEchoServer
{
    class EchoServer: BaseServer
    {   

        public EchoServer()
        {            
        }
        protected override void onDataPacketReaded(SocketAsyncEventArgs args, IDataPacket dp)
        {
            Debug.WriteLine("server received = {0}", ((DataPacket)dp).Data);
            startSend(dp);
        }

        private void startSend(IDataPacket dpForSend)
        {
            Task.Factory.StartNew(() =>
            {
                lock (((ICollection)_clientCollection).SyncRoot)
                {
                    List<Socket> aSocketsToDelete = new List<Socket>();
                    foreach (Socket client in _clientCollection)
                    {
                        if (client.Connected)
                        {
                            SocketAsyncEventArgs sendArgs;
                            if (!_sendArgsStack.TryPop(out sendArgs))
                            {
                                sendArgs = createSendAsyncEventArgs();
                            }

                            Debug.Assert(sendArgs.UserToken.GetType() == typeof(SendUserToken));
                            sendArgs.AcceptSocket = client;

                            SendUserToken token = (SendUserToken)sendArgs.UserToken;
                            token.DataPacket = dpForSend;
                            sendDataPacket(sendArgs);
                        }
                        else
                        {
                            aSocketsToDelete.Add(client);
                        }
                    }
                    for (int i = 0; i < aSocketsToDelete.Count; i++)
                    {
                        Socket socket = aSocketsToDelete[i];
                        _clientCollection.Remove(socket);
                    }
                }
            });
        }
    }
}
