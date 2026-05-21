using System;
using System.IO;
using Raylib_cs; // Import the new Raylib package

class Program
{
    static void Main(string[] args)
    {
        // Load your simple 32KB test game
        string romPath = "Tetris.gb";
        
        if (!File.Exists(romPath))
        {
            Console.WriteLine($"Error: {romPath} not found!");
            return;
        }

        Bus bus = new Bus();
        bus.JoypadController._bus = bus;
        byte[] romData = File.ReadAllBytes(romPath);
        bus.LoadCartridge(romData);
        CPU cpu = new CPU(bus);

        // Initialize a window (160x144 is the native Game Boy resolution)
        // We multiply it by 4 (640x576) so it's large enough to see easily on your screen
        Raylib.InitWindow(640, 576, "Game Boy Emulator");
        Raylib.SetTargetFPS(60);

        Console.WriteLine("Window opened. Listening for keyboard inputs...");

        // This loop runs continuously until you close the window or hit Escape
        while (!Raylib.WindowShouldClose())
        {
            // --- 1. CAPTURE INPUTS ---
            // Check your computer keys and feed them straight to your Joypad engine
            bus.JoypadController.UpdateKey(GameboyKey.Up,     Raylib.IsKeyDown(KeyboardKey.W));
            bus.JoypadController.UpdateKey(GameboyKey.Down,   Raylib.IsKeyDown(KeyboardKey.S));
            bus.JoypadController.UpdateKey(GameboyKey.Left,   Raylib.IsKeyDown(KeyboardKey.A));
            bus.JoypadController.UpdateKey(GameboyKey.Right,  Raylib.IsKeyDown(KeyboardKey.D));
            
            bus.JoypadController.UpdateKey(GameboyKey.A,      Raylib.IsKeyDown(KeyboardKey.J));      // Z key maps to A
            bus.JoypadController.UpdateKey(GameboyKey.B,      Raylib.IsKeyDown(KeyboardKey.K));      // X key maps to B
            bus.JoypadController.UpdateKey(GameboyKey.Start,  Raylib.IsKeyDown(KeyboardKey.Enter));  // Enter maps to Start
            bus.JoypadController.UpdateKey(GameboyKey.Select, Raylib.IsKeyDown(KeyboardKey.Space));  // Space maps to Select

            // --- 2. EXECUTE EMULATOR CYCLE ---
            // For a simple target, we execute roughly enough cycles to match one frame of video
            // A real Game Boy runs ~70224 cycles per frame at 60Hz
            int cyclesThisFrame = 0;
            while (cyclesThisFrame < 70224)
            {
                cpu.CheckInterrupt();
                int cycles = cpu.Step();
                bus.Tick(cycles);
                cyclesThisFrame += cycles;
            }

            // --- 3. RENDER BLANK WINDOW FRAME ---
            // For now, we clear the screen with a classic Game Boy green background color.
            // We'll fill this with actual pixels when we build the PPU!
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(155, 188, 15, 255)); // Classic OG Game Boy Green
            Raylib.EndDrawing();
        }

        // Clean up memory and close the window when exiting
        Raylib.CloseWindow();
        Console.WriteLine("Emulator closed gracefully.");
    }
}