using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class Texture : GraphicsResource
{
    internal Texture(GraphicsDevice graphicsDevice, BgfxHandle handle, SurfaceFormat format)
        : base(graphicsDevice)
    {
        Handle = handle;
        Format = format;
    }

    internal BgfxHandle Handle { get; set; }
    public SurfaceFormat Format { get; }

    protected override void Dispose(bool disposing) => GraphicsDevice.Backend.Destroy(Handle);
}

public class Texture2D : Texture
{
    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
        : this(graphicsDevice, width, height, false, SurfaceFormat.Color)
    {
    }

    public Texture2D(GraphicsDevice graphicsDevice, int width, int height, bool mipMap, SurfaceFormat format)
        : base(graphicsDevice, graphicsDevice.Backend.CreateTexture2D(width, height, mipMap, format, ReadOnlySpan<byte>.Empty), format)
    {
        Width = width;
        Height = height;
        LevelCount = mipMap ? CalculateMipLevels(width, height) : 1;
    }

    protected Texture2D(GraphicsDevice graphicsDevice, int width, int height, bool mipMap, SurfaceFormat format, BgfxHandle handle)
        : base(graphicsDevice, handle, format)
    {
        Width = width;
        Height = height;
        LevelCount = mipMap ? CalculateMipLevels(width, height) : 1;
    }

    public int Width { get; }
    public int Height { get; }
    public int LevelCount { get; }

    public void SetData<T>(T[] data) where T : unmanaged => SetData(data.AsSpan());
    public void SetData<T>(T[] data, int startIndex, int elementCount) where T : unmanaged => SetData(data.AsSpan(startIndex, elementCount));
    public void SetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : unmanaged => SetData(data.AsSpan(startIndex, elementCount));

    public void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        ThrowIfDisposed();
        ReadOnlySpan<byte> bytes;
        byte[]? serializedColor = null;
        if (typeof(T) == typeof(Color) && BgfxNativeBackend.IsBrowserRuntime())
        {
            serializedColor = new byte[data.Length * 4];
            ref T startRef = ref MemoryMarshal.GetReference(data);
            for (int i = 0; i < data.Length; i++)
            {
                ref T elemRef = ref System.Runtime.CompilerServices.Unsafe.Add(ref startRef, i);
                ref byte byteRef = ref System.Runtime.CompilerServices.Unsafe.As<T, byte>(ref elemRef);
                serializedColor[i * 4] = byteRef;
                serializedColor[i * 4 + 1] = System.Runtime.CompilerServices.Unsafe.Add(ref byteRef, 1);
                serializedColor[i * 4 + 2] = System.Runtime.CompilerServices.Unsafe.Add(ref byteRef, 2);
                serializedColor[i * 4 + 3] = System.Runtime.CompilerServices.Unsafe.Add(ref byteRef, 3);
            }
            bytes = serializedColor;
        }
        else
        {
            bytes = MemoryMarshal.AsBytes(data);
        }
        GraphicsDevice.Backend.Destroy(Handle);
        Handle = GraphicsDevice.Backend.CreateTexture2D(Width, Height, LevelCount > 1, Format, bytes);
    }

    public void GetData<T>(T[] data) where T : unmanaged => Array.Clear(data, 0, data.Length);
    public void GetData<T>(T[] data, int startIndex, int elementCount) where T : unmanaged => Array.Clear(data, startIndex, elementCount);
    public void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : unmanaged => GetData(data, startIndex, elementCount);

    public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
    {
        if (graphicsDevice is null)
        {
            throw new ArgumentNullException(nameof(graphicsDevice));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        throw new NotSupportedException("Texture decoding is not part of the XNA runtime surface in BgfXna. Decode image data externally and call SetData.");
    }

    public void SaveAsPng(Stream stream, int width, int height) => throw new NotSupportedException("Texture encoding is not implemented.");
    public void SaveAsJpeg(Stream stream, int width, int height) => throw new NotSupportedException("Texture encoding is not implemented.");

    private static int CalculateMipLevels(int width, int height)
    {
        int levels = 1;
        while (width > 1 || height > 1)
        {
            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
            levels++;
        }

        return levels;
    }
}

public sealed class RenderTarget2D : Texture2D
{
    public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height)
        : this(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8)
    {
    }

    public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
        : base(graphicsDevice, width, height, mipMap, preferredFormat, graphicsDevice.Backend.CreateRenderTarget(width, height, preferredFormat, preferredDepthFormat))
    {
        DepthStencilFormat = preferredDepthFormat;
    }

    public DepthFormat DepthStencilFormat { get; }
}
