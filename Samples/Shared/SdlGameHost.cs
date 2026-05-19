#if WINDOWS_DESKTOP_HOST || LINUX_DESKTOP_HOST || MACOS_DESKTOP_HOST
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace BgfXna.Samples;

internal static class SdlGameHost
{
    public static void Run()
    {
        using SdlAutoPongWindow window = new();
        window.Run();
    }
}

internal sealed class SdlAutoPongWindow : IDisposable
{
    private const int Width = 1280;
    private const int Height = 720;
    private const int FrameMilliseconds = 16;

    private readonly AutoPongGame _game = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly IntPtr _window;
    private TimeSpan _lastFrame;
    private bool _running;

    static SdlAutoPongWindow()
    {
#if !NETSTANDARD
        System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(
            typeof(SdlAutoPongWindow).Assembly,
            (libraryName, assembly, searchPath) =>
            {
                if (libraryName == "SDL3")
                {
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    {
                        // 1. Try loading from the local application directory first to ensure bundled/local libraries are prioritized
                        string localPath0 = System.IO.Path.Combine(System.AppContext.BaseDirectory, "libSDL3.so.0");
                        if (System.Runtime.InteropServices.NativeLibrary.TryLoad(localPath0, assembly, searchPath, out IntPtr handle))
                        {
                            return handle;
                        }
                        
                        string localPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "libSDL3.so");
                        if (System.Runtime.InteropServices.NativeLibrary.TryLoad(localPath, assembly, searchPath, out handle))
                        {
                            return handle;
                        }

                        // 2. Fall back to standard search paths (system-wide and LD_LIBRARY_PATH)
                        if (System.Runtime.InteropServices.NativeLibrary.TryLoad("libSDL3.so.0", assembly, searchPath, out handle))
                        {
                            return handle;
                        }
                        if (System.Runtime.InteropServices.NativeLibrary.TryLoad("libSDL3.so", assembly, searchPath, out handle))
                        {
                            return handle;
                        }
                        if (System.Runtime.InteropServices.NativeLibrary.TryLoad("SDL3.so", assembly, searchPath, out handle))
                        {
                            return handle;
                        }
                    }
                    else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                    {
                        // 1. Try loading from the local application directory first to ensure bundled/local libraries are prioritized
                        string localPath0 = System.IO.Path.Combine(System.AppContext.BaseDirectory, "libSDL3.0.dylib");
                        if (System.Runtime.InteropServices.NativeLibrary.TryLoad(localPath0, assembly, searchPath, out IntPtr handle))
                        {
                            return handle;
                        }
                        
                        string localPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "libSDL3.dylib");
                        if (System.Runtime.InteropServices.NativeLibrary.TryLoad(localPath, assembly, searchPath, out handle))
                        {
                            return handle;
                        }

                        // 2. Fall back to standard search paths
                        if (System.Runtime.InteropServices.NativeLibrary.TryLoad("libSDL3.0.dylib", assembly, searchPath, out handle))
                        {
                            return handle;
                        }
                        if (System.Runtime.InteropServices.NativeLibrary.TryLoad("libSDL3.dylib", assembly, searchPath, out handle))
                        {
                            return handle;
                        }
                        if (System.Runtime.InteropServices.NativeLibrary.TryLoad("SDL3.dylib", assembly, searchPath, out handle))
                        {
                            return handle;
                        }
                    }
                }
                return IntPtr.Zero;
            }
        );
#endif
    }

    public SdlAutoPongWindow()
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

        SDL_ShowWindow(_window);

        uint props = SDL_GetWindowProperties(_window);
        IntPtr windowHandle = IntPtr.Zero;
        IntPtr displayHandle = IntPtr.Zero;

        // Try Windows
        windowHandle = SDL_GetPointerProperty(props, "SDL.window.win32.hwnd", IntPtr.Zero);

        // Try Cocoa (macOS)
        if (windowHandle == IntPtr.Zero)
        {
            windowHandle = SDL_GetPointerProperty(props, "SDL.window.cocoa.window", IntPtr.Zero);
        }

        // Try X11 (Linux)
        if (windowHandle == IntPtr.Zero)
        {
            windowHandle = SDL_GetPointerProperty(props, "SDL.window.x11.window", IntPtr.Zero);
            displayHandle = SDL_GetPointerProperty(props, "SDL.window.x11.display", IntPtr.Zero);
        }

        // Try Wayland (Linux)
        if (windowHandle == IntPtr.Zero)
        {
            windowHandle = SDL_GetPointerProperty(props, "SDL.window.wayland.surface", IntPtr.Zero);
            displayHandle = SDL_GetPointerProperty(
                props,
                "SDL.window.wayland.display",
                IntPtr.Zero
            );
            _game.SetNativeWindowHandleType(
                Microsoft.Xna.Framework.Graphics.NativeWindowHandleKind.Wayland
            );
        }

        if (windowHandle == IntPtr.Zero)
        {
            throw new NotSupportedException("Could not retrieve native window handle from SDL3.");
        }

        _game.SetNativeWindowHandle(windowHandle, displayHandle);
    }

    public void Run()
    {
        _running = true;
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
                System.Threading.Thread.Sleep(1);
                continue;
            }

            TimeSpan elapsed = total - _lastFrame;
            _lastFrame = total;
            _game.Tick(new GameTime(total, elapsed));
            SDL_SetWindowTitle(
                _window,
                $"BgfXna AutoPong - requested {SamplePlatform.Backend}, actual {_game.GraphicsDevice.BackendName}"
            );

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

    // SDL3 P/Invokes
    [DllImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SDL_Init(uint flags);

    [DllImport("SDL3")]
    private static extern IntPtr SDL_CreateWindow(string title, int w, int h, ulong flags);

    [DllImport("SDL3")]
    private static extern void SDL_ShowWindow(IntPtr window);

    [DllImport("SDL3")]
    private static extern void SDL_DestroyWindow(IntPtr window);

    [DllImport("SDL3")]
    private static extern void SDL_Quit();

    [DllImport("SDL3")]
    private static extern uint SDL_GetWindowProperties(IntPtr window);

    [DllImport("SDL3")]
    private static extern IntPtr SDL_GetPointerProperty(
        uint props,
        string name,
        IntPtr default_value
    );

    [DllImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SDL_PollEvent(out SDL_Event ev);

    [DllImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SDL_SetWindowTitle(IntPtr window, string title);

    [DllImport("SDL3")]
    private static extern IntPtr SDL_GetError();

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct SDL_Event
    {
        [FieldOffset(0)]
        public uint type;
    }
}
#endif
