#if BROWSER_WEB_HOST
using Microsoft.Xna.Framework.Graphics;

namespace BgfXna.Samples;

internal static class BrowserBackendSelector
{
    public static GraphicsBackend Select(string[] args)
    {
        string requested = args.Length > 0 ? args[0] : string.Empty;
        return requested.Trim().ToLowerInvariant() switch
        {
            "webgpu" => GraphicsBackend.WebGPU,
            "wgpu" => GraphicsBackend.WebGPU,
            "webgl" => GraphicsBackend.WebGL,
            "gles" => GraphicsBackend.WebGL,
            _ => GraphicsBackend.Auto,
        };
    }
}
#endif
