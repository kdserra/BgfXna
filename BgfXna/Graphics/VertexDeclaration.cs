using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xna.Framework.Graphics;

public readonly struct VertexElement
{
    public VertexElement(int offset, VertexElementFormat elementFormat, VertexElementUsage elementUsage, int usageIndex)
    {
        Offset = offset;
        VertexElementFormat = elementFormat;
        VertexElementUsage = elementUsage;
        UsageIndex = usageIndex;
    }

    public int Offset { get; }
    public VertexElementFormat VertexElementFormat { get; }
    public VertexElementUsage VertexElementUsage { get; }
    public int UsageIndex { get; }
}

public sealed class VertexDeclaration
{
    private readonly VertexElement[] _elements;

    public VertexDeclaration(params VertexElement[] elements)
        : this(CalculateStride(elements), elements)
    {
    }

    public VertexDeclaration(int vertexStride, params VertexElement[] elements)
    {
        VertexStride = vertexStride;
        _elements = elements?.ToArray() ?? throw new ArgumentNullException(nameof(elements));
    }

    public int VertexStride { get; }
    public IReadOnlyList<VertexElement> GetVertexElements() => _elements;
    public static VertexDeclaration FromType(Type vertexType)
    {
        if (typeof(IVertexType).IsAssignableFrom(vertexType))
        {
            if (Activator.CreateInstance(vertexType) is IVertexType vertex)
            {
                return vertex.VertexDeclaration;
            }

            System.Reflection.FieldInfo? declaration = vertexType.GetField("Declaration", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (declaration?.GetValue(null) is VertexDeclaration value)
            {
                return value;
            }
        }

        throw new ArgumentException($"Type {vertexType.FullName} does not expose a vertex declaration.", nameof(vertexType));
    }

    private static int CalculateStride(IEnumerable<VertexElement> elements)
    {
        int stride = 0;
        foreach (VertexElement element in elements)
        {
            stride = Math.Max(stride, element.Offset + SizeOf(element.VertexElementFormat));
        }

        return stride;
    }

    public static int SizeOf(VertexElementFormat format) => format switch
    {
        VertexElementFormat.Single => 4,
        VertexElementFormat.Vector2 => 8,
        VertexElementFormat.Vector3 => 12,
        VertexElementFormat.Vector4 => 16,
        VertexElementFormat.Color => 4,
        VertexElementFormat.Byte4 => 4,
        VertexElementFormat.Short2 => 4,
        VertexElementFormat.Short4 => 8,
        VertexElementFormat.NormalizedShort2 => 4,
        VertexElementFormat.NormalizedShort4 => 8,
        VertexElementFormat.HalfVector2 => 4,
        VertexElementFormat.HalfVector4 => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };
}
