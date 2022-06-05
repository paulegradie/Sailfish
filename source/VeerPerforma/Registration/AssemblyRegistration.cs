using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VeerPerforma.Executor;
using VeerPerforma.Executor.Prep;

namespace VeerPerforma.Registration;

public static class AssemblyRegistrationExtensionMethods
{
    public static void RegisterVeerPerformaTypes(this ContainerBuilder builder)
    {
        builder.RegisterModule(new ExecutorModule());
    }

    public static void RegisterPerformanceTypes(this ContainerBuilder builder, params Type[] sourceTypes)
    {
        var testCollector = new TestCollector();
        var allPerfTypes = testCollector.CollectTestTypes(sourceTypes);
        builder.RegisterTypes(allPerfTypes);
    }

    public static void RegisterPerformanceTypes(this IServiceCollection serviceCollection)
    {
        throw new NotImplementedException();
    }

    public static void RegisterVeerPerformaTypes(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<ILogger>(
            c =>
            {
                return new LoggerConfiguration()
                    .CreateLogger();
            });

        serviceCollection.AddTransient<VeerPerformaExecutor>();
        serviceCollection.AddTransient<ITestExecutor, TestExecutor>();
        serviceCollection.AddTransient<ITestFilter, TestFilter>();
        serviceCollection.AddTransient<ITestListValidator, TestListValidator>();
        serviceCollection.AddTransient<ITestCollector, TestCollector>();
        serviceCollection.AddTransient<IParameterCombinationMaker, ParameterCombinationMaker>();
    }
}

public class ExecutorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.Register<ILogger>(
            (c, p) =>
            {
                return new LoggerConfiguration()
                    .CreateLogger();
            }).SingleInstance();

        builder.RegisterType<VeerPerformaExecutor>().AsSelf();
        builder.RegisterType<TestExecutor>().As<ITestExecutor>();
        builder.RegisterType<TestFilter>().As<ITestFilter>();
        builder.RegisterType<TestListValidator>().As<ITestListValidator>();
        builder.RegisterType<TestCollector>().As<ITestCollector>();
        builder.RegisterType<ParameterCombinationMaker>().As<IParameterCombinationMaker>();
    }
}