namespace Microsoft.Xna.Framework.Graphics;

public enum GraphicsBackend
{
    Auto,
    Direct3D11,
    Direct3D12,
    Metal,
    Vulkan,
    OpenGL,
    OpenGLES,
    WebGL,
    WebGPU,
    Noop
}

public enum SurfaceFormat
{
    Color,
    Bgr565,
    Bgra5551,
    Bgra4444,
    Dxt1,
    Dxt3,
    Dxt5,
    NormalizedByte2,
    NormalizedByte4,
    Rgba1010102,
    Rg32,
    Rgba64,
    Alpha8,
    Single,
    Vector2,
    Vector4,
    HalfSingle,
    HalfVector2,
    HalfVector4,
    HdrBlendable,
    ColorBgraEXT,
    ColorSrgbEXT,
    Dxt5SrgbEXT,
    Bc7EXT,
    Bc7SrgbEXT,
    ByteEXT,
    UShortEXT
}

public enum DepthFormat
{
    None,
    Depth16,
    Depth24,
    Depth24Stencil8
}

public enum BufferUsage
{
    None,
    WriteOnly
}

public enum PrimitiveType
{
    TriangleList,
    TriangleStrip,
    LineList,
    LineStrip,
    PointListEXT
}

public enum IndexElementSize
{
    SixteenBits,
    ThirtyTwoBits
}

public enum VertexElementFormat
{
    Single,
    Vector2,
    Vector3,
    Vector4,
    Color,
    Byte4,
    Short2,
    Short4,
    NormalizedShort2,
    NormalizedShort4,
    HalfVector2,
    HalfVector4
}

public enum VertexElementUsage
{
    Position,
    Color,
    TextureCoordinate,
    Normal,
    Binormal,
    Tangent,
    BlendIndices,
    BlendWeight,
    Depth,
    Fog,
    PointSize,
    Sample,
    TessellateFactor
}

public enum Blend
{
    Zero,
    One,
    SourceColor,
    InverseSourceColor,
    SourceAlpha,
    InverseSourceAlpha,
    DestinationColor,
    InverseDestinationColor,
    DestinationAlpha,
    InverseDestinationAlpha
}

public enum BlendFunction
{
    Add,
    Subtract,
    ReverseSubtract,
    Max,
    Min
}

public enum CompareFunction
{
    Always,
    Never,
    Less,
    LessEqual,
    Equal,
    GreaterEqual,
    Greater,
    NotEqual
}

public enum CullMode
{
    None,
    CullClockwiseFace,
    CullCounterClockwiseFace
}

public enum FillMode
{
    Solid,
    WireFrame
}

public enum TextureAddressMode
{
    Wrap,
    Clamp,
    Mirror
}

public enum TextureFilter
{
    Linear,
    Point,
    Anisotropic
}

public enum SpriteSortMode
{
    Deferred,
    Immediate,
    Texture,
    BackToFront,
    FrontToBack
}

public enum SpriteEffects
{
    None = 0,
    FlipHorizontally = 1,
    FlipVertically = 2
}

[System.Flags]
public enum ClearOptions
{
    Target = 1,
    DepthBuffer = 2,
    Stencil = 4
}

public enum CubeMapFace
{
    PositiveX,
    NegativeX,
    PositiveY,
    NegativeY,
    PositiveZ,
    NegativeZ
}

public enum GraphicsDeviceStatus
{
    Normal,
    Lost,
    NotReset
}

public enum GraphicsProfile
{
    Reach,
    HiDef
}

public enum PresentInterval
{
    Default = 0,
    One = 1,
    Two = 2,
    Immediate = 3
}

public enum RenderTargetUsage
{
    DiscardContents,
    PreserveContents,
    PlatformContents
}

public enum SetDataOptions
{
    None = 0,
    Discard = 1,
    NoOverwrite = 2
}

[System.Flags]
public enum ColorWriteChannels
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 4,
    Alpha = 8,
    All = 15
}

public enum StencilOperation
{
    Keep,
    Zero,
    Replace,
    Increment,
    Decrement,
    IncrementSaturation,
    DecrementSaturation,
    Invert
}
