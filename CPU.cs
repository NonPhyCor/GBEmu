using System;
public class CPU
{
    private Bus _bus;
    public byte A,B,C,D,E,H,L;
    public byte F;
    public ushort PC,SP;
    public bool IME;
    private bool _pendingIME;
    public bool Halted;
    //F Flags
    private const byte Z_FLAG=0x80;
    private const byte N_FLAG=0x40;
    private const byte H_FLAG=0x20;
    private const byte C_FLAG=0x10;

    public void SetFlag(byte flag, bool state)
    {
        if(state)
            F|=flag;
        else
            F&=(byte)~flag;
    }

    public bool GetFlag(byte flag)
    {
        return (F&flag)!=0;
    }

    public CPU(Bus bus)
    {
        _bus=bus;
        PC=0x100;
        SP=0xFFFE;
    }

    public ushort BC
    {
        get=>(ushort)((B<<8)|C);
        set
        {
            B=(byte)(value>>8);
            C=(byte)(value&0xFF);
        }
    }

    public ushort DE
    {
        get=>(ushort)((D<<8)|E);
        set
        {
            D=(byte)(value>>8);
            E=(byte)(value&0xFF);
        }
    }

    public ushort HL
    {
        get=>(ushort)((H<<8)|L);
        set
        {
            H=(byte)(value>>8);
            L=(byte)(value&0xFF);
        }
    }

    public ushort AF
    {
        get=>(ushort)((A<<8)|F);
        set
        {
            A=(byte)(value>>8);
            F=(byte)(value&0xF0);
        }
    }

    public void StackPush(ushort data)
    {
        byte high=(byte)(data>>8);
        byte low=(byte)(data&0xFF);
        _bus.Write(--SP,high);
        _bus.Write(--SP,low);
    }

    public ushort StackPop()
    {
        byte low=_bus.Read(SP++);
        byte high=_bus.Read(SP++);
        return (ushort)((high<<8)|low);
    }

    public void Fetch()
    {
        byte val;
        sbyte sval;
        byte opcode = Read8Bit();
        ushort a;
        switch (opcode)
        {
            case 0x00:
                break;
            case 0x01:
                this.BC = Read16Bit();
                break;
            case 0x02:  _bus.Write(BC,A);break;
            case 0x03:
                BC++;
                break;
            case 0x04:  B=Inc8(B);break;
            case 0x05:  B=Dec8(B);break;
            case 0x06:  B=Read8Bit();break;
            case 0x07:
                SetFlag(C_FLAG, (A & 0x80) != 0);
                A = (byte)((A << 1) | (A >> 7));
                SetFlag(Z_FLAG, false);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                break;
            case 0x08:
                ushort add=Read16Bit();
                _bus.Write(add,(byte)(SP&0xFF));
                _bus.Write((ushort)(add+1),(byte)(SP>>8));
                break;
            case 0x09:
                Add16(BC);
                break;
            case 0x0A:  A=_bus.Read(BC);break;
            case 0x0B:  BC--;break;
            case 0x0C:  C=Inc8(C);break;
            case 0x0D:  C=Dec8(C);break;
            case 0x0E:  C = Read8Bit();break;
            case 0x0F:
                SetFlag(C_FLAG, (A & 0x01) != 0);
                A = (byte)((A >> 1) | (A << 7));
                SetFlag(Z_FLAG, false);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                break;
            case 0x10:  Read8Bit(); break;
            case 0x11:  DE=Read16Bit();break;
            case 0x12:  _bus.Write(DE,A);break;
            case 0x13:  DE++;break;
            case 0x14:  D=Inc8(D);break;
            case 0x15:  D=Dec8(D);break;
            case 0x16:  D = Read8Bit();break;
            case 0x17:
                val = (byte)(GetFlag(C_FLAG) ? 1 : 0);
                SetFlag(C_FLAG, (A & 0x80) != 0);
                A = (byte)((A << 1) | val);
                SetFlag(Z_FLAG, false);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                break;
            case 0x18:
                PC = (ushort)(PC + Read8SignedBit());
                break;
            case 0x19:  Add16(DE);break;
            case 0x1A:  A=_bus.Read(DE);break;
            case 0x1B:  DE--;break;
            case 0x1C:  E=Inc8(E);break;
            case 0x1D:  E=Dec8(E);break;
            case 0x1E:  E = Read8Bit();break;
            case 0x1F:
                val = (byte)(GetFlag(C_FLAG) ? 1 : 0);
                SetFlag(C_FLAG, (A & 0x01) != 0);
                A = (byte)((A >> 1) | val << 7);
                SetFlag(Z_FLAG, false);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                break;
            case 0x20:
                sval = Read8SignedBit();
                if (!GetFlag(Z_FLAG))
                    PC = (ushort)(PC + sval);
                break;
            case 0x21:  HL=Read16Bit();break;
            case 0x22:  _bus.Write(HL,A);HL++;break;
            case 0x23:  HL++;break;
            case 0x24:  H=Inc8(H);break;
            case 0x25:  H=Dec8(H);break;
            case 0x26:  H=Read8Bit();break;
            case 0x27:
                ushort res = A;
                if (!GetFlag(N_FLAG))
                {
                    if (GetFlag(H_FLAG) || (res & 0x0F) > 0x09) res += 0x06;
                    if (GetFlag(C_FLAG) || res > 0x9F)
                    {
                        res += 0x60;
                        SetFlag(C_FLAG, true);
                    }
                }
                else
                {
                    if (GetFlag(H_FLAG)) res -= 0x06;
                    if (GetFlag(C_FLAG)) res -= 0x60;
                }
                A = (byte)res;
                SetFlag(Z_FLAG, A == 0);
                SetFlag(H_FLAG, false);
                break;
            case 0x28:
                sval = Read8SignedBit();
                if (GetFlag(Z_FLAG))
                    PC = (ushort)(PC + sval);
                break;
            case 0x29:  Add16(HL);break;
            case 0x2A:  A = _bus.Read(HL); HL++;break;
            case 0x2B:  HL--;break;
            case 0x2C:  L=Inc8(L);break;
            case 0x2D:  L=Dec8(L);break;
            case 0x2E:  L=Read8Bit();break;
            case 0x2F:
                A = (byte)~A;
                SetFlag(N_FLAG, true);
                SetFlag(H_FLAG, true);
                break;
            case 0x30:
                sval = Read8SignedBit();
                if (!GetFlag(C_FLAG))
                    PC = (ushort)(PC + sval);
                break;
            case 0x31:  SP = Read16Bit();break;
            case 0x32:  _bus.Write(HL, A);HL--;break;
            case 0x33:  SP++;break;
            case 0x34:  _bus.Write(HL, Inc8(_bus.Read(HL)));break;
            case 0x35:  _bus.Write(HL, Dec8(_bus.Read(HL)));break;
            case 0x36:  _bus.Write(HL,Read8Bit());break;
            case 0x37:
                SetFlag(C_FLAG, true);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                break;
            case 0x38:
                sval = Read8SignedBit();
                if (GetFlag(C_FLAG))
                    PC = (ushort)(PC + sval);
                break;
            case 0x39:  Add16(SP);break;
            case 0x3A:  A=_bus.Read(HL);HL--;break;
            case 0x3B:  SP--;break;
            case 0x3C:
                SetFlag(H_FLAG, (A & 0x0F) == 0x0F);
                A++;
                SetFlag(Z_FLAG, A == 0);
                SetFlag(N_FLAG, false);
                break;
            case 0x3D:  A=Dec8(A);break;
            case 0x3E:  A=Read8Bit();break;
            case 0x3F:
                SetFlag(C_FLAG, !GetFlag(C_FLAG));
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                break;
            case 0x40:  B=B; break;
            case 0x41:  B=C; break;
            case 0x42:  B=D; break;
            case 0x43:  B=E; break;
            case 0x44:  B=H; break;
            case 0x45:  B=L; break;
            case 0x46:  B=_bus.Read(HL); break;
            case 0x47:  B=A; break;
            case 0x48:  C=B; break;
            case 0x49:  C=C; break;
            case 0x4A:  C=D; break;
            case 0x4B:  C=E; break;
            case 0x4C:  C=H; break;
            case 0x4D:  C=L; break;
            case 0x4E:  C=_bus.Read(HL); break;
            case 0x4F:  C=A; break;
            case 0x50:  D=B; break;
            case 0x51:  D=C; break;
            case 0x52:  D=D; break;
            case 0x53:  D=E; break;
            case 0x54:  D=H; break;
            case 0x55:  D=L; break;
            case 0x56:  D=_bus.Read(HL); break;
            case 0x57:  D=A; break;
            case 0x58:  E=B;break;
            case 0x59:  E=C;break;
            case 0x5A:  E=D;break;
            case 0x5B:  E=E;break;
            case 0x5C:  E=H;break;
            case 0x5D:  E=L;break;
            case 0x5E:  E=_bus.Read(HL); break;
            case 0x5F:  E=A;break;
            case 0x60:  H=B;break;
            case 0x61:  H=C;break;
            case 0x62:  H=D;break;
            case 0x63:  H=E;break;
            case 0x64:  H=H;break;
            case 0x65:  H=L;break;
            case 0x66:  H=_bus.Read(HL); break;
            case 0x67:  H=A;break;
            case 0x68:  L=B;break;
            case 0x69:  L=C;break;
            case 0x6A:  L=D;break;
            case 0x6B:  L=E;break;
            case 0x6C:  L=H;break;
            case 0x6D:  L=L;break;
            case 0x6E:  L=_bus.Read(HL); break;
            case 0x6F:  L=A;break;
            case 0x70:  _bus.Write(HL, B); break;
            case 0x71:  _bus.Write(HL, C); break;
            case 0x72:  _bus.Write(HL, D); break;
            case 0x73:  _bus.Write(HL, E); break;
            case 0x74:  _bus.Write(HL, H); break;
            case 0x75:  _bus.Write(HL, L); break;
            case 0x76:  Halted=true;break;
            case 0x77:  _bus.Write(HL, A); break;
            case 0x78:  A=B; break;
            case 0x79:  A=C; break;
            case 0x7A:  A=D; break;
            case 0x7B:  A=E; break;
            case 0x7C:  A=H; break;
            case 0x7D:  A=L; break;
            case 0x7E:  A=_bus.Read(HL); break;
            case 0x7F:  A=A; break;
            case 0x80:  Add8(B);break;
            case 0x81:  Add8(C);break;
            case 0x82:  Add8(D);break;
            case 0x83:  Add8(E);break;
            case 0x84:  Add8(H);break;
            case 0x85:  Add8(L);break;
            case 0x86:  Add8(_bus.Read(HL));break;
            case 0x87:  Add8(A);break;
            case 0x88:  Adc8(B);break;
            case 0x89:  Adc8(C);break;
            case 0x8A:  Adc8(D);break;
            case 0x8B:  Adc8(E);break;
            case 0x8C:  Adc8(H);break;
            case 0x8D:  Adc8(L);break;
            case 0x8E:  Adc8(_bus.Read(HL));break;
            case 0x8F:  Adc8(A);break;
            case 0x90:  Sub8(B);break;
            case 0x91:  Sub8(C);break;
            case 0x92:  Sub8(D);break;
            case 0x93:  Sub8(E);break;
            case 0x94:  Sub8(H);break;
            case 0x95:  Sub8(L);break;
            case 0x96:  Sub8(_bus.Read(HL));break;
            case 0x97:  Sub8(A);break;
            case 0x98:  Sbc8(B);break;
            case 0x99:  Sbc8(C);break;
            case 0x9A:  Sbc8(D);break;
            case 0x9B:  Sbc8(E);break;
            case 0x9C:  Sbc8(H);break;
            case 0x9D:  Sbc8(L);break;
            case 0x9E:  Sbc8(_bus.Read(HL));break;
            case 0x9F:  Sbc8(A);break;
            case 0xA0: A &= B; SetFlags(true); break;
            case 0xA1: A &= C; SetFlags(true); break;
            case 0xA2: A &= D; SetFlags(true); break;
            case 0xA3: A &= E; SetFlags(true); break;
            case 0xA4: A &= H; SetFlags(true); break;
            case 0xA5: A &= L; SetFlags(true); break;
            case 0xA6: A &= _bus.Read(HL); SetFlags(true); break;
            case 0xA7: A &= A; SetFlags(true); break;
            case 0xA8: A ^= B; SetFlags(false); break;
            case 0xA9: A ^= C; SetFlags(false); break;
            case 0xAA: A ^= D; SetFlags(false); break;
            case 0xAB: A ^= E; SetFlags(false); break;
            case 0xAC: A ^= H; SetFlags(false); break;
            case 0xAD: A ^= L; SetFlags(false); break;
            case 0xAE: A ^= _bus.Read(HL); SetFlags(false); break;
            case 0xAF: A ^= A; SetFlags(false); break;
            case 0xB0: A |= B; SetFlags(false); break;
            case 0xB1: A |= C; SetFlags(false); break;
            case 0xB2: A |= D; SetFlags(false); break;
            case 0xB3: A |= E; SetFlags(false); break;
            case 0xB4: A |= H; SetFlags(false); break;
            case 0xB5: A |= L; SetFlags(false); break;
            case 0xB6: A |= _bus.Read(HL); SetFlags(false); break;
            case 0xB7: A |= A; SetFlags(false); break;
            case 0xB8:  Compare8(B);break;
            case 0xB9:  Compare8(C);break;
            case 0xBA:  Compare8(D);break;
            case 0xBB:  Compare8(E);break;
            case 0xBC:  Compare8(H);break;
            case 0xBD:  Compare8(L);break;
            case 0xBE:  Compare8(_bus.Read(HL));break;
            case 0xBF:  Compare8(A);break;
            case 0xC0:  if(!GetFlag(Z_FLAG)) PC = StackPop();break;
            case 0xC1:  BC=StackPop();break;
            case 0xC2:
                ushort addr = Read16Bit();
                if (!GetFlag(Z_FLAG))
                    PC = addr;
                break;
            case 0xC3:  PC=Read16Bit();break;
            case 0xC4:
                a = Read16Bit();
                if(!GetFlag(Z_FLAG))
                {
                    StackPush(PC);
                    PC = a;
                }
                break;
            case 0xC5:  StackPush(BC);break;
            case 0xC6:
                val = Read8Bit();
                SetFlag(H_FLAG, (A & 0x0F) + (val & 0x0F) > 0x0F);
                SetFlag(C_FLAG, A + val > 0xFF);
                A = (byte)(A + val);
                SetFlag(Z_FLAG, A == 0);
                SetFlag(N_FLAG, false);
                break;
            case 0xC7:
                StackPush(PC);
                PC=0x0000;
                break;
            case 0xC8:  if(GetFlag(Z_FLAG)) PC=StackPop();break;
            case 0xC9: PC = StackPop(); break;
            case 0xCA:  
                a = Read16Bit();
                if(GetFlag(Z_FLAG)) PC=a;
                break;
            case 0xCB:
                byte CBOC = Read8Bit();
                ExecuteCB(CBOC);
                break;
            case 0xCC:
                a = Read16Bit();
                if(GetFlag(Z_FLAG))
                {
                    StackPush(PC);
                    PC=a;
                }
                break;
            case 0xCD:
                ushort dest = Read16Bit();
                StackPush(PC);
                PC = dest;
                break;
            case 0xCE:  Adc8(Read8Bit());break;
            case 0xCF:
                StackPush(PC);
                PC=0x0008;
                break;
            case 0xD0:  if(!GetFlag(C_FLAG)) PC=StackPop();break;
            case 0xD1:  DE=StackPop();break;
            case 0xD2:
                a=Read16Bit();
                if(!GetFlag(C_FLAG)) PC=a;
                break;
            //case 0xD3: does not exist
            case 0xD4:
                a=Read16Bit();
                if(!GetFlag(C_FLAG))
                {
                    StackPush(PC);
                    PC=a;
                }
                break;
            case 0xD5:  StackPush(DE);break;
            case 0xD6:  Sub8(Read8Bit());break;
            case 0xD7:  StackPush(PC);PC=0x0010;break;
            case 0xD8:  if(GetFlag(C_FLAG)) PC=StackPop();break;
            case 0xD9:  PC = StackPop();IME=true;break;
            case 0xDA:
                a=Read16Bit();
                if(GetFlag(C_FLAG)) PC=a;
                break;
            //case 0xDB: does not exist
            case 0xDC:
                a=Read16Bit();
                if(GetFlag(C_FLAG))
                {
                    StackPush(PC);
                    PC=a;
                }
                break;
            //case 0xDD: does not exist
            case 0xDE:  Sbc8(Read8Bit());break;
            case 0xDF:  StackPush(PC);PC=0x0018;break;
            case 0xE0:
                _bus.Write((ushort)(0xFF00 + Read8Bit()), A);
                break;
            case 0xE1:  HL=StackPop();break;
            case 0xE2:
                _bus.Write((ushort)(0xFF00 + C), A);
                break;
            //case 0xE3: does not exisr
            //case 0xE4: does not exisr
            case 0xE5:  StackPush(HL);break;
            case 0xE6:  A&=Read8Bit();SetFlags(true);break;
            case 0xE7:  StackPush(PC);PC=0x0020;break;
            case 0xE8:
                sval = Read8SignedBit();
                SetFlag(Z_FLAG, false);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, (SP & 0x0F) + (sval & 0x0F) > 0x0F);
                SetFlag(C_FLAG, (SP & 0xFF) + (sval & 0xFF) > 0xFF);
                SP = (ushort)(SP + sval);
                break;
            case 0xE9:  PC=HL;break;
            case 0xEA:
                _bus.Write(Read16Bit(), A);
                break;
            case 0xEE:  A^=Read8Bit();SetFlags(false);break;
            case 0xEF:  StackPush(PC);PC=0x0028;break;
            case 0xF0:
                A = _bus.Read((ushort)(0xFF00 + Read8Bit()));
                break;
            case 0xF1:  AF=StackPop();break;
            case 0xF2:
                A = _bus.Read((ushort)(0xFF00 + C));
                break;
            case 0xF3:
                IME = false;
                _pendingIME = false;
                break;
            //case 0xF4: does not exist
            case 0xF5:  StackPush(AF);break;
            case 0xF6:  A|=Read8Bit();SetFlags(false);break;
            case 0xF7:  StackPush(PC);PC=0x0030;break;
            case 0xF8:
                sval=Read8SignedBit();
                HL=(ushort)(SP+sval);
                SetFlag(Z_FLAG,false);
                SetFlag(N_FLAG,false);
                SetFlag(H_FLAG,(SP&0x0F)+(sval&0x0F)>0x0F);
                SetFlag(C_FLAG,(SP&0xFF)+(sval&0xFF)>0xFF);
                break;
            case 0xF9: SP = HL; break;
            case 0xFA:
                A = _bus.Read(Read16Bit());
                break;
            case 0xFB:  _pendingIME=true;break;
            case 0xFE:
                val = Read8Bit();
                SetFlag(Z_FLAG, A == val);
                SetFlag(N_FLAG, true);
                SetFlag(H_FLAG, (A & 0x0F) < (val & 0x0F));
                SetFlag(C_FLAG, A < val);
                break;
            case 0xFF:  StackPush(PC);PC=0x0038;break;

            default:
                Console.WriteLine("Unknow Opcode");
                break;
        }
    }

    public void ExecuteCB(byte CBOC)
    {
        switch (CBOC)
        {
            case 0x7C:
                SetFlag(Z_FLAG, (H & 0x80) == 0);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, true);
                break;

            default:
                Console.WriteLine("Unknown Opcode");
                break;
        }
    }

    private byte Read8Bit()
    {
        return _bus.Read(PC++);
    }

    private ushort Read16Bit()
    {
        byte low=Read8Bit();
        byte high=Read8Bit();
        return (ushort)((high<<8)|low);
    }

    private sbyte Read8SignedBit()
    {
        return (sbyte)(Read8Bit());
    }

    private void Add8(byte val)
    {
        SetFlag(H_FLAG,(A&0x0F)+(val&0x0F)>0x0F);
        SetFlag(C_FLAG,A+val>0xFF);
        A=(byte)(A+val);
        SetFlag(Z_FLAG,A==0);
        SetFlag(N_FLAG,false);
    }
    private void Adc8(byte val)
    {
        int c=GetFlag(C_FLAG)?1:0;
        int res=A+val+c;
        SetFlag(H_FLAG,(A&0x0F)+(val&0x0F)+c>0x0F);
        SetFlag(C_FLAG,res>0xFF);
        A=(byte)res;
        SetFlag(Z_FLAG,A==0);
        SetFlag(N_FLAG,false);
    }
    private void Sub8(byte val)
    {
        SetFlag(H_FLAG, (A & 0x0F) < (val & 0x0F));
        SetFlag(C_FLAG, A < val);
        A = (byte)(A - val);
        SetFlag(Z_FLAG, A == 0);
        SetFlag(N_FLAG, true);
    }
    private void Sbc8(byte val)
    {
        int c=GetFlag(C_FLAG)?1:0;
        int res=A-val-c;
        SetFlag(H_FLAG,(A&0x0F)<((val&0x0F)+c));
        SetFlag(C_FLAG,(int)A-(val+c)<0);
        A=(byte)res;
        SetFlag(Z_FLAG,A==0);
        SetFlag(N_FLAG,true);
    }
    private byte Inc8(byte val)
    {
        SetFlag(H_FLAG, (val & 0x0F) == 0x0F);
        val++;
        SetFlag(Z_FLAG, val == 0);
        SetFlag(N_FLAG, false);
        return val;
    }

    private byte Dec8(byte val)
    {
        SetFlag(H_FLAG, (val & 0x0F) == 0);
        val--;
        SetFlag(Z_FLAG, val == 0);
        SetFlag(N_FLAG, true);
        return val;
    }
    private void Compare8(byte val)
    {
        SetFlag(Z_FLAG, A == val);
        SetFlag(N_FLAG, true);
        SetFlag(H_FLAG, (A & 0x0F) < (val & 0x0F));
        SetFlag(C_FLAG, A < val);
    }
    private void Add16(ushort value)
    {
        SetFlag(N_FLAG,false);
        SetFlag(H_FLAG,(HL&0xFFF)+(value&0xFFF)>0xFFF);
        SetFlag(C_FLAG,(HL+value)>0xFFFF);
        HL=(ushort)(HL+value);
    }

    private void SetFlags(bool isAnd)
    {
        SetFlag(Z_FLAG,A==0);
        SetFlag(N_FLAG,false);
        SetFlag(H_FLAG,isAnd);
        SetFlag(C_FLAG,false);
    }

    public void Step()
    {

        if (Halted)
        {
            return; 
        }
        bool imeWasPending = _pendingIME;
        Fetch();

        if (imeWasPending)
        {
            IME = true;
            _pendingIME = false;
        }
    }
}

//i was testing