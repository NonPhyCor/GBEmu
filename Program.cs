using System;
using System.IO;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
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
        
        // Use a try-catch to catch any out-of-bounds memory access during early testing
        try 
        {
            int t;
            while (running)
            {
                // 2. Handle Interrupts
                // This must be called every loop to check the IF and IE registers
                cpu.CheckInterrupt();

                // 3. Log state BEFORE execution
                ushort currentPC = cpu.PC;
                byte opcode = bus.Read(currentPC);

                // We only print if the CPU isn't Halted, otherwise it would spam the console
                if (!cpu.Halted)
                {
                    Console.Write($"{currentPC:X4}   | {opcode:X2} | ");
                    Console.Write($"{cpu.A:X2} | {cpu.BC:X4} | {cpu.DE:X4} | {cpu.HL:X4} | {cpu.SP:X4} | ");
                    Console.Write($"{(cpu.GetFlag(0x80) ? 'Z' : '-')}{(cpu.GetFlag(0x40) ? 'N' : '-')}{(cpu.GetFlag(0x20) ? 'H' : '-')}{(cpu.GetFlag(0x10) ? 'C' : '-')}");
                    Console.WriteLine(" | Running");
                }
                else
                {
                    // If halted, just show a status line occasionally or a simple message
                    // Console.WriteLine($"{currentPC:X4}   | -- | CPU HALTED - Waiting for Interrupt");
                }

                // 4. Step the CPU logic
                // This handles the Fetch, Execute, and the EI-delay
                t=cpu.Step();
                bus.Tick(t);


                // 5. Control Speed
                // Real Pokemon startup does thousands of instructions; 100ms is slow for debugging
                // Change this to 1 or 0 once you know the CPU is moving correctly
                Thread.Sleep(1); 

                // 6. Safety Break
                if (currentPC == 0x0000 && opcode == 0x00) 
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
        }
    }
}