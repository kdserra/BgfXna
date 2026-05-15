using Microsoft.Xna.Framework.Graphics;

namespace BgfXna.Samples;

internal static class SamplePlatform
{
    private static GraphicsBackend? _runtimeBackend;

    public static GraphicsBackend Backend => _runtimeBackend ?? CompileTimeBackend;

    public static void Configure(string[] args)
    {
#if SAMPLE_BACKEND_WEB_AUTO
        _runtimeBackend = BrowserBackendSelector.Select(args);
#else
        _runtimeBackend = null;
#endif
    }

#if SAMPLE_BACKEND_DX11
    private const GraphicsBackend CompileTimeBackend = GraphicsBackend.Direct3D11;
#elif SAMPLE_BACKEND_DX12
    private const GraphicsBackend CompileTimeBackend = GraphicsBackend.Direct3D12;
#elif SAMPLE_BACKEND_VULKAN
    private const GraphicsBackend CompileTimeBackend = GraphicsBackend.Vulkan;
#elif SAMPLE_BACKEND_METAL
    private const GraphicsBackend CompileTimeBackend = GraphicsBackend.Metal;
#elif SAMPLE_BACKEND_OPENGL
    private const GraphicsBackend CompileTimeBackend = GraphicsBackend.OpenGL;
#elif SAMPLE_BACKEND_OPENGLES
    private const GraphicsBackend CompileTimeBackend = GraphicsBackend.OpenGLES;
#elif SAMPLE_BACKEND_WEBGL
    private const GraphicsBackend CompileTimeBackend = GraphicsBackend.WebGL;
#elif SAMPLE_BACKEND_WEBGPU
    private const GraphicsBackend CompileTimeBackend = GraphicsBackend.WebGPU;
#elif SAMPLE_BACKEND_NOOP
    private const GraphicsBackend CompileTimeBackend = GraphicsBackend.Noop;
#else
    private const GraphicsBackend CompileTimeBackend = GraphicsBackend.Auto;
#endif
}
