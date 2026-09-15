using System.IO;
using System.Windows.Media.Imaging;

namespace Snap.App;

public sealed record CapturePreview(BitmapSource Image, string FilePath, string CreatedLabel, bool IsKept)
{
    public static CapturePreview Load(string filePath, bool isKept)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = 380;
        image.UriSource = new Uri(filePath, UriKind.Absolute);
        image.EndInit();
        image.Freeze();

        var createdAt = File.GetCreationTime(filePath);
        return new CapturePreview(image, filePath, createdAt.ToString("MM-dd HH:mm:ss"), isKept);
    }
}
