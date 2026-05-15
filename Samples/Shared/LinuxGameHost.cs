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
        using X11AutoPongWindow window = new();
        window.Run();
    }
}

internal sealed class X11AutoPongWindow : IDisposable
{
    private const int Width = 1280;
    private const int Height = 720;
    private const int FrameMilliseconds = 16;
    private const int ClientMessage = 33;
    private const int DestroyNotify = 17;
    private const long KeyPressMask = 1;
    private const long StructureNotifyMask = 1 << 17;
    private const long ExposureMask = 1 << 15;

    private readonly AutoPongGame _game = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly IntPtr _display;
    private readonly IntPtr _window;
    private readonly IntPtr _wmDeleteWindow;
    private TimeSpan _lastFrame;
    private bool _running;

    public X11AutoPongWindow()
    {
        _display = XOpenDisplay(IntPtr.Zero);
        if (_display == IntPtr.Zero)
        {
            throw new InvalidOperationException("XOpenDisplay failed. Ensure DISPLAY is set and an X11 server is available.");
        }

        int screen = XDefaultScreen(_display);
        IntPtr root = XRootWindow(_display, screen);
        ulong black = XBlackPixel(_display, screen);
        ulong white = XWhitePixel(_display, screen);
        _window = XCreateSimpleWindow(_display, root, 0, 0, Width, Height, 0, black, white);
        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException("XCreateSimpleWindow failed.");
        }

        XStoreName(_display, _window, $"BgfXna AutoPong - {SamplePlatform.Backend}");
        XSelectInput(_display, _window, KeyPressMask | StructureNotifyMask | ExposureMask);
        _wmDeleteWindow = XInternAtom(_display, "WM_DELETE_WINDOW", false);
        IntPtr protocol = _wmDeleteWindow;
        XSetWMProtocols(_display, _window, ref protocol, 1);
        XMapWindow(_display, _window);
        XFlush(_display);
        _game.SetNativeWindowHandle(_window, _display);
    }

    public void Run()
    {
        _running = true;
        while (_running)
        {
            while (XPending(_display) > 0)
            {
                XNextEvent(_display, out XEvent ev);
                if (ev.Type == DestroyNotify || (ev.Type == ClientMessage && ev.ClientData0 == _wmDeleteWindow))
                {
                    _running = false;
                }
            }

            TimeSpan total = _clock.Elapsed;
            if (total - _lastFrame < TimeSpan.FromMilliseconds(FrameMilliseconds))
            {
                Sleep(1);
                continue;
            }

            TimeSpan elapsed = total - _lastFrame;
            _lastFrame = total;
            _game.Tick(new GameTime(total, elapsed));
            XStoreName(_display, _window, $"BgfXna AutoPong - requested {SamplePlatform.Backend}, actual {_game.GraphicsDevice.BackendName}");

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
            XDestroyWindow(_display, _window);
        }

        if (_display != IntPtr.Zero)
        {
            XCloseDisplay(_display);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 192)]
    private struct XEvent
    {
        [FieldOffset(0)]
        public int Type;

        [FieldOffset(56)]
        public IntPtr ClientData0;
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport("libX11.so.6")]
    private static extern int XDefaultScreen(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XRootWindow(IntPtr display, int screenNumber);

    [DllImport("libX11.so.6")]
    private static extern ulong XBlackPixel(IntPtr display, int screenNumber);

    [DllImport("libX11.so.6")]
    private static extern ulong XWhitePixel(IntPtr display, int screenNumber);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, uint width, uint height, uint borderWidth, ulong border, ulong background);

    [DllImport("libX11.so.6")]
    private static extern int XStoreName(IntPtr display, IntPtr window, string windowName);

    [DllImport("libX11.so.6")]
    private static extern int XSelectInput(IntPtr display, IntPtr window, long eventMask);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XInternAtom(IntPtr display, string atomName, bool onlyIfExists);

    [DllImport("libX11.so.6")]
    private static extern int XSetWMProtocols(IntPtr display, IntPtr window, ref IntPtr protocols, int count);

    [DllImport("libX11.so.6")]
    private static extern int XMapWindow(IntPtr display, IntPtr window);

    [DllImport("libX11.so.6")]
    private static extern int XFlush(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XPending(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XNextEvent(IntPtr display, out XEvent ev);

    [DllImport("libX11.so.6")]
    private static extern int XDestroyWindow(IntPtr display, IntPtr window);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    private static void Sleep(int milliseconds)
    {
        if (milliseconds > 0)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }
    }
}
#endif
