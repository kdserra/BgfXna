using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics;

public interface IGraphicsDeviceService
{
    GraphicsDevice GraphicsDevice { get; }
    event EventHandler<EventArgs>? DeviceCreated;
    event EventHandler<EventArgs>? DeviceDisposing;
    event EventHandler<EventArgs>? DeviceReset;
    event EventHandler<EventArgs>? DeviceResetting;
}

public sealed class GraphicsAdapter
{
    public static GraphicsAdapter DefaultAdapter { get; } = new();
    public static IReadOnlyList<GraphicsAdapter> Adapters { get; } = new[] { DefaultAdapter };
    public string Description { get; init; } = "Default";
    public int DeviceId { get; init; }
    public int VendorId { get; init; }
    public bool IsDefaultAdapter { get; init; } = true;
    public DisplayMode CurrentDisplayMode { get; init; } = new(1280, 720, SurfaceFormat.Color);
    public DisplayModeCollection SupportedDisplayModes { get; init; } = new(new List<DisplayMode> { new(1280, 720, SurfaceFormat.Color) });
    public bool IsProfileSupported(GraphicsProfile graphicsProfile) => true;
}

[Serializable]
public sealed class DisplayMode
{
    internal DisplayMode(int width, int height, SurfaceFormat format)
    {
        Width = width;
        Height = height;
        Format = format;
    }

    public int Width { get; }
    public int Height { get; }
    public SurfaceFormat Format { get; }
    public float AspectRatio => Height == 0 ? 0f : (float)Width / Height;
    public Rectangle TitleSafeArea => new(0, 0, Width, Height);
}

public sealed class DisplayModeCollection : IEnumerable<DisplayMode>
{
    private readonly List<DisplayMode> _modes;

    internal DisplayModeCollection(List<DisplayMode> modes) => _modes = modes;
    public IEnumerable<DisplayMode> this[SurfaceFormat format] => _modes.FindAll(mode => mode.Format == format);
    public IEnumerator<DisplayMode> GetEnumerator() => _modes.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class ResourceCreatedEventArgs : EventArgs
{
    public ResourceCreatedEventArgs(GraphicsResource resource) => Resource = resource;
    public GraphicsResource Resource { get; }
}

public sealed class ResourceDestroyedEventArgs : EventArgs
{
    public ResourceDestroyedEventArgs(string name, object? tag)
    {
        Name = name;
        Tag = tag;
    }

    public string Name { get; }
    public object? Tag { get; }
}

public sealed class DeviceLostException : Exception
{
    public DeviceLostException() { }
    public DeviceLostException(string message) : base(message) { }
}

public sealed class DeviceNotResetException : Exception
{
    public DeviceNotResetException() { }
    public DeviceNotResetException(string message) : base(message) { }
}

public sealed class NoSuitableGraphicsDeviceException : Exception
{
    public NoSuitableGraphicsDeviceException() { }
    public NoSuitableGraphicsDeviceException(string message) : base(message) { }
}

public readonly struct VertexBufferBinding
{
    public VertexBufferBinding(VertexBuffer vertexBuffer)
        : this(vertexBuffer, 0, 0) { }

    public VertexBufferBinding(VertexBuffer vertexBuffer, int vertexOffset)
        : this(vertexBuffer, vertexOffset, 0) { }

    public VertexBufferBinding(VertexBuffer vertexBuffer, int vertexOffset, int instanceFrequency)
    {
        VertexBuffer = vertexBuffer;
        VertexOffset = vertexOffset;
        InstanceFrequency = instanceFrequency;
    }

    public VertexBuffer VertexBuffer { get; }
    public int VertexOffset { get; }
    public int InstanceFrequency { get; }
}

public readonly struct RenderTargetBinding
{
    public RenderTargetBinding(RenderTarget2D renderTarget)
    {
        RenderTarget = renderTarget;
        ArraySlice = 0;
    }

    public RenderTargetBinding(RenderTargetCube renderTarget, CubeMapFace cubeMapFace)
    {
        RenderTarget = renderTarget;
        ArraySlice = (int)cubeMapFace;
    }

    public Texture RenderTarget { get; }
    public int ArraySlice { get; }
}

public class DynamicVertexBuffer : VertexBuffer
{
    public DynamicVertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, vertexDeclaration, vertexCount, bufferUsage) { }

    public DynamicVertexBuffer(GraphicsDevice graphicsDevice, Type vertexType, int vertexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, VertexDeclaration.FromType(vertexType), vertexCount, bufferUsage) { }

    public bool IsContentLost => false;
    public event EventHandler<EventArgs>? ContentLost;
    public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride, SetDataOptions options) where T : unmanaged => SetData(data.AsSpan(startIndex, elementCount));
}

public class DynamicIndexBuffer : IndexBuffer
{
    public DynamicIndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, indexElementSize, indexCount, bufferUsage) { }

    public DynamicIndexBuffer(GraphicsDevice graphicsDevice, Type indexType, int indexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, indexType == typeof(int) || indexType == typeof(uint) ? IndexElementSize.ThirtyTwoBits : IndexElementSize.SixteenBits, indexCount, bufferUsage) { }

    public bool IsContentLost => false;
    public event EventHandler<EventArgs>? ContentLost;
    public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, SetDataOptions options) where T : unmanaged => SetData(data.AsSpan(startIndex, elementCount));
}

public class Texture3D : Texture
{
    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat format)
        : base(graphicsDevice, graphicsDevice.Backend.CreateTexture2D(width, height, mipMap, format, ReadOnlySpan<byte>.Empty), format)
    {
        Width = width;
        Height = height;
        Depth = depth;
        LevelCount = mipMap ? 1 + (int)Math.Floor(Math.Log(Math.Max(width, Math.Max(height, depth)), 2)) : 1;
    }

    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }
    public int LevelCount { get; }
}

public class TextureCube : Texture
{
    public TextureCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
        : base(graphicsDevice, graphicsDevice.Backend.CreateTexture2D(size, size, mipMap, format, ReadOnlySpan<byte>.Empty), format)
    {
        Size = size;
        LevelCount = mipMap ? 1 + (int)Math.Floor(Math.Log(size, 2)) : 1;
    }

    public int Size { get; }
    public int LevelCount { get; }
}

public sealed class RenderTargetCube : TextureCube
{
    public RenderTargetCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
        : base(graphicsDevice, size, mipMap, preferredFormat)
    {
        DepthStencilFormat = preferredDepthFormat;
    }

    public DepthFormat DepthStencilFormat { get; }
}

public sealed class OcclusionQuery : GraphicsResource
{
    public OcclusionQuery(GraphicsDevice graphicsDevice) : base(graphicsDevice) { }
    public bool IsComplete => true;
    public int PixelCount => 0;
    public void Begin() { }
    public void End() { }
}

public class SpriteFont
{
    public int LineSpacing { get; set; }
    public float Spacing { get; set; }
    public char? DefaultCharacter { get; set; }
    public Vector2 MeasureString(string text) => new(text?.Length * 8 ?? 0, LineSpacing == 0 ? 16 : LineSpacing);
    public Vector2 MeasureString(System.Text.StringBuilder text) => MeasureString(text?.ToString() ?? string.Empty);
}

public sealed class DirectionalLight
{
    public Vector3 DiffuseColor { get; set; } = Vector3.One;
    public Vector3 Direction { get; set; } = Vector3.Down;
    public bool Enabled { get; set; }
    public Vector3 SpecularColor { get; set; } = Vector3.One;
}

public interface IEffectMatrices
{
    Matrix World { get; set; }
    Matrix View { get; set; }
    Matrix Projection { get; set; }
}

public interface IEffectFog
{
    bool FogEnabled { get; set; }
    float FogStart { get; set; }
    float FogEnd { get; set; }
    Vector3 FogColor { get; set; }
}

public interface IEffectLights
{
    bool LightingEnabled { get; set; }
    DirectionalLight DirectionalLight0 { get; }
    DirectionalLight DirectionalLight1 { get; }
    DirectionalLight DirectionalLight2 { get; }
    Vector3 AmbientLightColor { get; set; }
    bool PreferPerPixelLighting { get; set; }
    void EnableDefaultLighting();
}

public enum EffectParameterClass
{
    Scalar,
    Vector,
    Matrix,
    Object,
    Struct
}

public enum EffectParameterType
{
    Void,
    Bool,
    Int32,
    Single,
    String,
    Texture,
    Texture1D,
    Texture2D,
    Texture3D,
    TextureCube
}

public sealed class EffectAnnotation
{
    public string Name { get; init; } = string.Empty;
}

public sealed class EffectAnnotationCollection : List<EffectAnnotation>
{
    public EffectAnnotation? this[string name] => Find(annotation => annotation.Name == name);
}

public class Model
{
    public ModelBoneCollection Bones { get; init; } = new(Array.Empty<ModelBone>());
    public ModelMeshCollection Meshes { get; init; } = new(Array.Empty<ModelMesh>());
    public ModelBone? Root { get; init; }
    public object? Tag { get; set; }
    public void Draw(Matrix world, Matrix view, Matrix projection) { }
}

public sealed class ModelBone
{
    public int Index { get; init; }
    public string Name { get; init; } = string.Empty;
    public Matrix Transform { get; set; } = Matrix.Identity;
    public ModelBone? Parent { get; init; }
    public ModelBoneCollection Children { get; init; } = new(Array.Empty<ModelBone>());
}

public sealed class ModelMesh
{
    public string Name { get; init; } = string.Empty;
    public BoundingSphere BoundingSphere { get; init; }
    public ModelBone? ParentBone { get; init; }
    public ModelMeshPartCollection MeshParts { get; init; } = new(Array.Empty<ModelMeshPart>());
    public ModelEffectCollection Effects { get; init; } = new(Array.Empty<Effect>());
    public object? Tag { get; set; }
    public void Draw() { }
}

public sealed class ModelMeshPart
{
    public int BaseVertex { get; set; }
    public int NumVertices { get; set; }
    public int PrimitiveCount { get; set; }
    public int StartIndex { get; set; }
    public object? Tag { get; set; }
    public Effect? Effect { get; set; }
    public IndexBuffer? IndexBuffer { get; set; }
    public VertexBuffer? VertexBuffer { get; set; }
}

public sealed class ModelBoneCollection : IReadOnlyList<ModelBone>
{
    private readonly ModelBone[] _items;
    public ModelBoneCollection(IReadOnlyList<ModelBone> items) => _items = new List<ModelBone>(items).ToArray();
    public ModelBone this[int index] => _items[index];
    public ModelBone? this[string name] => Array.Find(_items, item => item.Name == name);
    public int Count => _items.Length;
    public IEnumerator<ModelBone> GetEnumerator() => ((IEnumerable<ModelBone>)_items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class ModelMeshCollection : IReadOnlyList<ModelMesh>
{
    private readonly ModelMesh[] _items;
    public ModelMeshCollection(IReadOnlyList<ModelMesh> items) => _items = new List<ModelMesh>(items).ToArray();
    public ModelMesh this[int index] => _items[index];
    public ModelMesh? this[string name] => Array.Find(_items, item => item.Name == name);
    public int Count => _items.Length;
    public IEnumerator<ModelMesh> GetEnumerator() => ((IEnumerable<ModelMesh>)_items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class ModelMeshPartCollection : IReadOnlyList<ModelMeshPart>
{
    private readonly ModelMeshPart[] _items;
    public ModelMeshPartCollection(IReadOnlyList<ModelMeshPart> items) => _items = new List<ModelMeshPart>(items).ToArray();
    public ModelMeshPart this[int index] => _items[index];
    public int Count => _items.Length;
    public IEnumerator<ModelMeshPart> GetEnumerator() => ((IEnumerable<ModelMeshPart>)_items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class ModelEffectCollection : IReadOnlyList<Effect>
{
    private readonly Effect[] _items;
    public ModelEffectCollection(IReadOnlyList<Effect> items) => _items = new List<Effect>(items).ToArray();
    public Effect this[int index] => _items[index];
    public int Count => _items.Length;
    public IEnumerator<Effect> GetEnumerator() => ((IEnumerable<Effect>)_items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
