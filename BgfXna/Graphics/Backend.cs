using System;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics;

public readonly record struct BgfxHandle(ushort Id)
{
    public static BgfxHandle Invalid => new(ushort.MaxValue);
    public bool IsValid => Id != ushort.MaxValue;
}

public sealed class GraphicsDeviceOptions
{
    public GraphicsBackend Backend { get; init; } = GraphicsBackend.Auto;
    public IntPtr NativeDisplayHandle { get; init; }
    public IntPtr NativeWindowHandle { get; init; }
    public int BackBufferWidth { get; init; } = 1280;
    public int BackBufferHeight { get; init; } = 720;
    public SurfaceFormat BackBufferFormat { get; init; } = SurfaceFormat.Color;
    public DepthFormat DepthStencilFormat { get; init; } = DepthFormat.Depth24Stencil8;
    public bool Debug { get; init; }
    public bool VSync { get; init; } = true;
}

public readonly record struct BgfxCapabilities(
    GraphicsBackend Backend,
    bool SupportsCompute,
    bool SupportsInstancing,
    bool SupportsMultipleRenderTargets,
    bool OriginBottomLeft,
    bool HomogeneousDepth);

public interface IBgfxBackend : IDisposable
{
    BgfxCapabilities Capabilities { get; }
    void Initialize(GraphicsDeviceOptions options);
    void Reset(int width, int height, SurfaceFormat format, DepthFormat depthFormat, bool vsync);
    BgfxHandle CreateVertexBuffer(ReadOnlySpan<byte> data, VertexDeclaration declaration, BufferUsage usage);
    BgfxHandle CreateIndexBuffer(ReadOnlySpan<byte> data, IndexElementSize elementSize, BufferUsage usage);
    BgfxHandle CreateTexture2D(int width, int height, bool mipMap, SurfaceFormat format, ReadOnlySpan<byte> data);
    BgfxHandle CreateRenderTarget(int width, int height, SurfaceFormat format, DepthFormat depthFormat);
    BgfxHandle CreateShader(ReadOnlySpan<byte> shaderBytes, string? name);
    BgfxHandle CreateProgram(BgfxHandle vertexShader, BgfxHandle fragmentShader, bool destroyShaders);
    void Destroy(BgfxHandle handle);
    void SetViewClear(ushort viewId, Color color, float depth, byte stencil);
    void SetViewRect(ushort viewId, int x, int y, int width, int height);
    void SetRenderTarget(ushort viewId, BgfxHandle renderTarget);
    void Touch(ushort viewId);
    void SetState(RenderStateSnapshot state);
    void SetVertexBuffer(BgfxHandle handle, int vertexOffset, int vertexCount);
    void SetIndexBuffer(BgfxHandle handle, int indexOffset, int indexCount);
    void SetTexture(byte stage, BgfxHandle texture, SamplerState samplerState);
    void Submit(ushort viewId, BgfxHandle program);
    void Frame();
}

public sealed class NullBgfxBackend : IBgfxBackend
{
    private ushort _next = 1;

    public BgfxCapabilities Capabilities { get; private set; } = new(GraphicsBackend.Auto, false, false, false, false, true);

    public void Initialize(GraphicsDeviceOptions options)
    {
        Capabilities = new BgfxCapabilities(options.Backend, true, true, true, false, true);
    }

    public void Reset(int width, int height, SurfaceFormat format, DepthFormat depthFormat, bool vsync) { }
    public BgfxHandle CreateVertexBuffer(ReadOnlySpan<byte> data, VertexDeclaration declaration, BufferUsage usage) => Allocate();
    public BgfxHandle CreateIndexBuffer(ReadOnlySpan<byte> data, IndexElementSize elementSize, BufferUsage usage) => Allocate();
    public BgfxHandle CreateTexture2D(int width, int height, bool mipMap, SurfaceFormat format, ReadOnlySpan<byte> data) => Allocate();
    public BgfxHandle CreateRenderTarget(int width, int height, SurfaceFormat format, DepthFormat depthFormat) => Allocate();
    public BgfxHandle CreateShader(ReadOnlySpan<byte> shaderBytes, string? name) => Allocate();
    public BgfxHandle CreateProgram(BgfxHandle vertexShader, BgfxHandle fragmentShader, bool destroyShaders) => Allocate();
    public void Destroy(BgfxHandle handle) { }
    public void SetViewClear(ushort viewId, Color color, float depth, byte stencil) { }
    public void SetViewRect(ushort viewId, int x, int y, int width, int height) { }
    public void SetRenderTarget(ushort viewId, BgfxHandle renderTarget) { }
    public void Touch(ushort viewId) { }
    public void SetState(RenderStateSnapshot state) { }
    public void SetVertexBuffer(BgfxHandle handle, int vertexOffset, int vertexCount) { }
    public void SetIndexBuffer(BgfxHandle handle, int indexOffset, int indexCount) { }
    public void SetTexture(byte stage, BgfxHandle texture, SamplerState samplerState) { }
    public void Submit(ushort viewId, BgfxHandle program) { }
    public void Frame() { }
    public void Dispose() { }

    private BgfxHandle Allocate() => new(_next++);
}
