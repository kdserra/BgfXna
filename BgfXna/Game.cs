using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Content;

namespace Microsoft.Xna.Framework;

public abstract class Game : IDisposable
{
    private bool _disposed;
    private bool _initialized;
    private Graphics.GraphicsDeviceManager? _graphicsDeviceManager;

    protected Game()
    {
        Services = new GameServiceContainer();
        Components = new GameComponentCollection();
        Content = new ContentManager(Services);
        Window = new DefaultGameWindow();
        LaunchParameters = new LaunchParameters();
    }

    public GameComponentCollection Components { get; }
    public ContentManager Content { get; set; }
    public TimeSpan InactiveSleepTime { get; set; } = TimeSpan.FromMilliseconds(20);
    public bool IsActive { get; protected set; } = true;
    public bool IsFixedTimeStep { get; set; } = true;
    public bool IsMouseVisible { get; set; }
    public LaunchParameters LaunchParameters { get; }
    public GameServiceContainer Services { get; }
    public TimeSpan TargetElapsedTime { get; set; } = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
    public GameWindow Window { get; }
    public Graphics.GraphicsDevice GraphicsDevice => _graphicsDeviceManager?.GraphicsDevice ?? throw new InvalidOperationException("No GraphicsDeviceManager has been registered for this game.");

    public void Run()
    {
        InitializeOnce();

        Stopwatch clock = Stopwatch.StartNew();
        TimeSpan last = TimeSpan.Zero;

        for (int frame = 0; frame < 180; frame++)
        {
            TimeSpan total = clock.Elapsed;
            TimeSpan elapsed = IsFixedTimeStep ? TargetElapsedTime : total - last;
            last = total;

            GameTime gameTime = new(total, elapsed);
            Update(gameTime);
            Draw(gameTime);
            PresentFrame();
        }
    }

    public void Tick(GameTime gameTime)
    {
        InitializeOnce();
        Update(gameTime);
        Draw(gameTime);
        PresentFrame();
    }

    public void Exit()
    {
        IsActive = false;
    }

    internal void RegisterGraphicsDeviceManager(Graphics.GraphicsDeviceManager graphicsDeviceManager)
    {
        _graphicsDeviceManager = graphicsDeviceManager;
    }

    protected virtual void Initialize()
    {
        foreach (IGameComponent component in Components)
        {
            component.Initialize();
        }
    }

    protected virtual void LoadContent()
    {
    }

    protected virtual void UnloadContent()
    {
        Content.Unload();
    }

    protected virtual void Update(GameTime gameTime)
    {
        foreach (IGameComponent component in Components)
        {
            if (component is IUpdateable { Enabled: true } updateable)
            {
                updateable.Update(gameTime);
            }
        }
    }

    protected virtual void Draw(GameTime gameTime)
    {
        foreach (IGameComponent component in Components)
        {
            if (component is IDrawable { Visible: true } drawable)
            {
                drawable.Draw(gameTime);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnloadContent();
        Dispose(true);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    private void InitializeOnce()
    {
        if (_initialized)
        {
            return;
        }

        Initialize();
        LoadContent();
        _initialized = true;
    }

    private void PresentFrame()
    {
        if (_graphicsDeviceManager is not null)
        {
            _graphicsDeviceManager.GraphicsDevice.Present();
        }
    }

    private sealed class DefaultGameWindow : GameWindow
    {
        public override IntPtr Handle => IntPtr.Zero;
        public override string Title { get; set; } = string.Empty;
        public override Rectangle ClientBounds => Rectangle.Empty;
    }
}

public sealed class GameTime
{
    public GameTime()
        : this(TimeSpan.Zero, TimeSpan.Zero)
    {
    }

    public GameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime)
    {
        TotalGameTime = totalGameTime;
        ElapsedGameTime = elapsedGameTime;
    }

    public TimeSpan TotalGameTime { get; }
    public TimeSpan ElapsedGameTime { get; }
    public bool IsRunningSlowly { get; init; }
}
