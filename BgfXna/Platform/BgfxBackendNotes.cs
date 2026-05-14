namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Documents the intended native BGFX mapping for platform packages.
/// A production package should implement <see cref="IBgfxBackend"/> with native bgfx calls:
/// D3D11/D3D12 on Windows, Metal on Apple platforms, Vulkan/OpenGL/OpenGLES where available,
/// WebGL for browser targets, and WebGPU when exposed by the host runtime.
/// </summary>
public static class BgfxBackendNotes
{
    public static readonly GraphicsBackend[] SupportedBackends =
    {
        GraphicsBackend.Direct3D11,
        GraphicsBackend.Direct3D12,
        GraphicsBackend.Metal,
        GraphicsBackend.Vulkan,
        GraphicsBackend.OpenGL,
        GraphicsBackend.OpenGLES,
        GraphicsBackend.WebGL,
        GraphicsBackend.WebGPU
    };
}
