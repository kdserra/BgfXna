using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class SpriteBatch : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<SpriteBatchItem> _items = new();
    private readonly BasicEffect _effect;
    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private bool _begun;
    private SpriteSortMode _sortMode;
    private BlendState _blendState = BlendState.AlphaBlend;
    private SamplerState _samplerState = SamplerState.LinearClamp;
    private DepthStencilState _depthStencilState = DepthStencilState.None;
    private RasterizerState _rasterizerState = RasterizerState.CullNone;

    public SpriteBatch(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _effect = new BasicEffect(graphicsDevice);
        _vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColorTexture.Declaration, 4, BufferUsage.WriteOnly);
        _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);
        _indexBuffer.SetData(new ushort[] { 0, 1, 2, 2, 1, 3 });
    }

    public void Begin(
        SpriteSortMode sortMode = SpriteSortMode.Deferred,
        BlendState? blendState = null,
        SamplerState? samplerState = null,
        DepthStencilState? depthStencilState = null,
        RasterizerState? rasterizerState = null,
        Effect? effect = null,
        Matrix? transformMatrix = null)
    {
        if (_begun)
        {
            throw new InvalidOperationException("Begin cannot be called again until End has been called.");
        }

        _begun = true;
        _sortMode = sortMode;
        _blendState = blendState ?? BlendState.AlphaBlend;
        _samplerState = samplerState ?? SamplerState.LinearClamp;
        _depthStencilState = depthStencilState ?? DepthStencilState.None;
        _rasterizerState = rasterizerState ?? RasterizerState.CullNone;

        _effect.Projection = transformMatrix ?? Matrix.CreateOrthographicOffCenter(
            0,
            _graphicsDevice.Viewport.Width,
            _graphicsDevice.Viewport.Height,
            0,
            0,
            1);
    }

    public void Draw(Texture2D texture, Vector2 position, Color color)
    {
        Draw(texture, new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height), null, color);
    }

    public void Draw(Texture2D texture, Vector2 position, Rectangle sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
    {
        Rectangle destination = new(
            (int)(position.X - origin.X * scale),
            (int)(position.Y - origin.Y * scale),
            (int)(sourceRectangle.Width * scale),
            (int)(sourceRectangle.Height * scale));
        Draw(texture, destination, sourceRectangle, color, rotation, origin, effects, layerDepth);
    }

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Color color)
    {
        Draw(texture, destinationRectangle, null, color);
    }

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation = 0f, Vector2 origin = default, SpriteEffects effects = SpriteEffects.None, float layerDepth = 0f)
    {
        if (!_begun)
        {
            throw new InvalidOperationException("Begin must be called before Draw.");
        }

        _items.Add(new SpriteBatchItem(texture, destinationRectangle, sourceRectangle ?? new Rectangle(0, 0, texture.Width, texture.Height), color, rotation, origin, effects, layerDepth));

        if (_sortMode == SpriteSortMode.Immediate)
        {
            Flush();
        }
    }

    public void End()
    {
        if (!_begun)
        {
            throw new InvalidOperationException("Begin must be called before End.");
        }

        SortItems();
        Flush();
        _items.Clear();
        _begun = false;
    }

    public void Dispose()
    {
        _effect.Dispose();
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    private void Flush()
    {
        BlendState oldBlend = _graphicsDevice.BlendState;
        DepthStencilState oldDepth = _graphicsDevice.DepthStencilState;
        RasterizerState oldRasterizer = _graphicsDevice.RasterizerState;

        _graphicsDevice.BlendState = _blendState;
        _graphicsDevice.DepthStencilState = _depthStencilState;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.SamplerStates[0] = _samplerState;

        if (_graphicsDevice.Backend is BgfxNativeBackend nativeBackend)
        {
            FlushNative(nativeBackend);
            _graphicsDevice.BlendState = oldBlend;
            _graphicsDevice.DepthStencilState = oldDepth;
            _graphicsDevice.RasterizerState = oldRasterizer;
            return;
        }

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        foreach (SpriteBatchItem item in _items)
        {
            _effect.Texture = item.Texture;
            _graphicsDevice.Textures[0] = item.Texture;
            _vertexBuffer.SetData(CreateQuad(item));
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2, _effect.InnerEffect);
        }

        _graphicsDevice.BlendState = oldBlend;
        _graphicsDevice.DepthStencilState = oldDepth;
        _graphicsDevice.RasterizerState = oldRasterizer;
    }

    private void FlushNative(BgfxNativeBackend nativeBackend)
    {
        if (_items.Count == 0)
        {
            return;
        }

        const int verticesPerSprite = 4;
        const int indicesPerSprite = 6;
        SpriteBatchVertex[] vertices = new SpriteBatchVertex[_items.Count * verticesPerSprite];
        ushort[] indices = new ushort[_items.Count * indicesPerSprite];
        Texture2D? texture = null;

        for (int i = 0; i < _items.Count; i++)
        {
            SpriteBatchItem item = _items[i];
            texture ??= item.Texture;
            CreateNativeQuad(item).CopyTo(vertices, i * verticesPerSprite);

            ushort vertexStart = (ushort)(i * verticesPerSprite);
            int indexStart = i * indicesPerSprite;
            indices[indexStart] = vertexStart;
            indices[indexStart + 1] = (ushort)(vertexStart + 1);
            indices[indexStart + 2] = (ushort)(vertexStart + 2);
            indices[indexStart + 3] = (ushort)(vertexStart + 2);
            indices[indexStart + 4] = (ushort)(vertexStart + 1);
            indices[indexStart + 5] = (ushort)(vertexStart + 3);
        }

        if (texture is not null)
        {
            nativeBackend.DrawSpriteBatch(0, vertices, indices, texture.Handle, _samplerState, new RenderStateSnapshot(_blendState, _depthStencilState, _rasterizerState, PrimitiveType.TriangleList));
        }
    }

    private void SortItems()
    {
        Comparison<SpriteBatchItem>? comparison = _sortMode switch
        {
            SpriteSortMode.Texture => static (a, b) => a.Texture.GetHashCode().CompareTo(b.Texture.GetHashCode()),
            SpriteSortMode.BackToFront => static (a, b) => b.LayerDepth.CompareTo(a.LayerDepth),
            SpriteSortMode.FrontToBack => static (a, b) => a.LayerDepth.CompareTo(b.LayerDepth),
            _ => null
        };

        if (comparison is not null)
        {
            _items.Sort(comparison);
        }
    }

    private static VertexPositionColorTexture[] CreateQuad(SpriteBatchItem item)
    {
        Rectangle d = item.Destination;
        Rectangle s = item.Source;
        float left = d.X;
        float top = d.Y;
        float right = d.X + d.Width;
        float bottom = d.Y + d.Height;
        float u0 = (float)s.X / item.Texture.Width;
        float v0 = (float)s.Y / item.Texture.Height;
        float u1 = (float)(s.X + s.Width) / item.Texture.Width;
        float v1 = (float)(s.Y + s.Height) / item.Texture.Height;

        if ((item.Effects & SpriteEffects.FlipHorizontally) != 0)
        {
            (u0, u1) = (u1, u0);
        }

        if ((item.Effects & SpriteEffects.FlipVertically) != 0)
        {
            (v0, v1) = (v1, v0);
        }

        return new[]
        {
            new VertexPositionColorTexture(new Vector3(left, top, item.LayerDepth), item.Color, new Vector2(u0, v0)),
            new VertexPositionColorTexture(new Vector3(right, top, item.LayerDepth), item.Color, new Vector2(u1, v0)),
            new VertexPositionColorTexture(new Vector3(left, bottom, item.LayerDepth), item.Color, new Vector2(u0, v1)),
            new VertexPositionColorTexture(new Vector3(right, bottom, item.LayerDepth), item.Color, new Vector2(u1, v1))
        };
    }

    private static SpriteBatchVertex[] CreateNativeQuad(SpriteBatchItem item)
    {
        Rectangle d = item.Destination;
        Rectangle s = item.Source;
        float left = d.X;
        float top = d.Y;
        float right = d.X + d.Width;
        float bottom = d.Y + d.Height;
        float u0 = (float)s.X / item.Texture.Width;
        float v0 = (float)s.Y / item.Texture.Height;
        float u1 = (float)(s.X + s.Width) / item.Texture.Width;
        float v1 = (float)(s.Y + s.Height) / item.Texture.Height;

        if ((item.Effects & SpriteEffects.FlipHorizontally) != 0)
        {
            (u0, u1) = (u1, u0);
        }

        if ((item.Effects & SpriteEffects.FlipVertically) != 0)
        {
            (v0, v1) = (v1, v0);
        }

        Vector4 color = item.Color.ToVector4();
        return new[]
        {
            new SpriteBatchVertex(new Vector3(left, top, item.LayerDepth), color, new Vector2(u0, v0)),
            new SpriteBatchVertex(new Vector3(right, top, item.LayerDepth), color, new Vector2(u1, v0)),
            new SpriteBatchVertex(new Vector3(left, bottom, item.LayerDepth), color, new Vector2(u0, v1)),
            new SpriteBatchVertex(new Vector3(right, bottom, item.LayerDepth), color, new Vector2(u1, v1))
        };
    }

    private readonly record struct SpriteBatchItem(Texture2D Texture, Rectangle Destination, Rectangle Source, Color Color, float Rotation, Vector2 Origin, SpriteEffects Effects, float LayerDepth);
}
