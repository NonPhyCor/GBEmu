using System;
using System.IO;
using Raylib_cs;
class Program
{
    static void Main(string[] args)
    {
        string romPath = "Tetris.gb";
        if (!File.Exists(romPath))
        {
            Console.WriteLine($"Error: {romPath} not found!");
            return;
        }
        byte[] romData = File.ReadAllBytes(romPath);
        Bus bus = new Bus(romData);
        bus.JoypadController._bus = bus;
        CPU cpu = new CPU(bus);
        Raylib.InitWindow(640, 576, "Game Boy Emulator");
        Raylib.SetTargetFPS(60);
        Console.WriteLine("Window opened. Listening for keyboard inputs...");
        while (!Raylib.WindowShouldClose())
        {
            bus.JoypadController.UpdateKey(GameboyKey.Up,     Raylib.IsKeyDown(KeyboardKey.W));
            bus.JoypadController.UpdateKey(GameboyKey.Down,   Raylib.IsKeyDown(KeyboardKey.S));
            bus.JoypadController.UpdateKey(GameboyKey.Left,   Raylib.IsKeyDown(KeyboardKey.A));
            bus.JoypadController.UpdateKey(GameboyKey.Right,  Raylib.IsKeyDown(KeyboardKey.D));
            bus.JoypadController.UpdateKey(GameboyKey.A,      Raylib.IsKeyDown(KeyboardKey.J));
            bus.JoypadController.UpdateKey(GameboyKey.B,      Raylib.IsKeyDown(KeyboardKey.K));
            bus.JoypadController.UpdateKey(GameboyKey.Start,  Raylib.IsKeyDown(KeyboardKey.Enter));
            bus.JoypadController.UpdateKey(GameboyKey.Select, Raylib.IsKeyDown(KeyboardKey.Space));
            int cyclesThisFrame = 0;
            while (cyclesThisFrame < 70224)
            {
                cpu.CheckInterrupt();
                int cycles = cpu.Step();
                bus.Tick(cycles);
                cyclesThisFrame += cycles;
            }
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(155, 188, 15, 255));
            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
        Console.WriteLine("Emulator closed gracefully.");
    }
}