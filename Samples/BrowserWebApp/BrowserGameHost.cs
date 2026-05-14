#if BROWSER_WEB_HOST
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace BgfXna.Samples;

internal static class BrowserGameHost
{
    public static async Task RunAsync()
    {
        using AutoPongGame game = new();
        Stopwatch clock = Stopwatch.StartNew();
        TimeSpan previous = TimeSpan.Zero;

        while (game.IsActive)
        {
            TimeSpan total = clock.Elapsed;
            TimeSpan elapsed = total - previous;
            previous = total;
            game.Tick(new GameTime(total, elapsed));
            await Task.Delay(16);
        }
    }
}
#endif
