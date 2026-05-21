using System;
public class Bus
{
    private byte[] _fullRomData;
    private byte[] _vram=new byte[0x2000]; //8000-9FFF
    private byte[] _cartram=new byte[0x2000]; //A000-BFFF
    private byte[] _wram=new byte[0x2000]; //C000-DFFF
    private byte[] _oam=new byte[0xA0]; //FE00-FE9F
    private byte[] _io=new byte[0x80]; //FF00-FF7F
    private byte[] _hram=new byte[0x80]; //FF80-FFFE
    private byte _ie;
    private int _currentRomBank=1;
    private int DIV=0;
    private int ScanLines=0;
    private int timerCounter=0;
    public Joypad JoypadController;
    public Bus()
    {
        JoypadController = new Joypad(this);
    }
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
            if(offset<_fullRomData.Length)
                return _fullRomData[offset];
            else
                return 0xFF;
        }
        if(address>=0x8000 && address<=0x9FFF)  return _vram[address-0x8000];
        if(address>=0xA000 && address<=0xBFFF)  return _cartram[address-0xA000];
        if(address>=0xC000 && address<=0xDFFF)  return _wram[address-0xC000];
        if(address>=0xE000 && address<=0xFDFF)  return _wram[address-0xE000];
        if(address>=0xFE00 && address<=0xFE9F)  return _oam[address-0xFE00];
        if(address==0xFF00) return (byte)(_io[0x00] | 0x0F);
        if(address==0xFF00) return JoypadController.Read();
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
        if (address == 0xFF00)
        {
            JoypadController.Write(data);
            return;
        }
        if(address>=0x2000 && address<=0x3FFF)
        {
            int bank=data&0x7F;
            _currentRomBank=(bank==0)?1:bank;

        }
        if(address>=0x8000 && address<=0x9FFF)  _vram[address-0x8000]=data;
        if(address>=0xA000 && address<=0xBFFF)  _cartram[address-0xA000]=data;
        if(address>=0xC000 && address<=0xDFFF)  _wram[address-0xC000]=data;
        if(address>=0xE000 && address<=0xFDFF)  _wram[address-0xE000]=data;
        if(address>=0xFE00 && address<=0xFE9F)  _oam[address-0xFE00]=data;
        if(address==0xFF46)
        {
            _io[46]=data;
            ushort sourceAdress=(ushort)(data<<8);
            for(int i=0;i<0xA0;i++)
                _oam[i]=Read((ushort)(sourceAdress+i));
            return;
        }
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
        this.timerCounter+=t;
        int threshold=0;
        if((_io[0x07]&0x04)!=0)
        {
            switch((int)(_io[07]&0x03))
            {
                case 0: threshold=1024;break;
                case 1: threshold=16;break;
                case 2: threshold=64;break;
                case 3: threshold=256;break;
            }
            if(this.timerCounter>=threshold)
            {
                this.timerCounter-=threshold;
                _io[0x05]+=1;
                if(_io[0x05]==0)
                {
                    _io[0x05]=_io[0x06];
                    _io[0x0F]=(byte)(_io[0x0F]|0x04);
                }
            }
        }
    }
}