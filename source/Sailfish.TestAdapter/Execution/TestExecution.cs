using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Registration;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Execution;

internal static class TestExecution
{
    public static void ExecuteTests(List<TestCase> testCases, IFrameworkHandle? frameworkHandle, CancellationToken cancellationToken)
    {
        var builder = new ContainerBuilder();
        builder.CreateTestAdapterRegistrationContainerBuilder();

        var refTestType = RetrieveReferenceTypeForTestProject(testCases);

        SailfishTypeRegistrationUtility.InvokeRegistrationProviderCallbackMain(
                builder,
                new[] { refTestType },
                new[] { refTestType },
                cancellationToken)
            .Wait(cancellationToken);

        using var container = builder.Build();
        try
        {
            container.Resolve<ITestAdapterExecutionProgram>().Run(testCases, frameworkHandle, cancellationToken);
        }
        catch (Exception ex)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Error, ex.Message);
        }
    }

    private static Type RetrieveReferenceTypeForTestProject(IReadOnlyCollection<TestCase> testCases)
    {
        var assembly = Assembly.LoadFile(testCases.First().Source);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?

        var testTypeFullName = testCases
            .First()
            .GetPropertyHelper(SailfishTestTypeFullNameDefinition.SailfishTestTypeFullNameDefinitionProperty);

        var refTestType = assembly.GetType(testTypeFullName, true, true);
        if (refTestType is null) throw new Exception("First test type was null when starting test execution");
        return refTestType;
    }
}