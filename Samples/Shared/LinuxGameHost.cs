#if LINUX_DESKTOP_HOST
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace BgfXna.Samples;

internal static class LinuxGameHost
{
    public static void Run()
    {
        using SDL2AutoPongWindow window = new();
        window.Run();
    }
}

internal sealed class SDL2AutoPongWindow : IDisposable
{
    private const int Width = 1280;
    private const int Height = 720;
    private const int FrameMilliseconds = 16;

    private readonly AutoPongGame _game = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly IntPtr _window;
    private TimeSpan _lastFrame;
    private bool _running;

    public SDL2AutoPongWindow()
    {
        if (SDL_Init(0x00000020) != 0) // SDL_INIT_VIDEO
        {
            throw new InvalidOperationException($"SDL_Init failed: {Marshal.PtrToStringAnsi(SDL_GetError())}");
        }

        // Create window
        _window = SDL_CreateWindow($"BgfXna AutoPong - {SamplePlatform.Backend}", 100, 100, Width, Height, 0x4); // SDL_WINDOW_SHOWN
        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException($"SDL_CreateWindow failed: {Marshal.PtrToStringAnsi(SDL_GetError())}");
        }

        SDL_SysWMinfo info = default;
        info.version_major = 2;
        info.version_minor = 0;
        info.version_patch = 0;

        if (!SDL_GetWindowWMInfo(_window, ref info))
        {
            throw new InvalidOperationException("SDL_GetWindowWMInfo failed.");
        }

        IntPtr display = info.display;
        IntPtr window_or_surface = info.window_or_surface;

        _game.SetNativeWindowHandle(window_or_surface, display);
    }

    public void Run()
    {
        _running = true;
        while (_running)
        {
            SDL_Event ev;
            while (SDL_PollEvent(out ev) != 0)
            {
                if (ev.type == 0x100) // SDL_QUIT
                {
                    _running = false;
                }
            }

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

        if (_window != IntPtr.Zero)
        {
            SDL_DestroyWindow(_window);
        }

        SDL_Quit();
    }

    // SDL2 P/Invokes
    [DllImport("SDL2")]
    private static extern int SDL_Init(uint flags);

    [DllImport("SDL2")]
    private static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);

    [DllImport("SDL2")]
    private static extern void SDL_DestroyWindow(IntPtr window);

    [DllImport("SDL2")]
    private static extern void SDL_Quit();

    [DllImport("SDL2")]
    private static extern bool SDL_GetWindowWMInfo(IntPtr window, ref SDL_SysWMinfo info);

    [DllImport("SDL2")]
    private static extern int SDL_PollEvent(out SDL_Event ev);

    [DllImport("SDL2")]
    private static extern IntPtr SDL_GetError();

    [StructLayout(LayoutKind.Explicit)]
    private struct SDL_SysWMinfo
    {
        [FieldOffset(0)] public byte version_major;
        [FieldOffset(1)] public byte version_minor;
        [FieldOffset(2)] public byte version_patch;
        [FieldOffset(4)] public int subsystem;
        
        [FieldOffset(8)] public IntPtr display;
        [FieldOffset(16)] public IntPtr window_or_surface;
    }

    [StructLayout(LayoutKind.Explicit, Size = 56)]
    private struct SDL_Event
    {
        [FieldOffset(0)] public uint type;
    }
}
#endif
