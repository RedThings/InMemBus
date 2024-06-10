using InMemBus.Hosting;
using InMemBus.MemoryBus;
using InMemBus.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace InMemBus;

public static class ServiceCollectionExtensions
{
    public static void UseInMemBus(this IServiceCollection services, Action<InMemBusConfiguration> configurationAction)
    {
        var memBusConfiguration = new InMemBusConfiguration(services);

        configurationAction.Invoke(memBusConfiguration);

        services.AddSingleton(memBusConfiguration);
        services.AddSingleton<IInMemBus, InMemoryBus>();
        services.AddSingleton<IWorkflowManager, WorkflowManager>();
        services.AddSingleton<InMemBusObserver>();
        services.AddHostedService<InMemBusBackgroundService>();
    }
}