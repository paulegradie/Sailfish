using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Tests.Common.Builders;

public class CompiledTestCaseResultTrackingFormatBuilder
{
    private Exception? exception;
    private string? groupingId;
    private PerformanceRunResultTrackingFormat? performanceRunResult;
    private TestCaseId? testCaseId;

    public static CompiledTestCaseResultTrackingFormatBuilder Create()
    {
        return new CompiledTestCaseResultTrackingFormatBuilder();
    }

    public CompiledTestCaseResultTrackingFormatBuilder WithGroupingId(string? groupingId)
    {
        this.groupingId = groupingId;
        return this;
    }

    public CompiledTestCaseResultTrackingFormatBuilder WithPerformanceRunResult(PerformanceRunResultTrackingFormat? performanceRunResult)
    {
        this.performanceRunResult = performanceRunResult;
        return this;
    }

    public CompiledTestCaseResultTrackingFormatBuilder WithException(Exception? exception)
    {
        this.exception = exception;
        return this;
    }

    public CompiledTestCaseResultTrackingFormatBuilder WithTestCaseId(TestCaseId? testCaseId)
    {
        this.testCaseId = testCaseId;
        return this;
    }

    public CompiledTestCaseResultTrackingFormat Build()
    {
        return new CompiledTestCaseResultTrackingFormat(
            groupingId,
            performanceRunResult ?? PerformanceRunResultTrackingFormatBuilder.Create().Build(),
            exception,
            testCaseId);
    }
}