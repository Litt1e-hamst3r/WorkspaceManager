using System.IO;
using System.Text.Json;

namespace WorkspaceManager.Infrastructure.Configuration;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public AppSettingsStore()
    {
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkspaceManager");

        Directory.CreateDirectory(settingsDirectory);
        _settingsPath = Path.Combine(settingsDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return Normalize(new AppSettings());
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return Normalize(JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings());
        }
        catch
        {
            return Normalize(new AppSettings());
        }
    }

    public void Save(AppSettings settings)
    {
        var normalized = Normalize(settings);
        var json = JsonSerializer.Serialize(normalized, SerializerOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        settings.DesktopToggleHotkey = string.IsNullOrWhiteSpace(settings.DesktopToggleHotkey)
            ? AppSettings.DefaultDesktopToggleHotkey
            : settings.DesktopToggleHotkey;
        settings.ShowMainWindowHotkey = string.IsNullOrWhiteSpace(settings.ShowMainWindowHotkey)
            ? AppSettings.DefaultShowMainWindowHotkey
            : settings.ShowMainWindowHotkey;
        settings.DefaultModeId = string.IsNullOrWhiteSpace(settings.DefaultModeId)
            ? AppSettings.DefaultModeIdValue
            : settings.DefaultModeId;
        settings.WallpaperRotationIntervalMinutes = settings.WallpaperRotationIntervalMinutes is < 1 or > 1440
            ? AppSettings.DefaultWallpaperRotationIntervalMinutes
            : settings.WallpaperRotationIntervalMinutes;
        settings.FavoriteWallpaperSaveDirectory = AppSettings.NormalizeFavoriteWallpaperSaveDirectory(settings.FavoriteWallpaperSaveDirectory);
        settings.WallpaperSources = MergeWallpaperSources(settings.WallpaperSources);
        return settings;
    }

    private static List<WallpaperSourceSetting> MergeWallpaperSources(List<WallpaperSourceSetting>? currentSources)
    {
        var defaults = AppSettings.CreateDefaultWallpaperSources();
        if (currentSources is null || currentSources.Count == 0)
        {
            return defaults;
        }

        var builtInIds = defaults
            .Select(source => source.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var currentById = currentSources
            .Where(source => !string.IsNullOrWhiteSpace(source.Id))
            .GroupBy(source => source.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        var mergedSources = defaults
            .Select(defaultSource =>
            {
                if (!currentById.TryGetValue(defaultSource.Id, out var savedSource))
                {
                    return defaultSource;
                }

                return new WallpaperSourceSetting
                {
                    Id = defaultSource.Id,
                    Name = defaultSource.Name,
                    RequestUrl = defaultSource.RequestUrl,
                    Kind = WallpaperSourceKind.RemoteUrl,
                    Enabled = savedSource.Enabled
                };
            })
            .ToList();

        var customSources = currentSources
            .Where(source => !string.IsNullOrWhiteSpace(source.Id))
            .Where(source => !builtInIds.Contains(source.Id))
            .Where(source => !string.IsNullOrWhiteSpace(source.Name))
            .GroupBy(source => source.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var source = group.Last();
                if (!WallpaperSourceSetting.TryNormalizeLocation(source.RequestUrl, source.Kind, out var normalizedLocation))
                {
                    return null;
                }

                return new WallpaperSourceSetting
                {
                    Id = source.Id,
                    Name = source.Name.Trim(),
                    RequestUrl = normalizedLocation,
                    Kind = source.Kind,
                    Enabled = source.Enabled
                };
            })
            .Where(source => source is not null)
            .Select(source => source!);

        mergedSources.AddRange(customSources);
        return mergedSources;
    }
}
