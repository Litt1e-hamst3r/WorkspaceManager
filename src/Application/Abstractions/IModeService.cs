using WorkspaceManager.Domain.Models;

namespace WorkspaceManager.Application.Abstractions;

public interface IModeService
{
    Task<IReadOnlyList<DesktopMode>> GetModesAsync(CancellationToken cancellationToken = default);

    Task SwitchAsync(string modeId, CancellationToken cancellationToken = default);
}
