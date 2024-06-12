using System.Text.Json;
using InMemBus;
using InMemBus.TestInfrastructure;
using InMemBus.TestInfrastructure.ComplexWorkflow;
using InMemBus.Workflow;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

//using var fileStream = new FileStream("C:\\InMemBus\\log.txt", FileMode.OpenOrCreate, FileAccess.Write);
//using var streamWriter = new StreamWriter(fileStream);

//Console.SetOut(streamWriter);

builder.Services.UseInMemBus(config =>
{
    config.SetMaximumHandlingConcurrency(2000);
    config.UseDebugMode();

    TestHelper.Instance.ConfigureComplexWorkflow(config);
});

builder.Services.AddSingleton<TestDataAsserter>();

var app = builder.Build();

app.MapGet("livez", (HttpContext _) => Results.Ok());

app.MapPost("complete-purchase/{purchaseId:guid}",
    ([FromServices] IInMemBus inMemBus, [FromRoute] Guid purchaseId) =>
    {
        inMemBus.Publish(new ItemsPurchasedEvent(purchaseId));

        return Results.Accepted();
    });

app.MapGet(
    "purchase-status/{purchaseId:guid}",
    ([FromServices] TestDataAsserter testDataAsserter, [FromRoute] Guid purchaseId) => 
    testDataAsserter.Assert(purchaseId) ? Results.Ok() : Results.NotFound());

app.MapGet(
    "workflow/{purchaseId:guid}",
    ([FromServices] IWorkflowManager workflowManager, [FromRoute] Guid purchaseId) =>
    {
        var workflow = workflowManager.GetWorkflowForDebugging(purchaseId);

        if (workflow == null)
        {
            return Results.NotFound();
        }

        var type = workflow.GetType();
        var json = JsonSerializer.Serialize(workflow, type);

        return Results.Ok(json);
    });

app.Run();
