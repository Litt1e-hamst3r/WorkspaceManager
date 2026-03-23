using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using WorkspaceManager.Infrastructure.Configuration;

namespace WorkspaceManager.Infrastructure.Wallpapers;

public sealed class WallpaperImageStore
{
    private const int MaxCachedFiles = 12;
    private const int FavoriteHashLength = 16;
    private readonly string _wallpaperDirectory;
    private string _favoriteWallpaperDirectory;

    public WallpaperImageStore(string? favoriteWallpaperDirectory = null)
    {
        var workspaceDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkspaceManager");
        _wallpaperDirectory = Path.Combine(workspaceDirectory, "wallpapers");
        _favoriteWallpaperDirectory = AppSettings.NormalizeFavoriteWallpaperSaveDirectory(favoriteWallpaperDirectory);

        Directory.CreateDirectory(_wallpaperDirectory);
    }

    public string FavoriteWallpaperDirectory => _favoriteWallpaperDirectory;

    public void SetFavoriteWallpaperDirectory(string? directoryPath)
    {
        _favoriteWallpaperDirectory = AppSettings.NormalizeFavoriteWallpaperSaveDirectory(directoryPath);
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

    public (string SavedPath, bool AlreadyExists) SaveFavoriteCopy(string sourcePath, string? preferredName = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            throw new InvalidOperationException("当前壁纸文件不存在，无法收藏到本地。");
        }

        Directory.CreateDirectory(_favoriteWallpaperDirectory);
        var extension = NormalizeExtension(Path.GetExtension(sourcePath));
        var hash = ComputeFileHash(sourcePath);
        var existingPath = Directory
            .EnumerateFiles(_favoriteWallpaperDirectory, $"*--{hash}.*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(existingPath))
        {
            return (existingPath, true);
        }

        var safeName = BuildSafeFavoriteName(preferredName, sourcePath);
        var favoritePath = Path.Combine(_favoriteWallpaperDirectory, $"{safeName}--{hash}{extension}");
        File.Copy(sourcePath, favoritePath, overwrite: false);
        return (favoritePath, false);
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

    private static string BuildSafeFavoriteName(string? preferredName, string sourcePath)
    {
        var rawName = string.IsNullOrWhiteSpace(preferredName)
            ? Path.GetFileNameWithoutExtension(sourcePath)
            : preferredName.Trim();
        if (string.IsNullOrWhiteSpace(rawName))
        {
            rawName = "favorite-wallpaper";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(rawName.Length);
        foreach (var character in rawName)
        {
            builder.Append(invalidChars.Contains(character) ? '-' : character);
        }

        var sanitized = builder
            .ToString()
            .Trim()
            .Trim('.', '-');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "favorite-wallpaper";
        }

        return sanitized.Length > 48
            ? sanitized[..48].TrimEnd(' ', '.', '-')
            : sanitized;
    }

    private static string ComputeFileHash(string sourcePath)
    {
        using var fileStream = File.OpenRead(sourcePath);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(fileStream);
        return Convert.ToHexString(hash)[..FavoriteHashLength].ToLowerInvariant();
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
