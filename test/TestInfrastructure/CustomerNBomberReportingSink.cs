using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using Xunit.Abstractions;

namespace InMemBus.Tests.TestInfrastructure;

public class CustomerNBomberReportingSink(string testTitle, ITestOutputHelper testOutputHelper) : IReportingSink
{
    private ScenarioStats? finalStatForThisTest;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public async Task Init(IBaseContext context, IConfiguration infraConfig)
    {
        await Task.CompletedTask;

        testOutputHelper.WriteLine("*** Load tests initializing ***");
    }

    public async Task Start()
    {
        await Task.CompletedTask;

        testOutputHelper.WriteLine("*** Load tests starting ***");
    }

    public async Task SaveRealtimeStats(ScenarioStats[] stats)
    {
        await Task.CompletedTask;

        testOutputHelper.WriteLine("*** Saving realtime stats (not used) ***");
    }

    public async Task SaveFinalStats(NodeStats stats)
    {
        await Task.CompletedTask;

        testOutputHelper.WriteLine("*** Final stats ***");

        var statsForThisTest = stats.ScenarioStats.Where(x => x.ScenarioName == testTitle).ToArray();

        if (statsForThisTest.Length is < 1 or > 1)
        {
            throw new Exception($"More than one stat for {testTitle} found. Count was {statsForThisTest.Length}");
        }

        var statForThisTest = statsForThisTest.Single();

        testOutputHelper.WriteLine("Test '{0}' has {1} successes and {2} failures. Of the successes, the mean was {3}ms",
            testTitle, statForThisTest.AllOkCount, statForThisTest.AllFailCount, statForThisTest.Ok.Latency.MeanMs);

        finalStatForThisTest = statForThisTest;
    }

    public async Task Stop()
    {
        await Task.CompletedTask;

        testOutputHelper.WriteLine("*** Stopping ***");
    }

    public string SinkName => "Single-test Automated Testing Sink";

    public ScenarioStats GetStat() => finalStatForThisTest ?? throw new Exception("Stats not available (yet)");
}