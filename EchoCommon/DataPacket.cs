using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Common
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DataPacketHeader : IDataPacketHeader
    {
        //Packet Header
        [MarshalAs(UnmanagedType.U4)]
        public uint MagicKey;
        [MarshalAs(UnmanagedType.U4)]
        public uint Version;
        [MarshalAs(UnmanagedType.U4)]
        public int StringSize;

        public byte[] Serialize()
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(DataPacketHeader))];

            GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr pBuffer = gcHandle.AddrOfPinnedObject();

            Marshal.StructureToPtr(this, pBuffer, false);
            gcHandle.Free();

            return buffer;
        }

        public void Deserialize(byte[] newdata)
        {
            GCHandle gcHandle = GCHandle.Alloc(newdata, GCHandleType.Pinned);
            this = (DataPacketHeader)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(DataPacketHeader));
            gcHandle.Free();
        }

        public int DataPacketSize
        {
            get { return StringSize; }
            set { StringSize = value; }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DataPacket : IDataPacket
    {
        public String Data;

        public byte[] Serialize()
        {
            byte[] buffer = Encoding.UTF8.GetBytes(Data);

            return buffer;
        }

        public void Deserialize(byte[] data)
        {
            Data = Encoding.UTF8.GetString(data);
        }
    }
}