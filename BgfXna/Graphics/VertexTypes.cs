using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics;

public interface IVertexType
{
    VertexDeclaration VertexDeclaration { get; }
}

[StructLayout(LayoutKind.Sequential)]
public struct VertexPositionColorTexture : IVertexType
{
    public static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

    public VertexPositionColorTexture(Vector3 position, Color color, Vector2 textureCoordinate)
    {
        Position = position;
        Color = color;
        TextureCoordinate = textureCoordinate;
    }

    public Vector3 Position;
    public Color Color;
    public Vector2 TextureCoordinate;
    public VertexDeclaration VertexDeclaration => Declaration;
}

[StructLayout(LayoutKind.Sequential)]
public struct VertexPositionColor : IVertexType
{
    public static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0));

    public VertexPositionColor(Vector3 position, Color color)
    {
        Position = position;
        Color = color;
    }

    public Vector3 Position;
    public Color Color;
    public VertexDeclaration VertexDeclaration => Declaration;
}
