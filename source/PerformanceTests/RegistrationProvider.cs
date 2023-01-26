using System;
using Autofac;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Registration;
using Serilog;
using Test.API;

namespace PerformanceTests;

public class RegistrationProvider : IProvideARegistrationCallback
{
    public void Register(ContainerBuilder builder)
    {
        // These registrations will be used by Sailfish's internal DI container which
        // is necessary to resolve dependencies used by test classes.
        // Additionally, there are various MediatR handlers that can be overriden
        // using these additional registrations.
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
    }
}