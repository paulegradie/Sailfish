using Demo.API;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Registration;
using Tests.E2E.ExceptionHandling.Handlers;

namespace Tests.E2E.ExceptionHandling;

public class E2ETestExceptionHandlingProvider : IRegisterSailfishServices
{
    public async Task RegisterAsync(IServiceCollection services, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        services.AddTransient<WebApplicationFactory<DemoApp>>();
        services.AddSingleton<ILogger>(Log.Logger);
        services.AddTransient<INotificationHandler<TestRunCompletedNotification>, TestRunCompletedNotificationHandler>();
    }
}
