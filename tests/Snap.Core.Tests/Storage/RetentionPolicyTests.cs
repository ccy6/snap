using Snap.Core.Storage;

namespace Snap.Core.Tests.Storage;

public sealed class RetentionPolicyTests
{
    private static readonly TimeZoneInfo ChinaTimeZone =
        TimeZoneInfo.CreateCustomTimeZone("China Standard Time", TimeSpan.FromHours(8), "China", "China");

    [Fact]
    public void ShouldDelete_DailyRetentionAfterLocalMidnight_ReturnsTrue()
    {
        var capture = new CaptureItem(
            Guid.NewGuid(),
            "capture.png",
            new DateTimeOffset(2026, 9, 14, 15, 59, 59, TimeSpan.Zero),
            IsKept: false);
        var now = new DateTimeOffset(2026, 9, 14, 16, 0, 0, TimeSpan.Zero);

        var result = RetentionPolicy.ShouldDelete(capture, RetentionPeriod.Daily, now, ChinaTimeZone);

        Assert.True(result);
    }

    [Fact]
    public void ShouldDelete_DailyRetentionOnSameLocalDay_ReturnsFalse()
    {
        var capture = new CaptureItem(
            Guid.NewGuid(),
            "capture.png",
            new DateTimeOffset(2026, 9, 14, 16, 0, 0, TimeSpan.Zero),
            IsKept: false);
        var now = new DateTimeOffset(2026, 9, 15, 15, 59, 59, TimeSpan.Zero);

        var result = RetentionPolicy.ShouldDelete(capture, RetentionPeriod.Daily, now, ChinaTimeZone);

        Assert.False(result);
    }

    [Theory]
    [InlineData(RetentionPeriod.SevenDays, 7, true)]
    [InlineData(RetentionPeriod.SevenDays, 6, false)]
    [InlineData(RetentionPeriod.ThirtyDays, 30, true)]
    [InlineData(RetentionPeriod.ThirtyDays, 29, false)]
    public void ShouldDelete_AgeBasedRetention_UsesExactCreationTime(
        RetentionPeriod period,
        int ageInDays,
        bool expected)
    {
        var now = new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);
        var capture = new CaptureItem(Guid.NewGuid(), "capture.png", now.AddDays(-ageInDays), IsKept: false);

        var result = RetentionPolicy.ShouldDelete(capture, period, now, TimeZoneInfo.Utc);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShouldDelete_KeptCapture_ReturnsFalse()
    {
        var now = new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);
        var capture = new CaptureItem(Guid.NewGuid(), "capture.png", now.AddYears(-1), IsKept: true);

        var result = RetentionPolicy.ShouldDelete(capture, RetentionPeriod.Daily, now, TimeZoneInfo.Utc);

        Assert.False(result);
    }
}
