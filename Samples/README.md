# BgfXna Samples

Each project links the shared AutoPong-style game in `Samples/Shared` and selects a BGFX backend through a compile constant.

Projects:

- `WindowsDX11`: Windows Direct3D 11
- `WindowsDX12`: Windows Direct3D 12
- `BrowserWebApp`: Browser WebGL/WebGPU
- `DesktopVulkan`: Vulkan for Windows and Linux
- `MacMetal`: Metal for macOS
- `AndroidOpenGL`: Android OpenGL
- `iOSOpenGL`: iOS OpenGL
- `AndroidOpenGLES`: Android OpenGL ES
- `iOSOpenGLES`: iOS OpenGL ES

The current BGFX backend is represented by `IBgfxBackend`; these samples validate the XNA-facing integration path and are ready for concrete native bgfx backend bindings.

`BrowserWebApp` supports `BrowserGraphicsBackends=WebGL`, `BrowserGraphicsBackends=WebGPU`, or `BrowserGraphicsBackends=Both`. `Both` chooses WebGPU when `navigator.gpu` exists and otherwise falls back to WebGL; use `?backend=webgl` or `?backend=webgpu` to override at runtime.
