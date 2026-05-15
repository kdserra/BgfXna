namespace BgfXna.Samples;

internal static class Program
{
    [System.STAThread]
    public static async System.Threading.Tasks.Task Main(string[] args)
    {
        try
        {
            SamplePlatform.Configure(args);

#if BROWSER_WEB_HOST
            await BrowserGameHost.RunAsync();
#elif WINDOWS_DESKTOP_HOST
            WindowsGameHost.Run();
#elif LINUX_DESKTOP_HOST
            LinuxGameHost.Run();
#elif MACOS_DESKTOP_OPENGL_HOST
            throw new System.PlatformNotSupportedException("BGFX OpenGL is not available on macOS in the current BGFX source tree. Use the MacMetal sample for macOS.");
#elif IOS_GAME_HOST
            iOSGameHost.Run(args);
#else
            using AutoPongGame game = new();
            game.Run();
#endif
        }
        catch (System.Exception exception)
        {
#if BROWSER_WEB_HOST
            BrowserDiagnostics.ShowManagedError(exception.ToString());
#else
            _ = exception;
#endif
            throw;
        }
    }
}
