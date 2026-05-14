using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class GraphicsDevice : IDisposable
{
    private const ushort MainViewId = 0;
    private bool _disposed;
    private RenderTarget2D? _renderTarget;
    private RenderTargetBinding[] _renderTargetBindings = Array.Empty<RenderTargetBinding>();

    public GraphicsDevice(GraphicsDeviceOptions options)
        : this(options, new BgfxNativeBackend())
    {
    }

    public GraphicsDevice(GraphicsDeviceOptions options, IBgfxBackend backend)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Backend = backend ?? throw new ArgumentNullException(nameof(backend));
        Backend.Initialize(options);

        PresentationParameters = new PresentationParameters
        {
            BackBufferWidth = options.BackBufferWidth,
            BackBufferHeight = options.BackBufferHeight,
            BackBufferFormat = options.BackBufferFormat,
            DepthStencilFormat = options.DepthStencilFormat,
            SynchronizeWithVerticalRetrace = options.VSync
        };

        Viewport = new Viewport(0, 0, options.BackBufferWidth, options.BackBufferHeight);
        BlendState = BlendState.Opaque;
        DepthStencilState = DepthStencilState.Default;
        RasterizerState = RasterizerState.CullCounterClockwise;
        SamplerStates = new SamplerStateCollection();
        Textures = new TextureCollection();
    }

    internal IBgfxBackend Backend { get; }
    public GraphicsDeviceOptions Options { get; }
    public GraphicsAdapter Adapter { get; } = GraphicsAdapter.DefaultAdapter;
    public BgfxCapabilities Capabilities => Backend.Capabilities;
    public string BackendName => Backend is BgfxNativeBackend nativeBackend ? nativeBackend.ActualBackendName : Backend.Capabilities.Backend.ToString();
    public DisplayMode DisplayMode => Adapter.CurrentDisplayMode;
    public GraphicsDeviceStatus GraphicsDeviceStatus => GraphicsDeviceStatus.Normal;
    public GraphicsProfile GraphicsProfile { get; } = GraphicsProfile.HiDef;
    public PresentationParameters PresentationParameters { get; }
    public Viewport Viewport { get; set; }
    public BlendState BlendState { get; set; }
    public DepthStencilState DepthStencilState { get; set; }
    public RasterizerState RasterizerState { get; set; }
    public SamplerStateCollection SamplerStates { get; }
    public TextureCollection Textures { get; }
    public VertexBuffer? VertexBuffer { get; private set; }
    public IndexBuffer? Indices { get; set; }
    internal Effect? CurrentEffect { get; private set; }

    public event EventHandler<EventArgs>? DeviceLost;
    public event EventHandler<EventArgs>? DeviceReset;
    public event EventHandler<EventArgs>? DeviceResetting;
    public event EventHandler<ResourceCreatedEventArgs>? ResourceCreated;
    public event EventHandler<ResourceDestroyedEventArgs>? ResourceDestroyed;

    public void Clear(Color color) => Clear(color, 1f, 0);
    public void Clear(ClearOptions options, Color color, float depth, int stencil) => Clear(color, depth, stencil);

    public void Clear(Color color, float depth, int stencil)
    {
        ThrowIfDisposed();
        Backend.SetViewRect(MainViewId, Viewport.X, Viewport.Y, Viewport.Width, Viewport.Height);
        Backend.SetViewClear(MainViewId, color, depth, (byte)stencil);
        Backend.Touch(MainViewId);
    }

    public void Reset()
    {
        Reset(PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);
    }

    public void Reset(int width, int height)
    {
        ThrowIfDisposed();
        PresentationParameters.BackBufferWidth = width;
        PresentationParameters.BackBufferHeight = height;
        Viewport = new Viewport(0, 0, width, height);
        DeviceResetting?.Invoke(this, EventArgs.Empty);
        Backend.Reset(width, height, PresentationParameters.BackBufferFormat, PresentationParameters.DepthStencilFormat, PresentationParameters.SynchronizeWithVerticalRetrace);
        DeviceReset?.Invoke(this, EventArgs.Empty);
    }

    public void SetRenderTarget(RenderTarget2D? renderTarget)
    {
        ThrowIfDisposed();
        _renderTarget = renderTarget;
        Backend.SetRenderTarget(MainViewId, renderTarget?.Handle ?? BgfxHandle.Invalid);
        if (renderTarget is not null)
        {
            Viewport = new Viewport(0, 0, renderTarget.Width, renderTarget.Height);
        }
        else
        {
            Viewport = PresentationParameters.Bounds is Rectangle bounds ? new Viewport(bounds) : Viewport;
        }
    }

    public void SetRenderTargets(params RenderTargetBinding[] renderTargets)
    {
        ThrowIfDisposed();
        _renderTargetBindings = renderTargets ?? Array.Empty<RenderTargetBinding>();
        SetRenderTarget(_renderTargetBindings.Length == 0 ? null : _renderTargetBindings[0].RenderTarget as RenderTarget2D);
    }

    public RenderTargetBinding[] GetRenderTargets() => (RenderTargetBinding[])_renderTargetBindings.Clone();

    public void SetVertexBuffer(VertexBuffer? vertexBuffer)
    {
        ThrowIfDisposed();
        VertexBuffer = vertexBuffer;
    }

    public void SetVertexBuffers(params VertexBufferBinding[] vertexBuffers)
    {
        ThrowIfDisposed();
        if (vertexBuffers is null || vertexBuffers.Length == 0)
        {
            VertexBuffer = null;
            return;
        }

        VertexBuffer = vertexBuffers[0].VertexBuffer;
    }

    public void DrawPrimitives(PrimitiveType primitiveType, int vertexStart, int primitiveCount)
    {
        DrawPrimitives(primitiveType, vertexStart, primitiveCount, RequireCurrentEffect());
    }

    public void DrawPrimitives(PrimitiveType primitiveType, int vertexStart, int primitiveCount, Effect effect)
    {
        ThrowIfDisposed();
        if (VertexBuffer is null)
        {
            throw new InvalidOperationException("A vertex buffer must be set before drawing.");
        }

        int vertexCount = GetVertexCount(primitiveType, primitiveCount);
        ApplyBindings(primitiveType);
        Backend.SetVertexBuffer(VertexBuffer.Handle, vertexStart, vertexCount);
        effect.Apply(this);
        Backend.Submit(MainViewId, effect.ProgramHandle);
    }

    public void DrawIndexedPrimitives(PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount)
    {
        DrawIndexedPrimitives(primitiveType, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount, RequireCurrentEffect());
    }

    public void DrawIndexedPrimitives(PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount, Effect effect)
    {
        ThrowIfDisposed();
        if (VertexBuffer is null || Indices is null)
        {
            throw new InvalidOperationException("A vertex buffer and index buffer must be set before indexed drawing.");
        }

        ApplyBindings(primitiveType);
        Backend.SetVertexBuffer(VertexBuffer.Handle, baseVertex + minVertexIndex, numVertices);
        Backend.SetIndexBuffer(Indices.Handle, startIndex, GetIndexCount(primitiveType, primitiveCount));
        effect.Apply(this);
        Backend.Submit(MainViewId, effect.ProgramHandle);
    }

    public void DrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int primitiveCount)
        where T : unmanaged, IVertexType
    {
        DrawUserPrimitives(primitiveType, vertexData, vertexOffset, primitiveCount, vertexData[0].VertexDeclaration);
    }

    public void DrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int primitiveCount, VertexDeclaration vertexDeclaration)
        where T : unmanaged
    {
        ThrowIfDisposed();
        Effect effect = RequireCurrentEffect();
        int vertexCount = GetVertexCount(primitiveType, primitiveCount);
        using VertexBuffer vertexBuffer = new(this, vertexDeclaration, vertexData.Length, BufferUsage.WriteOnly);
        vertexBuffer.SetData(0, vertexData, vertexOffset, vertexCount, vertexDeclaration.VertexStride);
        VertexBuffer? previous = VertexBuffer;
        try
        {
            SetVertexBuffer(vertexBuffer);
            DrawPrimitives(primitiveType, 0, primitiveCount, effect);
        }
        finally
        {
            VertexBuffer = previous;
        }
    }

    public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, short[] indexData, int indexOffset, int primitiveCount)
        where T : unmanaged, IVertexType
    {
        DrawUserIndexedPrimitives(primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, vertexData[0].VertexDeclaration);
    }

    public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, short[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration)
        where T : unmanaged
    {
        DrawUserIndexedPrimitivesInternal(primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, vertexDeclaration, IndexElementSize.SixteenBits);
    }

    public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, int[] indexData, int indexOffset, int primitiveCount)
        where T : unmanaged, IVertexType
    {
        DrawUserIndexedPrimitives(primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, vertexData[0].VertexDeclaration);
    }

    public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, int[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration)
        where T : unmanaged
    {
        DrawUserIndexedPrimitivesInternal(primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, vertexDeclaration, IndexElementSize.ThirtyTwoBits);
    }

    public void Present()
    {
        ThrowIfDisposed();
        Backend.Frame();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Backend.Dispose();
        _disposed = true;
    }

    internal void SetCurrentEffect(Effect effect)
    {
        CurrentEffect = effect;
    }

    private void ApplyBindings(PrimitiveType primitiveType)
    {
        Backend.SetState(new RenderStateSnapshot(BlendState, DepthStencilState, RasterizerState, primitiveType));
        for (byte i = 0; i < Textures.Count; i++)
        {
            Texture? texture = Textures[i];
            if (texture is not null)
            {
                Backend.SetTexture(i, texture.Handle, SamplerStates[i] ?? SamplerState.LinearWrap);
            }
        }
    }

    private static int GetVertexCount(PrimitiveType primitiveType, int primitiveCount) => primitiveType switch
    {
        PrimitiveType.TriangleList => primitiveCount * 3,
        PrimitiveType.TriangleStrip => primitiveCount + 2,
        PrimitiveType.LineList => primitiveCount * 2,
        PrimitiveType.LineStrip => primitiveCount + 1,
        _ => throw new ArgumentOutOfRangeException(nameof(primitiveType), primitiveType, null)
    };

    private static int GetIndexCount(PrimitiveType primitiveType, int primitiveCount) => GetVertexCount(primitiveType, primitiveCount);

    private void DrawUserIndexedPrimitivesInternal<TVertex, TIndex>(PrimitiveType primitiveType, TVertex[] vertexData, int vertexOffset, int numVertices, TIndex[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration, IndexElementSize indexElementSize)
        where TVertex : unmanaged
        where TIndex : unmanaged
    {
        ThrowIfDisposed();
        Effect effect = RequireCurrentEffect();
        using VertexBuffer vertexBuffer = new(this, vertexDeclaration, vertexData.Length, BufferUsage.WriteOnly);
        using IndexBuffer indexBuffer = new(this, indexElementSize, indexData.Length, BufferUsage.WriteOnly);
        vertexBuffer.SetData(0, vertexData, vertexOffset, numVertices, vertexDeclaration.VertexStride);
        indexBuffer.SetData(0, indexData, indexOffset, GetIndexCount(primitiveType, primitiveCount));
        VertexBuffer? previousVertexBuffer = VertexBuffer;
        IndexBuffer? previousIndexBuffer = Indices;
        try
        {
            SetVertexBuffer(vertexBuffer);
            Indices = indexBuffer;
            DrawIndexedPrimitives(primitiveType, 0, 0, numVertices, 0, primitiveCount, effect);
        }
        finally
        {
            VertexBuffer = previousVertexBuffer;
            Indices = previousIndexBuffer;
        }
    }

    private Effect RequireCurrentEffect()
    {
        if (CurrentEffect is null)
        {
            throw new InvalidOperationException("No effect pass has been applied. Call EffectPass.Apply before drawing.");
        }

        return CurrentEffect;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(GraphicsDevice));
        }
    }
}

public sealed class TextureCollection
{
    private readonly Texture?[] _textures = new Texture?[16];

    public int Count => _textures.Length;

    public Texture? this[int index]
    {
        get => _textures[index];
        set => _textures[index] = value;
    }
}

public sealed class SamplerStateCollection
{
    private readonly SamplerState?[] _samplers = new SamplerState?[16];

    public SamplerState? this[int index]
    {
        get => _samplers[index];
        set => _samplers[index] = value;
    }
}
