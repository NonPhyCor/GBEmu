using System;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Bus bus = new Bus();
        CPU cpu = new CPU(bus);
        byte[] romData = File.ReadAllBytes("Tetris.gb");
        bus.LoadCartridge(romData);

        Console.WriteLine("CPU Initialized. Starting execution...");
        Console.WriteLine("-------------------------------------------------------------------");
        Console.WriteLine("PC     | OP | Instruction      | A  | BC   | DE   | HL   | SP   | F ");
        Console.WriteLine("-------------------------------------------------------------------");

        bool running = true;
        while (running)
        {
            // 1. Log the state BEFORE the instruction executes
            ushort currentPC = cpu.PC;
            byte opcode = bus.Read(currentPC);
            string mnemonic = GetMnemonic(opcode);

            Console.Write($"{currentPC:X4}   | {opcode:X2} | {mnemonic,-16} | ");
            Console.Write($"{cpu.A:X2} | {cpu.BC:X4} | {cpu.DE:X4} | {cpu.HL:X4} | {cpu.SP:X4} | ");
            Console.WriteLine($"{(cpu.GetFlag(0x80)?'Z':'-')}{(cpu.GetFlag(0x40)?'N':'-')}{(cpu.GetFlag(0x20)?'H':'-')}{(cpu.GetFlag(0x10)?'C':'-')}");

            // 2. Execute the instruction
            cpu.Step();

            // 3. Handle Interrupts (if any)
            // HandleInterrupts(cpu, bus);

            // 4. Slow down so we can read the output
            Thread.Sleep(100); 

            // Prevent infinite wrap-around stop for this test
            if (cpu.PC == 0x0000) {
                Console.WriteLine("Reached 0x0000. Stopping.");
                running = false;
            }
        }
    }

    // A very simple helper to name the opcodes we just injected
    static string GetMnemonic(byte opcode)
    {
        return opcode switch
        {
            0x00 => "NOP",
            0x06 => "LD B, d8",
            0x3E => "LD A, d8",
            0x80 => "ADD A, B",
            0xC3 => "JP a16",
            _ => $"UNK ({opcode:X2})"
        };
    }
}