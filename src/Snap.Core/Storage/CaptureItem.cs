namespace Snap.Core.Storage;

public sealed record CaptureItem(
    Guid Id,
    string FilePath,
    DateTimeOffset CreatedAt,
    bool IsKept);
