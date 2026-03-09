namespace WorkspaceManager.App;

public sealed class ModeService
{
    private readonly ModeStore _modeStore;
    private readonly DesktopIconService _desktopIconService;
    private readonly TaskbarService _taskbarService;
    private readonly DesktopLayoutService _desktopLayoutService;

    public ModeService(
        ModeStore modeStore,
        DesktopIconService desktopIconService,
        TaskbarService taskbarService,
        DesktopLayoutService desktopLayoutService)
    {
        _modeStore = modeStore;
        _desktopIconService = desktopIconService;
        _taskbarService = taskbarService;
        _desktopLayoutService = desktopLayoutService;
    }

    public IReadOnlyList<DesktopMode> GetModes()
    {
        return _modeStore.LoadAll();
    }

    public async Task<DesktopMode> SwitchAsync(string modeId, CancellationToken cancellationToken = default)
    {
        var mode = GetModes().FirstOrDefault(item => string.Equals(item.Id, modeId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("未找到指定模式。");

        await _desktopIconService.SetVisibleAsync(mode.DesktopIconsVisible, cancellationToken);
        await _taskbarService.SetVisibleAsync(mode.TaskbarVisible, cancellationToken);

        if (!string.IsNullOrWhiteSpace(mode.LayoutId))
        {
            _desktopLayoutService.Restore(mode.LayoutId);
        }

        return mode;
    }

    public DesktopMode UpdateLayoutBinding(string modeId, string? layoutId)
    {
        var modes = _modeStore.LoadAll().ToList();
        var mode = modes.FirstOrDefault(item => string.Equals(item.Id, modeId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("未找到指定模式。");

        mode.LayoutId = layoutId?.Trim() ?? string.Empty;
        mode.UpdatedAt = DateTimeOffset.Now;
        _modeStore.SaveAll(modes);
        return mode;
    }

    public DesktopMode CreateCustomMode(
        string name,
        string? description,
        bool desktopIconsVisible,
        bool taskbarVisible,
        string? layoutId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("自定义模式名称不能为空。");
        }

        var modes = _modeStore.LoadAll().ToList();
        if (modes.Any(mode => string.Equals(mode.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("已存在同名模式，请换一个名称。");
        }

        var mode = new DesktopMode
        {
            Id = $"custom-{Guid.NewGuid():N}",
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            DesktopIconsVisible = desktopIconsVisible,
            TaskbarVisible = taskbarVisible,
            LayoutId = layoutId?.Trim() ?? string.Empty,
            UpdatedAt = DateTimeOffset.Now
        };

        modes.Add(mode);
        _modeStore.SaveAll(modes);
        return mode;
    }

    public DesktopMode UpdateCustomMode(
        string id,
        string name,
        string? description,
        bool desktopIconsVisible,
        bool taskbarVisible,
        string? layoutId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("未找到指定模式。");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("自定义模式名称不能为空。");
        }

        var modes = _modeStore.LoadAll().ToList();
        var mode = modes.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("未找到指定模式。");

        if (mode.IsBuiltIn)
        {
            throw new InvalidOperationException("预设模式暂不支持编辑。");
        }

        var normalizedName = name.Trim();
        if (modes.Any(item =>
                !string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("已存在同名模式，请换一个名称。");
        }

        mode.Name = normalizedName;
        mode.Description = description?.Trim() ?? string.Empty;
        mode.DesktopIconsVisible = desktopIconsVisible;
        mode.TaskbarVisible = taskbarVisible;
        mode.LayoutId = layoutId?.Trim() ?? string.Empty;
        mode.UpdatedAt = DateTimeOffset.Now;

        _modeStore.SaveAll(modes);
        return mode;
    }

    public void DeleteCustomMode(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("未找到指定模式。");
        }

        var modes = _modeStore.LoadAll().ToList();
        var mode = modes.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("未找到指定模式。");

        if (mode.IsBuiltIn)
        {
            throw new InvalidOperationException("预设模式暂不支持删除。");
        }

        modes.RemoveAll(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        _modeStore.SaveAll(modes);
    }
}
