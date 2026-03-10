using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace WorkspaceManager.Interop.Layouts;

public sealed class DesktopLayoutPreviewService
{
    private const int MaxPreviewWidth = 1920;

    public void CaptureTo(string outputPath)
    {
        var screen = Screen.PrimaryScreen
            ?? throw new InvalidOperationException("无法读取主显示器信息。");

        var bounds = screen.Bounds;
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("预览图目录无效。"));

        using var sourceBitmap = new Bitmap(bounds.Width, bounds.Height);
        using (var graphics = Graphics.FromImage(sourceBitmap))
        {
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }

        using var previewBitmap = CreatePreviewBitmap(sourceBitmap);
        previewBitmap.Save(outputPath, ImageFormat.Png);
    }

    private static Bitmap CreatePreviewBitmap(Bitmap sourceBitmap)
    {
        if (sourceBitmap.Width <= MaxPreviewWidth)
        {
            return new Bitmap(sourceBitmap);
        }

        var scale = MaxPreviewWidth / (double)sourceBitmap.Width;
        var previewWidth = MaxPreviewWidth;
        var previewHeight = (int)Math.Round(sourceBitmap.Height * scale);

        var previewBitmap = new Bitmap(previewWidth, previewHeight);
        using (var graphics = Graphics.FromImage(previewBitmap))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(sourceBitmap, new Rectangle(0, 0, previewWidth, previewHeight));
        }

        return previewBitmap;
    }
}
