using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Common
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DataPacketHeader
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

        public void Deserialize(byte[] data, int offset, int count)
        {
            byte[] newdata = new byte[count];
            Buffer.BlockCopy(data, offset, newdata, 0, count);

            GCHandle gcHandle = GCHandle.Alloc(newdata, GCHandleType.Pinned);
            this = (DataPacketHeader)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(DataPacketHeader));
            gcHandle.Free();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DataPacket
    {
        //Packet Data
        //[MarshalAs(UnmanagedType.)]
        public String Data;

        public byte[] Serialize()
        {           
            byte[] buffer = Encoding.UTF8.GetBytes(Data);         
            //byte[] buffer = new byte[Marshal.SizeOf(typeof(DataPacket))];

            //GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            //IntPtr pBuffer = gcHandle.AddrOfPinnedObject();

            //Marshal.StructureToPtr(this, pBuffer, false);
            //gcHandle.Free();

            return buffer;
        }

        public void Deserialize(byte[] data)
        {
            //GCHandle gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            //this = (DataPacket)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(DataPacket));
            //gcHandle.Free();
            Data = Encoding.UTF8.GetString(data);
        }
    }
}