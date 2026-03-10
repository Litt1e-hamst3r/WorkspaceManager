using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WorkspaceManager.UI.Services;

public sealed class LayoutPreviewImageLoader
{
    public ImageSource? Load(string previewImagePath, int? targetPixelWidth = null)
    {
        if (string.IsNullOrWhiteSpace(previewImagePath) || !File.Exists(previewImagePath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(previewImagePath);
        using var stream = new MemoryStream(bytes);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames.FirstOrDefault();
        if (frame is null)
        {
            return null;
        }

        if (!targetPixelWidth.HasValue || frame.PixelWidth <= targetPixelWidth.Value)
        {
            frame.Freeze();
            return frame;
        }

        var scale = targetPixelWidth.Value / (double)frame.PixelWidth;
        var transformed = new TransformedBitmap(frame, new ScaleTransform(scale, scale));
        transformed.Freeze();
        return transformed;
    }
}
