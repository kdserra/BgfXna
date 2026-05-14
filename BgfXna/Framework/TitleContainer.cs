using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework;

public enum TitleLocation
{
    Path,
}

public static class TitleContainer
{
    public static string Location { get; set; } = AppContext.BaseDirectory;

    public static Stream OpenStream(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        string path = Path.IsPathRooted(name) ? name : Path.Combine(Location, name);
        return File.OpenRead(path);
    }

    public static IAsyncResult BeginOpenStream(string name, AsyncCallback? callback, object? state)
    {
        Task<Stream> task = Task.Run(() => OpenStream(name));
        if (callback is not null)
        {
            task.ContinueWith(t => callback(t), TaskScheduler.Default);
        }

        return task;
    }

    public static Stream EndOpenStream(IAsyncResult result)
    {
        if (result is not Task<Stream> task)
        {
            throw new ArgumentException("Invalid async result.", nameof(result));
        }

        return task.GetAwaiter().GetResult();
    }
}
