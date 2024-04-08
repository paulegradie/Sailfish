using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Tests.Common.Builders;

public class ClassExecutionSummaryTrackingFormatBuilder
{
    private Type? testClass;
    private ExecutionSettingsTrackingFormat? executionSettings;
    private List<CompiledTestCaseResultTrackingFormat> compiledTestCaseResults = new();

    public static ClassExecutionSummaryTrackingFormatBuilder Create() => new();

    public ClassExecutionSummaryTrackingFormatBuilder WithTestClass(Type testClass)
    {
        this.testClass = testClass;
        return this;
    }

    public ClassExecutionSummaryTrackingFormatBuilder WithExecutionSettings(ExecutionSettingsTrackingFormat executionSettings)
    {
        this.executionSettings = executionSettings;
        return this;
    }

    public ClassExecutionSummaryTrackingFormatBuilder WithExecutionSettings(Action<ExecutionSettingsTrackingFormatBuilder> configureAction)
    {
        var builder = new ExecutionSettingsTrackingFormatBuilder();
        configureAction(builder);
        executionSettings = builder.Build();
        return this;
    }

    public ClassExecutionSummaryTrackingFormatBuilder WithCompiledTestCaseResults(List<CompiledTestCaseResultTrackingFormat> compiledTestCaseResults)
    {
        this.compiledTestCaseResults = compiledTestCaseResults;
        return this;
    }

    public ClassExecutionSummaryTrackingFormatBuilder WithCompiledTestCaseResult(Action<CompiledTestCaseResultTrackingFormatBuilder> configureAction)
    {
        var builder = CompiledTestCaseResultTrackingFormatBuilder.Create();
        configureAction(builder);
        compiledTestCaseResults.Add(builder.Build());
        return this;
    }


    public ClassExecutionSummaryTrackingFormat Build()
    {
        if (compiledTestCaseResults.Count == 0)
        {
            compiledTestCaseResults.Add(CompiledTestCaseResultTrackingFormatBuilder.Create().Build());
        }

        return new ClassExecutionSummaryTrackingFormat(
            testClass ?? typeof(TestClass),
            executionSettings ?? ExecutionSettingsTrackingFormatBuilder.Create().Build(),
            compiledTestCaseResults);
    }

    public class TestClass
    {
    }
}