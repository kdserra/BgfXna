using System;
using System.IO;
using System.Linq;

var currentDir = Directory.GetCurrentDirectory();

var parentDir = Directory.GetParent(currentDir)?.FullName
    ?? throw new InvalidOperationException("No parent directory found.");

var targetNames = new[] { "bin", "obj" };

var directories = Directory
    .EnumerateDirectories(parentDir, "*", SearchOption.AllDirectories)
    .Where(path => targetNames.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
    .ToList();

foreach (var dir in directories)
{
    try
    {
        Console.WriteLine($"Removing: {dir}");
        Directory.Delete(dir, recursive: true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to remove {dir}: {ex.Message}");
    }
}