#if ANDROID
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Microsoft.Xna.Framework;

namespace BgfXna.Samples;

internal sealed class AndroidGameHost : SurfaceView, ISurfaceHolderCallback
{
    private const string LogTag = "BgfXna";
    private readonly object _gate = new();
    private AutoPongGame? _game;
    private Thread? _renderThread;
    private IntPtr _nativeWindow;
    private volatile bool _running;

    public AndroidGameHost(Context context)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(Holder);
        Holder.AddCallback(this);
        Focusable = true;
        FocusableInTouchMode = true;
    }

    public void SurfaceCreated(ISurfaceHolder holder)
    {
        Start(holder);
    }

    public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
    {
        Start(holder);
    }

    public void SurfaceDestroyed(ISurfaceHolder holder)
    {
        Stop();
    }

    public void Stop()
    {
        Thread? renderThread;
        AutoPongGame? game;
        IntPtr nativeWindow;

        lock (_gate)
        {
            _running = false;
            renderThread = _renderThread;
            _renderThread = null;
            game = _game;
            _game = null;
            nativeWindow = _nativeWindow;
            _nativeWindow = IntPtr.Zero;
        }

        if (renderThread is not null && renderThread != Thread.CurrentThread)
        {
            renderThread.Join();
        }

        game?.Dispose();

        if (nativeWindow != IntPtr.Zero)
        {
            ANativeWindow_release(nativeWindow);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stop();
            Holder?.RemoveCallback(this);
        }

        base.Dispose(disposing);
    }

    private void Start(ISurfaceHolder holder)
    {
        lock (_gate)
        {
            if (_running || holder.Surface is null || !holder.Surface.IsValid)
            {
                return;
            }

            _nativeWindow = ANativeWindow_fromSurface(JNIEnv.Handle, holder.Surface!.Handle);
            if (_nativeWindow == IntPtr.Zero)
            {
                throw new InvalidOperationException("ANativeWindow_fromSurface returned null.");
            }

            _game = new AutoPongGame();
            _game.SetNativeWindowHandle(_nativeWindow);
            _running = true;
            _renderThread = new Thread(RenderLoop)
            {
                IsBackground = true,
                Name = "BgfXna Android Render"
            };
            _renderThread.Start();
        }
    }

    private void RenderLoop()
    {
        Stopwatch clock = Stopwatch.StartNew();
        TimeSpan previous = TimeSpan.Zero;

        while (_running)
        {
            try
            {
                AutoPongGame? game = _game;
                if (game is null)
                {
                    return;
                }

                TimeSpan total = clock.Elapsed;
                TimeSpan elapsed = total - previous;
                previous = total;
                game.Tick(new GameTime(total, elapsed));
                Thread.Sleep(1);
            }
            catch (Exception ex)
            {
                Log.Error(LogTag, ex.ToString());
                _running = false;
            }
        }
    }

    [DllImport("android")]
    private static extern IntPtr ANativeWindow_fromSurface(IntPtr env, IntPtr surface);

    [DllImport("android")]
    private static extern void ANativeWindow_release(IntPtr window);
}
#endif
