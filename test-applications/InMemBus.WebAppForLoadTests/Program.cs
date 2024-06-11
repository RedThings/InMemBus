using InMemBus;
using InMemBus.TestInfrastructure;
using InMemBus.TestInfrastructure.ComplexWorkflow;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

//using var fileStream = new FileStream("C:\\InMemBus\\log.txt", FileMode.OpenOrCreate, FileAccess.Write);
//using var streamWriter = new StreamWriter(fileStream);

//Console.SetOut(streamWriter);

builder.Services.UseInMemBus(config =>
{
    config.SetMaximumHandlingConcurrency(5000);

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

app.Run();
