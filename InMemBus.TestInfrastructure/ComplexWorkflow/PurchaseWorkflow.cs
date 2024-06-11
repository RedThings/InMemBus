using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public class PurchaseWorkflow(ILogger<PurchaseWorkflow> logger, IInMemBus inMemBus, TestDataAsserter testDataAsserter) : InMemBusWorkflow<ItemsPurchasedEvent>,
    IInMemBusWorkflowStep<PurchasedItemsQueryResult>,
    IInMemBusWorkflowStep<PurchasedItemValidationSucceededEvent>,
    IInMemBusWorkflowStep<PurchasedItemValidationFailedEvent>,
    IInMemBusWorkflowStep<ItemsShippedEvent>
{
    private readonly Stopwatch sw = new();
    private readonly List<PurchasedItem> purchasedItems = [];
    private int numberOfSuccessfullyValidatedPurchasedItems;
    
    public override async Task HandleStartAsync(ItemsPurchasedEvent message, CancellationToken cancellationToken)
    {
        testDataAsserter.IncrementCounter();

        sw.Start();

        await Task.CompletedTask;

        if (message.TestInstruction == "add-timeout")
        {
            AddTimeout(() => DoAThing(message.TestValue), TimeSpan.FromSeconds(5));
            return;
        }
        
        inMemBus.Send(new GetPurchasedItemsQuery(message.PurchaseId));
    }

    public async Task HandleStepAsync(PurchasedItemsQueryResult message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        purchasedItems.AddRange(message.Items);

        foreach (var purchasedItem in message.Items)
        {
            inMemBus.Send(new ValidatePurchasedItemCommand(message.PurchaseId, purchasedItem));
        }
    }

    public async Task HandleStepAsync(PurchasedItemValidationSucceededEvent message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        numberOfSuccessfullyValidatedPurchasedItems++;

        if (numberOfSuccessfullyValidatedPurchasedItems < purchasedItems.Count)
        {
            return;
        }

        logger.LogInformation("Workflow {n} handled all PurchasedItemValidationSucceededEvent", message.PurchaseId);

        inMemBus.Send(new ShipItemsCommand(message.PurchaseId, ShippingId: Guid.NewGuid(), purchasedItems));
    }

    public async Task HandleStepAsync(PurchasedItemValidationFailedEvent message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        inMemBus.Publish(new PurchaseFailedEvent(message.PurchaseId, message.Reason));

        CompleteWorkflow();
    }

    public async Task HandleStepAsync(ItemsShippedEvent message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        testDataAsserter.Add(message.PurchaseId);

        CompleteWorkflow();

        sw.Stop();

        logger.LogInformation("Counter is at {n} for {id}", testDataAsserter.GetCounterValue(), message.PurchaseId);

        if (sw.ElapsedMilliseconds > 500)
        {
            logger.LogInformation("Workflow completed in > 500ms. In this case it was {ms}ms", sw.ElapsedMilliseconds);
        }
    }

    private async Task DoAThing(string value)
    {
        await Task.CompletedTask;
        testDataAsserter.Add(value);
        CompleteWorkflow();
    }
}