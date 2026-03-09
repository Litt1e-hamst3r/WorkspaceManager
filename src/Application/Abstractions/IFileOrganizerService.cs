namespace WorkspaceManager.Application.Abstractions;

public interface IFileOrganizerService
{
    Task OrganizeDesktopAsync(CancellationToken cancellationToken = default);

    Task UndoLastAsync(CancellationToken cancellationToken = default);
}
