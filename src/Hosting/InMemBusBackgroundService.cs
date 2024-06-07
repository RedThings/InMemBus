using InMemBus.MemoryBus;
using Microsoft.Extensions.Hosting;

namespace InMemBus.Hosting;

internal class InMemBusBackgroundService(InMemBusObserver inMemBusObserver) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) =>
        await inMemBusObserver.ExecuteAsync(stoppingToken);
}