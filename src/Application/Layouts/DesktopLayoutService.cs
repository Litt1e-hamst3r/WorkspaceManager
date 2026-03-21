using System.IO;
using System.Windows;
using WorkspaceManager.Domain.Layouts;
using WorkspaceManager.Infrastructure.Layouts;
using WorkspaceManager.Interop.Layouts;

namespace WorkspaceManager.Application.Layouts;

public sealed class DesktopLayoutService
{
    private readonly DesktopLayoutStore _layoutStore;
    private readonly DesktopLayoutPreviewService _previewService;
    private readonly DesktopLayoutInteropService _layoutInteropService;

    public DesktopLayoutService(
        DesktopLayoutStore layoutStore,
        DesktopLayoutPreviewService previewService,
        DesktopLayoutInteropService layoutInteropService)
    {
        _layoutStore = layoutStore;
        _previewService = previewService;
        _layoutInteropService = layoutInteropService;
    }

    public IReadOnlyList<DesktopLayoutSnapshot> GetSavedLayouts()
    {
        return _layoutStore.GetAll();
    }

    public DesktopLayoutSnapshot Capture(string? name = null)
    {
        var items = _layoutInteropService.CaptureItems();
        return new DesktopLayoutSnapshot
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(name)
                ? $"布局 {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                : name.Trim(),
            ResolutionWidth = (int)SystemParameters.PrimaryScreenWidth,
            ResolutionHeight = (int)SystemParameters.PrimaryScreenHeight,
            CreatedAt = DateTimeOffset.Now,
            Items = items.ToList()
        };
    }

    public void Save(DesktopLayoutSnapshot snapshot)
    {
        TryCapturePreview(snapshot);
        _layoutStore.Save(snapshot);
    }

    public void Restore(string id)
    {
        var snapshot = _layoutStore.Load(id)
            ?? throw new InvalidOperationException("未找到指定布局。");

        Restore(snapshot);
    }

    public void Restore(DesktopLayoutSnapshot snapshot)
    {
        _layoutInteropService.RestoreItems(snapshot.Items);
    }

    public void Delete(string id)
    {
        _layoutStore.Delete(id);
    }

    public string? GetPreviewPath(DesktopLayoutSnapshot snapshot)
    {
        var previewPath = _layoutStore.GetPreviewPath(snapshot.PreviewImageFileName);
        return previewPath is not null && File.Exists(previewPath)
            ? previewPath
            : null;
    }

    private void TryCapturePreview(DesktopLayoutSnapshot snapshot)
    {
        try
        {
            var previewPath = _layoutStore.GetPreviewPathForId(snapshot.Id);
            _previewService.CaptureTo(previewPath);
            snapshot.PreviewImageFileName = Path.GetFileName(previewPath);
        }
        catch
        {
            snapshot.PreviewImageFileName = string.Empty;
        }
    }
}
