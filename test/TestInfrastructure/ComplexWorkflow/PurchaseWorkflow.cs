namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public class PurchaseWorkflow(IInMemBus inMemBus, TestDataAsserter testDataAsserter) : InMemBusWorkflow<ItemsPurchasedEvent>,
    IInMemBusWorkflowStep<PurchasedItemsQueryResult>,
    IInMemBusWorkflowStep<PurchasedItemValidationSucceededEvent>,
    IInMemBusWorkflowStep<PurchasedItemValidationFailedEvent>,
    IInMemBusWorkflowStep<ItemsShippedEvent>
{
    private readonly List<PurchasedItem> purchasedItems = [];
    private int numberOfSuccessfullyValidatedPurchasedItems;
    
    public override async Task HandleStartAsync(ItemsPurchasedEvent message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        
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
    }
}