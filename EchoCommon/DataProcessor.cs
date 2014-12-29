using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
   
    public class DataProcessor : IDataProcessor
    {
        public IDataPacketHeader CreateDataPacketHeader()
        {
            DataPacketHeader dph = new DataPacketHeader();
            dph.MagicKey = 0xFFACBBFA;
            dph.Version = 0x0100;
            return dph;
        }

        public IDataPacket CreateDataPacket()
        {
            return new DataPacket();
        }

        public int DataPacketHeaderSize
        {
            get { return Marshal.SizeOf(typeof(DataPacketHeader)); }
        }
    }

   
}
