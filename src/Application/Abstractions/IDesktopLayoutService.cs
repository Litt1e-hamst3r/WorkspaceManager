using WorkspaceManager.Domain.Models;

namespace WorkspaceManager.Application.Abstractions;

public interface IDesktopLayoutService
{
    Task<DesktopLayoutSnapshot> CaptureAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(DesktopLayoutSnapshot snapshot, CancellationToken cancellationToken = default);

    Task RestoreAsync(string layoutId, CancellationToken cancellationToken = default);
}
