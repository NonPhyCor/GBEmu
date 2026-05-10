using System;
public class Bus
{
    private byte[] _fullRomData;
    private byte[] _vram=new byte[0x2000]; //8000-9FFF
    private byte[] _cartram=new byte[0x2000]; //A000-BFFF
    private byte[] _wram=new byte[0x2000]; //C000-DFFF
    private byte[] _io=new byte[0x80]; //FF00-FF7F
    private byte[] _hram=new byte[0x80]; //FF80-FFFE
    private byte _ie;
    private int _currentRomBank=1;
    private int DIV=0;
    private int ScanLines;
    public void LoadCartridge(byte[] RomData)
    {
        _fullRomData=RomData;
    }
    public byte Read(ushort address)
    {
        if(address<0x4000)  return _fullRomData[address];
        if(address<0x8000)
        {
            int offset=(_currentRomBank*0x4000)+(address-0x4000);
            return _fullRomData[offset];
        }
        if(address>=0x8000 && address<=0x9FFF)  return _vram[address-0x8000];
        if(address>=0xA000 && address<=0xBFFF)  return _cartram[address-0xA000];
        if(address>=0xC000 && address<=0xDFFF)  return _wram[address-0xC000];
        if(address>=0xFF00 && address<=0xFF7F)  return _io[address-0xFF00];
        if(address>=0xFF80 && address<=0xFFFE)  return _hram[address-0xFF80];
        if(address==0xFFFF) return _ie;
        return 0xFF;
    }
    public void Write(ushort address, byte data)
    {
        if(address==0xFF04)
        {
            this.DIV=0;
            _io[0x04]=0;
        }
        if(address>=0x2000 && address<=0x3FFF)
        {
            int bank=data&0x1F;
            _currentRomBank=(bank==0)?1:bank;

        }
        if(address>=0x8000 && address<=0x9FFF)  _vram[address-0x8000]=data;
        if(address>=0xA000 && address<=0xBFFF)  _cartram[address-0xA000]=data;
        if(address>=0xC000 && address<=0xDFFF)  _wram[address-0xC000]=data;
        if(address>=0xFF00 && address<=0xFF7F)  _io[address-0xFF00]=data;
        if(address>=0xFF80 && address<=0xFFFE)  _hram[address-0xFF80]=data;
        if(address==0xFFFF) _ie=data;
    }

    public void Tick(int t)
    {
        this.DIV+=t;
        if(this.DIV>=256)
        {
            this.DIV-=256;
            _io[0x04]+=(byte)1;
        }
        this.ScanLines+=t;
        if(this.ScanLines>=456)
        {
            this.ScanLines-=456;
            _io[0x44]+=(byte)1;
            if((int)_io[0x44]==144)
            {
                _io[0x0F]=(byte)(_io[0x0F]|1);
            }
            else if((int)_io[0x44]==154)
            {
                _io[0x44]=0;
            }
        }
    }
}