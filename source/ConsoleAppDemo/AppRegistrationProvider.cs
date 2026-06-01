using Demo.API;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PerformanceTestingUserInvokedConsoleApp.CustomHandlerOverrideExamples;
using Sailfish.Analysis.Ai;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Registration;
using PerformanceTests.Skipper;

namespace PerformanceTestingUserInvokedConsoleApp;

public class AppRegistrationProvider : IRegisterSailfishServices
{
    public async Task RegisterAsync(IServiceCollection services, CancellationToken cancellationToken = default)
    {
        services.AddTransient<WebApplicationFactory<DemoApp>>();
        services.AddTransient<INotificationHandler<TestRunCompletedNotification>, TestRunCompletedNotificationHandler>();
        services.AddTransient<ICloudWriter, CloudWriter>();

        // Reference agentic Skipper: drives the `claude` CLI to read the code under test and explain regressions.
        // Registering an ISailfishAgent is the only step needed to light up the AI analysis layer.
        services.AddSingleton<ISailfishAgent, ClaudeAgentModelProvider>();

        await Task.Yield();
    }
}
