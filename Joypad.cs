using System;

public class Joypad
{
    public Bus _bus;
    private byte _actionButtons=0x0F;
    private byte _directionButtons=0x0F;
    private byte _select=0x30;
    public Joypad(Bus bus)
    {
        _bus=bus;
    }
    public byte Read()
    {
        byte result=0xCF;
        result&=(byte)_select;
        if((_select&0x20)==0)
            result&=_actionButtons;
        if((_select&0x10)==0)
            result&=_directionButtons;
        return result;
    }
    public void Write(byte data)
    {
        _select=(byte)(data&0x30);
    }
    public void UpdateKey(GameboyKey key, bool pressed)
    {
        bool wasUnpressed=false;
        switch(key)
        {
            case GameboyKey.A:  wasUnpressed=ModifyButtons(ref _actionButtons,0,pressed);break;
            case GameboyKey.B:  wasUnpressed=ModifyButtons(ref _actionButtons,1,pressed);break;
            case GameboyKey.Select:  wasUnpressed=ModifyButtons(ref _actionButtons,2,pressed);break;
            case GameboyKey.Start:  wasUnpressed=ModifyButtons(ref _actionButtons,3,pressed);break;

            case GameboyKey.Right:  wasUnpressed=ModifyButtons(ref _directionButtons,0,pressed);break;
            case GameboyKey.Left:  wasUnpressed=ModifyButtons(ref _directionButtons,1,pressed);break;
            case GameboyKey.Up:  wasUnpressed=ModifyButtons(ref _directionButtons,2,pressed);break;
            case GameboyKey.Down:  wasUnpressed=ModifyButtons(ref _directionButtons,3,pressed);break;
        }

        if(pressed&&wasUnpressed)
        {
            byte ifReg=_bus.Read(0xFF0F);
            ifReg|=0x10;
            _bus.Write(0xFF0F,ifReg);
        }
    }
    private bool ModifyButtons(ref byte buttonGroup,int bit,bool pressed)
    {
        bool previouslyUnpressed=(buttonGroup&(1<<bit))!=0;
        if(pressed)
            buttonGroup&=(byte)~(1<<bit);
        else
            buttonGroup|=(byte)(1<<bit);
        return previouslyUnpressed;
    }
}

public enum GameboyKey
{
    Up, Down, Left, Right, A, B, Start, Select
}