using System.IO;
using System.Text.Json;

namespace WorkspaceManager.App;

public sealed class DesktopLayoutStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _layoutsDirectory;

    public DesktopLayoutStore()
    {
        _layoutsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkspaceManager",
            "layouts");

        Directory.CreateDirectory(_layoutsDirectory);
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
        var path = Path.Combine(_layoutsDirectory, $"{id}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
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
}
