using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class BlendState
{
    public static BlendState Opaque { get; } = new() { ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.Zero, AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.Zero };
    public static BlendState AlphaBlend { get; } = new() { ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.InverseSourceAlpha, AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha };
    public static BlendState Additive { get; } = new() { ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.One, AlphaSourceBlend = Blend.SourceAlpha, AlphaDestinationBlend = Blend.One };
    public static BlendState NonPremultiplied { get; } = new() { ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha, AlphaSourceBlend = Blend.SourceAlpha, AlphaDestinationBlend = Blend.InverseSourceAlpha };

    public Blend ColorSourceBlend { get; set; } = Blend.One;
    public Blend ColorDestinationBlend { get; set; } = Blend.Zero;
    public Blend AlphaSourceBlend { get; set; } = Blend.One;
    public Blend AlphaDestinationBlend { get; set; } = Blend.Zero;
    public BlendFunction ColorBlendFunction { get; set; } = BlendFunction.Add;
    public BlendFunction AlphaBlendFunction { get; set; } = BlendFunction.Add;
    public ColorWriteChannels ColorWriteChannels { get; set; } = ColorWriteChannels.All;
    public ColorWriteChannels ColorWriteChannels1 { get; set; } = ColorWriteChannels.All;
    public ColorWriteChannels ColorWriteChannels2 { get; set; } = ColorWriteChannels.All;
    public ColorWriteChannels ColorWriteChannels3 { get; set; } = ColorWriteChannels.All;
    public Color BlendFactor { get; set; } = Color.White;
    public int MultiSampleMask { get; set; } = -1;
}

public sealed class DepthStencilState
{
    public static DepthStencilState Default { get; } = new();
    public static DepthStencilState None { get; } = new() { DepthBufferEnable = false, DepthBufferWriteEnable = false };
    public static DepthStencilState DepthRead { get; } = new() { DepthBufferEnable = true, DepthBufferWriteEnable = false };

    public bool DepthBufferEnable { get; set; } = true;
    public bool DepthBufferWriteEnable { get; set; } = true;
    public CompareFunction DepthBufferFunction { get; set; } = CompareFunction.LessEqual;
    public bool StencilEnable { get; set; }
    public CompareFunction StencilFunction { get; set; } = CompareFunction.Always;
    public StencilOperation StencilPass { get; set; } = StencilOperation.Keep;
    public StencilOperation StencilFail { get; set; } = StencilOperation.Keep;
    public StencilOperation StencilDepthBufferFail { get; set; } = StencilOperation.Keep;
    public int ReferenceStencil { get; set; }
    public int StencilMask { get; set; } = int.MaxValue;
    public int StencilWriteMask { get; set; } = int.MaxValue;
    public bool TwoSidedStencilMode { get; set; }
    public CompareFunction CounterClockwiseStencilFunction { get; set; } = CompareFunction.Always;
    public StencilOperation CounterClockwiseStencilFail { get; set; } = StencilOperation.Keep;
    public StencilOperation CounterClockwiseStencilPass { get; set; } = StencilOperation.Keep;
    public StencilOperation CounterClockwiseStencilDepthBufferFail { get; set; } = StencilOperation.Keep;
}

public sealed class RasterizerState
{
    public static RasterizerState CullCounterClockwise { get; } = new();
    public static RasterizerState CullClockwise { get; } = new() { CullMode = CullMode.CullClockwiseFace };
    public static RasterizerState CullNone { get; } = new() { CullMode = CullMode.None };

    public CullMode CullMode { get; set; } = CullMode.CullCounterClockwiseFace;
    public FillMode FillMode { get; set; } = FillMode.Solid;
    public bool ScissorTestEnable { get; set; }
    public float DepthBias { get; set; }
    public float SlopeScaleDepthBias { get; set; }
    public bool MultiSampleAntiAlias { get; set; } = true;
}

public sealed class SamplerState
{
    public static SamplerState LinearWrap { get; } = new();
    public static SamplerState LinearClamp { get; } = new() { AddressU = TextureAddressMode.Clamp, AddressV = TextureAddressMode.Clamp };
    public static SamplerState PointWrap { get; } = new() { Filter = TextureFilter.Point };
    public static SamplerState PointClamp { get; } = new() { Filter = TextureFilter.Point, AddressU = TextureAddressMode.Clamp, AddressV = TextureAddressMode.Clamp };
    public static SamplerState AnisotropicWrap { get; } = new() { Filter = TextureFilter.Anisotropic };
    public static SamplerState AnisotropicClamp { get; } = new() { Filter = TextureFilter.Anisotropic, AddressU = TextureAddressMode.Clamp, AddressV = TextureAddressMode.Clamp };

    public TextureFilter Filter { get; set; } = TextureFilter.Linear;
    public TextureAddressMode AddressU { get; set; } = TextureAddressMode.Wrap;
    public TextureAddressMode AddressV { get; set; } = TextureAddressMode.Wrap;
    public TextureAddressMode AddressW { get; set; } = TextureAddressMode.Wrap;
    public int MaxAnisotropy { get; set; } = 4;
    public int MaxMipLevel { get; set; }
    public float MipMapLevelOfDetailBias { get; set; }
}

public readonly record struct RenderStateSnapshot(
    BlendState BlendState,
    DepthStencilState DepthStencilState,
    RasterizerState RasterizerState,
    PrimitiveType PrimitiveType);
