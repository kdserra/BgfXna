namespace BgfXna.Samples;

internal static class Program
{
    [System.STAThread]
    public static async System.Threading.Tasks.Task Main(string[] args)
    {
        SamplePlatform.Configure(args);

#if BROWSER_WEB_HOST
        await BrowserGameHost.RunAsync();
#elif WINDOWS_DESKTOP_HOST
        WindowsGameHost.Run();
#else
        using AutoPongGame game = new();
        game.Run();
#endif
    }
}
