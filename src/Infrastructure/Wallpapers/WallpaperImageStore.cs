using System.IO;
using System.Windows.Media.Imaging;

namespace WorkspaceManager.Infrastructure.Wallpapers;

public sealed class WallpaperImageStore
{
    private const int MaxCachedFiles = 12;
    private readonly string _wallpaperDirectory;

    public WallpaperImageStore()
    {
        _wallpaperDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkspaceManager",
            "wallpapers");

        Directory.CreateDirectory(_wallpaperDirectory);
    }

    public string Save(byte[] imageBytes, string extension)
    {
        var safeExtension = NormalizeExtension(extension);
        var baseName = $"wallpaper-{DateTime.Now:yyyyMMdd-HHmmssfff}";
        var rawPath = Path.Combine(_wallpaperDirectory, baseName + safeExtension);
        File.WriteAllBytes(rawPath, imageBytes);

        var jpegPath = Path.Combine(_wallpaperDirectory, baseName + ".jpg");
        if (TryConvertToJpeg(rawPath, jpegPath))
        {
            TryDelete(rawPath);
            CleanupOldFiles();
            return jpegPath;
        }

        CleanupOldFiles();
        return rawPath;
    }

    private void CleanupOldFiles()
    {
        var files = new DirectoryInfo(_wallpaperDirectory)
            .GetFiles()
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(MaxCachedFiles)
            .ToList();

        foreach (var file in files)
        {
            TryDelete(file.FullName);
        }
    }

    private static bool TryConvertToJpeg(string inputPath, string outputPath)
    {
        try
        {
            using var inputStream = File.OpenRead(inputPath);
            var decoder = BitmapDecoder.Create(inputStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0)
            {
                return false;
            }

            var encoder = new JpegBitmapEncoder
            {
                QualityLevel = 92
            };

            encoder.Frames.Add(decoder.Frames[0]);
            using var outputStream = File.Create(outputPath);
            encoder.Save(outputStream);
            return true;
        }
        catch
        {
            TryDelete(outputPath);
            return false;
        }
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".jpg";
        }

        return extension.StartsWith('.')
            ? extension.ToLowerInvariant()
            : "." + extension.ToLowerInvariant();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
