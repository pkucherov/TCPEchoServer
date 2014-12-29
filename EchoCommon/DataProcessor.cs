using System.Data;
using System.Runtime.InteropServices;

namespace Common
{
    public class DataProcessor : IDataProcessor
    {
        private const uint NMagicKey = 0xFFACBBFA;
        private const uint NVersion = 0x0100;
        public IDataPacketHeader CreateDataPacketHeader()
        {
            DataPacketHeader dph = new DataPacketHeader();
            dph.MagicKey = NMagicKey;
            dph.Version = NVersion;
            return dph;
        }
        public IDataPacket CreateDataPacket()
        {
            return new DataPacket();
        }

        public bool IsValidHeader(IDataPacketHeader header)
        {
            DataPacketHeader dph = (DataPacketHeader) header;
            return dph.MagicKey == NMagicKey && dph.Version == NVersion;
        }

        public int DataPacketHeaderSize
        {
            get { return Marshal.SizeOf(typeof(DataPacketHeader)); }
        }
    }
}
