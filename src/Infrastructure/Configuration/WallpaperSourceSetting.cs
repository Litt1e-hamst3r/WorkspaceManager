using System.IO;

namespace WorkspaceManager.Infrastructure.Configuration;

public sealed class WallpaperSourceSetting
{
    private static readonly HashSet<string> SupportedLocalImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".gif",
        ".webp"
    };

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string RequestUrl { get; set; } = string.Empty;

    public WallpaperSourceKind Kind { get; set; } = WallpaperSourceKind.RemoteUrl;

    public bool Enabled { get; set; } = true;

    public static bool TryNormalizeLocation(string? value, WallpaperSourceKind kind, out string normalizedValue)
    {
        return kind switch
        {
            WallpaperSourceKind.LocalFile => TryNormalizeLocalFilePath(value, out normalizedValue),
            WallpaperSourceKind.LocalFolder => TryNormalizeLocalDirectoryPath(value, out normalizedValue),
            _ => TryNormalizeRemoteUrl(value, out normalizedValue)
        };
    }

    public static bool TryNormalizeRemoteUrl(string? value, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var requestUri)
            || (requestUri.Scheme != Uri.UriSchemeHttp && requestUri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        normalizedUrl = requestUri.AbsoluteUri.TrimEnd('/');
        return true;
    }

    public static bool TryNormalizeLocalFilePath(string? value, out string normalizedPath)
    {
        if (!TryNormalizeLocalPath(value, out normalizedPath))
        {
            return false;
        }

        return IsSupportedLocalImagePath(normalizedPath);
    }

    public static bool TryNormalizeLocalDirectoryPath(string? value, out string normalizedPath)
    {
        return TryNormalizeLocalPath(value, out normalizedPath);
    }

    public static IReadOnlyList<string> GetSupportedLocalImageFiles(string? directoryPath)
    {
        if (!TryNormalizeLocalDirectoryPath(directoryPath, out var normalizedDirectory)
            || !Directory.Exists(normalizedDirectory))
        {
            return [];
        }

        try
        {
            return Directory
                .EnumerateFiles(normalizedDirectory, "*", SearchOption.TopDirectoryOnly)
                .Where(IsSupportedLocalImagePath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static bool IsSupportedLocalImagePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return !string.IsNullOrWhiteSpace(extension)
            && SupportedLocalImageExtensions.Contains(extension);
    }

    private static bool TryNormalizeLocalPath(string? value, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim().Trim('"');
        if (Uri.TryCreate(candidate, UriKind.Absolute, out var fileUri) && fileUri.IsFile)
        {
            candidate = fileUri.LocalPath;
        }

        if (!Path.IsPathFullyQualified(candidate))
        {
            return false;
        }

        try
        {
            normalizedPath = Path.GetFullPath(candidate);
        }
        catch
        {
            return false;
        }

        return true;
    }
}
