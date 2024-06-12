using Microsoft.Extensions.Logging;

namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public class PurchaseWorkflow(ILogger<PurchaseWorkflow> logger, IInMemBus inMemBus, TestDataAsserter testDataAsserter) : InMemBusWorkflow<ItemsPurchasedEvent>,
    IInMemBusWorkflowStep<PurchasedItemsQueryResult>,
    IInMemBusWorkflowStep<PurchasedItemValidationSucceededEvent>,
    IInMemBusWorkflowStep<PurchasedItemValidationFailedEvent>,
    IInMemBusWorkflowStep<ItemsShippedEvent>
{
    //private readonly Stopwatch sw = new();
    public List<PurchasedItem> PurchasedItems { get; set; } = [];
    public int NumberOfSuccessfullyValidatedPurchasedItems { get; set; }
    public Guid PurchaseId { get; set; }
    public string State { get; set; } = string.Empty;

    public override async Task HandleStartAsync(ItemsPurchasedEvent message, CancellationToken cancellationToken)
    {
        //testDataAsserter.IncrementCounter();

        //sw.Start();

        await Task.CompletedTask;

        PurchaseId = message.PurchaseId;

        if (message.TestInstruction == "add-timeout")
        {
            AddTimeout(() => DoAThing(message.TestValue), TimeSpan.FromSeconds(5));
            return;
        }

        inMemBus.Send(new GetPurchasedItemsQuery(message.PurchaseId));

        State = $"Handled {message}";
    }

    public async Task HandleStepAsync(PurchasedItemsQueryResult message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        PurchasedItems.AddRange(message.Items);

        if (PurchasedItems.Count < 1)
        {
            CompleteWorkflow();
            return;
        }

        foreach (var purchasedItem in message.Items)
        {
            inMemBus.Send(new ValidatePurchasedItemCommand(message.PurchaseId, purchasedItem));
        }

        State = $"Handled {message} - item count = ";
    }

    public async Task HandleStepAsync(PurchasedItemValidationSucceededEvent message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        NumberOfSuccessfullyValidatedPurchasedItems++;

        if (NumberOfSuccessfullyValidatedPurchasedItems < PurchasedItems.Count)
        {
            return;
        }

        //logger.LogInformation("Workflow {n} handled all PurchasedItemValidationSucceededEvent", message.PurchaseId);

        inMemBus.Send(new ShipItemsCommand(message.PurchaseId, ShippingId: Guid.NewGuid(), PurchasedItems));

        State = $"Handled {message}";
    }

    public async Task HandleStepAsync(PurchasedItemValidationFailedEvent message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        inMemBus.Publish(new PurchaseFailedEvent(message.PurchaseId, message.Reason));

        CompleteWorkflow();

        State = $"Handled {message}";
    }

    public async Task HandleStepAsync(ItemsShippedEvent message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        testDataAsserter.Add(message.PurchaseId);

        CompleteWorkflow();

        // sw.Stop();

        // logger.LogInformation("Counter is at {n} for {id}", testDataAsserter.GetCounterValue(), message.PurchaseId);

        //if (sw.ElapsedMilliseconds > 500)
        //{
        //    logger.LogInformation("Workflow completed in > 500ms. In this case it was {ms}ms", sw.ElapsedMilliseconds);
        //}

        State = $"Handled {message}";
    }

    private async Task DoAThing(string value)
    {
        await Task.CompletedTask;
        testDataAsserter.Add(value);
        CompleteWorkflow();
    }
}