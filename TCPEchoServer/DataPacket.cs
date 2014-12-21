using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DataPacket
{
    //Packet Header
    [MarshalAs(UnmanagedType.U4)]
    public uint MagicKey;
    [MarshalAs(UnmanagedType.U4)]
    public uint Version;
    [MarshalAs(UnmanagedType.U4)]
    public uint StringSize;
    //Packet Data
    [MarshalAs(UnmanagedType.BStr)]
    public String Data;
           
    public byte[] Serialize()
    {        
        byte[] buffer = new byte[Marshal.SizeOf(typeof(DataPacket))];
        
        GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        IntPtr pBuffer = gcHandle.AddrOfPinnedObject();
                
        Marshal.StructureToPtr(this, pBuffer, false);
        gcHandle.Free();

        return buffer;
    }
    
    public void Deserialize(ref byte[] data)
    {
        GCHandle gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        this = (DataPacket)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(DataPacket));
        gcHandle.Free();
    }
}