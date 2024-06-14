using System.Diagnostics;
using InMemBus.Tests.TestInfrastructure;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace InMemBus.Tests;

public class LoadTests(ITestOutputHelper testOutputHelper) : IDisposable
{
    private int jsonLogCount;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly HttpClient httpClient = new();

    public void Dispose()
    {
        cancellationTokenSource.Cancel();

        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData(1, 1000, 10)]
    [InlineData(10, 500, 20)]
    [InlineData(20, 500, 20)]
    [InlineData(30, 500, 20)]
    [InlineData(40, 500, 20)]
    [InlineData(50, 500, 20)]
    [InlineData(60, 500, 20)]
    [InlineData(70, 500, 20)]
    [InlineData(80, 500, 20)]
    [InlineData(90, 500, 20)]
    [InlineData(100, 500, 20)]
    public async Task AtScale_WorkflowShouldComplete_InAReasonableTime(int users, int intervalMs, int durationSeconds)
    {
        // Arrange
        const string pingUrl = "http://localhost:5292/livez";
        using var pingResponse = await httpClient.GetAsync(pingUrl);

        if (!pingResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Url {pingUrl} could not be reached. Test will not run.");
        }
        
        const string testTitle = "Complex Workflow";
        var sink = new CustomerNBomberReportingSink(testTitle, testOutputHelper);

        var runner = NBomberRunner
            .RegisterScenarios(GetScenario(testTitle, users, intervalMs, durationSeconds))
            .WithReportingSinks(sink)
            .WithLicense(Environment.GetEnvironmentVariable("NBOMBER_LICENSE"));

        var intervalFactor = 1000 / intervalMs;
        var expectedMeanMaximum = 600 * users * intervalFactor;

        // Act
        runner.Run();

        // Assert
        var stat = sink.GetStat();

        if (stat.AllFailCount > 0)
        {
            throw new Exception($"Fail count was {stat.AllFailCount}");
        }

        var actualMean = stat.Ok.Latency.MeanMs;

        testOutputHelper.WriteLine("Expected mean max = {0}ms, actual mean = {1}ms", expectedMeanMaximum, actualMean);

        Assert.True(expectedMeanMaximum > actualMean);
    }

    private ScenarioProps GetScenario(
        string testTitle,
        int users,
        int intervalMs,
        int durationSeconds)
    {
        var scenario = Scenario.Create(testTitle, async _ =>
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var purchaseId = Guid.NewGuid();

                var postUrl = $"http://localhost:5292/complete-purchase/{purchaseId}";

                using var postRequest = Http.CreateRequest("POST", postUrl);
                var postResponse = await Http.Send(httpClient, postRequest);
                
                if (postResponse.IsError)
                {
                    testOutputHelper.WriteLine("!!! Test failed due to http error !!!");
                    return Response.Fail();
                }

                var getUrl = $"http://localhost:5292/purchase-status/{purchaseId}";
                var attempts = 0;
                const int maxAttempts = 10;
                const int interval = 500;

                while (attempts < maxAttempts)
                {
                    using var getRequest = Http.CreateRequest("GET", getUrl);
                    var getResponse = await Http.Send(httpClient, getRequest);

                    if (!getResponse.IsError)
                    {
                        sw.Stop();
                        // testOutputHelper.WriteLine($"******** Workflow completed in {sw.ElapsedMilliseconds}ms ******");
                        return Response.Ok();
                    }

                    await Task.Delay(interval);
                    attempts++;
                }

                sw.Stop();
                //testOutputHelper.WriteLine($"*!!***** Workflow {purchaseId} failed in {sw.ElapsedMilliseconds}ms ***!!*");

                if (jsonLogCount < 4)
                {
                    jsonLogCount++;
                    using var workflowRequest = Http.CreateRequest("GET", $"http://localhost:5292/workflow/{purchaseId}");
                    var workflowResponse = await Http.Send(httpClient, workflowRequest);

                    if (workflowResponse.IsError)
                    {
                        testOutputHelper.WriteLine($"Getting failed workflow failed. Response was {workflowResponse.StatusCode}");
                    }
                    else
                    {
                        testOutputHelper.WriteLine("Failed workflow exists:");
                        testOutputHelper.WriteLine("");
                        testOutputHelper.WriteLine(await workflowResponse.Payload.Value.Content.ReadAsStringAsync());
                        testOutputHelper.WriteLine("");
                    }
                }

                return Response.Fail();
            }
            catch (Exception ex)
            {
                testOutputHelper.WriteLine($"!!!!!!!!!!!!!!! CAUGHT: {ex.Message} {ex.InnerException?.Message} !!!!!!!!!!!!!!!!!");
                return Response.Fail();
            }
        });

        return scenario
            .WithoutWarmUp()
            .WithLoadSimulations(Simulation.Inject(users, TimeSpan.FromMilliseconds(intervalMs), TimeSpan.FromSeconds(durationSeconds)));
    }
}