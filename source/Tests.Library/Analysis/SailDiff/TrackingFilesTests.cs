using System;
using System.IO;
using Sailfish.Analysis.SailDiff;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class TrackingFilesTests
{
    [Fact]
    public void MostRecentIn_NullOrWhitespace_ReturnsNull()
    {
        TrackingFiles.MostRecentIn("").ShouldBeNull();
        TrackingFiles.MostRecentIn("   ").ShouldBeNull();
    }

    [Fact]
    public void MostRecentIn_NonexistentDirectory_ReturnsNull()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sf_tf_missing_" + Guid.NewGuid().ToString("N"));
        TrackingFiles.MostRecentIn(dir).ShouldBeNull();
    }

    [Fact]
    public void MostRecentIn_EmptyDirectory_ReturnsNull()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "sf_tf_empty_" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            TrackingFiles.MostRecentIn(dir).ShouldBeNull();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void MostRecentIn_ReturnsNewestTrackingFileByLastWriteTime()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "sf_tf_" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var older = Path.Combine(dir, "PerformanceTracking_older.json.tracking");
            var newer = Path.Combine(dir, "PerformanceTracking_newer.json.tracking");
            File.WriteAllText(older, "{}");
            File.WriteAllText(newer, "{}");
            var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(older, baseTime);
            File.SetLastWriteTimeUtc(newer, baseTime.AddMinutes(5));

            TrackingFiles.MostRecentIn(dir).ShouldBe(newer);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void AllIn_IgnoresNonTrackingFiles_AndOrdersNewestFirst()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "sf_tf_all_" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var t1 = Path.Combine(dir, "PerformanceTracking_1.json.tracking");
            var t2 = Path.Combine(dir, "PerformanceTracking_2.json.tracking");
            File.WriteAllText(t1, "{}");
            File.WriteAllText(t2, "{}");
            File.WriteAllText(Path.Combine(dir, "notes.txt"), "x");
            File.WriteAllText(Path.Combine(dir, "results.json"), "{}");
            var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(t1, baseTime);
            File.SetLastWriteTimeUtc(t2, baseTime.AddMinutes(1));

            var all = TrackingFiles.AllIn(dir);

            all.Count.ShouldBe(2);
            all.ShouldBe(new[] { t2, t1 }); // newest-first
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
