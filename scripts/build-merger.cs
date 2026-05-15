var root = Directory.GetCurrentDirectory();
var configs = new[] { "Debug", "Release" };

foreach (var dir in Directory.GetDirectories(root, "bgfx-*", SearchOption.TopDirectoryOnly))
{
    var dirName = Path.GetFileName(dir);

    var config = configs.FirstOrDefault(c =>
        dirName.EndsWith("-" + c, StringComparison.OrdinalIgnoreCase));

    if (config is null)
        continue;

    var sourceConfigDir = Path.Combine(dir, config);
    if (!Directory.Exists(sourceConfigDir))
        continue;

    foreach (var platformDir in Directory.GetDirectories(sourceConfigDir))
    {
        var platform = Path.GetFileName(platformDir);
        var targetDir = Path.Combine(root, config, platform);

        Console.WriteLine($"{platformDir} -> {targetDir}");

        Directory.CreateDirectory(targetDir);
        MoveContents(platformDir, targetDir);
    }

    DeleteIfEmpty(sourceConfigDir);
    DeleteIfEmpty(dir);
}

Console.WriteLine("Done.");

static void MoveContents(string sourceDir, string targetDir)
{
    foreach (var file in Directory.GetFiles(sourceDir))
    {
        var targetFile = Path.Combine(targetDir, Path.GetFileName(file));

        if (File.Exists(targetFile))
            File.Delete(targetFile);

        File.Move(file, targetFile);
    }

    foreach (var dir in Directory.GetDirectories(sourceDir))
    {
        var targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));

        if (Directory.Exists(targetSubDir))
            Directory.Delete(targetSubDir, recursive: true);

        Directory.Move(dir, targetSubDir);
    }
}

static void DeleteIfEmpty(string dir)
{
    if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
        Directory.Delete(dir);
}