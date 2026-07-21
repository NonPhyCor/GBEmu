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
    
    public int Fetch()
    {
        int t=0;
        byte val;
        sbyte sval;
        byte opcode = Read8Bit();
        ushort a;
#pragma warning disable CS1717 // Assignment made to same variable
        switch (opcode)
        {
            case 0x00:  t=4;break;
            case 0x01:  this.BC=Read16Bit();t=12;break;
            case 0x02:  _bus.Write(BC,A);t=8;break;
            case 0x03:  BC++;t=8;break;
            case 0x04:  B=Inc8(B);t=4;break;
            case 0x05:  B=Dec8(B);t=4;break;
            case 0x06:  B=Read8Bit();t=8;break;
            case 0x07:
                SetFlag(C_FLAG, (A & 0x80) != 0);
                A = (byte)((A << 1) | (A >> 7));
                SetFlag(Z_FLAG, false);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                t=4;
                break;
            case 0x08:
                ushort add=Read16Bit();
                _bus.Write(add,(byte)(SP&0xFF));
                _bus.Write((ushort)(add+1),(byte)(SP>>8));
                t=20;
                break;
            case 0x09:  Add16(BC);t=8;break;
            case 0x0A:  A=_bus.Read(BC);t=8;break;
            case 0x0B:  BC--;t=8;break;
            case 0x0C:  C=Inc8(C);t=4;break;
            case 0x0D:  C=Dec8(C);t=4;break;
            case 0x0E:  C=Read8Bit();t=8;break;
            case 0x0F:
                SetFlag(C_FLAG, (A & 0x01) != 0);
                A = (byte)((A >> 1) | (A << 7));
                SetFlag(Z_FLAG, false);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                t=4;
                break;
            case 0x10:  Read8Bit();t=4;break;
            case 0x11:  DE=Read16Bit();t=12;break;
            case 0x12:  _bus.Write(DE,A);t=8;break;
            case 0x13:  DE++;t=8;break;
            case 0x14:  D=Inc8(D);t=4;break;
            case 0x15:  D=Dec8(D);t=4;break;
            case 0x16:  D = Read8Bit();t=8;break;
            case 0x17:
                val = (byte)(GetFlag(C_FLAG) ? 1 : 0);
                SetFlag(C_FLAG, (A & 0x80) != 0);
                A = (byte)((A << 1) | val);
                SetFlag(Z_FLAG, false);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                t=4;
                break;
            case 0x18:
                PC = (ushort)(PC + Read8SignedBit());
                t=12;
                break;
            case 0x19:  Add16(DE);t=8;break;
            case 0x1A:  A=_bus.Read(DE);t=8;break;
            case 0x1B:  DE--;t=8;break;
            case 0x1C:  E=Inc8(E);t=4;break;
            case 0x1D:  E=Dec8(E);t=4;break;
            case 0x1E:  E = Read8Bit();t=8;break;
            case 0x1F:
                val = (byte)(GetFlag(C_FLAG) ? 1 : 0);
                SetFlag(C_FLAG, (A & 0x01) != 0);
                A = (byte)((A >> 1) | val << 7);
                SetFlag(Z_FLAG, false);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                t=4;
                break;
            case 0x20:
                sval = Read8SignedBit();
                if (!GetFlag(Z_FLAG))
                {
                    PC = (ushort)(PC + sval);
                    t=12;
                }
                else t=8;
                break;
            case 0x21:  HL=Read16Bit();t=12;break;
            case 0x22:  _bus.Write(HL,A);HL++;t=8;break;
            case 0x23:  HL++;t=8;break;
            case 0x24:  H=Inc8(H);t=4;break;
            case 0x25:  H=Dec8(H);t=4;break;
            case 0x26:  H=Read8Bit();t=8;break;
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
                t=4;
                break;
            case 0x28:
                sval = Read8SignedBit();
                if (GetFlag(Z_FLAG))
                {
                    PC = (ushort)(PC + sval);
                    t=12;
                }
                else t=8;
                break;
            case 0x29:  Add16(HL);t=8;break;
            case 0x2A:  A = _bus.Read(HL); HL++;t=8;break;
            case 0x2B:  HL--;t=8;break;
            case 0x2C:  L=Inc8(L);t=4;break;
            case 0x2D:  L=Dec8(L);t=4;break;
            case 0x2E:  L=Read8Bit();t=8;break;
            case 0x2F:
                A = (byte)~A;
                SetFlag(N_FLAG, true);
                SetFlag(H_FLAG, true);
                t=4;
                break;
            case 0x30:
                sval = Read8SignedBit();
                if (!GetFlag(C_FLAG))
                {
                    PC = (ushort)(PC + sval);
                    t=12;
                }
                else t=8;
                break;
            case 0x31:  SP = Read16Bit();t=12;break;
            case 0x32:  _bus.Write(HL, A);HL--;t=8;break;
            case 0x33:  SP++;t=8;break;
            case 0x34:  _bus.Write(HL, Inc8(_bus.Read(HL)));t=12;break;
            case 0x35:  _bus.Write(HL, Dec8(_bus.Read(HL)));t=12;break;
            case 0x36:  _bus.Write(HL,Read8Bit());t=12;break;
            case 0x37:
                SetFlag(C_FLAG, true);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                t=4;
                break;
            case 0x38:
                sval = Read8SignedBit();
                if (GetFlag(C_FLAG))
                {
                    PC = (ushort)(PC + sval);
                    t=12;
                }
                else t=8;
                break;
            case 0x39:  Add16(SP);t=8;break;
            case 0x3A:  A=_bus.Read(HL);HL--;t=8;break;
            case 0x3B:  SP--;t=8;break;
            case 0x3C:
                SetFlag(H_FLAG, (A & 0x0F) == 0x0F);
                A++;
                SetFlag(Z_FLAG, A == 0);
                SetFlag(N_FLAG, false);
                t=4;
                break;
            case 0x3D:  A=Dec8(A);t=4;break;
            case 0x3E:  A=Read8Bit();t=8;break;
            case 0x3F:
                SetFlag(C_FLAG, !GetFlag(C_FLAG));
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, false);
                t=4;
                break;
            case 0x40:  B=B; t=4;break;
            case 0x41:  B=C; t=4;break;
            case 0x42:  B=D; t=4;break;
            case 0x43:  B=E; t=4;break;
            case 0x44:  B=H; t=4;break;
            case 0x45:  B=L; t=4;break;
            case 0x46:  B=_bus.Read(HL); t=8;break;
            case 0x47:  B=A; t=4;break;
            case 0x48:  C=B; t=4;break;
            case 0x49:  C=C; t=4;break;
            case 0x4A:  C=D; t=4;break;
            case 0x4B:  C=E; t=4;break;
            case 0x4C:  C=H; t=4;break;
            case 0x4D:  C=L; t=4;break;
            case 0x4E:  C=_bus.Read(HL); t=8;break;
            case 0x4F:  C=A; t=4;break;
            case 0x50:  D=B; t=4;break;
            case 0x51:  D=C; t=4;break;
            case 0x52:  D=D; t=4;break;
            case 0x53:  D=E; t=4;break;
            case 0x54:  D=H; t=4;break;
            case 0x55:  D=L; t=4;break;
            case 0x56:  D=_bus.Read(HL); t=8;break;
            case 0x57:  D=A; t=4;break;
            case 0x58:  E=B;t=4;break;
            case 0x59:  E=C;t=4;break;
            case 0x5A:  E=D;t=4;break;
            case 0x5B:  E=E;t=4;break;
            case 0x5C:  E=H;t=4;break;
            case 0x5D:  E=L;t=4;break;
            case 0x5E:  E=_bus.Read(HL); t=8;break;
            case 0x5F:  E=A;t=4;break;
            case 0x60:  H=B;t=4;break;
            case 0x61:  H=C;t=4;break;
            case 0x62:  H=D;t=4;break;
            case 0x63:  H=E;t=4;break;
            case 0x64:  H=H;t=4;break;
            case 0x65:  H=L;t=4;break;
            case 0x66:  H=_bus.Read(HL); t=8;break;
            case 0x67:  H=A;t=4;break;
            case 0x68:  L=B;t=4;break;
            case 0x69:  L=C;t=4;break;
            case 0x6A:  L=D;t=4;break;
            case 0x6B:  L=E;t=4;break;
            case 0x6C:  L=H;t=4;break;
            case 0x6D:  L=L;t=4;break;
            case 0x6E:  L=_bus.Read(HL); t=8;break;
            case 0x6F:  L=A;t=4;break;
            case 0x70:  _bus.Write(HL, B); t=8;break;
            case 0x71:  _bus.Write(HL, C); t=8;break;
            case 0x72:  _bus.Write(HL, D); t=8;break;
            case 0x73:  _bus.Write(HL, E); t=8;break;
            case 0x74:  _bus.Write(HL, H); t=8;break;
            case 0x75:  _bus.Write(HL, L); t=8;break;
            case 0x76:  Halted=true;t=4;break;
            case 0x77:  _bus.Write(HL, A); t=8;break;
            case 0x78:  A=B; t=4;break;
            case 0x79:  A=C; t=4;break;
            case 0x7A:  A=D; t=4;break;
            case 0x7B:  A=E; t=4;break;
            case 0x7C:  A=H; t=4;break;
            case 0x7D:  A=L; t=4;break;
            case 0x7E:  A=_bus.Read(HL); t=8;break;
            case 0x7F:  A=A; t=4;break;
            case 0x80:  Add8(B);t=4;break;
            case 0x81:  Add8(C);t=4;break;
            case 0x82:  Add8(D);t=4;break;
            case 0x83:  Add8(E);t=4;break;
            case 0x84:  Add8(H);t=4;break;
            case 0x85:  Add8(L);t=4;break;
            case 0x86:  Add8(_bus.Read(HL));t=8;break;
            case 0x87:  Add8(A);t=4;break;
            case 0x88:  Adc8(B);t=4;break;
            case 0x89:  Adc8(C);t=4;break;
            case 0x8A:  Adc8(D);t=4;break;
            case 0x8B:  Adc8(E);t=4;break;
            case 0x8C:  Adc8(H);t=4;break;
            case 0x8D:  Adc8(L);t=4;break;
            case 0x8E:  Adc8(_bus.Read(HL));t=8;break;
            case 0x8F:  Adc8(A);t=4;break;
            case 0x90:  Sub8(B);t=4;break;
            case 0x91:  Sub8(C);t=4;break;
            case 0x92:  Sub8(D);t=4;break;
            case 0x93:  Sub8(E);t=4;break;
            case 0x94:  Sub8(H);t=4;break;
            case 0x95:  Sub8(L);t=4;break;
            case 0x96:  Sub8(_bus.Read(HL));t=8;break;
            case 0x97:  Sub8(A);t=4;break;
            case 0x98:  Sbc8(B);t=4;break;
            case 0x99:  Sbc8(C);t=4;break;
            case 0x9A:  Sbc8(D);t=4;break;
            case 0x9B:  Sbc8(E);t=4;break;
            case 0x9C:  Sbc8(H);t=4;break;
            case 0x9D:  Sbc8(L);t=4;break;
            case 0x9E:  Sbc8(_bus.Read(HL));t=8;break;
            case 0x9F:  Sbc8(A);t=4;break;
            case 0xA0: A &= B; SetFlags(true); t=4;break;
            case 0xA1: A &= C; SetFlags(true); t=4;break;
            case 0xA2: A &= D; SetFlags(true); t=4;break;
            case 0xA3: A &= E; SetFlags(true); t=4;break;
            case 0xA4: A &= H; SetFlags(true); t=4;break;
            case 0xA5: A &= L; SetFlags(true); t=4;break;
            case 0xA6: A &= _bus.Read(HL); SetFlags(true); t=8;break;
            case 0xA7: A &= A; SetFlags(true); t=4;break;
            case 0xA8: A ^= B; SetFlags(false); t=4;break;
            case 0xA9: A ^= C; SetFlags(false); t=4;break;
            case 0xAA: A ^= D; SetFlags(false); t=4;break;
            case 0xAB: A ^= E; SetFlags(false); t=4;break;
            case 0xAC: A ^= H; SetFlags(false); t=4;break;
            case 0xAD: A ^= L; SetFlags(false); t=4;break;
            case 0xAE: A ^= _bus.Read(HL); SetFlags(false); t=8;break;
            case 0xAF: A ^= A; SetFlags(false); t=4;break;
            case 0xB0: A |= B; SetFlags(false); t=4;break;
            case 0xB1: A |= C; SetFlags(false); t=4;break;
            case 0xB2: A |= D; SetFlags(false); t=4;break;
            case 0xB3: A |= E; SetFlags(false); t=4;break;
            case 0xB4: A |= H; SetFlags(false); t=4;break;
            case 0xB5: A |= L; SetFlags(false); t=4;break;
            case 0xB6: A |= _bus.Read(HL); SetFlags(false); t=8;break;
            case 0xB7: A |= A; SetFlags(false); t=4;break;
            case 0xB8:  Compare8(B);t=4;break;
            case 0xB9:  Compare8(C);t=4;break;
            case 0xBA:  Compare8(D);t=4;break;
            case 0xBB:  Compare8(E);t=4;break;
            case 0xBC:  Compare8(H);t=4;break;
            case 0xBD:  Compare8(L);t=4;break;
            case 0xBE:  Compare8(_bus.Read(HL));t=8;break;
            case 0xBF:  Compare8(A);t=4;break;
            case 0xC0:  if(!GetFlag(Z_FLAG)) {PC = StackPop(); t=20;} else t=8;break;
            case 0xC1:  BC=StackPop();t=12;break;
            case 0xC2:
                ushort addr = Read16Bit();
                if (!GetFlag(Z_FLAG))
                {
                    PC = addr;
                    t=16;
                }
                else t=12;
                break;
            case 0xC3:  PC=Read16Bit();t=16;break;
            case 0xC4:
                a = Read16Bit();
                if(!GetFlag(Z_FLAG))
                {
                    StackPush(PC);
                    PC = a;
                    t=24;
                }
                else t=12;
                break;
            case 0xC5:  StackPush(BC);t=16;break;
            case 0xC6:
                val = Read8Bit();
                SetFlag(H_FLAG, (A & 0x0F) + (val & 0x0F) > 0x0F);
                SetFlag(C_FLAG, A + val > 0xFF);
                A = (byte)(A + val);
                SetFlag(Z_FLAG, A == 0);
                SetFlag(N_FLAG, false);
                t=8;
                break;
            case 0xC7:
                StackPush(PC);
                PC=0x0000;
                t=16;
                break;
            case 0xC8:  if(GetFlag(Z_FLAG)) {PC=StackPop(); t=20;} else t=8;break;
            case 0xC9: PC = StackPop(); t=16;break;
            case 0xCA:  
                a = Read16Bit();
                if(GetFlag(Z_FLAG)) {PC=a; t=16;} else t=12;
                break;
            case 0xCB:
                byte CBOC = Read8Bit();
                t=4+ExecuteCB(CBOC); // Base fetch cost, ExecuteCB should return its own
                break;
            case 0xCC:
                a = Read16Bit();
                if(GetFlag(Z_FLAG))
                {
                    StackPush(PC);
                    PC=a;
                    t=24;
                }
                else t=12;
                break;
            case 0xCD:
                ushort dest = Read16Bit();
                StackPush(PC);
                PC = dest;
                t=24;
                break;
            case 0xCE:  Adc8(Read8Bit());t=8;break;
            case 0xCF:
                StackPush(PC);
                PC=0x0008;
                t=16;
                break;
            case 0xD0:  if(!GetFlag(C_FLAG)) {PC=StackPop(); t=20;} else t=8;break;
            case 0xD1:  DE=StackPop();t=12;break;
            case 0xD2:
                a=Read16Bit();
                if(!GetFlag(C_FLAG)) {PC=a; t=16;} else t=12;
                break;
            case 0xD4:
                a=Read16Bit();
                if(!GetFlag(C_FLAG))
                {
                    StackPush(PC);
                    PC=a;
                    t=24;
                }
                else t=12;
                break;
            case 0xD5:  StackPush(DE);t=16;break;
            case 0xD6:  Sub8(Read8Bit());t=8;break;
            case 0xD7:  StackPush(PC);PC=0x0010;t=16;break;
            case 0xD8:  if(GetFlag(C_FLAG)) {PC=StackPop(); t=20;} else t=8;break;
            case 0xD9:  PC = StackPop();IME=true;t=16;break;
            case 0xDA:
                a=Read16Bit();
                if(GetFlag(C_FLAG)) {PC=a; t=16;} else t=12;
                break;
            case 0xDC:
                a=Read16Bit();
                if(GetFlag(C_FLAG))
                {
                    StackPush(PC);
                    PC=a;
                    t=24;
                }
                else t=12;
                break;
            case 0xDE:  Sbc8(Read8Bit());t=8;break;
            case 0xDF:  StackPush(PC);PC=0x0018;t=16;break;
            case 0xE0:
                _bus.Write((ushort)(0xFF00+Read8Bit()),A);
                t=12;
                break;
            case 0xE1:  HL=StackPop();t=12;break;
            case 0xE2:
                _bus.Write((ushort)(0xFF00 + C), A);
                t=8;
                break;
            case 0xE5:  StackPush(HL);t=16;break;
            case 0xE6:  A&=Read8Bit();SetFlags(true);t=8;break;
            case 0xE7:  StackPush(PC);PC=0x0020;t=16;break;
            case 0xE8:
                sval = Read8SignedBit();
                SetFlag(Z_FLAG, false);
                SetFlag(N_FLAG, false);
                SetFlag(H_FLAG, (SP & 0x0F) + (sval & 0x0F) > 0x0F);
                SetFlag(C_FLAG, (SP & 0xFF) + (sval & 0xFF) > 0xFF);
                SP = (ushort)(SP + sval);
                t=16;
                break;
            case 0xE9:  PC=HL;t=4;break;
            case 0xEA:
                _bus.Write(Read16Bit(), A);
                t=16;
                break;
            case 0xEE:  A^=Read8Bit();SetFlags(false);t=8;break;
            case 0xEF:  StackPush(PC);PC=0x0028;t=16;break;
            case 0xF0:
                A = _bus.Read((ushort)(0xFF00 + Read8Bit()));
                t=12;
                break;
            case 0xF1:  AF=StackPop();t=12;break;
            case 0xF2:
                A = _bus.Read((ushort)(0xFF00 + C));
                t=8;
                break;
            case 0xF3:
                IME = false;
                _pendingIME = false;
                t=4;
                break;
            case 0xF5:  StackPush(AF);t=16;break;
            case 0xF6:  A|=Read8Bit();SetFlags(false);t=8;break;
            case 0xF7:  StackPush(PC);PC=0x0030;t=16;break;
            case 0xF8:
                sval=Read8SignedBit();
                HL=(ushort)(SP+sval);
                SetFlag(Z_FLAG,false);
                SetFlag(N_FLAG,false);
                SetFlag(H_FLAG,(SP&0x0F)+(sval&0x0F)>0x0F);
                SetFlag(C_FLAG,(SP&0xFF)+(sval&0xFF)>0xFF);
                t=12;
                break;
            case 0xF9: SP = HL; t=8;break;
            case 0xFA:
                A = _bus.Read(Read16Bit());
                t=16;
                break;
            case 0xFB:  _pendingIME=true;t=4;break;
            case 0xFE:
                val = Read8Bit();
                SetFlag(Z_FLAG, A == val);
                SetFlag(N_FLAG, true);
                SetFlag(H_FLAG, (A & 0x0F) < (val & 0x0F));
                SetFlag(C_FLAG, A < val);
                t=8;
                break;
            case 0xFF:  StackPush(PC);PC=0x0038;t=16;break;

            default:
                Console.WriteLine("Unknow Opcode");
                t=4;
                break;
        }
        return t;
    }
    public int ExecuteCB(byte cbOpcode)
    {
        int t = 8;
        int regIndex = cbOpcode & 0x07;
        if (regIndex == 6) t = 16;
        byte val = GetRegisterByIndex(regIndex);
        int bit = (cbOpcode >> 3) & 0x07;
        if (cbOpcode < 0x40) 
        {
            val = ExecuteRotationShift(cbOpcode >> 3, val);
        }
        else if (cbOpcode < 0x80) 
        {
            if (regIndex == 6) t = 12;
            BitTest(bit, val);
            return t;
        }
        else if (cbOpcode < 0xC0) 
        {
            val = (byte)(val & ~(1 << bit));
        }
        else 
        {
            val = (byte)(val | (1 << bit));
        }
        SetRegisterByIndex(regIndex, val);
        return t;
    }

    private byte GetRegisterByIndex(int index)
    {
        return index switch
        {
            0=>B,1=>C,2=>D,3=>E,4=>H,5=>L,6=>_bus.Read(HL),7=>A,_=>0
        };
    }

    private void SetRegisterByIndex(int index, byte val)
    {
        switch (index) {
            case 0: B = val; break;
            case 1: C = val; break;
            case 2: D = val; break;
            case 3: E = val; break;
            case 4: H = val; break;
            case 5: L = val; break;
            case 6: _bus.Write(HL, val); break;
            case 7: A = val; break;
        }
    }
    private void BitTest(int bit,byte val)
    {
        SetFlag(Z_FLAG, (val & (1 << bit)) == 0);
        SetFlag(N_FLAG, false);
        SetFlag(H_FLAG, true);
    }
    private byte ExecuteRotationShift(int type, byte val)
    {
        int c = GetFlag(C_FLAG) ? 1 : 0;
        byte result = 0;

        switch (type)
        {
            case 0:
                SetFlag(C_FLAG, (val & 0x80) != 0);
                result = (byte)((val << 1) | (val >> 7));
                break;
            case 1:
                SetFlag(C_FLAG, (val & 0x01) != 0);
                result = (byte)((val >> 1) | (val << 7));
                break;
            case 2:
                SetFlag(C_FLAG, (val & 0x80) != 0);
                result = (byte)((val << 1) | c);
                break;
            case 3:
                SetFlag(C_FLAG, (val & 0x01) != 0);
                result = (byte)((val >> 1) | (c << 7));
                break;
            case 4:
                SetFlag(C_FLAG, (val & 0x80) != 0);
                result = (byte)(val << 1);
                break;
            case 5:
                SetFlag(C_FLAG, (val & 0x01) != 0);
                result = (byte)((val >> 1) | (val & 0x80));
                break;
            case 6:
                SetFlag(C_FLAG, false);
                result = (byte)((val << 4) | (val >> 4));
                break;
            case 7:
                SetFlag(C_FLAG, (val & 0x01) != 0);
                result = (byte)(val >> 1);
                break;
        }

        SetFlag(Z_FLAG, result == 0);
        SetFlag(N_FLAG, false);
        SetFlag(H_FLAG, false);
        return result;
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

    public int Step()
    {
        int t;
        if (Halted)
        {
            return 4; 
        }
        bool imeWasPending = _pendingIME;
        t=Fetch();

        if (imeWasPending)
        {
            IME = true;
            _pendingIME = false;
        }
        return t;
    }

    public void CheckInterrupt()
    {
        byte requested=_bus.Read(0xFF0F);
        byte enabled=_bus.Read(0xFFFF);

        if((requested&enabled)!=0)
        {
            Halted=false;
            if(!IME)    return;
            for (int i = 0; i < 5; i++)
            {
                if (((requested&enabled)&(1<<i))!=0)
                {
                    ServiceInterrupt(i);
                    break;
                }
            }
        }
    }

    private void ServiceInterrupt(int interruptBit)
    {
        IME = false;
        byte ifReg = _bus.Read(0xFF0F);
        ifReg &= (byte)~(1 << interruptBit);
        _bus.Write(0xFF0F, ifReg);
        StackPush(PC);
        switch (interruptBit)
        {
            case 0: PC = 0x0040; break;
            case 1: PC = 0x0048; break;
            case 2: PC = 0x0050; break;
            case 3: PC = 0x0058; break;
            case 4: PC = 0x0060; break;
        }
    }
}