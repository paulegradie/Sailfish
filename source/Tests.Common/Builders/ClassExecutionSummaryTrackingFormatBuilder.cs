using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Tests.Common.Builders;

public class ClassExecutionSummaryTrackingFormatBuilder
{
    private List<CompiledTestCaseResultTrackingFormat> _compiledTestCaseResults = new();
    private ExecutionSettingsTrackingFormat? _executionSettings;
    private Type? _testClass;

    public static ClassExecutionSummaryTrackingFormatBuilder Create()
    {
        return new ClassExecutionSummaryTrackingFormatBuilder();
    }

    public ClassExecutionSummaryTrackingFormatBuilder WithTestClass(Type testClass)
    {
        _testClass = testClass;
        return this;
    }

    public ClassExecutionSummaryTrackingFormatBuilder WithExecutionSettings(ExecutionSettingsTrackingFormat executionSettings)
    {
        _executionSettings = executionSettings;
        return this;
    }

    public ClassExecutionSummaryTrackingFormatBuilder WithExecutionSettings(Action<ExecutionSettingsTrackingFormatBuilder> configureAction)
    {
        var builder = new ExecutionSettingsTrackingFormatBuilder();
        configureAction(builder);
        _executionSettings = builder.Build();
        return this;
    }

    public ClassExecutionSummaryTrackingFormatBuilder WithCompiledTestCaseResult(List<CompiledTestCaseResultTrackingFormat> compiledTestCaseResults)
    {
        _compiledTestCaseResults = compiledTestCaseResults;
        return this;
    }

    public ClassExecutionSummaryTrackingFormatBuilder WithCompiledTestCaseResult(Action<CompiledTestCaseResultTrackingFormatBuilder> configureAction)
    {
        var builder = CompiledTestCaseResultTrackingFormatBuilder.Create();
        configureAction(builder);
        _compiledTestCaseResults.Add(builder.Build());
        return this;
    }


    public ClassExecutionSummaryTrackingFormat Build()
    {
        if (_compiledTestCaseResults.Count == 0) _compiledTestCaseResults.Add(CompiledTestCaseResultTrackingFormatBuilder.Create().Build());

        return new ClassExecutionSummaryTrackingFormat(
            _testClass ?? typeof(TestClass),
            _executionSettings ?? ExecutionSettingsTrackingFormatBuilder.Create().Build(),
            _compiledTestCaseResults);
    }

    public class TestClass
    {
    }
}