using Microsoft.Extensions.DependencyInjection;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     Registration helpers for the Skipper AI analysis layer, called from an <c>IRegisterSailfishServices</c>
///     provider.
/// </summary>
public static class SkipperServiceCollectionExtensions
{
    /// <summary>
    ///     Wires the framework's Skipper pipeline to a consumer-supplied <typeparamref name="TTransport" />. This is
    ///     the one-line way to light up AI analysis: it registers the transport and selects
    ///     <see cref="PromptDrivenSailfishAgent" /> as the <see cref="ISailfishAgent" /> (overriding the no-op
    ///     default), so the framework owns prompt-building and response-parsing and you own only the model call.
    ///     <para>
    ///         Pair with <c>RunSettingsBuilder.WithAiAnalysis()</c> (or <c>"AiAnalysisSettings": { "Enabled": true }</c>
    ///         in <c>.sailfish.json</c>). A consumer who needs to own prompt-building or parsing as well can instead
    ///         register their own <see cref="ISailfishAgent" /> directly and skip this helper.
    ///     </para>
    /// </summary>
    public static IServiceCollection AddSkipperTransport<TTransport>(this IServiceCollection services)
        where TTransport : class, ISkipperTransport
    {
        services.AddSingleton<ISkipperTransport, TTransport>();
        services.AddSingleton<ISailfishAgent, PromptDrivenSailfishAgent>();
        return services;
    }
}
