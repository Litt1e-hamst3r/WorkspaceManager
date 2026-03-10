namespace WorkspaceManager.UI.ViewModels;

public sealed class DesktopModeViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool DesktopIconsVisible { get; set; }

    public bool TaskbarVisible { get; set; }

    public string DesktopIconsVisibleText => DesktopIconsVisible ? "显示" : "隐藏";

    public string TaskbarVisibleText => TaskbarVisible ? "显示" : "隐藏";

    public string LayoutId { get; set; } = string.Empty;

    public string LayoutName { get; set; } = "未绑定布局";

    public string StateSummary { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public bool IsBuiltIn { get; set; }

    public bool IsCustom => !IsBuiltIn;

    public string KindText => IsBuiltIn ? "预设模式" : "自定义模式";
}
