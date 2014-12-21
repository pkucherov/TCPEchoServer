using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class DataUserToken
    {
        public int ProcessedDataCount { get; set; }
        public bool IsHeaderReaded { get; set; }
        public DataPacketHeader DataPacketHeader { get; set; }
        public byte[] ReadedData { get; set; }
        public int ReadedDataOffset { get; set; }

        public DataUserToken()
        {
            ProcessedDataCount = 0;
            IsHeaderReaded = false;
        }

        public void Reset()
        {
            IsHeaderReaded = false;
            ProcessedDataCount = 0;
            ReadedData = null;
            ReadedDataOffset = 0;
        }
    }
}
