using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Accord.Collections;
using Sailfish.Analysis;
using Sailfish.Exceptions;

namespace Sailfish.Presentation;

public static class DefaultFileSettings
{
    public const string CsvSuffix = ".csv";
    public const string MarkdownSuffix = ".md";
    public const string SortableFormat = "yyyy-dd-M--HH-mm-ss";
    public const string TrackingSuffix = $"{CsvSuffix}.tracking";
    public const string TagsPrefix = "tags-";
    public const string KeyValueDelimiter = "=";
    public const string MapDelimiter = "__";

    public static readonly Func<DateTime, string> DefaultPerformanceFileNameStem =
        (DateTime timestamp) =>
            $"PerformanceResults_{timestamp.ToString(SortableFormat)}"; // sortable file name with date

    public static readonly Func<DateTime, TestType, string> DefaultTTestMarkdownFileName =
        (DateTime timeStamp, TestType testType) => $"test_{testType.ToString()}_{timeStamp.ToString(SortableFormat)}{MarkdownSuffix}";

    public static readonly Func<DateTime, TestType, string> DefaultTTestCsvFileName =
        (DateTime timeStamp, TestType testType) => $"test_{testType.ToString()}_{timeStamp.ToString(SortableFormat)}{CsvSuffix}";

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

    public static Dictionary<string, string> ExtractDataFromFileNameWithTagSection(string filename)
    {
        var parts =
            filename.Split(TagsPrefix);
        if (parts.Length == 1) return new Dictionary<string, string>();
        var dataSection = parts.TakeLast(1)
            .SingleOrDefault()?.Replace(TrackingSuffix, string.Empty);
        if (dataSection is null) throw new SailfishException("Invalid File Name Structure");
        var keyValues = dataSection.Split(MapDelimiter);
        return keyValues.Select(keyValue => keyValue.Split(KeyValueDelimiter))
            .ToDictionary(keyVal => keyVal[0], keyVal => keyVal[1]);
    }
}