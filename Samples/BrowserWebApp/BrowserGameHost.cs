#if BROWSER_WEB_HOST
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace BgfXna.Samples;

internal static class BrowserGameHost
{
    public static async Task RunAsync()
    {
        using BrowserSdlWindow window = new();
        await window.RunAsync();
    }
}

internal sealed class BrowserSdlWindow : IDisposable
{
    private const int Width = 1280;
    private const int Height = 720;
    private const int FrameMilliseconds = 16;

    private readonly AutoPongGame _game = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly IntPtr _window;
    private TimeSpan _lastFrame;
    private bool _running;

    public BrowserSdlWindow()
    {
        if (!SDL_Init(0x00000020)) // SDL_INIT_VIDEO
        {
            throw new InvalidOperationException(
                $"SDL_Init failed: {Marshal.PtrToStringAnsi(SDL_GetError())}"
            );
        }

        _window = SDL_CreateWindow($"BgfXna AutoPong - {SamplePlatform.Backend}", Width, Height, 0);
        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"SDL_CreateWindow failed: {Marshal.PtrToStringAnsi(SDL_GetError())}"
            );
        }

        // On WebAssembly, SDL3 binds to the canvas and manages input.
        // We set the native window handle to IntPtr.Zero so that BGFX binds to "#canvas"
        _game.SetNativeWindowHandle(IntPtr.Zero, IntPtr.Zero);
    }

    public async Task RunAsync()
    {
        _running = true;
        _lastFrame = _clock.Elapsed;

        while (_running)
        {
            SDL_Event ev;
            while (SDL_PollEvent(out ev))
            {
                if (ev.type == 0x100) // SDL_QUIT
                {
                    _running = false;
                }
            }

            TimeSpan total = _clock.Elapsed;
            if (total - _lastFrame < TimeSpan.FromMilliseconds(FrameMilliseconds))
            {
                await Task.Delay(1);
                continue;
            }

            TimeSpan elapsed = total - _lastFrame;
            _lastFrame = total;
            _game.Tick(new GameTime(total, elapsed));

            if (!_game.IsActive)
            {
                _running = false;
            }

            await Task.Yield();
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

    // SDL3 P/Invokes
    [DllImport("libSDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SDL_Init(uint flags);

    [DllImport("libSDL3")]
    private static extern IntPtr SDL_CreateWindow(string title, int w, int h, ulong flags);

    [DllImport("libSDL3")]
    private static extern void SDL_DestroyWindow(IntPtr window);

    [DllImport("libSDL3")]
    private static extern void SDL_Quit();

    [DllImport("libSDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SDL_PollEvent(out SDL_Event ev);

    [DllImport("libSDL3")]
    private static extern IntPtr SDL_GetError();

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct SDL_Event
    {
        [FieldOffset(0)]
        public uint type;
    }
}
#endif
