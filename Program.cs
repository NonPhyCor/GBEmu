using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        // Ensure a ROM path is passed as a command-line argument
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: Emulator <path_to_rom.gb>");
            return;
        }

        string romPath = args[0];
        if (!File.Exists(romPath))
        {
            Console.WriteLine($"Error: ROM file '{romPath}' not found.");
            return;
        }

        // 1. Read the ROM into a byte array
        byte[] romData = File.ReadAllBytes(romPath);

        // 2. Initialize the Bus with the ROM data
        Bus bus = new Bus(romData);

        // 3. Initialize the CPU with the connected Bus
        CPU cpu = new CPU(bus);

        Console.WriteLine("ROM loaded successfully. Starting emulation...");

        // 4. Main Emulation Loop
        try
        {
            while (true)
            {
                ushort previousPC = cpu.PC;
                byte opcode = bus.Read(cpu.PC);

                int cycles = cpu.Step(); 
                bus.Tick(cycles); 
                cpu.CheckInterrupt(); 

                // THE REFINED TRAP: Only trigger if we jump back to the exact boot vectors
                if ((cpu.PC == 0x0000 || cpu.PC == 0x0100 || cpu.PC == 0x0150) && previousPC > 0x0200)
                {
                    Console.WriteLine($"\n--- TRUE REBOOT DETECTED ---");
                    Console.WriteLine($"The CPU executed Opcode 0x{opcode:X2} at PC:0x{previousPC:X4}");
                    Console.WriteLine($"This caused the PC to illegally reset to 0x{cpu.PC:X4}");
                    Console.WriteLine($"Final CPU State: A=0x{cpu.A:X2}, F=0x{cpu.F:X2}, B=0x{cpu.B:X2}, C=0x{cpu.C:X2}, D=0x{cpu.D:X2}, E=0x{cpu.E:X2}, H=0x{cpu.H:X2}, L=0x{cpu.L:X2}, SP=0x{cpu.SP:X4}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nEmulation crashed: {ex.Message}");
            Console.WriteLine($"Final CPU State: PC=0x{cpu.PC:X4}, SP=0x{cpu.SP:X4}, A=0x{cpu.A:X2}, F=0x{cpu.F:X2}");
        }
    }
}