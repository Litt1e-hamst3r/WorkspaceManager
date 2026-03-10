using System.IO;
using System.Text.Json;
using WorkspaceManager.Domain.Layouts;

namespace WorkspaceManager.Infrastructure.Layouts;

public sealed class DesktopLayoutStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _layoutsDirectory;
    private readonly string _previewDirectory;

    public DesktopLayoutStore()
    {
        var workspaceDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkspaceManager");

        _layoutsDirectory = Path.Combine(workspaceDirectory, "layouts");
        _previewDirectory = Path.Combine(workspaceDirectory, "layout-previews");

        Directory.CreateDirectory(_layoutsDirectory);
        Directory.CreateDirectory(_previewDirectory);
    }

    public IReadOnlyList<DesktopLayoutSnapshot> GetAll()
    {
        return Directory
            .EnumerateFiles(_layoutsDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(LoadFromPath)
            .Where(snapshot => snapshot is not null)
            .Cast<DesktopLayoutSnapshot>()
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .ToList();
    }

    public DesktopLayoutSnapshot? Load(string id)
    {
        var path = Path.Combine(_layoutsDirectory, $"{id}.json");
        return File.Exists(path) ? LoadFromPath(path) : null;
    }

    public void Save(DesktopLayoutSnapshot snapshot)
    {
        var path = Path.Combine(_layoutsDirectory, $"{snapshot.Id}.json");
        var json = JsonSerializer.Serialize(snapshot, SerializerOptions);
        File.WriteAllText(path, json);
    }

    public void Delete(string id)
    {
        var snapshot = Load(id);
        var path = Path.Combine(_layoutsDirectory, $"{id}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var previewPath = GetPreviewPath(snapshot?.PreviewImageFileName);
        if (previewPath is not null && File.Exists(previewPath))
        {
            TryDeleteFile(previewPath);
        }

        var legacyPreviewPath = Path.Combine(_previewDirectory, $"{id}.png");
        if (File.Exists(legacyPreviewPath))
        {
            TryDeleteFile(legacyPreviewPath);
        }
    }

    public string GetPreviewPathForId(string id)
    {
        return Path.Combine(_previewDirectory, $"{id}.png");
    }

    public string? GetPreviewPath(string? previewImageFileName)
    {
        if (string.IsNullOrWhiteSpace(previewImageFileName))
        {
            return null;
        }

        return Path.Combine(_previewDirectory, previewImageFileName);
    }

    private static DesktopLayoutSnapshot? LoadFromPath(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<DesktopLayoutSnapshot>(json, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }
}
