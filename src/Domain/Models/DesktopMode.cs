namespace WorkspaceManager.Domain.Models;

public sealed record DesktopMode(
    string Id,
    string Name,
    bool DesktopIconsVisible,
    string? LayoutId,
    bool EnableAutoOrganize,
    IReadOnlyList<string> EnabledRuleIds,
    bool HideSensitiveItems);
