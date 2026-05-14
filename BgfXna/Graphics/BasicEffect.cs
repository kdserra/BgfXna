using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class BasicEffect : GraphicsResource
{
    private readonly Effect _effect;

    public BasicEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
        _effect = new Effect(graphicsDevice, System.Array.Empty<byte>(), System.Array.Empty<byte>());
        TextureEnabled = true;
        VertexColorEnabled = true;
        World = Matrix.Identity;
        View = Matrix.Identity;
        Projection = Matrix.Identity;
    }

    public bool TextureEnabled { get; set; }
    public bool VertexColorEnabled { get; set; }
    public Texture2D? Texture { get; set; }
    public Matrix World { get; set; }
    public Matrix View { get; set; }
    public Matrix Projection { get; set; }
    internal Effect InnerEffect => _effect;

    public EffectTechnique CurrentTechnique => _effect.CurrentTechnique;

    protected override void Dispose(bool disposing) => _effect.Dispose();
}
