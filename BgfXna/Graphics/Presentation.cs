using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class PresentationParameters
{
    public int BackBufferWidth { get; set; } = 1280;
    public int BackBufferHeight { get; set; } = 720;
    public SurfaceFormat BackBufferFormat { get; set; } = SurfaceFormat.Color;
    public DepthFormat DepthStencilFormat { get; set; } = DepthFormat.Depth24Stencil8;
    public IntPtr DeviceDisplayHandle { get; set; }
    public IntPtr DeviceWindowHandle { get; set; }
    public NativeWindowHandleKind DeviceWindowHandleKind { get; set; } = NativeWindowHandleKind.Default;
    public bool IsFullScreen { get; set; }
    public int MultiSampleCount { get; set; }
    public PresentInterval PresentationInterval { get; set; } = PresentInterval.One;
    public RenderTargetUsage RenderTargetUsage { get; set; } = RenderTargetUsage.DiscardContents;
    public bool SynchronizeWithVerticalRetrace { get; set; } = true;
    public Rectangle Bounds => new(0, 0, BackBufferWidth, BackBufferHeight);
    public PresentationParameters Clone() => (PresentationParameters)MemberwiseClone();
}

public readonly record struct Viewport(int X, int Y, int Width, int Height)
{
    public float MinDepth { get; init; } = 0f;
    public float MaxDepth { get; init; } = 1f;

    public Viewport(Rectangle bounds)
        : this(bounds.X, bounds.Y, bounds.Width, bounds.Height)
    {
    }

    public Rectangle Bounds => new(X, Y, Width, Height);
    public float AspectRatio => Height == 0 ? 0 : (float)Width / Height;
}
