using System.IO;
using System.Text.Json;

namespace WorkspaceManager.App;

public sealed class ModeStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _modesPath;

    public ModeStore()
    {
        var workspaceDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkspaceManager");

        Directory.CreateDirectory(workspaceDirectory);
        _modesPath = Path.Combine(workspaceDirectory, "modes.json");
    }

    public IReadOnlyList<DesktopMode> LoadAll()
    {
        var storedModes = LoadFromDisk();
        var mergedModes = MergeWithDefaults(storedModes);

        if (!AreSame(storedModes, mergedModes))
        {
            SaveAll(mergedModes);
        }

        return mergedModes;
    }

    public void SaveAll(IEnumerable<DesktopMode> modes)
    {
        var json = JsonSerializer.Serialize(modes.ToList(), SerializerOptions);
        File.WriteAllText(_modesPath, json);
    }

    private List<DesktopMode> LoadFromDisk()
    {
        if (!File.Exists(_modesPath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_modesPath);
            return JsonSerializer.Deserialize<List<DesktopMode>>(json, SerializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<DesktopMode> MergeWithDefaults(IEnumerable<DesktopMode> storedModes)
    {
        var storedList = storedModes.ToList();
        var storedMap = storedList
            .Where(mode => !string.IsNullOrWhiteSpace(mode.Id))
            .ToDictionary(mode => mode.Id, StringComparer.OrdinalIgnoreCase);

        var result = new List<DesktopMode>();
        foreach (var preset in CreateDefaults())
        {
            if (storedMap.TryGetValue(preset.Id, out var stored))
            {
                result.Add(new DesktopMode
                {
                    Id = preset.Id,
                    Name = preset.Name,
                    Description = preset.Description,
                    DesktopIconsVisible = stored.DesktopIconsVisible,
                    TaskbarVisible = stored.TaskbarVisible,
                    LayoutId = stored.LayoutId ?? string.Empty,
                    UpdatedAt = stored.UpdatedAt == default ? DateTimeOffset.Now : stored.UpdatedAt
                });
            }
            else
            {
                result.Add(preset);
            }
        }

        foreach (var customMode in storedList.Where(mode => !mode.IsBuiltIn && !string.IsNullOrWhiteSpace(mode.Id)))
        {
            result.Add(new DesktopMode
            {
                Id = customMode.Id,
                Name = string.IsNullOrWhiteSpace(customMode.Name) ? "自定义模式" : customMode.Name,
                Description = customMode.Description ?? string.Empty,
                DesktopIconsVisible = customMode.DesktopIconsVisible,
                TaskbarVisible = customMode.TaskbarVisible,
                LayoutId = customMode.LayoutId ?? string.Empty,
                UpdatedAt = customMode.UpdatedAt == default ? DateTimeOffset.Now : customMode.UpdatedAt
            });
        }

        result = result
            .OrderBy(mode => mode.IsBuiltIn ? 0 : 1)
            .ThenBy(mode => mode.UpdatedAt, Comparer<DateTimeOffset>.Create((left, right) => right.CompareTo(left)))
            .ToList();

        return result;
    }

    private static bool AreSame(IReadOnlyList<DesktopMode> left, IReadOnlyList<DesktopMode> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            var leftMode = left[index];
            var rightMode = right[index];
            if (!string.Equals(leftMode.Id, rightMode.Id, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(leftMode.Name, rightMode.Name, StringComparison.Ordinal)
                || !string.Equals(leftMode.Description, rightMode.Description, StringComparison.Ordinal)
                || leftMode.DesktopIconsVisible != rightMode.DesktopIconsVisible
                || leftMode.TaskbarVisible != rightMode.TaskbarVisible
                || !string.Equals(leftMode.LayoutId, rightMode.LayoutId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static List<DesktopMode> CreateDefaults()
    {
        return
        [
            new DesktopMode
            {
                Id = DesktopMode.DefaultModeId,
                Name = "默认模式",
                Description = "适合日常使用，桌面图标和任务栏都保持可见。",
                DesktopIconsVisible = true,
                TaskbarVisible = true,
                UpdatedAt = DateTimeOffset.Now
            },
            new DesktopMode
            {
                Id = DesktopMode.WorkModeId,
                Name = "工作模式",
                Description = "聚焦内容区，隐藏桌面图标，保留任务栏。",
                DesktopIconsVisible = false,
                TaskbarVisible = true,
                UpdatedAt = DateTimeOffset.Now
            },
            new DesktopMode
            {
                Id = DesktopMode.PresentationModeId,
                Name = "演示模式",
                Description = "尽量减少干扰，隐藏桌面图标和任务栏。",
                DesktopIconsVisible = false,
                TaskbarVisible = false,
                UpdatedAt = DateTimeOffset.Now
            }
        ];
    }
}
