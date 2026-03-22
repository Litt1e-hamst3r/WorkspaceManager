using System.IO;
using System.Net.Http;
using System.Text.Json;
using WorkspaceManager.Infrastructure.Configuration;
using WorkspaceManager.Infrastructure.Wallpapers;
using WorkspaceManager.Interop.Desktop;

namespace WorkspaceManager.Application.Wallpapers;

public sealed class WallpaperRotationService
{
    private readonly HttpClient _httpClient;
    private readonly WallpaperImageStore _wallpaperImageStore;
    private readonly DesktopWallpaperService _desktopWallpaperService;
    private readonly object _prefetchSync = new();
    private PreparedWallpaper? _prefetchedWallpaper;
    private Task<PreparedWallpaper>? _prefetchTask;
    private string _prefetchSourceKey = string.Empty;

    public WallpaperRotationService(
        HttpClient httpClient,
        WallpaperImageStore wallpaperImageStore,
        DesktopWallpaperService desktopWallpaperService)
    {
        _httpClient = httpClient;
        _wallpaperImageStore = wallpaperImageStore;
        _desktopWallpaperService = desktopWallpaperService;
    }

    public void WarmUp(IEnumerable<WallpaperSourceSetting> sources)
    {
        var candidates = NormalizeSources(sources);
        if (candidates.Count == 0)
        {
            return;
        }

        var sourceKey = BuildSourceKey(candidates);
        lock (_prefetchSync)
        {
            ResetPrefetchStateIfNeededLocked(sourceKey);
            if (_prefetchedWallpaper is not null || _prefetchTask is not null)
            {
                return;
            }

            _prefetchTask = DownloadPreparedWallpaperAsync(candidates, sourceKey, CancellationToken.None);
        }
    }

    public async Task<WallpaperChangeResult> ApplyRandomAsync(
        IEnumerable<WallpaperSourceSetting> sources,
        CancellationToken cancellationToken = default)
    {
        var candidates = NormalizeSources(sources);
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("请先至少启用一个壁纸源。");
        }

        var sourceKey = BuildSourceKey(candidates);
        PreparedWallpaper? preparedWallpaper;
        Task<PreparedWallpaper>? pendingPrefetchTask;

        lock (_prefetchSync)
        {
            ResetPrefetchStateIfNeededLocked(sourceKey);
            preparedWallpaper = ConsumePrefetchedWallpaperLocked();
            pendingPrefetchTask = _prefetchTask;

            if (preparedWallpaper is null && pendingPrefetchTask is null)
            {
                pendingPrefetchTask = DownloadPreparedWallpaperAsync(candidates, sourceKey, cancellationToken);
                _prefetchTask = pendingPrefetchTask;
            }
        }

        if (preparedWallpaper is null)
        {
            preparedWallpaper = await pendingPrefetchTask!;
            lock (_prefetchSync)
            {
                ConsumePrefetchedWallpaperIfMatchesLocked(preparedWallpaper);
                _prefetchTask = null;
            }
        }

        _desktopWallpaperService.SetWallpaper(preparedWallpaper.SavedPath);
        WarmUp(candidates);

        return new WallpaperChangeResult
        {
            SourceId = preparedWallpaper.SourceId,
            SourceName = preparedWallpaper.SourceName,
            SavedPath = preparedWallpaper.SavedPath
        };
    }

    private async Task<PreparedWallpaper> DownloadPreparedWallpaperAsync(
        IReadOnlyList<WallpaperSourceSetting> sources,
        string sourceKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var preparedWallpaper = await DownloadPreparedWallpaperCoreAsync(sources, cancellationToken);
            lock (_prefetchSync)
            {
                if (string.Equals(_prefetchSourceKey, sourceKey, StringComparison.Ordinal))
                {
                    _prefetchedWallpaper = preparedWallpaper;
                    _prefetchTask = null;
                }
            }

            return preparedWallpaper;
        }
        catch
        {
            lock (_prefetchSync)
            {
                if (string.Equals(_prefetchSourceKey, sourceKey, StringComparison.Ordinal))
                {
                    _prefetchTask = null;
                }
            }

            throw;
        }
    }

    private async Task<PreparedWallpaper> DownloadPreparedWallpaperCoreAsync(
        IReadOnlyList<WallpaperSourceSetting> sources,
        CancellationToken cancellationToken)
    {
        var failures = new List<string>();
        foreach (var source in sources)
        {
            try
            {
                return await PrepareWallpaperFromSourceAsync(source, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failures.Add($"{source.Name}：{ex.Message}");
            }
        }

        throw new InvalidOperationException(
            failures.Count == 0
                ? "所有壁纸源都不可用。"
                : $"所有壁纸源都不可用。{failures[0]}");
    }

    private async Task<PreparedWallpaper> PrepareWallpaperFromSourceAsync(
        WallpaperSourceSetting source,
        CancellationToken cancellationToken)
    {
        if (source.Kind == WallpaperSourceKind.LocalFile)
        {
            if (!WallpaperSourceSetting.TryNormalizeLocalFilePath(source.RequestUrl, out var localPath))
            {
                throw new InvalidOperationException("本地图片路径无效。");
            }

            if (!File.Exists(localPath))
            {
                throw new FileNotFoundException("本地图片不存在。", localPath);
            }

            return await PrepareLocalImageFileAsync(source.Id, source.Name, localPath, cancellationToken);
        }

        if (source.Kind == WallpaperSourceKind.LocalFolder)
        {
            if (!WallpaperSourceSetting.TryNormalizeLocalDirectoryPath(source.RequestUrl, out var localDirectory))
            {
                throw new InvalidOperationException("本地图片文件夹路径无效。");
            }

            if (!Directory.Exists(localDirectory))
            {
                throw new DirectoryNotFoundException($"本地图片文件夹不存在：{localDirectory}");
            }

            var candidateFiles = WallpaperSourceSetting.GetSupportedLocalImageFiles(localDirectory);
            if (candidateFiles.Count == 0)
            {
                throw new InvalidOperationException("本地图片文件夹中没有可用图片。");
            }

            var selectedFile = candidateFiles[Random.Shared.Next(candidateFiles.Count)];
            return await PrepareLocalImageFileAsync(source.Id, source.Name, selectedFile, cancellationToken);
        }

        var downloadedImage = await DownloadResolvedImageAsync(source.RequestUrl, cancellationToken);
        var savedPath = _wallpaperImageStore.Save(downloadedImage.Content, downloadedImage.Extension);
        return new PreparedWallpaper(source.Id, source.Name, savedPath);
    }

    private async Task<PreparedWallpaper> PrepareLocalImageFileAsync(
        string sourceId,
        string sourceName,
        string localPath,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllBytesAsync(localPath, cancellationToken);
        var savedPath = _wallpaperImageStore.Save(content, Path.GetExtension(localPath));
        return new PreparedWallpaper(sourceId, sourceName, savedPath);
    }

    private async Task<DownloadedImage> DownloadResolvedImageAsync(string requestUrl, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (IsImageMediaType(mediaType))
        {
            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var extension = ResolveExtension(mediaType, response.RequestMessage?.RequestUri?.AbsolutePath);
            return new DownloadedImage(content, extension);
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (LooksLikeJson(mediaType, payload))
        {
            var imageUrl = TryExtractImageUrlFromJson(payload);
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new InvalidOperationException("接口返回 JSON，但没有可用图片地址。");
            }

            return await DownloadDirectImageAsync(imageUrl, cancellationToken);
        }

        if (Uri.TryCreate(payload.Trim(), UriKind.Absolute, out var directUri))
        {
            return await DownloadDirectImageAsync(directUri.ToString(), cancellationToken);
        }

        throw new InvalidOperationException("接口未返回可识别的图片内容。");
    }

    private async Task<DownloadedImage> DownloadDirectImageAsync(string imageUrl, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!IsImageMediaType(mediaType))
        {
            throw new InvalidOperationException("解析出的图片地址未返回图片内容。");
        }

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var extension = ResolveExtension(mediaType, response.RequestMessage?.RequestUri?.AbsolutePath);
        return new DownloadedImage(content, extension);
    }

    private static List<WallpaperSourceSetting> NormalizeSources(IEnumerable<WallpaperSourceSetting> sources)
    {
        return sources
            .Where(source => source.Enabled)
            .Select(source =>
            {
                if (!WallpaperSourceSetting.TryNormalizeLocation(source.RequestUrl, source.Kind, out var normalizedLocation))
                {
                    return null;
                }

                switch (source.Kind)
                {
                    case WallpaperSourceKind.LocalFile when !File.Exists(normalizedLocation):
                        return null;
                    case WallpaperSourceKind.LocalFolder:
                        if (!Directory.Exists(normalizedLocation)
                            || WallpaperSourceSetting.GetSupportedLocalImageFiles(normalizedLocation).Count == 0)
                        {
                            return null;
                        }

                        break;
                }

                return new WallpaperSourceSetting
                {
                    Id = source.Id,
                    Name = source.Name,
                    RequestUrl = normalizedLocation,
                    Kind = source.Kind,
                    Enabled = source.Enabled
                };
            })
            .Where(source => source is not null)
            .Select(source => source!)
            .ToList();
    }

    private static string BuildSourceKey(IEnumerable<WallpaperSourceSetting> sources)
    {
        return string.Join(
            '|',
            sources.Select(source => $"{source.Id}:{source.Kind}:{source.RequestUrl}:{source.Enabled}"));
    }

    private void ResetPrefetchStateIfNeededLocked(string sourceKey)
    {
        if (string.Equals(_prefetchSourceKey, sourceKey, StringComparison.Ordinal))
        {
            return;
        }

        _prefetchSourceKey = sourceKey;
        _prefetchedWallpaper = null;
        _prefetchTask = null;
    }

    private PreparedWallpaper? ConsumePrefetchedWallpaperLocked()
    {
        var preparedWallpaper = _prefetchedWallpaper;
        _prefetchedWallpaper = null;
        return preparedWallpaper;
    }

    private void ConsumePrefetchedWallpaperIfMatchesLocked(PreparedWallpaper preparedWallpaper)
    {
        if (_prefetchedWallpaper is not null
            && string.Equals(_prefetchedWallpaper.SavedPath, preparedWallpaper.SavedPath, StringComparison.OrdinalIgnoreCase))
        {
            _prefetchedWallpaper = null;
        }
    }

    private static bool IsImageMediaType(string? mediaType)
    {
        return !string.IsNullOrWhiteSpace(mediaType)
            && mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeJson(string? mediaType, string payload)
    {
        return (!string.IsNullOrWhiteSpace(mediaType)
                && mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
            || payload.TrimStart().StartsWith('{')
            || payload.TrimStart().StartsWith('[');
    }

    private static string? TryExtractImageUrlFromJson(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return FindImageUrl(document.RootElement);
    }

    private static string? FindImageUrl(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var name in new[] { "original", "regular", "large", "url", "imgurl", "image", "pic" })
                {
                    if (element.TryGetProperty(name, out var prioritized))
                    {
                        var prioritizedUrl = FindImageUrl(prioritized);
                        if (!string.IsNullOrWhiteSpace(prioritizedUrl))
                        {
                            return prioritizedUrl;
                        }
                    }
                }

                foreach (var property in element.EnumerateObject())
                {
                    var nestedUrl = FindImageUrl(property.Value);
                    if (!string.IsNullOrWhiteSpace(nestedUrl))
                    {
                        return nestedUrl;
                    }
                }

                return null;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nestedUrl = FindImageUrl(item);
                    if (!string.IsNullOrWhiteSpace(nestedUrl))
                    {
                        return nestedUrl;
                    }
                }

                return null;

            case JsonValueKind.String:
                var value = element.GetString();
                if (Uri.TryCreate(value, UriKind.Absolute, out _))
                {
                    return value;
                }

                return null;

            default:
                return null;
        }
    }

    private static string ResolveExtension(string? mediaType, string? absolutePath)
    {
        if (!string.IsNullOrWhiteSpace(mediaType))
        {
            return mediaType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/bmp" => ".bmp",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ResolveExtensionFromPath(absolutePath)
            };
        }

        return ResolveExtensionFromPath(absolutePath);
    }

    private static string ResolveExtensionFromPath(string? absolutePath)
    {
        var extension = Path.GetExtension(absolutePath ?? string.Empty);
        return string.IsNullOrWhiteSpace(extension)
            ? ".jpg"
            : extension;
    }

    private sealed record DownloadedImage(byte[] Content, string Extension);

    private sealed record PreparedWallpaper(string SourceId, string SourceName, string SavedPath);
}
