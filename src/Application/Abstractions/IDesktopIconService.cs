namespace WorkspaceManager.Application.Abstractions;

public interface IDesktopIconService
{
    bool IsVisible();

    Task SetVisibleAsync(bool visible, CancellationToken cancellationToken = default);

    Task ToggleAsync(CancellationToken cancellationToken = default);
}
