using System;

namespace Sailfish.Presentation;

public static class DefaultFileSettings
{
    public static readonly string SortableFormat = "yyyy-dd-M--HH-mm-ss";
    public static readonly Func<DateTime, string> DefaultPerformanceFileNameStem = (DateTime timestamp) => $"PerformanceResults_{timestamp.ToString(SortableFormat)}"; // sortable file name with date
    public static readonly Func<DateTime, string> DefaultTTestFileNameStem = (DateTime timeStamp) => $"t-test_{timeStamp.ToString(SortableFormat)}.md";
    public static readonly Func<DateTime, string> DefaultTrackingFileName = (timeStamp) => $"PerformanceTracking_{timeStamp.ToLocalTime().ToString(SortableFormat)}.cvs.tracking";
}