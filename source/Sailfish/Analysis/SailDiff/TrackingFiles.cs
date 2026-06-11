using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation;

namespace Sailfish.Analysis.SailDiff;

/// <summary>
///     Helpers for discovering Sailfish tracking files on disk so a comparison's <c>before</c> file can be chosen
///     <em>explicitly</em>.
///     <para>
///         Sailfish does not auto-compare a run against the previous one. To compare against an earlier run, resolve
///         that run's tracking file with one of these helpers and pass it to
///         <c>RunSettingsBuilder.WithProvidedBeforeTrackingFile(...)</c> (or supply it from a custom
///         <c>IRequestHandler&lt;BeforeAndAfterFileLocationRequest, BeforeAndAfterFileLocationResponse&gt;</c>):
///     </para>
///     <code>
///         var trackingDir = Path.Combine(outputDir, TrackingFiles.DefaultTrackingDirectoryName);
///         var previousRun = TrackingFiles.MostRecentIn(trackingDir); // resolved before this run writes its file
///         var settings = RunSettingsBuilder.CreateBuilder()
///             .WithSailDiff()
///             .WithLocalOutputDirectory(outputDir)
///             .WithProvidedBeforeTrackingFile(previousRun!)
///             .Build();
///     </code>
/// </summary>
public static class TrackingFiles
{
    private static readonly ITrackingFileDirectoryReader Reader = new DefaultTrackingFileDirectoryReader();

    /// <summary>
    ///     The default tracking-output subdirectory name (created beneath the run's local output directory). Combine
    ///     it with your output directory to locate the default tracking directory.
    /// </summary>
    public const string DefaultTrackingDirectoryName = DefaultFileSettings.DefaultExecutionSummaryTrackingDirectory;

    /// <summary>
    ///     Returns every Sailfish tracking file in <paramref name="directory" />, ordered newest-first. Returns an
    ///     empty list when the directory is null/empty, does not exist, or contains no tracking files.
    /// </summary>
    public static IReadOnlyList<string> AllIn(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return new List<string>();
        return Reader.FindTrackingFilesInDirectoryOrderedByLastModified(directory);
    }

    /// <summary>
    ///     Returns the most recent Sailfish tracking file in <paramref name="directory" />, or <c>null</c> when the
    ///     directory does not exist or contains no tracking files. Call this <em>before</em> a run to get the previous
    ///     run's file, then pass it to <c>RunSettingsBuilder.WithProvidedBeforeTrackingFile(...)</c>.
    /// </summary>
    public static string? MostRecentIn(string directory)
    {
        return AllIn(directory).FirstOrDefault();
    }

    /// <summary>
    ///     Returns the most recent tracking file in the tracking directory configured by
    ///     <paramref name="runSettings" />, or <c>null</c> when none exists.
    /// </summary>
    public static string? MostRecentIn(IRunSettings runSettings)
    {
        return MostRecentIn(runSettings.GetRunSettingsTrackingDirectoryPath());
    }
}
