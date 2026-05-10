using System;
using System.IO;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        // Switching to Tetris for this test as per your updated path
        string romPath = "Pokemon.gb";
        
        if (!File.Exists(romPath))
        {
            Console.WriteLine($"Error: {romPath} not found!");
            return;
        }

        // 1. Initialize Hardware
        Bus bus = new Bus();
        byte[] romData = File.ReadAllBytes(romPath);
        bus.LoadCartridge(romData);
        
        CPU cpu = new CPU(bus);

        Console.WriteLine("CPU Initialized. Starting execution...");
        Console.WriteLine("-------------------------------------------------------------------");
        Console.WriteLine("PC     | OP | A  | BC   | DE   | HL   | SP   | Flags | Status");
        Console.WriteLine("-------------------------------------------------------------------");

        bool running = true;
        long instructionCount = 0; // Track total instructions to filter logs
        
        try 
        {
            while (running)
            {
                // 2. Handle Interrupts
                cpu.CheckInterrupt();

                // 3. Monitor for V-Blank Interrupt Vector
                if (cpu.PC == 0x0040)
                {
                    Console.WriteLine("\n*** V-BLANK INTERRUPT TRIGGERED! PC is at 0040 ***");
                    Console.WriteLine("Press Enter to continue execution...");
                    Console.ReadLine(); 
                }

                // 4. State Logging with Filter
                // Printing every line is slow. We log every 10,000 instructions
                // or if we are at the interrupt vector to keep the emulator fast.
                if (!cpu.Halted && (instructionCount % 10000 == 0 || cpu.PC == 0x0040))
                {
                    ushort currentPC = cpu.PC;
                    byte opcode = bus.Read(currentPC);

                    Console.Write($"{currentPC:X4}   | {opcode:X2} | ");
                    Console.Write($"{cpu.A:X2} | {cpu.BC:X4} | {cpu.DE:X4} | {cpu.HL:X4} | {cpu.SP:X4} | ");
                    Console.Write($"{(cpu.GetFlag(0x80) ? 'Z' : '-')}{(cpu.GetFlag(0x40) ? 'N' : '-')}{(cpu.GetFlag(0x20) ? 'H' : '-')}{(cpu.GetFlag(0x10) ? 'C' : '-')}");
                    Console.WriteLine($" | Count: {instructionCount}");
                }

                // 5. Step and Tick
                int t = cpu.Step();
                bus.Tick(t);
                instructionCount++;

                // 6. Safety Break
                if (cpu.PC == 0x0000 && bus.Read(cpu.PC) == 0x00) 
                {
                    Console.WriteLine("-------------------------------------------------------------------");
                    Console.WriteLine("Reached empty memory (0x0000). Stopping.");
                    running = false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nCPU CRASHED at PC: {cpu.PC:X4}");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }
}