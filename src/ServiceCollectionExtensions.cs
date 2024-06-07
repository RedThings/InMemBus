using InMemBus.Hosting;
using InMemBus.MemoryBus;
using InMemBus.Saga;
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
        services.AddSingleton<ISagaManager, SagaManager>();
        services.AddSingleton<InMemBusObserver>();
        services.AddHostedService<InMemBusBackgroundService>();
    }
}