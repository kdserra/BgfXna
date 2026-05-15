#if HEADLESS_HOST
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace BgfXna.Samples;

internal static class HeadlessGameHost
{
    public static void Run()
    {
        using HeadlessAutoPongWindow window = new();
        window.Run();
    }
}

internal sealed class HeadlessAutoPongWindow : IDisposable
{
    private const int FrameMilliseconds = 16;

    private readonly AutoPongGame _game = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan _lastFrame;
    private bool _running;

    public HeadlessAutoPongWindow()
    {
        Console.WriteLine("Initializing Headless Game Host...");

        // BGFX Noop backend doesn't need a window handle.
        _game.SetNativeWindowHandle(IntPtr.Zero, IntPtr.Zero);
    }

    public void Run()
    {
        _running = true;
        Console.WriteLine("Headless Game Host is running. Press Ctrl+C to exit.");

        // Handle Ctrl+C
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Shutting down Headless Game Host...");
            _running = false;
            e.Cancel = true; // Prevent immediate termination
        };

        while (_running)
        {
            TimeSpan total = _clock.Elapsed;
            if (total - _lastFrame < TimeSpan.FromMilliseconds(FrameMilliseconds))
            {
                System.Threading.Thread.Sleep(1);
                continue;
            }

            TimeSpan elapsed = total - _lastFrame;
            _lastFrame = total;
            _game.Tick(new GameTime(total, elapsed));

            if (!_game.IsActive)
            {
                _running = false;
            }
        }
    }

    public void Dispose()
    {
        _game.Dispose();
        Console.WriteLine("Headless Game Host stopped.");
    }
}
#endif
