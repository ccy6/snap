using System.IO;
using System.Windows.Media.Imaging;

namespace Snap.App.Storage;

public sealed class CaptureVault
{
    private readonly string _directoryPath;

    public CaptureVault(string directoryPath)
    {
        _directoryPath = Path.GetFullPath(directoryPath);
    }

    public string Save(BitmapSource image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Directory.CreateDirectory(_directoryPath);

        var fileName = $"Snap_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{Guid.NewGuid():N}.png";
        var filePath = Path.Join(_directoryPath, fileName);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        encoder.Save(stream);
        return filePath;
    }

    public IReadOnlyList<string> GetCaptures()
    {
        if (!Directory.Exists(_directoryPath))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(_directoryPath, "*.png", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetCreationTime)
            .ToArray();
    }
}
