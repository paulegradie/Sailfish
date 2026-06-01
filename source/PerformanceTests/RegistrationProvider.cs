using System.Threading;
using System.Threading.Tasks;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Analysis.Ai;
using Sailfish.Registration;
using PerformanceTests.Skipper;

namespace PerformanceTests;

public class RegistrationProvider : IRegisterSailfishServices
{
    public async Task RegisterAsync(IServiceCollection services, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        services.AddTransient<WebApplicationFactory<DemoApp>>();

        // Skipper AI analysis: register the agentic provider. Combined with "AiAnalysisSettings": { "Enabled": true }
        // in .sailfish.json, this lights up Skipper for the Test Adapter (dotnet test / Test Explorer) path.
        services.AddSingleton<ISailfishAgent, ClaudeAgentModelProvider>();
    }
}
