using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using WorkspaceManager.Infrastructure.Configuration;

namespace WorkspaceManager.UI.ViewModels;

public sealed class WallpaperSourceViewModel : INotifyPropertyChanged
{
    private bool _enabled = true;
    private string _name = string.Empty;
    private string _requestUrl = string.Empty;
    private WallpaperSourceKind _kind = WallpaperSourceKind.RemoteUrl;
    private bool _isBuiltIn;

    public string Id { get; set; } = string.Empty;

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public string RequestUrl
    {
        get => _requestUrl;
        set
        {
            if (_requestUrl == value)
            {
                return;
            }

            _requestUrl = value;
            OnPropertyChanged();
            RaiseDerivedStateChanged();
        }
    }

    public WallpaperSourceKind Kind
    {
        get => _kind;
        set
        {
            if (_kind == value)
            {
                return;
            }

            _kind = value;
            OnPropertyChanged();
            RaiseDerivedStateChanged();
        }
    }

    public bool IsBuiltIn
    {
        get => _isBuiltIn;
        set
        {
            if (_isBuiltIn == value)
            {
                return;
            }

            _isBuiltIn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(SourceTypeText));
        }
    }

    public bool CanDelete => !IsBuiltIn;

    public bool CanReselect => Kind is WallpaperSourceKind.LocalFile or WallpaperSourceKind.LocalFolder;

    public string SourceTypeText => IsBuiltIn
        ? "内置源"
        : Kind == WallpaperSourceKind.LocalFile
            ? "本地图片"
            : Kind == WallpaperSourceKind.LocalFolder
                ? "本地文件夹"
            : "自定义接口";

    public string EnabledText => Enabled ? "已启用" : "已停用";

    public bool HasHealthIssue => !IsAvailable;

    public bool IsAvailable
    {
        get
        {
            return Kind switch
            {
                WallpaperSourceKind.LocalFile => WallpaperSourceSetting.TryNormalizeLocalFilePath(RequestUrl, out var localPath)
                    && File.Exists(localPath),
                WallpaperSourceKind.LocalFolder => WallpaperSourceSetting.TryNormalizeLocalDirectoryPath(RequestUrl, out var localDirectory)
                    && Directory.Exists(localDirectory)
                    && WallpaperSourceSetting.GetSupportedLocalImageFiles(localDirectory).Count > 0,
                _ => WallpaperSourceSetting.TryNormalizeRemoteUrl(RequestUrl, out _)
            };
        }
    }

    public string AvailabilityText
    {
        get
        {
            if (IsAvailable)
            {
                return "可用";
            }

            return Kind switch
            {
                WallpaperSourceKind.LocalFile => WallpaperSourceSetting.TryNormalizeLocalFilePath(RequestUrl, out var localPath) && !File.Exists(localPath)
                    ? "文件缺失"
                    : "路径无效",
                WallpaperSourceKind.LocalFolder => ResolveLocalFolderAvailabilityText(),
                _ => "地址无效"
            };
        }
    }

    public string AvailabilityDetailText
    {
        get
        {
            if (Kind == WallpaperSourceKind.LocalFile)
            {
                if (WallpaperSourceSetting.TryNormalizeLocalFilePath(RequestUrl, out var localPath))
                {
                    return File.Exists(localPath)
                        ? "本地图片可访问"
                        : "文件不存在，请重新选择";
                }

                return "路径无效，请重新选择";
            }

            if (Kind == WallpaperSourceKind.LocalFolder)
            {
                if (!WallpaperSourceSetting.TryNormalizeLocalDirectoryPath(RequestUrl, out var localDirectory))
                {
                    return "路径无效，请重新选择文件夹";
                }

                if (!Directory.Exists(localDirectory))
                {
                    return "文件夹不存在，请重新选择";
                }

                var imageCount = WallpaperSourceSetting.GetSupportedLocalImageFiles(localDirectory).Count;
                return imageCount > 0
                    ? $"文件夹内有 {imageCount} 张可用图片"
                    : "文件夹中没有可用图片";
            }

            return WallpaperSourceSetting.TryNormalizeRemoteUrl(RequestUrl, out _)
                ? "地址格式有效"
                : "请输入有效的 http/https 地址";
        }
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EnabledText));
        }
    }

    public void RefreshComputedState()
    {
        RaiseDerivedStateChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void RaiseDerivedStateChanged()
    {
        OnPropertyChanged(nameof(CanReselect));
        OnPropertyChanged(nameof(SourceTypeText));
        OnPropertyChanged(nameof(IsAvailable));
        OnPropertyChanged(nameof(HasHealthIssue));
        OnPropertyChanged(nameof(AvailabilityText));
        OnPropertyChanged(nameof(AvailabilityDetailText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string ResolveLocalFolderAvailabilityText()
    {
        if (!WallpaperSourceSetting.TryNormalizeLocalDirectoryPath(RequestUrl, out var localDirectory))
        {
            return "路径无效";
        }

        if (!Directory.Exists(localDirectory))
        {
            return "目录缺失";
        }

        return WallpaperSourceSetting.GetSupportedLocalImageFiles(localDirectory).Count > 0
            ? "可用"
            : "目录为空";
    }
}
