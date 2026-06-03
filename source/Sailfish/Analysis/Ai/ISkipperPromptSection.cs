using System.Text;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     A contributable <b>body</b> section of the Skipper prompt. The default prompt
///     (<see cref="DefaultSkipperPromptBuilder" />) is assembled as a framework-owned grounding preamble, then every
///     registered section in ascending <see cref="Order" />, then a framework-owned output-schema contract. Those two
///     bookends are emitted by the builder itself and cannot be dropped or reordered — they are the half of the
///     contract that must never drift from <see cref="ISkipperResponseParser" />.
///     <para>
///         Register additional sections from an <c>IRegisterSailfishServices</c> provider to enrich the prompt with
///         your own grounding (service topology, domain hints, "weight allocations heavily"):
///         <c>services.AddSingleton&lt;ISkipperPromptSection, MySection&gt;()</c>. They compose by
///         <see cref="Order" /> alongside the framework's defaults — see <see cref="SkipperPromptOrder" /> for the
///         default slots so you can place yours before or after them.
///     </para>
/// </summary>
public interface ISkipperPromptSection
{
    /// <summary>Ascending sort key. Lower contributes earlier. Ties keep registration order.</summary>
    int Order { get; }

    /// <summary>Append this section's text to <paramref name="prompt" />, reading only the grounded session.</summary>
    void Contribute(StringBuilder prompt, SkipperSession session);
}

/// <summary>
///     The <see cref="ISkipperPromptSection.Order" /> values of the framework's default body sections. Use these as
///     anchors when registering your own — e.g. <c>SkipperPromptOrder.Comparisons - 1</c> to contribute domain
///     context just before the SailDiff comparisons, or <c>SkipperPromptOrder.ResultTable + 1</c> to append after the
///     verbatim result table.
/// </summary>
public static class SkipperPromptOrder
{
    /// <summary>SailDiff before / after comparisons.</summary>
    public const int Comparisons = 100;

    /// <summary>ScaleFish complexity fits and projections.</summary>
    public const int Scaling = 200;

    /// <summary>Environment snapshot and reproducibility concerns.</summary>
    public const int Environment = 300;

    /// <summary>The verbatim SailDiff / ScaleFish result table (authoritative units).</summary>
    public const int ResultTable = 400;
}
