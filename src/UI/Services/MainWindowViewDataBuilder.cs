using WorkspaceManager.Domain.Layouts;
using WorkspaceManager.Domain.Modes;
using WorkspaceManager.Infrastructure.Configuration;
using WorkspaceManager.UI.ViewModels;

namespace WorkspaceManager.UI.Services;

public sealed class MainWindowViewDataBuilder
{
    private readonly LayoutPreviewImageLoader _layoutPreviewImageLoader;

    public MainWindowViewDataBuilder(LayoutPreviewImageLoader layoutPreviewImageLoader)
    {
        _layoutPreviewImageLoader = layoutPreviewImageLoader;
    }

    public void ApplySettings(MainWindowViewModel viewModel, AppSettings settings, string? defaultModeName)
    {
        viewModel.SetLaunchAtStartup(settings.LaunchAtStartup);
        viewModel.SetStartMinimizedToTray(settings.StartMinimizedToTray);
        viewModel.SetMinimizeToTrayOnMinimize(settings.MinimizeToTrayOnMinimize);
        viewModel.SetCloseToTrayOnClose(settings.CloseToTrayOnClose);
        viewModel.SetWallpaperChangeOnStartup(settings.WallpaperChangeOnStartup);
        viewModel.SetWallpaperAutoRotateEnabled(settings.WallpaperAutoRotateEnabled);
        viewModel.SetWallpaperRotationIntervalMinutesInput(settings.WallpaperRotationIntervalMinutes.ToString());
        viewModel.SetWallpaperScheduleText(BuildWallpaperScheduleText(settings));
        viewModel.SetWallpaperSources(BuildWallpaperSources(settings.WallpaperSources));
        viewModel.SetDefaultModeId(string.IsNullOrWhiteSpace(settings.DefaultModeId)
            ? AppSettings.DefaultModeIdValue
            : settings.DefaultModeId);
        viewModel.SetDesktopToggleHotkeyInput(string.IsNullOrWhiteSpace(settings.DesktopToggleHotkey)
            ? AppSettings.DefaultDesktopToggleHotkey
            : settings.DesktopToggleHotkey);
        viewModel.SetShowMainWindowHotkeyInput(string.IsNullOrWhiteSpace(settings.ShowMainWindowHotkey)
            ? AppSettings.DefaultShowMainWindowHotkey
            : settings.ShowMainWindowHotkey);
        viewModel.SetDefaultModeName(defaultModeName);
    }

    public IReadOnlyList<WallpaperSourceViewModel> BuildWallpaperSources(IEnumerable<WallpaperSourceSetting> sources)
    {
        return sources
            .Select(source => new WallpaperSourceViewModel
            {
                Id = source.Id,
                Name = source.Name,
                RequestUrl = source.RequestUrl,
                Enabled = source.Enabled
            })
            .ToList();
    }

    public IReadOnlyList<LayoutSummaryViewModel> BuildLayouts(
        IEnumerable<DesktopLayoutSnapshot> layouts,
        Func<DesktopLayoutSnapshot, string?> getPreviewPath)
    {
        return layouts
            .Select(layout =>
            {
                var previewImagePath = getPreviewPath(layout) ?? string.Empty;
                return new LayoutSummaryViewModel
                {
                    Id = layout.Id,
                    Name = layout.Name,
                    PreviewImagePath = previewImagePath,
                    ThumbnailImage = _layoutPreviewImageLoader.Load(previewImagePath, 320),
                    PreviewImage = _layoutPreviewImageLoader.Load(previewImagePath, 960),
                    ItemCount = layout.Items.Count,
                    DisplayText = BuildLayoutDisplayText(layout)
                };
            })
            .ToList();
    }

    public IReadOnlyList<DesktopModeViewModel> BuildModes(
        IEnumerable<DesktopMode> modes,
        IEnumerable<DesktopLayoutSnapshot> layouts,
        string? defaultModeId,
        string? currentModeId)
    {
        var layoutsById = layouts.ToDictionary(layout => layout.Id, layout => layout.Name, StringComparer.OrdinalIgnoreCase);

        return modes
            .Select(mode => new DesktopModeViewModel
            {
                Id = mode.Id,
                Name = mode.Name,
                Description = mode.Description,
                DesktopIconsVisible = mode.DesktopIconsVisible,
                TaskbarVisible = mode.TaskbarVisible,
                LayoutId = mode.LayoutId,
                LayoutName = ResolveLayoutName(mode.LayoutId, layoutsById),
                StateSummary = BuildModeStateSummary(mode, layoutsById),
                IsDefault = string.Equals(defaultModeId, mode.Id, StringComparison.OrdinalIgnoreCase),
                IsActive = string.Equals(currentModeId, mode.Id, StringComparison.OrdinalIgnoreCase),
                IsBuiltIn = mode.IsBuiltIn
            })
            .ToList();
    }

    public IReadOnlyList<ModeLayoutOptionViewModel> BuildModeLayoutOptions(IEnumerable<DesktopLayoutSnapshot> layouts)
    {
        var options = new List<ModeLayoutOptionViewModel>
        {
            new()
            {
                Id = string.Empty,
                Name = "不恢复布局"
            }
        };

        options.AddRange(layouts.Select(layout => new ModeLayoutOptionViewModel
        {
            Id = layout.Id,
            Name = layout.Name
        }));

        return options;
    }

    public IReadOnlyList<ModeOptionViewModel> BuildModeOptions(IEnumerable<DesktopMode> modes)
    {
        return modes
            .Select(mode => new ModeOptionViewModel
            {
                Id = mode.Id,
                Name = mode.Name
            })
            .ToList();
    }

    public string? FindModeName(IEnumerable<DesktopMode> modes, string? modeId)
    {
        return modes
            .FirstOrDefault(mode => string.Equals(mode.Id, modeId, StringComparison.OrdinalIgnoreCase))
            ?.Name;
    }

    public System.Windows.Media.ImageSource? LoadPreviewImage(string previewImagePath)
    {
        return _layoutPreviewImageLoader.Load(previewImagePath);
    }

    private static string BuildWallpaperScheduleText(AppSettings settings)
    {
        if (!settings.WallpaperAutoRotateEnabled)
        {
            return "定时轮换：未开启";
        }

        if (settings.WallpaperSources.All(source => !source.Enabled))
        {
            return "定时轮换：未启动，请先启用至少一个图源";
        }

        return $"定时轮换：每 {settings.WallpaperRotationIntervalMinutes} 分钟自动切换";
    }

    private static string BuildLayoutDisplayText(DesktopLayoutSnapshot layout)
    {
        var parts = new List<string>
        {
            layout.Name,
            $"{layout.Items.Count} 个图标",
            layout.CreatedAt.ToString("yyyy-MM-dd HH:mm")
        };

        return string.Join(" · ", parts);
    }

    private static string ResolveLayoutName(string? layoutId, IReadOnlyDictionary<string, string> layoutsById)
    {
        if (string.IsNullOrWhiteSpace(layoutId))
        {
            return "未绑定布局";
        }

        return layoutsById.TryGetValue(layoutId, out var name)
            ? name
            : "布局已丢失";
    }

    private static string BuildModeStateSummary(DesktopMode mode, IReadOnlyDictionary<string, string> layoutsById)
    {
        var parts = new List<string>
        {
            mode.DesktopIconsVisible ? "图标显示" : "图标隐藏",
            mode.TaskbarVisible ? "任务栏显示" : "任务栏隐藏",
            $"布局 {ResolveLayoutName(mode.LayoutId, layoutsById)}"
        };

        return string.Join(" · ", parts);
    }
}
