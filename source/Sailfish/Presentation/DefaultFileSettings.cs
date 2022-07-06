using System;

namespace Sailfish.Presentation;

public static class DefaultFileSettings
{
    public static readonly string SortableFormat = "yyyy-dd-M--HH-mm-ss";
    public static readonly string TrackingSuffix = ".cvs.tracking";

    public static readonly Func<DateTime, string> DefaultPerformanceFileNameStem = (DateTime timestamp) => $"PerformanceResults_{timestamp.ToString(SortableFormat)}"; // sortable file name with date
    public static readonly Func<DateTime, string> DefaultTTestMarkdownFileName = (DateTime timeStamp) => $"t-test_{timeStamp.ToString(SortableFormat)}.md";
    public static readonly Func<DateTime, string> DefaultTTestCsvFileName = (DateTime timeStamp) => $"t-test_{timeStamp.ToString(SortableFormat)}.csv";
    public static readonly Func<DateTime, string> DefaultTrackingFileName = (timeStamp) => $"PerformanceTracking_{timeStamp.ToLocalTime().ToString(SortableFormat)}{TrackingSuffix}";
}