using System;
using System.IO;
using System.Runtime.InteropServices;
using Raylib_cs;

class Program
{
    private const int SampleRate = 44100;
    private const int FrameCycles = 70224; // Standard Game Boy cycles per frame
    private const int BufferFrames = 2048; // Standard buffer chunk frame count

    static void Main(string[] args)
    {
        string romPath = "Tetris.gb"; 
        byte[] romData = File.Exists(romPath) ? File.ReadAllBytes(romPath) : new byte[0x8000];

        Bus bus = new Bus(romData);
        CPU cpu = new CPU(bus);

        // MUST be called before InitAudioDevice to configure Raylib's internal sub-buffer allocation
        Raylib.SetAudioStreamBufferSizeDefault(BufferFrames);
        
        Raylib.InitWindow(160 * 3, 144 * 3, "Game Boy Emulator");
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(60);

        AudioStream audioStream = Raylib.LoadAudioStream(SampleRate, 16, 2);
        Raylib.PlayAudioStream(audioStream);

        // Buffer sized for 2048 stereo frames (4096 short values)
        short[] pcmBuffer = new short[BufferFrames * 2];

        Console.WriteLine("Emulator Running...");

        while (!Raylib.WindowShouldClose())
        {
            // 1. Input Handling
            bus.JoypadController.UpdateKey(GameboyKey.Right,  Raylib.IsKeyDown(KeyboardKey.Right));
            bus.JoypadController.UpdateKey(GameboyKey.Left,   Raylib.IsKeyDown(KeyboardKey.Left));
            bus.JoypadController.UpdateKey(GameboyKey.Up,     Raylib.IsKeyDown(KeyboardKey.Up));
            bus.JoypadController.UpdateKey(GameboyKey.Down,   Raylib.IsKeyDown(KeyboardKey.Down));
            bus.JoypadController.UpdateKey(GameboyKey.A,      Raylib.IsKeyDown(KeyboardKey.Z));
            bus.JoypadController.UpdateKey(GameboyKey.B,      Raylib.IsKeyDown(KeyboardKey.X));
            bus.JoypadController.UpdateKey(GameboyKey.Select, Raylib.IsKeyDown(KeyboardKey.RightShift) || Raylib.IsKeyDown(KeyboardKey.Backspace));
            bus.JoypadController.UpdateKey(GameboyKey.Start,  Raylib.IsKeyDown(KeyboardKey.Enter));

            // 2. Step emulation for one frame
            int elapsedCycles = 0;
            while (elapsedCycles < FrameCycles)
            {
                int cycles = cpu.Step();
                cpu.CheckInterrupt();
                bus.Tick(cycles);
                elapsedCycles += cycles;
            }

            // 3. Audio Streaming
            if (Raylib.IsAudioStreamProcessed(audioStream))
            {
                int availableFrames = bus.APUController.AudioBuffer.Count / 2;
                int framesToRead = Math.Min(availableFrames, BufferFrames);

                if (framesToRead > 0)
                {
                    // Pull available stereo samples from the APU queue
                    for (int i = 0; i < framesToRead * 2; i += 2)
                    {
                        float left = bus.APUController.AudioBuffer.Dequeue();
                        float right = bus.APUController.AudioBuffer.Dequeue();

                        pcmBuffer[i]     = (short)(Math.Clamp(left,  -1.0f, 1.0f) * 32767f);
                        pcmBuffer[i + 1] = (short)(Math.Clamp(right, -1.0f, 1.0f) * 32767f);
                    }

                    // Pad remaining buffer with silence if APU output was slightly short
                    for (int i = framesToRead * 2; i < BufferFrames * 2; i++)
                    {
                        pcmBuffer[i] = 0;
                    }

                    unsafe
                    {
                        fixed (short* ptr = pcmBuffer)
                        {
                            Raylib.UpdateAudioStream(audioStream, ptr, BufferFrames);
                        }
                    }
                }
            }

            // 4. Prevent excessive backlog accumulation
            if (bus.APUController.AudioBuffer.Count > 16384)
            {
                while (bus.APUController.AudioBuffer.Count > 4096)
                {
                    bus.APUController.AudioBuffer.Dequeue();
                }
            }

            // 5. Render
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Raylib.DrawText("Game Boy Emulator", 20, 20, 20, Color.Green);
            Raylib.DrawText("D-Pad = Arrows | A = Z | B = X | Start = Enter | Select = RShift", 20, 50, 14, Color.RayWhite);
            Raylib.EndDrawing();
        }

        Raylib.UnloadAudioStream(audioStream);
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }
}