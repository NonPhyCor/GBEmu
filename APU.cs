using System;

public class APU
{
    private Bus _bus;
    private byte[] _apuRegisters=new byte[0x30];
    private byte[] _waveRAM=new byte[0x10];
    public bool MasterSoundEnable { get; private set; } = true;
    private bool _channel1Enabled=false;
    private int _channel1Timer=0;
    private int _channel1DutyIndex=0;
    private int _channel1Volume=0;
    private int Channel1DutyPattern=>(_apuRegisters[0x11-0x10]>>6)&0x03;
    private int Channel1Frequency=>(_apuRegisters[0x13-0x10]|((_apuRegisters[0x14-0x10]&0x07)<<8));
    private int _channel1EnvTimer=0;
    private int _channel1EnvPace=0;
    private bool _channel1EnvIncrease=false;
    private static readonly int[,] DutyCycles =
    {
        { 0, 0, 0, 0, 0, 0, 0, 1 }, 
        { 1, 0, 0, 0, 0, 0, 0, 1 }, 
        { 1, 0, 0, 0, 0, 1, 1, 1 }, 
        { 0, 1, 1, 1, 1, 1, 1, 0 }
    };
    private int _frameSequenceTimer=8192;
    private int _frameSequencerStep=0;
    public APU(Bus bus)
    {
        _bus=bus;
    }

    public byte Read(ushort address)
    {
        if(address>=0xFF10 && address<=0xFF2F)
            return _apuRegisters[address-0xFF10];
        else if(address>=0xFF30 && address<=0xFF3F)
            return _waveRAM[address-0xFF30];
        return 0xFF;
    }
    public void Write(ushort address, byte data)
    {
        if(address==0xFF26)
        {
            MasterSoundEnable=(data&0x80)!=0;
            if(!MasterSoundEnable)
                Array.Clear(_apuRegisters,0,_apuRegisters.Length);
            _apuRegisters[0x16]=(byte)(data&0x80);
            return;
        }
        if(!MasterSoundEnable && address<0xFF30)
            return;
        if(address>=0xFF10 && address<=0xFF2F)
        {
            _apuRegisters[address-0xFF10]=data;
            if(address==0xFF14)
            {
                if((data&0x80)!=0)
                {
                    _channel1Enabled=true;
                    _channel1Timer=(2048-Channel1Frequency)*4;
                    _channel1Volume=(_apuRegisters[0x12-0x10]>>4)&0x0F;
                    _channel1EnvPace=_apuRegisters[0x12-0x10]&0x07;
                    _channel1EnvIncrease=(_apuRegisters[0x12-0x10]&0x08)!=0;
                    if(_channel1EnvPace==0) _channel1EnvTimer=8;
                    else    _channel1EnvTimer=_channel1EnvPace;
                }
            }
        }
        else if(address>=0xFF30 && address<=0xFF3F)
            _waveRAM[address-0xFF30]=data;

    }
    public int GetChannelSample()
    {
        if(!_channel1Enabled)
            return 0;
        int pattern=Channel1DutyPattern;
        int stepValue=DutyCycles[pattern,_channel1DutyIndex];
        return stepValue*_channel1Volume;
    }
    public void Tick(int cycles)
    {
        if(!MasterSoundEnable)
            return;
        if(_channel1Enabled)
        {
            _channel1Timer-=cycles;
            if(_channel1Timer<=0)
            {
                _channel1Timer+=(2048-Channel1Frequency)*4;
                _channel1DutyIndex=(_channel1DutyIndex+1)%8;
            }
        }
        _frameSequenceTimer-=cycles;
        if(_frameSequenceTimer<=0)
        {
            _frameSequenceTimer+=8192;
            _frameSequencerStep=(_frameSequencerStep+1)%8;
            if(_frameSequencerStep==7)
            {
                if(_channel1EnvPace!=0)
                {
                    _channel1EnvTimer-=1;
                    if(_channel1EnvTimer<=0)
                    {
                        _channel1EnvTimer=_channel1EnvPace;
                        if(_channel1EnvIncrease==true && _channel1Volume<15)    _channel1Volume+=1;
                        else if(_channel1EnvIncrease==false && _channel1Volume>0)   _channel1Volume-=1;
                    }
                }
            }
        }
    }
}