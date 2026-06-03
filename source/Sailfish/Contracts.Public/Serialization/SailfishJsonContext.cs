using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.JsonConverters;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Results;

namespace Sailfish.Contracts.Public.Serialization;

/// <summary>
///     Source-generated <see cref="System.Text.Json.Serialization.JsonSerializerContext" /> covering every
///     contract Sailfish persists at the end of a run: the V1 tracking-file graph
///     (<see cref="ClassExecutionSummaryTrackingFormat" /> and everything reachable from it) and the
///     reproducibility manifest.
///     <para>
///         Why this exists: <c>System.Text.Json</c> refuses reflection-based (de)serialization in any host
///         where the feature switch <c>JsonSerializerIsReflectionEnabledByDefault</c> is off — Native AOT,
///         <c>PublishTrimmed</c>, and .NET 10 file-based apps (<c>dotnet run app.cs</c>, which default to
///         <c>PublishAot=true</c>). In those hosts the benchmarks measure fine but the run used to crash at
///         the end (exit 134) the moment the tracking file was read back. Providing source-generated metadata
///         lets <see cref="SailfishSerializer" /> resolve these contracts without any ambient reflection
///         resolver, so the post-run pipeline works in every host.
///     </para>
///     <para>
///         The default generation mode (<c>Metadata</c>) is used deliberately: the tracking graph relies on
///         runtime <see cref="JsonConverter" />s (added on the options) and on an
///         <see cref="object" />-typed property (<see cref="TestCaseVariable.Value" />), both of which require
///         the metadata-based contract rather than the fast-path serializer.
///     </para>
/// </summary>
// V1 tracking-file graph (top-level persisted shape is a list of class summaries).
[JsonSerializable(typeof(List<ClassExecutionSummaryTrackingFormat>))]
[JsonSerializable(typeof(ClassExecutionSummaryTrackingFormat))]
// ExecutionSummaryTrackingFormatConverter serializes the class summary through this bridge type.
[JsonSerializable(typeof(ExecutionSummaryTrackingFormatConverter.TrackingFileSerializationHelper))]
[JsonSerializable(typeof(ExecutionSettingsTrackingFormat))]
[JsonSerializable(typeof(IEnumerable<CompiledTestCaseResultTrackingFormat>))]
[JsonSerializable(typeof(List<CompiledTestCaseResultTrackingFormat>))]
[JsonSerializable(typeof(CompiledTestCaseResultTrackingFormat))]
[JsonSerializable(typeof(PerformanceRunResultTrackingFormat))]
// TestCaseId is written via TestCaseIdConverter, which round-trips these nested contracts.
[JsonSerializable(typeof(TestCaseId))]
[JsonSerializable(typeof(TestCaseName))]
[JsonSerializable(typeof(TestCaseVariables))]
[JsonSerializable(typeof(IEnumerable<TestCaseVariable>))]
[JsonSerializable(typeof(List<TestCaseVariable>))]
[JsonSerializable(typeof(TestCaseVariable))]
// Reproducibility manifest (nested snapshot types are pulled in transitively).
[JsonSerializable(typeof(ReproducibilityManifest))]
// Leaf / helper shapes reached through the graph or written directly by the custom converters.
[JsonSerializable(typeof(double[]))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(decimal))]
internal sealed partial class SailfishJsonContext : JsonSerializerContext
{
}
