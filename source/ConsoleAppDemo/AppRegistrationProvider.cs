using Demo.API;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PerformanceTestingUserInvokedConsoleApp.CustomHandlerOverrideExamples;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Registration;

namespace PerformanceTestingUserInvokedConsoleApp;

public class AppRegistrationProvider : IRegisterSailfishServices
{
    public async Task RegisterAsync(IServiceCollection services, CancellationToken cancellationToken = default)
    {
        services.AddTransient<WebApplicationFactory<DemoApp>>();
        services.AddTransient<INotificationHandler<TestRunCompletedNotification>, TestRunCompletedNotificationHandler>();
        services.AddTransient<ICloudWriter, CloudWriter>();
        await Task.Yield();
    }
}
