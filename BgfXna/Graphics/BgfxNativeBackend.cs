using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Bgfx;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics;

public sealed unsafe class BgfxNativeBackend : IBgfxBackend
{
    private bool _initialized;
    private GraphicsBackend _requestedBackend;
    private bgfx.RendererType _rendererType;
    private readonly Dictionary<ushort, bgfx.VertexBufferHandle> _vertexBuffers = new();
    private readonly Dictionary<ushort, bgfx.IndexBufferHandle> _indexBuffers = new();
    private readonly Dictionary<ushort, bgfx.TextureHandle> _textures = new();
    private readonly Dictionary<ushort, bgfx.ShaderHandle> _shaders = new();
    private readonly Dictionary<ushort, bgfx.ProgramHandle> _programs = new();
    private readonly Dictionary<ushort, bgfx.VertexLayoutHandle> _vertexLayouts = new();
    private ushort _nextHandle = 1;
    private bgfx.UniformHandle _spriteSampler;
    private bgfx.ProgramHandle _spriteProgram;
    private bgfx.VertexBufferHandle _currentVertexBuffer;
    private bgfx.IndexBufferHandle _currentIndexBuffer;
    private bgfx.TextureHandle _currentTexture;
    private int _backBufferWidth;
    private int _backBufferHeight;
    private ulong _currentState = (ulong)bgfx.StateFlags.Default;
    private static readonly float[] IdentityTransform =
    {
        1f, 0f, 0f, 0f,
        0f, 1f, 0f, 0f,
        0f, 0f, 1f, 0f,
        0f, 0f, 0f, 1f
    };

    public BgfxCapabilities Capabilities { get; private set; } =
        new(GraphicsBackend.Auto, false, false, false, false, true);
    public GraphicsBackend ActualBackend { get; private set; } = GraphicsBackend.Auto;
    public string ActualBackendName { get; private set; } = "Unknown";

    public void Initialize(GraphicsDeviceOptions options)
    {
        if (options.NativeWindowHandle == IntPtr.Zero && !IsBrowserRuntime())
        {
            throw new InvalidOperationException(
                "A native window handle is required to initialize BGFX."
            );
        }

        _requestedBackend = options.Backend;

        _backBufferWidth = options.BackBufferWidth;
        _backBufferHeight = options.BackBufferHeight;

        bool initialized;
        IntPtr browserCanvasHandle = IntPtr.Zero;
        try
        {
            bgfx.Init init;
            bgfx.init_ctor(&init);
            GraphicsBackend backend = NormalizeBackend(options.Backend);
            init.type = ToBgfxRenderer(backend);
            if (options.NativeDisplayHandle != IntPtr.Zero)
            {
                init.platformData.ndt = options.NativeDisplayHandle.ToPointer();
            }

            if (options.NativeWindowHandle != IntPtr.Zero)
            {
                init.platformData.nwh = options.NativeWindowHandle.ToPointer();
                init.platformData.type = bgfx.NativeWindowHandleType.Default;
            }
            else if (IsBrowserRuntime())
            {
                browserCanvasHandle = Marshal.StringToHGlobalAnsi("#canvas");
                init.platformData.nwh = browserCanvasHandle.ToPointer();
                init.platformData.type = bgfx.NativeWindowHandleType.Default;
            }
            init.resolution.width = (uint)options.BackBufferWidth;
            init.resolution.height = (uint)options.BackBufferHeight;
            init.resolution.reset = options.VSync
                ? (uint)bgfx.ResetFlags.Vsync
                : (uint)bgfx.ResetFlags.None;
            init.resolution.formatColor = ToBgfxTextureFormat(options.BackBufferFormat);
            init.resolution.formatDepthStencil = ToBgfxTextureFormat(options.DepthStencilFormat);
            if (RequiresSingleThreadedRenderer())
            {
                bgfx.render_frame(-1);
            }

            initialized = bgfx.init(&init);
        }
        catch (DllNotFoundException exception)
        {
            throw new InvalidOperationException(
                "BGFX native library was not found. Build native BGFX with scripts/build-bgfx.cs, or place bgfx_debug.dll/bgfx.dll next to the sample executable on Windows or libbgfx_debug.so/libbgfx.so in the Android package.",
                exception
            );
        }
        catch (EntryPointNotFoundException exception)
        {
            throw new InvalidOperationException(
                "The loaded BGFX native library does not expose the expected bgfx C99 API entry points. Rebuild BGFX as a shared library with the C99 API enabled.",
                exception
            );
        }
        finally
        {
            if (browserCanvasHandle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(browserCanvasHandle);
            }
        }

        if (!initialized)
        {
            throw new InvalidOperationException(
                $"bgfx.init failed for requested backend {options.Backend}."
            );
        }

        _initialized = true;
        _rendererType = bgfx.get_renderer_type();
        ActualBackend =
            _requestedBackend == GraphicsBackend.WebGL && _rendererType == bgfx.RendererType.OpenGLES
                ? GraphicsBackend.WebGL
                : FromBgfxRenderer(_rendererType);
        ActualBackendName = GetRendererName(_rendererType);
        _spriteSampler = bgfx.create_uniform("s_texColor", bgfx.UniformType.Sampler, 1);
        Capabilities = new BgfxCapabilities(ActualBackend, true, true, true, false, true);
    }

    public void Reset(
        int width,
        int height,
        SurfaceFormat format,
        DepthFormat depthFormat,
        bool vsync
    )
    {
        EnsureInitialized();
        _backBufferWidth = width;
        _backBufferHeight = height;
        bgfx.reset(
            (uint)width,
            (uint)height,
            vsync ? (uint)bgfx.ResetFlags.Vsync : (uint)bgfx.ResetFlags.None,
            ToBgfxTextureFormat(format)
        );
    }

    public BgfxHandle CreateVertexBuffer(
        ReadOnlySpan<byte> data,
        VertexDeclaration declaration,
        BufferUsage usage
    )
    {
        EnsureInitialized();
        if (data.IsEmpty)
        {
            return BgfxHandle.Invalid;
        }

        byte[] converted = ConvertVertexDataToClipSpace(data, declaration);
        fixed (byte* dataPointer = converted)
        {
            bgfx.Memory* memory = bgfx.copy(dataPointer, (uint)converted.Length);
            bgfx.VertexLayout layout = CreateVertexLayout(declaration);
            bgfx.VertexBufferHandle vertexBuffer = bgfx.create_vertex_buffer(memory, &layout, 0);
            bgfx.VertexLayoutHandle layoutHandle = bgfx.create_vertex_layout(&layout);
            BgfxHandle handle = AllocateHandle();
            _vertexBuffers[handle.Id] = vertexBuffer;
            _vertexLayouts[handle.Id] = layoutHandle;
            return handle;
        }
    }

    public BgfxHandle CreateIndexBuffer(
        ReadOnlySpan<byte> data,
        IndexElementSize elementSize,
        BufferUsage usage
    )
    {
        EnsureInitialized();
        if (data.IsEmpty)
        {
            return BgfxHandle.Invalid;
        }

        fixed (byte* dataPointer = data)
        {
            bgfx.Memory* memory = bgfx.copy(dataPointer, (uint)data.Length);
            ushort flags =
                elementSize == IndexElementSize.ThirtyTwoBits
                    ? (ushort)bgfx.BufferFlags.Index32
                    : (ushort)bgfx.BufferFlags.None;
            bgfx.IndexBufferHandle indexBuffer = bgfx.create_index_buffer(memory, flags);
            BgfxHandle handle = AllocateHandle();
            _indexBuffers[handle.Id] = indexBuffer;
            return handle;
        }
    }

    public BgfxHandle CreateTexture2D(
        int width,
        int height,
        bool mipMap,
        SurfaceFormat format,
        ReadOnlySpan<byte> data
    )
    {
        EnsureInitialized();
        bgfx.Memory* memory = null;
        if (!data.IsEmpty)
        {
            fixed (byte* dataPointer = data)
            {
                memory = bgfx.copy(dataPointer, (uint)data.Length);
            }
        }

        bgfx.TextureHandle texture = bgfx.create_texture_2d(
            (ushort)width,
            (ushort)height,
            mipMap,
            1,
            ToBgfxTextureFormat(format),
            0,
            memory,
            0
        );
        BgfxHandle handle = AllocateHandle();
        _textures[handle.Id] = texture;
        return handle;
    }

    public BgfxHandle CreateRenderTarget(
        int width,
        int height,
        SurfaceFormat format,
        DepthFormat depthFormat
    ) => BgfxHandle.Invalid;

    public BgfxHandle CreateShader(ReadOnlySpan<byte> shaderBytes, string? name)
    {
        EnsureInitialized();
        if (shaderBytes.IsEmpty)
        {
            return BgfxHandle.Invalid;
        }

        fixed (byte* dataPointer = shaderBytes)
        {
            bgfx.Memory* memory = bgfx.copy(dataPointer, (uint)shaderBytes.Length);
            bgfx.ShaderHandle shader = bgfx.create_shader(memory);
            if (!string.IsNullOrEmpty(name))
            {
                bgfx.set_shader_name(shader, name, name.Length);
            }

            BgfxHandle handle = AllocateHandle();
            _shaders[handle.Id] = shader;
            return handle;
        }
    }

    public BgfxHandle CreateProgram(
        BgfxHandle vertexShader,
        BgfxHandle fragmentShader,
        bool destroyShaders
    )
    {
        EnsureInitialized();
        if (!vertexShader.IsValid || !fragmentShader.IsValid)
        {
            return BgfxHandle.Invalid;
        }

        bgfx.ProgramHandle program = bgfx.create_program(
            _shaders[vertexShader.Id],
            _shaders[fragmentShader.Id],
            destroyShaders
        );
        BgfxHandle handle = AllocateHandle();
        _programs[handle.Id] = program;
        return handle;
    }

    public void Destroy(BgfxHandle handle)
    {
        if (!handle.IsValid || !_initialized)
        {
            return;
        }

        if (_vertexBuffers.Remove(handle.Id, out bgfx.VertexBufferHandle vertexBuffer))
        {
            bgfx.destroy_vertex_buffer(vertexBuffer);
        }

        if (_vertexLayouts.Remove(handle.Id, out bgfx.VertexLayoutHandle layout))
        {
            bgfx.destroy_vertex_layout(layout);
        }

        if (_indexBuffers.Remove(handle.Id, out bgfx.IndexBufferHandle indexBuffer))
        {
            bgfx.destroy_index_buffer(indexBuffer);
        }

        if (_textures.Remove(handle.Id, out bgfx.TextureHandle texture))
        {
            bgfx.destroy_texture(texture);
        }

        if (_programs.Remove(handle.Id, out bgfx.ProgramHandle program))
        {
            bgfx.destroy_program(program);
        }

        if (_shaders.Remove(handle.Id, out bgfx.ShaderHandle shader))
        {
            bgfx.destroy_shader(shader);
        }
    }

    public void SetViewClear(ushort viewId, Color color, float depth, byte stencil)
    {
        EnsureInitialized();
        bgfx.set_view_clear(
            viewId,
            (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth | bgfx.ClearFlags.Stencil),
            ToRgba(color),
            depth,
            stencil
        );
    }

    public void SetViewRect(ushort viewId, int x, int y, int width, int height)
    {
        EnsureInitialized();
        bgfx.set_view_rect(viewId, (ushort)x, (ushort)y, (ushort)width, (ushort)height);
        bgfx.set_view_mode(viewId, bgfx.ViewMode.Sequential);
        SetIdentityViewTransform(viewId);
    }

    public void SetRenderTarget(ushort viewId, BgfxHandle renderTarget) { }

    public void Touch(ushort viewId)
    {
        EnsureInitialized();
        bgfx.touch(viewId);
    }

    public void SetState(RenderStateSnapshot state)
    {
        ulong flags = (ulong)(
            bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA | bgfx.StateFlags.Msaa
        );
        if (state.DepthStencilState.DepthBufferEnable)
        {
            flags |= ToDepthTest(state.DepthStencilState.DepthBufferFunction);
        }

        if (state.DepthStencilState.DepthBufferWriteEnable)
        {
            flags |= (ulong)bgfx.StateFlags.WriteZ;
        }

        flags |= ToBlend(state.BlendState.ColorSourceBlend, state.BlendState.ColorDestinationBlend);
        flags |= state.RasterizerState.CullMode switch
        {
            CullMode.CullClockwiseFace => (ulong)bgfx.StateFlags.CullCw,
            CullMode.CullCounterClockwiseFace => (ulong)bgfx.StateFlags.CullCcw,
            _ => 0,
        };
        flags |= state.PrimitiveType switch
        {
            PrimitiveType.TriangleStrip => (ulong)bgfx.StateFlags.PtTristrip,
            PrimitiveType.LineList => (ulong)bgfx.StateFlags.PtLines,
            PrimitiveType.LineStrip => (ulong)bgfx.StateFlags.PtLinestrip,
            _ => 0,
        };

        _currentState = flags;
    }

    public void SetVertexBuffer(BgfxHandle handle, int vertexOffset, int vertexCount)
    {
        _currentVertexBuffer =
            handle.IsValid
            && _vertexBuffers.TryGetValue(handle.Id, out bgfx.VertexBufferHandle vertexBuffer)
                ? vertexBuffer
                : new bgfx.VertexBufferHandle { idx = ushort.MaxValue };

        if (_currentVertexBuffer.Valid)
        {
            bgfx.set_vertex_buffer(0, _currentVertexBuffer, (uint)vertexOffset, (uint)vertexCount);
        }
    }

    public void SetIndexBuffer(BgfxHandle handle, int indexOffset, int indexCount)
    {
        _currentIndexBuffer =
            handle.IsValid
            && _indexBuffers.TryGetValue(handle.Id, out bgfx.IndexBufferHandle indexBuffer)
                ? indexBuffer
                : new bgfx.IndexBufferHandle { idx = ushort.MaxValue };

        if (_currentIndexBuffer.Valid)
        {
            bgfx.set_index_buffer(_currentIndexBuffer, (uint)indexOffset, (uint)indexCount);
        }
    }

    public void SetTexture(byte stage, BgfxHandle texture, SamplerState samplerState)
    {
        _currentTexture =
            texture.IsValid
            && _textures.TryGetValue(texture.Id, out bgfx.TextureHandle nativeTexture)
                ? nativeTexture
                : new bgfx.TextureHandle { idx = ushort.MaxValue };

        if (_currentTexture.Valid)
        {
            bgfx.set_texture(stage, _spriteSampler, _currentTexture, ToSamplerFlags(samplerState));
        }
    }

    public void Submit(ushort viewId, BgfxHandle program)
    {
        EnsureInitialized();
        bgfx.ProgramHandle nativeProgram =
            program.IsValid
            && _programs.TryGetValue(program.Id, out bgfx.ProgramHandle programHandle)
                ? programHandle
                : GetSpriteProgram();

        bgfx.set_state(_currentState, 0);
        SetIdentityTransform();
        bgfx.submit(viewId, nativeProgram, 0, (byte)bgfx.DiscardFlags.All);
    }

    internal void DrawSpriteBatch(
        ushort viewId,
        ReadOnlySpan<VertexPositionColorTexture> vertices,
        ReadOnlySpan<ushort> indices,
        BgfxHandle texture,
        SamplerState samplerState,
        RenderStateSnapshot state
    )
    {
        EnsureInitialized();
        if (vertices.IsEmpty || indices.IsEmpty)
        {
            return;
        }

        bgfx.TransientVertexBuffer tvb;
        bgfx.TransientIndexBuffer tib;
        bgfx.VertexLayout layout = CreateVertexLayout(VertexPositionColorTexture.Declaration);
        if (
            !bgfx.alloc_transient_buffers(
                &tvb,
                &layout,
                (uint)vertices.Length,
                &tib,
                (uint)indices.Length,
                false
            )
        )
        {
            return;
        }

        byte[] vertexBytes = ConvertVertexDataToClipSpace(
            MemoryMarshal.AsBytes(vertices),
            VertexPositionColorTexture.Declaration
        );
        fixed (byte* vertexPointer = vertexBytes)
        fixed (ushort* indexPointer = indices)
        {
            Buffer.MemoryCopy(vertexPointer, tvb.data, tvb.size, vertexBytes.Length);
            Buffer.MemoryCopy(indexPointer, tib.data, tib.size, indices.Length * sizeof(ushort));
        }

        SetState(state);
        if (
            texture.IsValid
            && _textures.TryGetValue(texture.Id, out bgfx.TextureHandle nativeTexture)
        )
        {
            bgfx.set_texture(0, _spriteSampler, nativeTexture, ToSamplerFlags(samplerState));
        }

        bgfx.set_transient_vertex_buffer(0, &tvb, 0, (uint)vertices.Length);
        bgfx.set_transient_index_buffer(&tib, 0, (uint)indices.Length);
        bgfx.set_state(_currentState, 0);
        SetIdentityTransform();
        bgfx.submit(viewId, GetSpriteProgram(), 0, (byte)bgfx.DiscardFlags.All);
    }

    public void Frame()
    {
        EnsureInitialized();
        bgfx.frame(0);
    }

    public void Dispose()
    {
        if (_initialized)
        {
            if (_spriteProgram.Valid)
            {
                bgfx.destroy_program(_spriteProgram);
                _spriteProgram = default;
            }

            if (_spriteSampler.Valid)
            {
                bgfx.destroy_uniform(_spriteSampler);
                _spriteSampler = default;
            }

            bgfx.shutdown();
            _initialized = false;
        }
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                $"BGFX backend {_requestedBackend} has not been initialized."
            );
        }
    }

    private static uint ToRgba(Color color) =>
        (uint)(color.R << 24 | color.G << 16 | color.B << 8 | color.A);

    private BgfxHandle AllocateHandle() => new(_nextHandle++);

    private static void SetIdentityTransform()
    {
        fixed (float* transform = IdentityTransform)
        {
            bgfx.set_transform(transform, 1);
        }
    }

    private static void SetIdentityViewTransform(ushort viewId)
    {
        fixed (float* transform = IdentityTransform)
        {
            bgfx.set_view_transform(viewId, transform, transform);
        }
    }

    private byte[] ConvertVertexDataToClipSpace(
        ReadOnlySpan<byte> data,
        VertexDeclaration declaration
    )
    {
        byte[] converted = data.ToArray();
        VertexElement? positionElement = null;
        foreach (VertexElement element in declaration.GetVertexElements())
        {
            if (
                element.VertexElementUsage == VertexElementUsage.Position
                && element.VertexElementFormat == VertexElementFormat.Vector3
            )
            {
                positionElement = element;
                break;
            }
        }

        if (positionElement is null || _backBufferWidth <= 0 || _backBufferHeight <= 0)
        {
            return converted;
        }

        int stride = declaration.VertexStride;
        int offset = positionElement.Value.Offset;
        for (int i = 0; i + stride <= converted.Length; i += stride)
        {
            Span<byte> vertex = converted.AsSpan(i + offset);
            float x = MemoryMarshal.Read<float>(vertex);
            float y = MemoryMarshal.Read<float>(vertex[4..]);
            float clipX = x / _backBufferWidth * 2f - 1f;
            float clipY = 1f - y / _backBufferHeight * 2f;
#if NETSTANDARD2_1
            MemoryMarshal.Write(vertex, ref clipX);
            MemoryMarshal.Write(vertex[4..], ref clipY);
#else
            MemoryMarshal.Write(vertex, in clipX);
            MemoryMarshal.Write(vertex[4..], in clipY);
#endif
        }

        return converted;
    }

    private bgfx.VertexLayout CreateVertexLayout(VertexDeclaration declaration)
    {
        bgfx.VertexLayout layout = default;
        bgfx.vertex_layout_begin(&layout, _rendererType);
        foreach (VertexElement element in declaration.GetVertexElements())
        {
            bgfx.vertex_layout_add(
                &layout,
                ToBgfxAttrib(element.VertexElementUsage),
                (byte)GetElementCount(element.VertexElementFormat),
                ToBgfxAttribType(element.VertexElementFormat),
                element.VertexElementFormat == VertexElementFormat.Color,
                false
            );
        }

        bgfx.vertex_layout_end(&layout);
        return layout;
    }

    private bgfx.ProgramHandle GetSpriteProgram()
    {
        if (_spriteProgram.Valid)
        {
            return _spriteProgram;
        }

        byte[] vertexShader = LoadEmbeddedDebugDrawShader("vs_debugdraw_fill_texture");
        byte[] fragmentShader = LoadEmbeddedDebugDrawShader("fs_debugdraw_fill_texture");
        fixed (byte* vertexPointer = vertexShader)
        fixed (byte* fragmentPointer = fragmentShader)
        {
            bgfx.ShaderHandle vs = bgfx.create_shader(
                bgfx.copy(vertexPointer, (uint)vertexShader.Length)
            );
            bgfx.ShaderHandle fs = bgfx.create_shader(
                bgfx.copy(fragmentPointer, (uint)fragmentShader.Length)
            );
            _spriteProgram = bgfx.create_program(vs, fs, true);
            return _spriteProgram;
        }
    }

    private byte[] LoadEmbeddedDebugDrawShader(string shaderName)
    {
        string profile = _rendererType switch
        {
            bgfx.RendererType.Direct3D11 => "dxbc",
            bgfx.RendererType.Direct3D12 => "dxil",
            bgfx.RendererType.Metal => "mtl",
            bgfx.RendererType.Vulkan => "spv",
            bgfx.RendererType.OpenGLES => "essl",
            bgfx.RendererType.WebGPU => "wgsl",
            _ => "glsl",
        };

        string? headerPath = TryFindShaderHeader(shaderName);
        if (headerPath is not null)
        {
            string source = File.ReadAllText(headerPath);
            Match match = Regex.Match(
                source,
                $@"{shaderName}_{profile}\s*\[\d+\]\s*=\s*\{{(?<bytes>.*?)\}};",
                RegexOptions.Singleline
            );
            if (!match.Success)
            {
                throw new InvalidOperationException(
                    $"Could not find BGFX shader profile '{profile}' in {headerPath}."
                );
            }

            List<byte> bytes = new();
            foreach (Match byteMatch in Regex.Matches(match.Groups["bytes"].Value, @"0x[0-9a-fA-F]{2}"))
            {
                bytes.Add(Convert.ToByte(byteMatch.Value[2..], 16));
            }

            return bytes.ToArray();
        }

        byte[]? embedded = BgfxEmbeddedDebugDrawShaders.TryGet(shaderName, profile);
        if (embedded is not null)
        {
            return embedded;
        }

        throw new InvalidOperationException(
            $"Missing BGFX debugdraw shader for {shaderName}_{profile}. Run dotnet run .\\scripts\\build-bgfx.cs so .native-src is available."
        );
    }

    private static string? TryFindShaderHeader(string shaderName)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (string rootName in new[] { "native-src", ".native-src" })
            {
                string candidate = Path.Combine(
                    directory.FullName,
                    rootName,
                    "bgfx",
                    "examples",
                    "common",
                    "debugdraw",
                    $"{shaderName}.bin.h"
                );
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bgfx.Attrib ToBgfxAttrib(VertexElementUsage usage) =>
        usage switch
        {
            VertexElementUsage.Position => bgfx.Attrib.Position,
            VertexElementUsage.Color => bgfx.Attrib.Color0,
            VertexElementUsage.TextureCoordinate => bgfx.Attrib.TexCoord0,
            VertexElementUsage.Normal => bgfx.Attrib.Normal,
            VertexElementUsage.Tangent => bgfx.Attrib.Tangent,
            VertexElementUsage.Binormal => bgfx.Attrib.Bitangent,
            VertexElementUsage.BlendIndices => bgfx.Attrib.Indices,
            VertexElementUsage.BlendWeight => bgfx.Attrib.Weight,
            _ => bgfx.Attrib.Position,
        };

    private static int GetElementCount(VertexElementFormat format) =>
        format switch
        {
            VertexElementFormat.Single => 1,
            VertexElementFormat.Vector2 => 2,
            VertexElementFormat.Vector3 => 3,
            VertexElementFormat.Vector4 => 4,
            VertexElementFormat.Color => 4,
            VertexElementFormat.Byte4 => 4,
            VertexElementFormat.Short2 => 2,
            VertexElementFormat.Short4 => 4,
            _ => 4,
        };

    private static bgfx.AttribType ToBgfxAttribType(VertexElementFormat format) =>
        format switch
        {
            VertexElementFormat.Color => bgfx.AttribType.Uint8,
            VertexElementFormat.Byte4 => bgfx.AttribType.Uint8,
            VertexElementFormat.Short2 => bgfx.AttribType.Int16,
            VertexElementFormat.Short4 => bgfx.AttribType.Int16,
            _ => bgfx.AttribType.Float,
        };

    private static ulong ToDepthTest(CompareFunction function) =>
        function switch
        {
            CompareFunction.Always => (ulong)bgfx.StateFlags.DepthTestAlways,
            CompareFunction.Never => (ulong)bgfx.StateFlags.DepthTestNever,
            CompareFunction.Less => (ulong)bgfx.StateFlags.DepthTestLess,
            CompareFunction.LessEqual => (ulong)bgfx.StateFlags.DepthTestLequal,
            CompareFunction.Equal => (ulong)bgfx.StateFlags.DepthTestEqual,
            CompareFunction.GreaterEqual => (ulong)bgfx.StateFlags.DepthTestGequal,
            CompareFunction.Greater => (ulong)bgfx.StateFlags.DepthTestGreater,
            CompareFunction.NotEqual => (ulong)bgfx.StateFlags.DepthTestNotequal,
            _ => (ulong)bgfx.StateFlags.DepthTestLequal,
        };

    private static ulong ToBlend(Blend source, Blend destination)
    {
        ulong color = (ulong)ToBgfxBlend(source) | ((ulong)ToBgfxBlend(destination) << 4);
        return color | (color << 8);
    }

    private static bgfx.StateFlags ToBgfxBlend(Blend blend) =>
        blend switch
        {
            Blend.Zero => bgfx.StateFlags.BlendZero,
            Blend.One => bgfx.StateFlags.BlendOne,
            Blend.SourceColor => bgfx.StateFlags.BlendSrcColor,
            Blend.InverseSourceColor => bgfx.StateFlags.BlendInvSrcColor,
            Blend.SourceAlpha => bgfx.StateFlags.BlendSrcAlpha,
            Blend.InverseSourceAlpha => bgfx.StateFlags.BlendInvSrcAlpha,
            Blend.DestinationColor => bgfx.StateFlags.BlendDstColor,
            Blend.InverseDestinationColor => bgfx.StateFlags.BlendInvDstColor,
            Blend.DestinationAlpha => bgfx.StateFlags.BlendDstAlpha,
            Blend.InverseDestinationAlpha => bgfx.StateFlags.BlendInvDstAlpha,
            _ => bgfx.StateFlags.BlendOne,
        };

    private static uint ToSamplerFlags(SamplerState samplerState)
    {
        uint flags = 0;
        flags |= samplerState.AddressU switch
        {
            TextureAddressMode.Clamp => (uint)bgfx.SamplerFlags.UClamp,
            TextureAddressMode.Mirror => (uint)bgfx.SamplerFlags.UMirror,
            _ => 0,
        };
        flags |= samplerState.AddressV switch
        {
            TextureAddressMode.Clamp => (uint)bgfx.SamplerFlags.VClamp,
            TextureAddressMode.Mirror => (uint)bgfx.SamplerFlags.VMirror,
            _ => 0,
        };
        flags |= samplerState.AddressW switch
        {
            TextureAddressMode.Clamp => (uint)bgfx.SamplerFlags.WClamp,
            TextureAddressMode.Mirror => (uint)bgfx.SamplerFlags.WMirror,
            _ => 0,
        };

        if (samplerState.Filter == TextureFilter.Point)
        {
            flags |= (uint)(
                bgfx.SamplerFlags.MinPoint | bgfx.SamplerFlags.MagPoint | bgfx.SamplerFlags.MipPoint
            );
        }
        else if (samplerState.Filter == TextureFilter.Anisotropic)
        {
            flags |= (uint)(bgfx.SamplerFlags.MinAnisotropic | bgfx.SamplerFlags.MagAnisotropic);
        }

        return flags;
    }

    private static GraphicsBackend NormalizeBackend(GraphicsBackend backend)
    {
#if IOS
        return backend == GraphicsBackend.Auto ? GraphicsBackend.Metal : backend;
#else
        return backend;
#endif
    }

    private static bool RequiresSingleThreadedRenderer()
    {
#if IOS
        return true;
#else
        return false;
#endif
    }

    private static bgfx.RendererType ToBgfxRenderer(GraphicsBackend backend) =>
        backend switch
        {
            GraphicsBackend.Auto => bgfx.RendererType.Count,
            GraphicsBackend.Direct3D11 => bgfx.RendererType.Direct3D11,
            GraphicsBackend.Direct3D12 => bgfx.RendererType.Direct3D12,
            GraphicsBackend.Metal => bgfx.RendererType.Metal,
            GraphicsBackend.Vulkan => bgfx.RendererType.Vulkan,
            GraphicsBackend.OpenGL => bgfx.RendererType.OpenGL,
            GraphicsBackend.OpenGLES => bgfx.RendererType.OpenGLES,
            GraphicsBackend.WebGL => bgfx.RendererType.OpenGLES,
            GraphicsBackend.WebGPU => bgfx.RendererType.WebGPU,
            _ => bgfx.RendererType.Count,
        };

    private static GraphicsBackend FromBgfxRenderer(bgfx.RendererType renderer) =>
        renderer switch
        {
            bgfx.RendererType.Direct3D11 => GraphicsBackend.Direct3D11,
            bgfx.RendererType.Direct3D12 => GraphicsBackend.Direct3D12,
            bgfx.RendererType.Metal => GraphicsBackend.Metal,
            bgfx.RendererType.Vulkan => GraphicsBackend.Vulkan,
            bgfx.RendererType.OpenGL => GraphicsBackend.OpenGL,
            bgfx.RendererType.OpenGLES => GraphicsBackend.OpenGLES,
            bgfx.RendererType.WebGPU => GraphicsBackend.WebGPU,
            _ => GraphicsBackend.Auto,
        };

    private static bgfx.TextureFormat ToBgfxTextureFormat(SurfaceFormat format) =>
        format switch
        {
            SurfaceFormat.Color => bgfx.TextureFormat.RGBA8,
            SurfaceFormat.Bgr565 => bgfx.TextureFormat.R5G6B5,
            SurfaceFormat.Bgra4444 => bgfx.TextureFormat.RGBA4,
            SurfaceFormat.Dxt1 => bgfx.TextureFormat.BC1,
            SurfaceFormat.Dxt3 => bgfx.TextureFormat.BC2,
            SurfaceFormat.Dxt5 => bgfx.TextureFormat.BC3,
            SurfaceFormat.HalfVector4 => bgfx.TextureFormat.RGBA16F,
            SurfaceFormat.Single => bgfx.TextureFormat.R32F,
            SurfaceFormat.Vector2 => bgfx.TextureFormat.RG32F,
            SurfaceFormat.Vector4 => bgfx.TextureFormat.RGBA32F,
            _ => bgfx.TextureFormat.RGBA8,
        };

    private static bgfx.TextureFormat ToBgfxTextureFormat(DepthFormat format) =>
        format switch
        {
            DepthFormat.Depth16 => bgfx.TextureFormat.D16,
            DepthFormat.Depth24 => bgfx.TextureFormat.D24,
            DepthFormat.Depth24Stencil8 => bgfx.TextureFormat.D24S8,
            _ => bgfx.TextureFormat.Count,
        };

    private static string GetRendererName(bgfx.RendererType rendererType)
    {
        IntPtr pointer = bgfx.get_renderer_name(rendererType);
        return pointer == IntPtr.Zero
            ? rendererType.ToString()
            : Marshal.PtrToStringAnsi(pointer) ?? rendererType.ToString();
    }

    private static bool IsBrowserRuntime()
    {
#if BROWSER
        return true;
#else
        return false;
#endif
    }
}
