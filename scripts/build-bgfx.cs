using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

Options options = Options.Parse(args);
string scriptPath = SourcePath();
string repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(scriptPath)!, ".."));
string sourceRoot = Path.GetFullPath(Path.Combine(repoRoot, options.SourceRoot));
string bxPath = Path.Combine(sourceRoot, "bx");
string bimgPath = Path.Combine(sourceRoot, "bimg");
string bgfxPath = Path.Combine(sourceRoot, "bgfx");
string outputRoot = Path.Combine(repoRoot, "native", "bgfx", "bin", options.Configuration, ToNativeOutputTarget(options.Target));

Directory.CreateDirectory(sourceRoot);

if (!options.SkipClone)
{
    CloneIfMissing("https://github.com/bkaradzic/bx.git", bxPath);
    CloneIfMissing("https://github.com/bkaradzic/bimg.git", bimgPath);
    CloneIfMissing("https://github.com/bkaradzic/bgfx.git", bgfxPath);
}

if (!Directory.Exists(bgfxPath))
{
    throw new InvalidOperationException($"BGFX source not found at {bgfxPath}. Run without --skip-clone or pass --source-root.");
}

string genie = FindGenie(bxPath);

if (options.IsAndroid)
{
    string gcc = ToAndroidGcc(options.Target);
    string projectDirectory = Path.Combine(bgfxPath, ".build", "projects", $"gmake-{gcc}");
    string androidNdkRoot = FindAndroidNdkRoot(required: true)!;
    IReadOnlyDictionary<string, string> androidEnvironment = CreateAndroidBuildEnvironment(androidNdkRoot);
    Run(genie, ["--with-shared-lib", $"--gcc={gcc}", "gmake"], bgfxPath, androidEnvironment);
    if (options.IsAndroidVulkan)
    {
        PatchAndroidVulkanGeneratedMakefiles(projectDirectory);
        CleanAndroidBuildOutput(bgfxPath, gcc, options.Configuration);
    }

    string make = FindMake();
    Run(make, ["-R", "-C", projectDirectory, $"config={options.Configuration.ToLowerInvariant()}"], bgfxPath, androidEnvironment);

    CopyBuiltLibraries(
        FindBuiltLibraries(Path.Combine(bgfxPath, ".build", gcc, "bin"), options.Configuration, ".so"),
        outputRoot,
        options.Configuration.Equals("Debug", StringComparison.OrdinalIgnoreCase) ? "libbgfx_debug.so" : "libbgfx.so");
    CopyAndroidRuntimeDependencies(options.Target, outputRoot);
}
else if (options.IsBrowserWasm)
{
    string projectDirectory = Path.Combine(bgfxPath, ".build", "projects", "gmake-wasm");
    EmscriptenToolchain emscripten = FindEmscriptenToolchain();
    Run(genie, ["--gcc=wasm", "gmake"], bgfxPath, emscripten.Environment);
    string make = FindMake();
    Run(emscripten.Emmake, [make, "-R", "-C", projectDirectory, $"config={options.Configuration.ToLowerInvariant()}"], bgfxPath, emscripten.Environment);

    CopyBuiltLibraries(
        FindWasmBuiltLibraries(Path.Combine(bgfxPath, ".build", "wasm", "bin"), options.Configuration),
        outputRoot,
        options.Configuration.Equals("Debug", StringComparison.OrdinalIgnoreCase) ? "libbgfx_debug.a" : "libbgfx.a");
}
else if (options.IsIOS)
{
    string gcc = ToIosGcc(options.Target);
    string projectDirectory = Path.Combine(bgfxPath, ".build", "projects", $"gmake-{gcc}");
    IosToolchain iosToolchain = FindIosToolchain(options.Target);
    PatchMetalCppForIos(bgfxPath);
    Run(genie, [$"--gcc={gcc}", "gmake"], bgfxPath);
    PatchIosGeneratedMakefiles(projectDirectory, iosToolchain);
    CleanIosBuildOutput(bgfxPath, gcc, options.Configuration);
    string make = FindMake();
    Run(make,
        [
            "-R",
            "-C",
            projectDirectory,
            $"config={options.Configuration.ToLowerInvariant()}",
            $"CC={iosToolchain.Clang}",
            $"CXX={iosToolchain.Clangxx}",
            $"AR={iosToolchain.Ar}",
            $"CFLAGS={iosToolchain.CompilerFlags}",
            $"CXXFLAGS={iosToolchain.CompilerFlags}",
            $"LDFLAGS={iosToolchain.LinkerFlags}",
        ],
        bgfxPath);

    IReadOnlyList<string> libraries = FindStaticBuiltLibraries(Path.Combine(bgfxPath, ".build", gcc, "bin"), options.Configuration);
    ValidateBgfxC99Symbols(libraries);
    CopyBuiltLibraries(
        libraries,
        outputRoot,
        options.Configuration.Equals("Debug", StringComparison.OrdinalIgnoreCase) ? "libbgfx_debug.a" : "libbgfx.a");
}
else if (options.IsDesktopUnix)
{
    EnsureDesktopUnixTargetCanBuild(options.Target);
    string projectDirectory = Path.Combine(bgfxPath, ".build", "projects", "gmake");
    Run(genie, ["--with-shared-lib", "gmake"], bgfxPath);
    string make = FindMake();
    Run(make, ["-R", "-C", projectDirectory, $"config={options.Configuration.ToLowerInvariant()}", "bgfx-shared-lib"], bgfxPath);

    string extension = options.Target.StartsWith("osx-", StringComparison.OrdinalIgnoreCase) ? ".dylib" : ".so";
    string expectedName = options.Configuration.Equals("Debug", StringComparison.OrdinalIgnoreCase)
        ? $"libbgfx_debug{extension}"
        : $"libbgfx{extension}";
    CopyBuiltLibraries(
        FindBuiltLibraries(Path.Combine(bgfxPath, ".build"), options.Configuration, extension),
        outputRoot,
        expectedName);
}
else
{
    Run(genie, ["--with-shared-lib", options.Generator], bgfxPath);

    string project = Path.Combine(bgfxPath, ".build", "projects", options.Generator, "bgfx-shared-lib.vcxproj");
    if (!File.Exists(project))
    {
        throw new InvalidOperationException($"Generated BGFX shared library project not found at {project}.");
    }

    string msbuild = FindMsBuild();
    Run(msbuild, [project, "/m", "/t:Build", $"/p:Configuration={options.Configuration}", $"/p:Platform={options.Platform}"], bgfxPath);

    CopyBuiltLibraries(
        FindBuiltLibraries(Path.Combine(bgfxPath, ".build"), options.Configuration, ".dll"),
        outputRoot,
        options.Configuration.Equals("Debug", StringComparison.OrdinalIgnoreCase) ? "bgfx_debug.dll" : "bgfx.dll");
}

Console.WriteLine($"BGFX native libraries copied to {outputRoot}");

static string ToNativeOutputTarget(string target) =>
    target switch
    {
        "android-vulkan-arm" => "android-arm",
        "android-vulkan-arm64" => "android-arm64",
        "android-vulkan-x86" => "android-x86",
        "android-vulkan-x64" => "android-x64",
        _ => target,
    };

static string ToAndroidGcc(string target) =>
    target switch
    {
        "android-arm" => "android-arm",
        "android-arm64" => "android-arm64",
        "android-x86" => "android-x86",
        "android-x64" => "android-x86_64",
        "android-vulkan-arm" => "android-arm",
        "android-vulkan-arm64" => "android-arm64",
        "android-vulkan-x86" => "android-x86",
        "android-vulkan-x64" => "android-x86_64",
        _ => throw new ArgumentException($"Target '{target}' is not an Android target."),
    };

static string ToAndroidTriple(string target) =>
    target switch
    {
        "android-arm" => "arm-linux-androideabi",
        "android-arm64" => "aarch64-linux-android",
        "android-x86" => "i686-linux-android",
        "android-x64" => "x86_64-linux-android",
        "android-vulkan-arm" => "arm-linux-androideabi",
        "android-vulkan-arm64" => "aarch64-linux-android",
        "android-vulkan-x86" => "i686-linux-android",
        "android-vulkan-x64" => "x86_64-linux-android",
        _ => throw new ArgumentException($"Target '{target}' is not an Android target."),
    };

static void PatchAndroidVulkanGeneratedMakefiles(string projectDirectory)
{
    if (!Directory.Exists(projectDirectory))
    {
        return;
    }

    foreach (string file in Directory.EnumerateFiles(projectDirectory, "*", SearchOption.TopDirectoryOnly)
        .Where(path => Path.GetFileName(path).Equals("Makefile", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(path).Equals(".make", StringComparison.OrdinalIgnoreCase)))
    {
        string contents = File.ReadAllText(file);
        string patched = AddMakefileFlag(contents, "DEFINES", "-DBGFX_CONFIG_RENDERER_VULKAN=1");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_DEBUG_OBJECT_NAME=0");
        if (!string.Equals(contents, patched, StringComparison.Ordinal))
        {
            File.WriteAllText(file, patched);
        }
    }
}

static void CleanAndroidBuildOutput(string bgfxPath, string gcc, string configuration)
{
    string buildRoot = Path.Combine(bgfxPath, ".build", gcc);
    string objRoot = Path.Combine(buildRoot, "obj", configuration);
    if (Directory.Exists(objRoot))
    {
        Directory.Delete(objRoot, recursive: true);
    }

    string binRoot = Path.Combine(buildRoot, "bin");
    if (Directory.Exists(binRoot))
    {
        foreach (string library in Directory.EnumerateFiles(binRoot, $"*{configuration}.so", SearchOption.TopDirectoryOnly))
        {
            File.Delete(library);
        }
    }
}

static IReadOnlyDictionary<string, string> CreateAndroidBuildEnvironment(string androidNdkRoot)
{
    string makeSafeNdkRoot = OperatingSystem.IsWindows()
        ? GetWindowsShortPath(androidNdkRoot)
        : androidNdkRoot;

    Dictionary<string, string> environment = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ANDROID_NDK_ROOT"] = makeSafeNdkRoot,
        ["ANDROID_NDK_HOME"] = makeSafeNdkRoot,
    };

    return environment;
}

static string? FindAndroidNdkRoot(bool required)
{
    List<string> candidates = new();
    AddIfNotEmpty(candidates, Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT"));
    AddIfNotEmpty(candidates, Environment.GetEnvironmentVariable("ANDROID_NDK_HOME"));

    foreach (string sdkRoot in FindAndroidSdkRoots())
    {
        string ndkRoot = Path.Combine(sdkRoot, "ndk");
        if (Directory.Exists(ndkRoot))
        {
            candidates.AddRange(Directory.EnumerateDirectories(ndkRoot).OrderByDescending(Path.GetFileName));
        }

        AddIfDirectory(candidates, Path.Combine(sdkRoot, "ndk-bundle"));
    }

    foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (IsValidAndroidNdkRoot(candidate))
        {
            return Path.GetFullPath(candidate);
        }
    }

    if (required)
    {
        throw new InvalidOperationException("Android NDK was not found. Install the .NET Android workload/Android SDK NDK, or set ANDROID_NDK_ROOT.");
    }

    return null;
}

static IEnumerable<string> FindAndroidSdkRoots()
{
    List<string> roots = new();
    AddIfNotEmpty(roots, Environment.GetEnvironmentVariable("ANDROID_HOME"));
    AddIfNotEmpty(roots, Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"));

    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    AddIfDirectory(roots, Path.Combine(localAppData, "Android", "Sdk"));

    string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    AddIfDirectory(roots, Path.Combine(programFilesX86, "Android", "android-sdk"));

    return roots.Distinct(StringComparer.OrdinalIgnoreCase);
}

static void AddIfNotEmpty(List<string> values, string? value)
{
    if (!string.IsNullOrWhiteSpace(value))
    {
        values.Add(value);
    }
}

static void AddIfDirectory(List<string> values, string path)
{
    if (Directory.Exists(path))
    {
        values.Add(path);
    }
}

static bool IsValidAndroidNdkRoot(string path)
{
    if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
    {
        return false;
    }

    string prebuilt = FindAndroidNdkPrebuiltDirectory(path);
    string clang = Path.Combine(prebuilt, "bin", OperatingSystem.IsWindows() ? "clang.exe" : "clang");
    return File.Exists(clang);
}

static string FindAndroidNdkPrebuiltDirectory(string ndkRoot)
{
    string host = OperatingSystem.IsWindows()
        ? "windows-x86_64"
        : OperatingSystem.IsMacOS()
            ? "darwin-x86_64"
            : "linux-x86_64";

    string candidate = Path.Combine(ndkRoot, "toolchains", "llvm", "prebuilt", host);
    if (Directory.Exists(candidate))
    {
        return candidate;
    }

    string prebuiltRoot = Path.Combine(ndkRoot, "toolchains", "llvm", "prebuilt");
    if (Directory.Exists(prebuiltRoot))
    {
        string? fallback = Directory.EnumerateDirectories(prebuiltRoot).FirstOrDefault();
        if (fallback is not null)
        {
            return fallback;
        }
    }

    return candidate;
}

static string GetWindowsShortPath(string path)
{
    StringBuilder buffer = new(512);
    uint length = WindowsNative.GetShortPathName(path, buffer, (uint)buffer.Capacity);
    if (length == 0)
    {
        return path;
    }

    if (length > buffer.Capacity)
    {
        buffer.EnsureCapacity((int)length);
        length = WindowsNative.GetShortPathName(path, buffer, (uint)buffer.Capacity);
    }

    return length == 0 ? path : buffer.ToString();
}

static string ToIosGcc(string target) =>
    target switch
    {
        "ios-arm64" => "ios-arm64",
        "ios-simulator" => "ios-simulator",
        _ => throw new ArgumentException($"Target '{target}' is not an iOS target."),
    };

static void EnsureDesktopUnixTargetCanBuild(string target)
{
    if (target.StartsWith("linux-", StringComparison.OrdinalIgnoreCase) && !OperatingSystem.IsLinux())
    {
        throw new InvalidOperationException($"{target} BGFX native libraries must be built on Linux.");
    }

    if (target.StartsWith("osx-", StringComparison.OrdinalIgnoreCase) && !OperatingSystem.IsMacOS())
    {
        throw new InvalidOperationException($"{target} BGFX native libraries must be built on macOS.");
    }

    string expectedArchitecture = target.EndsWith("-arm64", StringComparison.OrdinalIgnoreCase) ? "Arm64" : "X64";
    if (!RuntimeInformation.ProcessArchitecture.ToString().Equals(expectedArchitecture, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"{target} BGFX native libraries must be built on a {expectedArchitecture} host.");
    }
}

static IosToolchain FindIosToolchain(string target)
{
    if (!OperatingSystem.IsMacOS())
    {
        throw new InvalidOperationException("iOS BGFX native libraries must be built on macOS with Xcode installed.");
    }

    string sdk = target.Equals("ios-simulator", StringComparison.OrdinalIgnoreCase)
        ? "iphonesimulator"
        : "iphoneos";
    string platform = target.Equals("ios-simulator", StringComparison.OrdinalIgnoreCase)
        ? "iPhoneSimulator"
        : "iPhoneOS";

    string sdkPath = Capture("xcrun", ["--sdk", sdk, "--show-sdk-path"], Directory.GetCurrentDirectory()).Trim();
    if (string.IsNullOrWhiteSpace(sdkPath) || !Directory.Exists(sdkPath))
    {
        throw new InvalidOperationException($"Could not resolve the {sdk} SDK path. Install Xcode and run: sudo xcode-select -s /Applications/Xcode.app/Contents/Developer");
    }

    string architecture = target.Equals("ios-simulator", StringComparison.OrdinalIgnoreCase)
        ? Capture("uname", ["-m"], Directory.GetCurrentDirectory()).Trim()
        : "arm64";
    if (!architecture.Equals("x86_64", StringComparison.OrdinalIgnoreCase))
    {
        architecture = "arm64";
    }

    string targetTriple = target.Equals("ios-simulator", StringComparison.OrdinalIgnoreCase)
        ? $"{architecture}-apple-ios16.0-simulator"
        : "arm64-apple-ios16.0";

    string minimumVersionFlag = target.Equals("ios-simulator", StringComparison.OrdinalIgnoreCase)
        ? "-mios-simulator-version-min=16.0"
        : "-mios-version-min=16.0";

    string clang = Capture("xcrun", ["--sdk", sdk, "--find", "clang"], Directory.GetCurrentDirectory()).Trim();
    string clangxx = Capture("xcrun", ["--sdk", sdk, "--find", "clang++"], Directory.GetCurrentDirectory()).Trim();
    string ar = Capture("xcrun", ["--sdk", sdk, "--find", "ar"], Directory.GetCurrentDirectory()).Trim();
    string compilerFlags = $"-target {targetTriple} -isysroot \"{sdkPath}\" -arch {architecture} {minimumVersionFlag}";
    string linkerFlags = compilerFlags;

    return new IosToolchain(platform, sdkPath, targetTriple, clang, clangxx, ar, compilerFlags, linkerFlags);
}

static void CleanIosBuildOutput(string bgfxPath, string gcc, string configuration)
{
    string buildRoot = Path.Combine(bgfxPath, ".build", gcc);
    string objRoot = Path.Combine(buildRoot, "obj", configuration);
    if (Directory.Exists(objRoot))
    {
        Directory.Delete(objRoot, recursive: true);
    }

    string binRoot = Path.Combine(buildRoot, "bin");
    if (Directory.Exists(binRoot))
    {
        foreach (string archive in Directory.EnumerateFiles(binRoot, $"*{configuration}.a", SearchOption.TopDirectoryOnly))
        {
            File.Delete(archive);
        }
    }
}

static void PatchIosGeneratedMakefiles(string projectDirectory, IosToolchain toolchain)
{
    if (!Directory.Exists(projectDirectory))
    {
        return;
    }

    string sdkPattern = $@"/Applications/Xcode\.app/Contents/Developer/Platforms/{Regex.Escape(toolchain.Platform)}\.platform/Developer/SDKs/{Regex.Escape(toolchain.Platform)}[^""\s]*?\.sdk";
    foreach (string file in Directory.EnumerateFiles(projectDirectory, "*", SearchOption.TopDirectoryOnly)
        .Where(path => Path.GetFileName(path).Equals("Makefile", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(path).Equals(".make", StringComparison.OrdinalIgnoreCase)))
    {
        string contents = File.ReadAllText(file);
        string patched = Regex.Replace(contents, sdkPattern, toolchain.SdkPath.Replace("\\", "/"));
        patched = AddMakefileFlag(patched, "INCLUDES", "-I\"../../../../bx/include/compat/ios\"");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_RENDERER_AGC=0");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_RENDERER_DIRECT3D11=0");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_RENDERER_DIRECT3D12=0");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_RENDERER_GNM=0");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_RENDERER_METAL=1");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_RENDERER_NVN=0");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_RENDERER_OPENGL=0");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_RENDERER_OPENGLES=0");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_RENDERER_VULKAN=0");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFX_CONFIG_RENDERER_WEBGPU=0");
        patched = AddMakefileFlag(patched, "DEFINES", "-DBGFXNA_FORCE_METALCPP_DLSYM_CONSTANTS=1");
        patched = AddMakefileFlag(patched, "ALL_ASMFLAGS", $"-target {toolchain.TargetTriple}");
        patched = AddMakefileFlag(patched, "ALL_CFLAGS", $"-target {toolchain.TargetTriple}");
        patched = AddMakefileFlag(patched, "ALL_CXXFLAGS", $"-target {toolchain.TargetTriple}");
        patched = AddMakefileFlag(patched, "ALL_OBJCFLAGS", $"-target {toolchain.TargetTriple}");
        patched = AddMakefileFlag(patched, "ALL_OBJCPPFLAGS", $"-target {toolchain.TargetTriple}");
        if (!string.Equals(contents, patched, StringComparison.Ordinal))
        {
            File.WriteAllText(file, patched);
        }
    }
}

static void PatchMetalCppForIos(string bgfxPath)
{
    string metalHeader = Path.Combine(bgfxPath, "3rdparty", "metal-cpp", "metal.hpp");
    if (!File.Exists(metalHeader))
    {
        return;
    }

    const string original = "#elif defined(__MAC_26_0) || defined(__IPHONE_26_0) || defined(__TVOS_26_0)";
    const string patched = "#elif !defined(BGFXNA_FORCE_METALCPP_DLSYM_CONSTANTS) && (defined(__MAC_26_0) || defined(__IPHONE_26_0) || defined(__TVOS_26_0))";
    string contents = File.ReadAllText(metalHeader);
    if (contents.Contains(original, StringComparison.Ordinal))
    {
        File.WriteAllText(metalHeader, contents.Replace(original, patched, StringComparison.Ordinal));
    }
}

static string AddMakefileFlag(string contents, string variable, string flag)
{
    if (contents.Contains(flag, StringComparison.Ordinal))
    {
        return contents;
    }

    return Regex.Replace(
        contents,
        $@"(^\s*{Regex.Escape(variable)}\s*\+=.*)$",
        $"$1 {flag}",
        RegexOptions.Multiline);
}

static string SourcePath([CallerFilePath] string path = "") => path;

static string FindGenie(string bxPath)
{
    string[] candidates = OperatingSystem.IsMacOS()
        ?
        [
            Path.Combine(bxPath, "tools", "bin", "darwin", "genie"),
            Path.Combine(bxPath, "tools", "bin", "osx", "genie"),
            Path.Combine(bxPath, "tools", "bin", "windows", "genie.exe"),
        ]
        : OperatingSystem.IsLinux()
            ?
            [
                Path.Combine(bxPath, "tools", "bin", "linux", "genie"),
                Path.Combine(bxPath, "tools", "bin", "freebsd", "genie"),
                Path.Combine(bxPath, "tools", "bin", "windows", "genie.exe"),
            ]
            :
            [
                Path.Combine(bxPath, "tools", "bin", "windows", "genie.exe"),
                Path.Combine(bxPath, "tools", "bin", "darwin", "genie"),
                Path.Combine(bxPath, "tools", "bin", "linux", "genie"),
                Path.Combine(bxPath, "tools", "bin", "freebsd", "genie"),
            ];

    foreach (string candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            EnsureExecutable(candidate);
            return candidate;
        }
    }

    throw new InvalidOperationException($"GENie was not found under {Path.Combine(bxPath, "tools", "bin")}. Make sure bx was cloned correctly.");
}

static void EnsureExecutable(string path)
{
    if (OperatingSystem.IsWindows())
    {
        return;
    }

    UnixFileMode mode = File.GetUnixFileMode(path);
    UnixFileMode executableBits = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
    if ((mode & executableBits) != executableBits)
    {
        File.SetUnixFileMode(path, mode | executableBits);
    }

    if (OperatingSystem.IsMacOS())
    {
        RemoveMacQuarantine(path);
    }
}

static void RemoveMacQuarantine(string path)
{
    using Process process = Process.Start(new ProcessStartInfo
    {
        FileName = "xattr",
        WorkingDirectory = Path.GetDirectoryName(path)!,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    }.WithArguments(["-d", "com.apple.quarantine", path])) ?? throw new InvalidOperationException("Failed to start xattr.");

    process.WaitForExit();
}

void CloneIfMissing(string repositoryUrl, string targetPath)
{
    if (!Directory.Exists(targetPath))
    {
        Run("git", ["clone", repositoryUrl, targetPath], sourceRoot);
    }
}

static string FindMsBuild()
{
    string? fromPath = FindOnPath("msbuild.exe");
    if (fromPath is not null)
    {
        return fromPath;
    }

    string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    string vswhere = Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
    if (File.Exists(vswhere))
    {
        string installPath = Capture(vswhere, ["-latest", "-requires", "Microsoft.Component.MSBuild", "-property", "installationPath"], Directory.GetCurrentDirectory()).Trim();
        if (!string.IsNullOrWhiteSpace(installPath))
        {
            string candidate = Path.Combine(installPath, "MSBuild", "Current", "Bin", "MSBuild.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    throw new InvalidOperationException("MSBuild.exe was not found. Install Visual Studio Build Tools with Desktop development with C++.");
}

static string FindMake()
{
    string? fromPath = FindOnPath("make.exe") ?? FindOnPath("mingw32-make.exe") ?? FindOnPath("gmake.exe") ?? FindOnPath("make");
    if (fromPath is not null)
    {
        return fromPath;
    }

    string? ndkRoot = Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT");
    if (!string.IsNullOrWhiteSpace(ndkRoot))
    {
        string candidate = Path.Combine(ndkRoot, "prebuilt", "windows-x86_64", "bin", "make.exe");
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }

    throw new InvalidOperationException("GNU make was not found. Install GNU Make, add make/mingw32-make/gmake to PATH, or install Android NDK and set ANDROID_NDK_ROOT.");
}

static EmscriptenToolchain FindEmscriptenToolchain()
{
    string? emcc = FindOnPath("emcc.bat") ?? FindOnPath("emcc");
    if (emcc is not null)
    {
        string emscriptenPath = Path.GetDirectoryName(emcc)!;
        string emmake = Path.Combine(emscriptenPath, OperatingSystem.IsWindows() ? "emmake.bat" : "emmake");
        if (File.Exists(emmake))
        {
            return EmscriptenToolchain.Create(emscriptenPath, emmake);
        }
    }

    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    string packsRoot = Path.Combine(programFiles, "dotnet", "packs");
    if (Directory.Exists(packsRoot))
    {
        string[] candidates = Directory
            .EnumerateDirectories(packsRoot, "Microsoft.NET.Runtime.Emscripten.*.Sdk.*")
            .SelectMany(pack => Directory.EnumerateDirectories(pack)
                .Select(version => Path.Combine(version, "tools", "emscripten")))
            .Where(Directory.Exists)
            .OrderByDescending(path => path)
            .ToArray();

        foreach (string emscriptenPath in candidates)
        {
            string emmake = Path.Combine(emscriptenPath, OperatingSystem.IsWindows() ? "emmake.bat" : "emmake");
            string emccCandidate = Path.Combine(emscriptenPath, OperatingSystem.IsWindows() ? "emcc.bat" : "emcc");
            if (File.Exists(emmake) && File.Exists(emccCandidate))
            {
                return EmscriptenToolchain.Create(emscriptenPath, emmake);
            }
        }
    }

    throw new InvalidOperationException("Emscripten was not found. Install the .NET wasm-tools workload or put emcc/emmake on PATH.");
}

static string? FindOnPath(string fileName)
{
    string? path = System.Environment.GetEnvironmentVariable("PATH");
    if (path is null)
    {
        return null;
    }

    foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
    {
        string candidate = Path.Combine(directory.Trim(), fileName);
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }

    return null;
}

static IReadOnlyList<string> FindBuiltLibraries(string buildRoot, string configuration, string extension)
{
    if (!Directory.Exists(buildRoot))
    {
        return Array.Empty<string>();
    }

    string[] all = Directory
        .EnumerateFiles(buildRoot, $"*{extension}", SearchOption.AllDirectories)
        .Where(path =>
        {
            string name = Path.GetFileName(path);
            return name.StartsWith("bgfx", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("libbgfx", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("bgfx-shared-lib", StringComparison.OrdinalIgnoreCase);
        })
        .ToArray();

    string[] matching = all
        .Where(path => path.Contains($"{Path.DirectorySeparatorChar}{configuration}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(path).Contains(configuration, StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => Path.GetFileName(path).StartsWith("bgfx-shared-lib", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ThenBy(path => path)
        .ToArray();

    return matching.Length == 0 ? all : matching;
}

static IReadOnlyList<string> FindWasmBuiltLibraries(string buildRoot, string configuration)
    => FindStaticBuiltLibraries(buildRoot, configuration);

static IReadOnlyList<string> FindStaticBuiltLibraries(string buildRoot, string configuration)
{
    if (!Directory.Exists(buildRoot))
    {
        return Array.Empty<string>();
    }

    string[] names = ["bgfx", "libbgfx", "bimg", "libbimg", "bx", "libbx"];
    string[] all = Directory
        .EnumerateFiles(buildRoot, "*.a", SearchOption.AllDirectories)
        .Where(path =>
        {
            string name = Path.GetFileNameWithoutExtension(path);
            return names.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        })
        .ToArray();

    string[] matching = all
        .Where(path => path.Contains($"{Path.DirectorySeparatorChar}{configuration}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(path).Contains(configuration, StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => Path.GetFileName(path).Contains("bgfx", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ThenBy(path => path)
        .ToArray();

    return matching.Length == 0 ? all : matching;
}

static void ValidateBgfxC99Symbols(IReadOnlyList<string> libraries)
{
    string? bgfxArchive = libraries.FirstOrDefault(path =>
        Path.GetFileNameWithoutExtension(path).Contains("bgfx", StringComparison.OrdinalIgnoreCase));

    if (bgfxArchive is null)
    {
        throw new InvalidOperationException("The native build did not produce a bgfx static archive.");
    }

    string? nm = FindOnPath("nm") ?? FindOnPath("llvm-nm") ?? FindOnPath("llvm-nm.exe");
    if (nm is null)
    {
        Console.WriteLine("warning: nm/llvm-nm was not found; skipping bgfx C99 symbol validation.");
        return;
    }

    string symbols = Capture(nm, [bgfxArchive], Directory.GetCurrentDirectory());
    if (!symbols.Contains("bgfx_alloc", StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"The bgfx archive '{bgfxArchive}' does not export bgfx C99 API symbols such as bgfx_alloc. Rebuild BGFX from a clean native-src/bgfx tree.");
    }
}

static void CopyBuiltLibraries(IReadOnlyList<string> candidates, string outputRoot, string expectedName)
{
    if (candidates.Count == 0)
    {
        throw new InvalidOperationException("Could not find a built BGFX shared library.");
    }

    Directory.CreateDirectory(outputRoot);
    foreach (string library in candidates)
    {
        File.Copy(library, Path.Combine(outputRoot, Path.GetFileName(library)), overwrite: true);
    }

    File.Copy(candidates[0], Path.Combine(outputRoot, expectedName), overwrite: true);
}

static void CopyAndroidRuntimeDependencies(string target, string outputRoot)
{
    string? ndkRoot = FindAndroidNdkRoot(required: false);
    if (string.IsNullOrWhiteSpace(ndkRoot))
    {
        return;
    }

    string prebuiltDirectory = FindAndroidNdkPrebuiltDirectory(ndkRoot);
    string libcxx = Path.Combine(
        prebuiltDirectory,
        "sysroot",
        "usr",
        "lib",
        ToAndroidTriple(target),
        "libc++_shared.so");

    if (File.Exists(libcxx))
    {
        File.Copy(libcxx, Path.Combine(outputRoot, "libc++_shared.so"), overwrite: true);
    }
}

static void Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string>? environment = null)
{
    Console.WriteLine($">> {fileName} {string.Join(" ", arguments.Select(QuoteIfNeeded))}");

    ProcessStartInfo startInfo = new()
    {
        FileName = fileName,
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = false,
        RedirectStandardError = false,
    };

    if (environment != null)
    {
        foreach (var variable in environment)
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }
    }

    using Process process = Process.Start(startInfo.WithArguments(arguments)) ?? throw new InvalidOperationException($"Failed to start {fileName}.");

    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {fileName} {string.Join(" ", arguments.Select(QuoteIfNeeded))}");
    }
}

static string Capture(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
{
    using Process process = Process.Start(new ProcessStartInfo
    {
        FileName = fileName,
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    }.WithArguments(arguments)) ?? throw new InvalidOperationException($"Failed to start {fileName}.");

    string stdout = process.StandardOutput.ReadToEnd();
    string stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {fileName} {string.Join(" ", arguments.Select(QuoteIfNeeded))}{Environment.NewLine}{stderr}");
    }

    return stdout;
}

static string QuoteIfNeeded(string value) => value.Contains(' ') ? $"\"{value}\"" : value;

internal static class ProcessStartInfoExtensions
{
    public static ProcessStartInfo WithArguments(this ProcessStartInfo startInfo, IReadOnlyList<string> arguments)
    {
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}

internal sealed record IosToolchain(
    string Platform,
    string SdkPath,
    string TargetTriple,
    string Clang,
    string Clangxx,
    string Ar,
    string CompilerFlags,
    string LinkerFlags);

internal sealed record Options(string Configuration, string Platform, string Target, string SourceRoot, string Generator, bool SkipClone)
{
    public bool IsAndroid => Target.StartsWith("android-", StringComparison.OrdinalIgnoreCase);
    public bool IsAndroidVulkan => Target.StartsWith("android-vulkan-", StringComparison.OrdinalIgnoreCase);
    public bool IsBrowserWasm => Target.Equals("browser-wasm", StringComparison.OrdinalIgnoreCase);
    public bool IsIOS => Target.StartsWith("ios-", StringComparison.OrdinalIgnoreCase);
    public bool IsDesktopUnix => Target.StartsWith("linux-", StringComparison.OrdinalIgnoreCase)
        || Target.StartsWith("osx-", StringComparison.OrdinalIgnoreCase);

    public static Options Parse(string[] args)
    {
        string configuration = "Debug";
        string platform = "x64";
        string target = "win-x64";
        string sourceRoot = ".native-src";
        string generator = "vs2026";
        bool skipClone = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "-c":
                case "--configuration":
                    configuration = RequireValue(args, ref i, arg);
                    break;
                case "-p":
                case "--platform":
                    platform = RequireValue(args, ref i, arg);
                    break;
                case "-t":
                case "--target":
                    target = RequireValue(args, ref i, arg);
                    break;
                case "--source-root":
                    sourceRoot = RequireValue(args, ref i, arg);
                    break;
                case "--generator":
                    generator = RequireValue(args, ref i, arg);
                    break;
                case "--skip-clone":
                    skipClone = true;
                    break;
                case "-h":
                case "--help":
                    PrintUsageAndExit();
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        Validate(configuration, ["Debug", "Release"], "configuration");
        Validate(platform, ["x64"], "platform");
        Validate(target, ["win-x64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64", "android-arm", "android-arm64", "android-x86", "android-x64", "android-vulkan-arm", "android-vulkan-arm64", "android-vulkan-x86", "android-vulkan-x64", "browser-wasm", "ios-arm64", "ios-simulator"], "target");
        Validate(generator, ["vs2022", "vs2026"], "generator");

        return new Options(configuration, platform, target, sourceRoot, generator, skipClone);
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        index++;
        return args[index];
    }

    private static void Validate(string value, IReadOnlyCollection<string> allowed, string name)
    {
        if (!allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid {name} '{value}'. Expected one of: {string.Join(", ", allowed)}.");
        }
    }

    private static void PrintUsageAndExit()
    {
        Console.WriteLine("""
        Usage:
          dotnet run .\scripts\build-bgfx.cs -- [options]

        Options:
          -c, --configuration <Debug|Release>   Build configuration. Default: Debug
          -p, --platform <x64>                  Native platform. Default: x64
          -t, --target <target>                 Native target. Default: win-x64
                                                Values: win-x64, linux-x64, linux-arm64, osx-x64, osx-arm64,
                                                        android-arm, android-arm64, android-x86, android-x64,
                                                        android-vulkan-arm, android-vulkan-arm64, android-vulkan-x86, android-vulkan-x64,
                                                        browser-wasm, ios-arm64, ios-simulator
          --source-root <path>                  bx/bimg/bgfx clone root. Default: .native-src
          --generator <vs2022|vs2026>           GENie generator. Default: vs2026
          --skip-clone                          Do not clone missing bx/bimg/bgfx repositories.
        """);
        Environment.Exit(0);
    }
}

internal sealed record EmscriptenToolchain(string EmscriptenPath, string Emmake, IReadOnlyDictionary<string, string> Environment)
{
    public static EmscriptenToolchain Create(string emscriptenPath, string emmake)
    {
        string toolsPath = Path.GetFullPath(Path.Combine(emscriptenPath, ".."));
        string sdkVersionPath = Path.GetFullPath(Path.Combine(toolsPath, ".."));
        string sdkVersion = Path.GetFileName(sdkVersionPath);
        string sdkPackPath = Path.GetFullPath(Path.Combine(sdkVersionPath, ".."));
        string packsRoot = Path.GetFullPath(Path.Combine(sdkPackPath, ".."));
        string sdkPackName = Path.GetFileName(sdkPackPath);
        string emscriptenVersion = GetEmscriptenVersionFromPackName(sdkPackName);
        string nodePackName = sdkPackName.Replace(".Sdk.", ".Node.", StringComparison.OrdinalIgnoreCase);

        string llvmRoot = Path.Combine(toolsPath, "bin");
        string binaryenRoot = toolsPath;
        string cacheRoot = Path.Combine(Directory.GetCurrentDirectory(), "native", "bgfx", "obj", "emscripten-cache");
        string nodePath = Path.Combine(packsRoot, nodePackName, sdkVersion, "tools", "bin", OperatingSystem.IsWindows() ? "node.exe" : "node");
        string nodeDirectory = Path.GetDirectoryName(nodePath)!;

        if (!File.Exists(Path.Combine(llvmRoot, OperatingSystem.IsWindows() ? "clang.exe" : "clang")))
        {
            throw new InvalidOperationException($"Emscripten LLVM tools were not found at {llvmRoot}.");
        }

        if (!File.Exists(Path.Combine(binaryenRoot, "bin", OperatingSystem.IsWindows() ? "wasm-opt.exe" : "wasm-opt")))
        {
            throw new InvalidOperationException($"Emscripten Binaryen tools were not found at {binaryenRoot}.");
        }

        if (!File.Exists(nodePath))
        {
            throw new InvalidOperationException($"Emscripten Node.js runtime was not found at {nodePath}.");
        }

        Directory.CreateDirectory(cacheRoot);

        string? existingPath = System.Environment.GetEnvironmentVariable("PATH");
        string[] pathEntries = [emscriptenPath, llvmRoot, nodeDirectory];
        string[] versionParts = emscriptenVersion.Split('.');
        string emscriptenVersionDefines = string.Join(' ', [
            $"-D__EMSCRIPTEN_MAJOR__={versionParts[0]}",
            $"-D__EMSCRIPTEN_MINOR__={versionParts[1]}",
            $"-D__EMSCRIPTEN_TINY__={versionParts[2]}",
        ]);
        Dictionary<string, string> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["EMSCRIPTEN"] = emscriptenPath,
            ["EM_CACHE"] = cacheRoot,
            ["EM_FROZEN_CACHE"] = "0",
            ["DOTNET_EMSCRIPTEN_LLVM_ROOT"] = llvmRoot,
            ["DOTNET_EMSCRIPTEN_NODE_JS"] = nodePath,
            ["DOTNET_EMSCRIPTEN_BINARYEN_ROOT"] = binaryenRoot,
            ["CFLAGS"] = MergeFlags(emscriptenVersionDefines, System.Environment.GetEnvironmentVariable("CFLAGS")),
            ["CXXFLAGS"] = MergeFlags(emscriptenVersionDefines, System.Environment.GetEnvironmentVariable("CXXFLAGS")),
            ["CPPFLAGS"] = MergeFlags(emscriptenVersionDefines, System.Environment.GetEnvironmentVariable("CPPFLAGS")),
            ["PATH"] = string.IsNullOrWhiteSpace(existingPath)
                ? string.Join(Path.PathSeparator, pathEntries)
                : string.Join(Path.PathSeparator, pathEntries) + Path.PathSeparator + existingPath,
        };

        return new EmscriptenToolchain(emscriptenPath, emmake, environment);
    }

    private static string GetEmscriptenVersionFromPackName(string sdkPackName)
    {
        const string prefix = "Microsoft.NET.Runtime.Emscripten.";
        int start = sdkPackName.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        int end = sdkPackName.IndexOf(".Sdk.", StringComparison.OrdinalIgnoreCase);
        if (start < 0 || end < 0 || end <= start + prefix.Length)
        {
            throw new InvalidOperationException($"Could not determine Emscripten version from SDK pack name '{sdkPackName}'.");
        }

        string version = sdkPackName[(start + prefix.Length)..end];
        if (version.Split('.').Length != 3)
        {
            throw new InvalidOperationException($"Unexpected Emscripten version '{version}' in SDK pack name '{sdkPackName}'.");
        }

        return version;
    }

    private static string MergeFlags(string requiredFlags, string? existingFlags) =>
        string.IsNullOrWhiteSpace(existingFlags)
            ? requiredFlags
            : requiredFlags + " " + existingFlags;
}

internal static partial class WindowsNative
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint GetShortPathName(string longPath, StringBuilder shortPath, uint bufferLength);
}
