using System.Runtime.InteropServices;

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
