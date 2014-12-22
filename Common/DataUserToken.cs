using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ReceiveUserToken
    {
        public int ProcessedDataCount { get; set; }
        public bool IsHeaderReaded { get; set; }
        public DataPacketHeader DataPacketHeader { get; set; }
        public byte[] ReadData { get; set; }
        public int ReadDataOffset { get; set; }

        public ReceiveUserToken()
        {
            ProcessedDataCount = 0;
            IsHeaderReaded = false;
        }

        public void Reset()
        {
            IsHeaderReaded = false;
            ProcessedDataCount = 0;
            ReadData = null;
            ReadDataOffset = 0;
        }
    }

    public class SendUserToken
    {
        public int ProcessedDataRemains { get; set; }        
        public byte[] DataToSend { get; set; }
        public int SentDataOffset { get; set; }

        public DataPacket DataPacket { get; set; }

        public SendUserToken()
        {
            ProcessedDataRemains = 0;           
        }

        public void Reset()
        {
            ProcessedDataRemains = 0;
            DataToSend = null;
            SentDataOffset = 0;            
        }
    }
}
