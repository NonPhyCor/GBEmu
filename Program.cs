using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string romPath = "Pokemon.gb";
        if (!File.Exists(romPath))
        {
            Console.WriteLine($"Error: {romPath} not found! Ensure it's in your project folder.");
            return;
        }
        try
        {
            byte[] romData = File.ReadAllBytes(romPath);

            Bus bus = new Bus();
            bus.LoadCartridge(romData);
            Console.Write("Verifying Bus Memory... Game Title: ");
            for (ushort addr = 0x0134; bus.Read(addr)!=0; addr++)
            {
                byte b = bus.Read(addr);
                Console.Write((char)b);
            }
            Console.WriteLine();
            Console.WriteLine("First 4 bytes at Entry Point (0x0100):");
            for (ushort i = 0; i < 4; i++)
            {
                ushort targetAddr = (ushort)(0x0100 + i);
                byte val = bus.Read(targetAddr);
                Console.WriteLine($"  Address: 0x{targetAddr:X4} | Value: 0x{val:X2}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }
}