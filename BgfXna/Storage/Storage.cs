using System;
using System.IO;
using System.Threading;

namespace Microsoft.Xna.Framework.Storage;

public sealed class StorageDeviceNotConnectedException : Exception
{
    public StorageDeviceNotConnectedException() { }
    public StorageDeviceNotConnectedException(string message) : base(message) { }
    public StorageDeviceNotConnectedException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class StorageContainer : IDisposable
{
    internal StorageContainer(string titleName)
    {
        DisplayName = titleName;
        Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), titleName);
        Directory.CreateDirectory(Path);
    }

    public string DisplayName { get; }
    public string Path { get; }
    public void CreateDirectory(string directory) => Directory.CreateDirectory(System.IO.Path.Combine(Path, directory));
    public Stream CreateFile(string file) => File.Create(System.IO.Path.Combine(Path, file));
    public void DeleteDirectory(string directory) => Directory.Delete(System.IO.Path.Combine(Path, directory), true);
    public void DeleteFile(string file) => File.Delete(System.IO.Path.Combine(Path, file));
    public bool DirectoryExists(string directory) => Directory.Exists(System.IO.Path.Combine(Path, directory));
    public bool FileExists(string file) => File.Exists(System.IO.Path.Combine(Path, file));
    public string[] GetDirectoryNames() => Directory.GetDirectories(Path);
    public string[] GetDirectoryNames(string searchPattern) => Directory.GetDirectories(Path, searchPattern);
    public string[] GetFileNames() => Directory.GetFiles(Path);
    public string[] GetFileNames(string searchPattern) => Directory.GetFiles(Path, searchPattern);
    public Stream OpenFile(string file, FileMode fileMode) => File.Open(System.IO.Path.Combine(Path, file), fileMode);
    public Stream OpenFile(string file, FileMode fileMode, FileAccess fileAccess) => File.Open(System.IO.Path.Combine(Path, file), fileMode, fileAccess);
    public Stream OpenFile(string file, FileMode fileMode, FileAccess fileAccess, FileShare fileShare) => File.Open(System.IO.Path.Combine(Path, file), fileMode, fileAccess, fileShare);
    public void Dispose() { }
}

public sealed class StorageDevice
{
    public static IAsyncResult BeginShowSelector(AsyncCallback? callback, object? state) => Complete(new StorageDevice(), callback, state);
    public static IAsyncResult BeginShowSelector(PlayerIndex player, AsyncCallback? callback, object? state) => BeginShowSelector(callback, state);
    public static StorageDevice EndShowSelector(IAsyncResult result) => ((CompletedAsyncResult<StorageDevice>)result).Value;

    public bool IsConnected { get; } = true;
    public long FreeSpace { get; } = long.MaxValue;
    public long TotalSpace { get; } = long.MaxValue;

    public IAsyncResult BeginOpenContainer(string displayName, AsyncCallback? callback, object? state) => Complete(new StorageContainer(displayName), callback, state);
    public StorageContainer EndOpenContainer(IAsyncResult result) => ((CompletedAsyncResult<StorageContainer>)result).Value;
    public void DeleteContainer(string titleName) => Directory.Delete(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), titleName), true);

    private static CompletedAsyncResult<T> Complete<T>(T value, AsyncCallback? callback, object? state)
    {
        CompletedAsyncResult<T> result = new(value, state);
        callback?.Invoke(result);
        return result;
    }

    private sealed class CompletedAsyncResult<T> : IAsyncResult
    {
        public CompletedAsyncResult(T value, object? state)
        {
            Value = value;
            AsyncState = state;
        }

        public T Value { get; }
        public object? AsyncState { get; }
        public WaitHandle AsyncWaitHandle => throw new NotSupportedException();
        public bool CompletedSynchronously => true;
        public bool IsCompleted => true;
    }
}
