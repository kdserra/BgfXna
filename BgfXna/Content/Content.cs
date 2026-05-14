using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Xna.Framework.Content;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ContentSerializerAttribute : Attribute
{
    public string? ElementName { get; set; }
    public bool FlattenContent { get; set; }
    public bool Optional { get; set; }
    public bool SharedResource { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ContentSerializerRuntimeTypeAttribute : Attribute
{
    public ContentSerializerRuntimeTypeAttribute(string runtimeType) => RuntimeType = runtimeType;
    public string RuntimeType { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ContentSerializerTypeVersionAttribute : Attribute
{
    public ContentSerializerTypeVersionAttribute(int typeVersion) => TypeVersion = typeVersion;
    public int TypeVersion { get; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ContentSerializerIgnoreAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ContentSerializerCollectionItemNameAttribute : Attribute
{
    public ContentSerializerCollectionItemNameAttribute(string collectionItemName) => CollectionItemName = collectionItemName;
    public string CollectionItemName { get; }
}

public sealed class ContentLoadException : Exception
{
    public ContentLoadException() { }
    public ContentLoadException(string message) : base(message) { }
    public ContentLoadException(string message, Exception innerException) : base(message, innerException) { }
}

public class ContentManager : IDisposable
{
    private readonly Dictionary<string, object> _loadedAssets = new(StringComparer.OrdinalIgnoreCase);

    public ContentManager(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;
    public ContentManager(IServiceProvider serviceProvider, string rootDirectory) : this(serviceProvider) => RootDirectory = rootDirectory;
    public IServiceProvider ServiceProvider { get; }
    public string RootDirectory { get; set; } = string.Empty;

    public virtual T Load<T>(string assetName)
    {
        if (_loadedAssets.TryGetValue(assetName, out object? value))
        {
            return (T)value;
        }

        throw new ContentLoadException($"No content reader is registered for '{assetName}'. Load runtime assets directly or provide a custom ContentManager.");
    }

    public virtual void Unload() => _loadedAssets.Clear();
    protected void RegisterLoadedAsset<T>(string assetName, T asset) where T : notnull => _loadedAssets[assetName] = asset;
    public void Dispose() => Unload();
}

public class ResourceContentManager : ContentManager
{
    public ResourceContentManager(IServiceProvider serviceProvider, IDictionary<string, object> resources)
        : base(serviceProvider)
    {
        Resources = resources;
    }

    public IDictionary<string, object> Resources { get; }
    public override T Load<T>(string assetName)
    {
        if (Resources.TryGetValue(assetName, out object? value))
        {
            return (T)value;
        }

        return base.Load<T>(assetName);
    }
}

public abstract class ContentTypeReader
{
    public abstract Type TargetType { get; }
}

public abstract class ContentTypeReader<T> : ContentTypeReader
{
    public override Type TargetType => typeof(T);
}

public class ContentReader : BinaryReader
{
    public ContentReader(Stream input) : base(input) { }
}

public static class ContentTypeReaderManager { }
