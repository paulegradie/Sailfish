using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Execution;
using Sailfish.TestAdapter.Discovery;

namespace Sailfish.TestAdapter.Execution;

internal static class TestExecution
{
    public static void ExecuteTests(List<TestCase> testCases, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken)
    {
        var builder = new ContainerBuilder();
        builder.CreateTestAdapterRegistrationContainerBuilder();

        var refTestType = RetrieveReferenceTypeForTestProject(testCases);

        SailfishTypeRegistrationUtility.InvokeRegistrationProviderCallbackAdapter(
                builder,
                refTestType,
                cancellationToken)
            .Wait(cancellationToken);

        var container = builder.Build();

        container.Resolve<ITestAdapterExecutionProgram>().Run(testCases, frameworkHandle, cancellationToken);
    }

    private static Type RetrieveReferenceTypeForTestProject(IReadOnlyCollection<TestCase> testCases)
    {
        var assembly = Assembly.LoadFile(testCases.First().Source);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
        var testTypeTrait = testCases.First().Traits.Single(trait => trait.Name == TestCaseItemCreator.TestTypeFullName);
        var testTypeFullName = testTypeTrait.Value;
        var refTestType = assembly.GetType(testTypeFullName, true, true);
        if (refTestType is null) throw new Exception("First test type was null when starting test execution");
        return refTestType;
    }
}