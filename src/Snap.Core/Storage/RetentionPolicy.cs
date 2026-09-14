namespace Snap.Core.Storage;

public static class RetentionPolicy
{
    public static bool ShouldDelete(
        CaptureItem capture,
        RetentionPeriod retentionPeriod,
        DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentNullException.ThrowIfNull(timeZone);

        if (capture.IsKept)
        {
            return false;
        }

        return retentionPeriod switch
        {
            RetentionPeriod.Daily =>
                DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(capture.CreatedAt, timeZone).DateTime) <
                DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime),
            RetentionPeriod.SevenDays => capture.CreatedAt <= now.AddDays(-7),
            RetentionPeriod.ThirtyDays => capture.CreatedAt <= now.AddDays(-30),
            _ => throw new ArgumentOutOfRangeException(nameof(retentionPeriod), retentionPeriod, null),
        };
    }
}
