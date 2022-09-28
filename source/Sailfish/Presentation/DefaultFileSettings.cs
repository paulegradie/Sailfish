using System;
using System.IO;
using System.Linq;
using System.Text;
using Accord.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace Sailfish.Presentation;

public static class DefaultFileSettings
{
    public const string SortableFormat = "yyyy-dd-M--HH-mm-ss";
    public const string TrackingSuffix = ".cvs.tracking";
    public const string TagsPrefix = "tags-";
    public const string KeyValueDelimiter = "=";
    public const string MapDelimiter = "__";

    public static readonly Func<DateTime, string> DefaultPerformanceFileNameStem =
        (DateTime timestamp) =>
            $"PerformanceResults_{timestamp.ToString(SortableFormat)}"; // sortable file name with date

    public static readonly Func<DateTime, string> DefaultTTestMarkdownFileName =
        (DateTime timeStamp) => $"t-test_{timeStamp.ToString(SortableFormat)}.md";

    public static readonly Func<DateTime, string> DefaultTTestCsvFileName =
        (DateTime timeStamp) => $"t-test_{timeStamp.ToString(SortableFormat)}.csv";

    public static readonly Func<DateTime, string> DefaultTrackingFileName = (timeStamp) =>
        $"PerformanceTracking_{timeStamp.ToLocalTime().ToString(SortableFormat)}{TrackingSuffix}";

    public static string JoinTags(OrderedDictionary<string, string> tags)
    {
        if (!tags.Any()) return string.Empty;

        var result = new StringBuilder();
        result.Append(TagsPrefix);
        foreach (var tagPair in tags)
        {
            var joinedTag = string.Join(KeyValueDelimiter, tagPair.Key, tagPair.Value);
            result.Append(joinedTag + MapDelimiter);
        }

        return result.ToString().TrimEnd(MapDelimiter.ToCharArray());
    }

    public static string AppendTagsToFilename(string fileName, OrderedDictionary<string, string> tags)
    {
        if (!tags.Any()) return fileName;
        var joinedTags = JoinTags(tags);

        if (fileName.EndsWith(TrackingSuffix))
        {
            var strippedFileName = fileName.Replace(TrackingSuffix, string.Empty);
            return $"{strippedFileName}.{joinedTags}" + TrackingSuffix;
        }

        if (!Path.HasExtension(fileName)) return string.Join(MapDelimiter, joinedTags);
        var filenameStem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return string.Join(".", filenameStem, joinedTags) + extension;
    }
}