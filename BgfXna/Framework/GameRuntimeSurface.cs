using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework;

public interface IGameComponent
{
    void Initialize();
}

public interface IUpdateable
{
    bool Enabled { get; }
    int UpdateOrder { get; }
    event EventHandler<EventArgs>? EnabledChanged;
    event EventHandler<EventArgs>? UpdateOrderChanged;
    void Update(GameTime gameTime);
}

public interface IDrawable
{
    bool Visible { get; }
    int DrawOrder { get; }
    event EventHandler<EventArgs>? DrawOrderChanged;
    event EventHandler<EventArgs>? VisibleChanged;
    void Draw(GameTime gameTime);
}

public class GameComponent : IGameComponent, IUpdateable, IDisposable
{
    private bool _enabled = true;
    private int _updateOrder;

    public GameComponent(Game game) => Game = game;
    public Game Game { get; }
    public bool Enabled { get => _enabled; set { if (_enabled != value) { _enabled = value; EnabledChanged?.Invoke(this, EventArgs.Empty); } } }
    public int UpdateOrder { get => _updateOrder; set { if (_updateOrder != value) { _updateOrder = value; UpdateOrderChanged?.Invoke(this, EventArgs.Empty); } } }
    public event EventHandler<EventArgs>? EnabledChanged;
    public event EventHandler<EventArgs>? UpdateOrderChanged;
    public virtual void Initialize() { }
    public virtual void Update(GameTime gameTime) { }
    public void Dispose() => Dispose(true);
    protected virtual void Dispose(bool disposing) { }
}

public class DrawableGameComponent : GameComponent, IDrawable
{
    private int _drawOrder;
    private bool _visible = true;

    public DrawableGameComponent(Game game) : base(game) { }
    public bool Visible { get => _visible; set { if (_visible != value) { _visible = value; VisibleChanged?.Invoke(this, EventArgs.Empty); } } }
    public int DrawOrder { get => _drawOrder; set { if (_drawOrder != value) { _drawOrder = value; DrawOrderChanged?.Invoke(this, EventArgs.Empty); } } }
    public event EventHandler<EventArgs>? DrawOrderChanged;
    public event EventHandler<EventArgs>? VisibleChanged;
    public virtual void Draw(GameTime gameTime) { }
}

public sealed class GameComponentCollection : CollectionBase
{
    public event EventHandler<GameComponentCollectionEventArgs>? ComponentAdded;
    public event EventHandler<GameComponentCollectionEventArgs>? ComponentRemoved;
    public IGameComponent this[int index] => (IGameComponent)List[index]!;
    public int Add(IGameComponent component)
    {
        int index = List.Add(component);
        ComponentAdded?.Invoke(this, new GameComponentCollectionEventArgs(component));
        return index;
    }

    public void Remove(IGameComponent component)
    {
        List.Remove(component);
        ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(component));
    }
}

public sealed class GameComponentCollectionEventArgs : EventArgs
{
    public GameComponentCollectionEventArgs(IGameComponent gameComponent) => GameComponent = gameComponent;
    public IGameComponent GameComponent { get; }
}

public sealed class GameServiceContainer : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();
    public void AddService(Type type, object provider) => _services[type] = provider;
    public void RemoveService(Type type) => _services.Remove(type);
    public object? GetService(Type serviceType) => _services.TryGetValue(serviceType, out object? service) ? service : null;
}

public abstract class GameWindow
{
    public abstract IntPtr Handle { get; }
    public abstract string Title { get; set; }
    public abstract Rectangle ClientBounds { get; }
    public bool AllowUserResizing { get; set; }
    public DisplayOrientation CurrentOrientation { get; protected set; }
    public event EventHandler<EventArgs>? ClientSizeChanged;
    public event EventHandler<EventArgs>? OrientationChanged;
    public event EventHandler<EventArgs>? ScreenDeviceNameChanged;
    protected void OnClientSizeChanged() => ClientSizeChanged?.Invoke(this, EventArgs.Empty);
}

public static class FrameworkDispatcher
{
    public static void Update() { }
}

public sealed class GraphicsDeviceInformation
{
    public Graphics.GraphicsAdapter Adapter { get; set; } = Graphics.GraphicsAdapter.DefaultAdapter;
    public Graphics.GraphicsDevice? GraphicsDevice { get; set; }
    public Graphics.GraphicsProfile GraphicsProfile { get; set; } = Graphics.GraphicsProfile.HiDef;
    public Graphics.PresentationParameters PresentationParameters { get; set; } = new();
}

public sealed class PreparingDeviceSettingsEventArgs : EventArgs
{
    public PreparingDeviceSettingsEventArgs(GraphicsDeviceInformation graphicsDeviceInformation) => GraphicsDeviceInformation = graphicsDeviceInformation;
    public GraphicsDeviceInformation GraphicsDeviceInformation { get; }
}

public interface IGraphicsDeviceManager
{
    bool BeginDraw();
    void CreateDevice();
    void EndDraw();
}

public sealed class LaunchParameters : Dictionary<string, string> { }
