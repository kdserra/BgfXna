#if WINDOWS_DESKTOP_HOST
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace BgfXna.Samples;

internal static class WindowsGameHost
{
    public static void Run()
    {
        using NativeAutoPongWindow window = new();
        window.Run();
    }
}

internal sealed class NativeAutoPongWindow : IDisposable
{
    private const string ClassName = "BgfXnaAutoPongWindow";
    private const int Width = 1280;
    private const int Height = 720;
    private const int FrameMilliseconds = 16;
    private const uint ColorWindow = 5;
    private const uint CsHredraw = 0x0002;
    private const uint CsVredraw = 0x0001;
    private const int CwUseDefault = unchecked((int)0x80000000);
    private const uint SwShow = 5;
    private const uint WmDestroy = 0x0002;
    private const uint WmKeyDown = 0x0100;
    private const uint WmClose = 0x0010;
    private const int VkEscape = 0x1B;
    private const uint PmRemove = 0x0001;
    private const uint WsOverlappedWindow = 0x00CF0000;
    private const int SwpNoZOrder = 0x0004;
    private const int SwpNoMove = 0x0002;

    private readonly AutoPongGame _game = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly WndProc _wndProc;
    private TimeSpan _lastFrame;
    private IntPtr _hwnd;
    private bool _running;

    public NativeAutoPongWindow()
    {
        _wndProc = WindowProc;
        RegisterWindowClass();
        IntPtr hInstance = GetModuleHandle(null);
        _hwnd = CreateWindowEx(
            0,
            ClassName,
            $"BgfXna AutoPong - {SamplePlatform.Backend}",
            WsOverlappedWindow,
            CwUseDefault,
            CwUseDefault,
            Width,
            Height,
            IntPtr.Zero,
            IntPtr.Zero,
            hInstance,
            IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException($"CreateWindowEx failed: {Marshal.GetLastWin32Error()}");
        }

        ResizeClientArea(Width, Height);
        _game.SetNativeWindowHandle(_hwnd);
    }

    public void Run()
    {
        ShowWindow(_hwnd, SwShow);
        UpdateWindow(_hwnd);
        _running = true;

        while (_running)
        {
            while (PeekMessage(out MSG message, IntPtr.Zero, 0, 0, PmRemove))
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
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
            SetWindowText(_hwnd, $"BgfXna AutoPong - requested {SamplePlatform.Backend}, actual {_game.GraphicsDevice.BackendName}");

            if (!_game.IsActive)
            {
                DestroyWindow(_hwnd);
                continue;
            }
        }
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
        {
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }

        _game.Dispose();
    }

    private IntPtr WindowProc(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam)
    {
        switch (message)
        {
            case WmKeyDown when wParam.ToUInt32() == VkEscape:
            case WmClose:
                DestroyWindow(hwnd);
                return IntPtr.Zero;
            case WmDestroy:
                _running = false;
                PostQuitMessage(0);
                return IntPtr.Zero;
            default:
                return DefWindowProc(hwnd, message, wParam, lParam);
        }
    }

    private void RegisterWindowClass()
    {
        WNDCLASSEX windowClass = new()
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            style = CsHredraw | CsVredraw,
            hInstance = GetModuleHandle(null),
            hCursor = LoadCursor(IntPtr.Zero, new IntPtr(32512)),
            hbrBackground = new IntPtr(ColorWindow + 1),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            lpszClassName = ClassName
        };

        ushort atom = RegisterClassEx(ref windowClass);
        if (atom == 0)
        {
            int error = Marshal.GetLastWin32Error();
            const int alreadyExists = 1410;
            if (error != alreadyExists)
            {
                throw new InvalidOperationException($"RegisterClassEx failed: {error}");
            }
        }
    }

    private void ResizeClientArea(int width, int height)
    {
        RECT rect = new() { Left = 0, Top = 0, Right = width, Bottom = height };
        AdjustWindowRectEx(ref rect, WsOverlappedWindow, false, 0);
        SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top, SwpNoMove | SwpNoZOrder);
    }

    private delegate IntPtr WndProc(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetWindowText(IntPtr hWnd, string lpString);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern void Sleep(uint dwMilliseconds);

}
#endif
