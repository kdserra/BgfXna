# BgfXna Samples

Each project links the shared AutoPong-style game in `Samples/Shared` and selects a BGFX backend through a compile constant.

Projects:

- `WindowsDX11`: Direct3D 11 for Windows.
- `WindowsDX12`: Direct3D 12 for Windows.
- `BrowserWebApp`: WebGL/WebGPU for Browser.
- `DesktopHeadless`: Headless Game Server CLI for Windows, MacOS, and Linux.
- `DesktopOpenGL`: OpenGL for Windows and Linux. The current BGFX source tree does not expose a macOS OpenGL renderer/context path; use `MacMetal` on macOS.
- `DesktopVulkan`: Vulkan for Windows and Linux.
- `MacMetal`: Metal for macOS.
- `iOSMetal`: Metal for iOS.
- `AndroidOpenGLES`: OpenGL ES for Android.
- `AndroidVulkan`: Vulkan for Android.

The current BGFX backend is represented by `IBgfxBackend`; these samples validate the XNA-facing integration path and are ready for concrete native bgfx backend bindings.

`BrowserWebApp` supports `BrowserGraphicsBackends=WebGL`, `BrowserGraphicsBackends=WebGPU`, or `BrowserGraphicsBackends=Both`. `Both` chooses WebGPU when `navigator.gpu` exists and otherwise falls back to WebGL; use `?backend=webgl` or `?backend=webgpu` to override at runtime.

**Note:** SDL2 is used as it makes it easy to support various Linux display managers like X11, Wayland, and more.

Therefore, SDL2 is required to be installed on the target OS:

- Windows: Automatically included.
- MacOS: Run `brew install SDL2`
- Linux (Ubuntu): Run `sudo apt install libsdl2-dev`
- Linux (Other): Research how to install SDL2 for your distro.
- Other: Automatically included.
