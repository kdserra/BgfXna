using System;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class GraphicsDeviceManager : IDisposable, IGraphicsDeviceManager, IGraphicsDeviceService
{
    private readonly Game _game;
    private GraphicsDevice? _graphicsDevice;

    public GraphicsDeviceManager(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _game.RegisterGraphicsDeviceManager(this);
    }

    public int PreferredBackBufferWidth { get; set; } = 1280;
    public int PreferredBackBufferHeight { get; set; } = 720;
    public SurfaceFormat PreferredBackBufferFormat { get; set; } = SurfaceFormat.Color;
    public DepthFormat PreferredDepthStencilFormat { get; set; } = DepthFormat.Depth24Stencil8;
    public GraphicsProfile GraphicsProfile { get; set; } = GraphicsProfile.HiDef;
    public bool IsFullScreen { get; set; }
    public bool PreferMultiSampling { get; set; }
    public bool SynchronizeWithVerticalRetrace { get; set; } = true;
    public DisplayOrientation SupportedOrientations { get; set; } = DisplayOrientation.Default;
    public GraphicsBackend PreferredBackend { get; set; } = GraphicsBackend.Auto;
    public IntPtr NativeWindowHandle { get; set; }
    public GraphicsDevice GraphicsDevice
    {
        get
        {
            CreateDevice();
            return _graphicsDevice!;
        }
    }

    public event EventHandler<EventArgs>? DeviceCreated;
    public event EventHandler<EventArgs>? DeviceDisposing;
    public event EventHandler<EventArgs>? DeviceReset;
    public event EventHandler<EventArgs>? DeviceResetting;
    public event EventHandler<PreparingDeviceSettingsEventArgs>? PreparingDeviceSettings;

    public bool BeginDraw() => true;

    public void ApplyChanges()
    {
        if (_graphicsDevice is null)
        {
            return;
        }

        _graphicsDevice.Reset(PreferredBackBufferWidth, PreferredBackBufferHeight);
        DeviceReset?.Invoke(this, EventArgs.Empty);
    }

    public void CreateDevice()
    {
        if (_graphicsDevice is not null)
        {
            return;
        }

        _graphicsDevice = CreateDeviceCore();
        DeviceCreated?.Invoke(this, EventArgs.Empty);
    }

    public void EndDraw()
    {
    }

    public void Dispose()
    {
        DeviceDisposing?.Invoke(this, EventArgs.Empty);
        _graphicsDevice?.Dispose();
    }

    private GraphicsDevice CreateDeviceCore()
    {
        GraphicsDeviceInformation information = new()
        {
            GraphicsProfile = GraphicsProfile,
            PresentationParameters = new PresentationParameters
            {
                BackBufferWidth = PreferredBackBufferWidth,
                BackBufferHeight = PreferredBackBufferHeight,
                BackBufferFormat = PreferredBackBufferFormat,
                DepthStencilFormat = PreferredDepthStencilFormat,
                IsFullScreen = IsFullScreen,
                DeviceWindowHandle = NativeWindowHandle,
                PresentationInterval = SynchronizeWithVerticalRetrace ? PresentInterval.One : PresentInterval.Immediate,
                MultiSampleCount = PreferMultiSampling ? 4 : 0
            }
        };
        PreparingDeviceSettings?.Invoke(this, new PreparingDeviceSettingsEventArgs(information));

        PresentationParameters presentation = information.PresentationParameters;
        return new GraphicsDevice(new GraphicsDeviceOptions
        {
            Backend = PreferredBackend,
            BackBufferWidth = presentation.BackBufferWidth,
            BackBufferHeight = presentation.BackBufferHeight,
            BackBufferFormat = presentation.BackBufferFormat,
            DepthStencilFormat = presentation.DepthStencilFormat,
            NativeWindowHandle = presentation.DeviceWindowHandle,
            VSync = presentation.PresentationInterval != PresentInterval.Immediate
        });
    }
}
