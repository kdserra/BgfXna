using System.Diagnostics;
using System.Runtime.CompilerServices;

Options options = Options.Parse(args);
string scriptPath = SourcePath();
string repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(scriptPath)!, ".."));
string sourceRoot = Path.GetFullPath(Path.Combine(repoRoot, options.SourceRoot));
string bxPath = Path.Combine(sourceRoot, "bx");
string bimgPath = Path.Combine(sourceRoot, "bimg");
string bgfxPath = Path.Combine(sourceRoot, "bgfx");
string outputRoot = Path.Combine(repoRoot, "native", "bgfx", "bin", options.Configuration, options.Target);

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

string genie = Path.Combine(bxPath, "tools", "bin", "windows", "genie.exe");
if (!File.Exists(genie))
{
    throw new InvalidOperationException($"GENie not found at {genie}. Make sure bx was cloned correctly.");
}

if (options.IsAndroid)
{
    string gcc = ToAndroidGcc(options.Target);
    string projectDirectory = Path.Combine(bgfxPath, ".build", "projects", $"gmake-{gcc}");
    Run(genie, ["--with-shared-lib", $"--gcc={gcc}", "gmake"], bgfxPath);
    string make = FindMake();
    Run(make, ["-R", "-C", projectDirectory, $"config={options.Configuration.ToLowerInvariant()}"], bgfxPath);

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

static string ToAndroidGcc(string target) =>
    target switch
    {
        "android-arm" => "android-arm",
        "android-arm64" => "android-arm64",
        "android-x86" => "android-x86",
        "android-x64" => "android-x86_64",
        _ => throw new ArgumentException($"Target '{target}' is not an Android target."),
    };

static string ToAndroidTriple(string target) =>
    target switch
    {
        "android-arm" => "arm-linux-androideabi",
        "android-arm64" => "aarch64-linux-android",
        "android-x86" => "i686-linux-android",
        "android-x64" => "x86_64-linux-android",
        _ => throw new ArgumentException($"Target '{target}' is not an Android target."),
    };

static string SourcePath([CallerFilePath] string path = "") => path;

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
    string? ndkRoot = Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT");
    if (string.IsNullOrWhiteSpace(ndkRoot))
    {
        return;
    }

    string libcxx = Path.Combine(
        ndkRoot,
        "toolchains",
        "llvm",
        "prebuilt",
        "windows-x86_64",
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

internal sealed record Options(string Configuration, string Platform, string Target, string SourceRoot, string Generator, bool SkipClone)
{
    public bool IsAndroid => Target.StartsWith("android-", StringComparison.OrdinalIgnoreCase);
    public bool IsBrowserWasm => Target.Equals("browser-wasm", StringComparison.OrdinalIgnoreCase);

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
        Validate(target, ["win-x64", "android-arm", "android-arm64", "android-x86", "android-x64", "browser-wasm"], "target");
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
                                                Values: win-x64, android-arm, android-arm64, android-x86, android-x64, browser-wasm
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
