namespace Common
{
    public interface IDataProcessor
    {
        int DataPacketHeaderSize { get; }

        IDataPacketHeader CreateDataPacketHeader();
        IDataPacket CreateDataPacket();
        bool IsValidHeader(IDataPacketHeader header);
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
