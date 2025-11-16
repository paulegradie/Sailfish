using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Tests.Common.Builders;

public class CompiledTestCaseResultTrackingFormatBuilder
{
    private Exception? _exception;
    private string? _groupingId;
    private PerformanceRunResultTrackingFormat? _performanceRunResult;
    private TestCaseId? _testCaseId;

    public static CompiledTestCaseResultTrackingFormatBuilder Create()
    {
        return new CompiledTestCaseResultTrackingFormatBuilder();
    }

    public CompiledTestCaseResultTrackingFormatBuilder WithGroupingId(string? groupingId)
    {
        _groupingId = groupingId;
        return this;
    }

    public CompiledTestCaseResultTrackingFormatBuilder WithPerformanceRunResult(PerformanceRunResultTrackingFormat? performanceRunResult)
    {
        _performanceRunResult = performanceRunResult;
        return this;
    }

    public CompiledTestCaseResultTrackingFormatBuilder WithException(Exception? exception)
    {
        _exception = exception;
        return this;
    }

    public CompiledTestCaseResultTrackingFormatBuilder WithTestCaseId(TestCaseId? testCaseId)
    {
        _testCaseId = testCaseId;
        return this;
    }

    public CompiledTestCaseResultTrackingFormat Build()
    {
        return new CompiledTestCaseResultTrackingFormat(
            _groupingId,
            _performanceRunResult ?? PerformanceRunResultTrackingFormatBuilder.Create().Build(),
            _exception,
            _testCaseId);
    }
}