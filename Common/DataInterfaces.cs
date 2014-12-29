using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public interface IDataProcessor
    {
        IDataPacketHeader CreateDataPacketHeader();
        IDataPacket CreateDataPacket();

        int DataPacketHeaderSize { get; }
    }

    public interface IDataPacket
    {
        void Deserialize(byte[] data);
        byte[] Serialize();

    }

    public interface IDataPacketHeader
    {
        int DataPacketSize { get; set; }

        void Deserialize(byte[] data);

        byte[] Serialize();
    }
}
