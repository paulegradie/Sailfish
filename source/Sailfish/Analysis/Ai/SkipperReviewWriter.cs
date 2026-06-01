using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation;

namespace Sailfish.Analysis.Ai;

internal interface ISkipperReviewWriter
{
    Task WriteAsync(SkipperReview review, CancellationToken cancellationToken);
}

/// <summary>
///     Persists a <see cref="SkipperReview" /> as <c>skipper-review_*.json</c> beside the run output. This is the
///     artifact a decoupled orchestrator consumes in the action-taking future (the "act via artifacts" half of the
///     hybrid topology).
/// </summary>
internal sealed class SkipperReviewWriter : ISkipperReviewWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IRunSettings runSettings;

    public SkipperReviewWriter(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public async Task WriteAsync(SkipperReview review, CancellationToken cancellationToken)
    {
        var directory = runSettings.LocalOutputDirectory;
        Directory.CreateDirectory(directory);

        var fileName = DefaultFileSettings.AppendTagsToFilename(
            $"skipper-review_{runSettings.TimeStamp:yyyyMMdd_HHmmss}.json",
            runSettings.Tags);
        var filePath = Path.Join(directory, fileName);

        var json = JsonSerializer.Serialize(review, SerializerOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
    }
}
