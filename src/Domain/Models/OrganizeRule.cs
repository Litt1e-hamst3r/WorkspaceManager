namespace WorkspaceManager.Domain.Models;

public sealed record OrganizeRule(
    string Id,
    string Name,
    bool Enabled,
    int Priority,
    IReadOnlyList<string> Extensions,
    IReadOnlyList<string> Keywords,
    string TargetPath,
    int DelaySeconds);
