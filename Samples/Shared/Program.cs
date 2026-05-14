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
