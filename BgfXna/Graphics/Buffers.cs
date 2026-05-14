using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class VertexBuffer : GraphicsResource
{
    internal BgfxHandle Handle { get; private set; }

    public VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage)
        : base(graphicsDevice)
    {
        VertexDeclaration = vertexDeclaration ?? throw new ArgumentNullException(nameof(vertexDeclaration));
        VertexCount = vertexCount;
        BufferUsage = bufferUsage;
        Handle = graphicsDevice.Backend.CreateVertexBuffer(ReadOnlySpan<byte>.Empty, vertexDeclaration, bufferUsage);
    }

    public VertexDeclaration VertexDeclaration { get; }
    public int VertexCount { get; }
    public BufferUsage BufferUsage { get; }

    public void SetData<T>(T[] data) where T : unmanaged => SetData(data.AsSpan());
    public void SetData<T>(T[] data, int startIndex, int elementCount) where T : unmanaged => SetData(data.AsSpan(startIndex, elementCount));
    public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : unmanaged => SetData(data.AsSpan(startIndex, elementCount));

    public void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        ThrowIfDisposed();
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(data);
        GraphicsDevice.Backend.Destroy(Handle);
        Handle = GraphicsDevice.Backend.CreateVertexBuffer(bytes, VertexDeclaration, BufferUsage);
    }

    protected override void Dispose(bool disposing) => GraphicsDevice.Backend.Destroy(Handle);
}

public class IndexBuffer : GraphicsResource
{
    internal BgfxHandle Handle { get; private set; }

    public IndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage bufferUsage)
        : base(graphicsDevice)
    {
        IndexElementSize = indexElementSize;
        IndexCount = indexCount;
        BufferUsage = bufferUsage;
        Handle = graphicsDevice.Backend.CreateIndexBuffer(ReadOnlySpan<byte>.Empty, indexElementSize, bufferUsage);
    }

    public IndexElementSize IndexElementSize { get; }
    public int IndexCount { get; }
    public BufferUsage BufferUsage { get; }

    public void SetData<T>(T[] data) where T : unmanaged => SetData(data.AsSpan());
    public void SetData<T>(T[] data, int startIndex, int elementCount) where T : unmanaged => SetData(data.AsSpan(startIndex, elementCount));
    public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount) where T : unmanaged => SetData(data.AsSpan(startIndex, elementCount));

    public void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        ThrowIfDisposed();
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(data);
        GraphicsDevice.Backend.Destroy(Handle);
        Handle = GraphicsDevice.Backend.CreateIndexBuffer(bytes, IndexElementSize, BufferUsage);
    }

    protected override void Dispose(bool disposing) => GraphicsDevice.Backend.Destroy(Handle);
}
