using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.Ai;

internal interface ISkipperResponseCache
{
    /// <summary>A stable key for a context packet under a given role. Identical inputs produce identical keys.</summary>
    string ComputeKey(PerformanceNarrativeContext context, SkipperRole role);

    Task<SkipperReview?> TryGetAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(string key, SkipperReview review, CancellationToken cancellationToken);
}

/// <summary>
///     File-backed cache keyed on a hash of the grounded context packet. Guarantees that the same numbers yield
///     the same narrative across runs — no re-spend on the model, and stable, reproducible output. A corrupt or
///     unreadable entry is treated as a miss and never breaks a run.
/// </summary>
internal sealed class FileSkipperResponseCache : ISkipperResponseCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IRunSettings runSettings;

    public FileSkipperResponseCache(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public string ComputeKey(PerformanceNarrativeContext context, SkipperRole role)
    {
        var payload = role + "|" + JsonSerializer.Serialize(context, SerializerOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    public async Task<SkipperReview?> TryGetAsync(string key, CancellationToken cancellationToken)
    {
        var path = PathFor(key);
        if (!File.Exists(path)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SkipperReview>(json, SerializerOptions);
        }
        catch
        {
            return null; // a corrupt cache entry must never break a run
        }
    }

    public async Task SetAsync(string key, SkipperReview review, CancellationToken cancellationToken)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(review, SerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    private string PathFor(string key) => Path.Join(runSettings.LocalOutputDirectory, "skipper_cache", key + ".json");
}
