using System;

namespace Microsoft.Xna.Framework.Graphics;

public abstract class GraphicsResource : IDisposable
{
    private bool _disposed;

    protected GraphicsResource(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
    }

    public GraphicsDevice GraphicsDevice { get; }
    public string? Name { get; set; }
    public object? Tag { get; set; }
    public bool IsDisposed => _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Dispose(true);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) { }

    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}
