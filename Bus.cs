using System;
public class Bus
{
    //64kb of memory
    private byte[] Memory=new byte[0x10000];

    //load data
    public void LoadCartridge(byte[] RomData)
    {
        int SizeToCopy=Math.Min(0x8000,RomData.Length);
        Array.Copy(RomData,0,Memory,0,SizeToCopy);
    }
    //read data
    public byte Read(ushort address)
    {
        return Memory[address];
    }

    //write data
    public void Write(ushort address, byte data)
    {
        if(address<0x8000)
            return;
        Memory[address]=data;
    }
}