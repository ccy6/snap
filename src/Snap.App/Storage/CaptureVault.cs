using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using Snap.Core.Storage;

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
            .EnumerateFiles(_directoryPath, "*.png", System.IO.SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetCreationTime)
            .ToArray();
    }

    public bool IsKept(string filePath) => File.Exists(GetKeepMarkerPath(filePath));

    public void SetKept(string filePath, bool isKept)
    {
        var markerPath = GetKeepMarkerPath(filePath);
        if (isKept)
        {
            File.WriteAllText(markerPath, string.Empty);
        }
        else
        {
            File.Delete(markerPath);
        }
    }

    public void Delete(string filePath)
    {
        if (File.Exists(filePath))
        {
            FileSystem.DeleteFile(
                filePath,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.DoNothing);
        }

        File.Delete(GetKeepMarkerPath(filePath));
    }

    public void Cleanup(RetentionPeriod retentionPeriod)
    {
        var now = DateTimeOffset.Now;
        foreach (var filePath in GetCaptures())
        {
            var capture = new CaptureItem(
                Guid.Empty,
                filePath,
                new DateTimeOffset(File.GetCreationTimeUtc(filePath), TimeSpan.Zero),
                IsKept(filePath));
            if (RetentionPolicy.ShouldDelete(capture, retentionPeriod, now, TimeZoneInfo.Local))
            {
                Delete(filePath);
            }
        }
    }

    private static string GetKeepMarkerPath(string filePath) => $"{filePath}.keep";
}
