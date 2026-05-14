#if IOS_GAME_HOST
using System;
using CoreAnimation;
using Foundation;
using ObjCRuntime;
using UIKit;
using Microsoft.Xna.Framework;

namespace BgfXna.Samples;

internal static class iOSGameHost
{
    public static void Run(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}

[Register("AppDelegate")]
internal sealed class AppDelegate : UIApplicationDelegate
{
    private UIWindow? _window;

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
#pragma warning disable CA1422
        _window = new UIWindow(UIScreen.MainScreen.Bounds)
        {
            RootViewController = new GameViewController()
        };
#pragma warning restore CA1422
        _window.MakeKeyAndVisible();
        return true;
    }
}

internal sealed class GameViewController : UIViewController
{
    private AutoPongGame? _game;
    private CADisplayLink? _displayLink;
    private DateTime _started;
    private DateTime _previous;

    public override void LoadView()
    {
        View = new BgfxGameView(UIScreen.MainScreen.Bounds);
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View!.BackgroundColor = UIColor.Black;
    }

    public override void ViewDidAppear(bool animated)
    {
        base.ViewDidAppear(animated);
        StartGame();
    }

    public override void ViewWillDisappear(bool animated)
    {
        StopGame();
        base.ViewWillDisappear(animated);
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();

        if (View?.Layer is CAMetalLayer metalLayer)
        {
            nfloat scale = UIScreen.MainScreen.Scale;
            metalLayer.Frame = View.Bounds;
            metalLayer.ContentsScale = scale;
            metalLayer.DrawableSize = new CoreGraphics.CGSize(
                View.Bounds.Width * scale,
                View.Bounds.Height * scale
            );
        }
    }

    private void StartGame()
    {
        if (_game is not null || View is null)
        {
            return;
        }

        _game = new AutoPongGame();
        _game.SetNativeWindowHandle(View.Layer.Handle);

        _started = DateTime.UtcNow;
        _previous = _started;
        _displayLink = CADisplayLink.Create(Tick);
        _displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
    }

    private void StopGame()
    {
        _displayLink?.Invalidate();
        _displayLink?.Dispose();
        _displayLink = null;
        _game?.Dispose();
        _game = null;
    }

    private void Tick()
    {
        AutoPongGame? game = _game;
        if (game is null)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        TimeSpan total = now - _started;
        TimeSpan elapsed = now - _previous;
        _previous = now;
        game.Tick(new GameTime(total, elapsed));
    }
}

[Register("BgfxGameView")]
internal sealed class BgfxGameView : UIView
{
    public BgfxGameView(CoreGraphics.CGRect frame)
        : base(frame)
    {
        ContentScaleFactor = UIScreen.MainScreen.Scale;
        MultipleTouchEnabled = true;
    }

    [Export("layerClass")]
    public static Class GetLayerClass()
    {
        return new Class(typeof(CAMetalLayer));
    }
}
#endif
