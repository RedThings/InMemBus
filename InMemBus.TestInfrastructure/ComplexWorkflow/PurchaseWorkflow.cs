namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public class PurchaseWorkflow(IInMemBus inMemBus, TestDataAsserter testDataAsserter) : InMemBusWorkflow<ItemsPurchasedEvent>,
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
        PurchaseId = message.PurchaseId;

        if (message.TestInstruction == "add-timeout")
        {
            AddTimeout(() => DoAThing(message.TestValue), TimeSpan.FromSeconds(5));
            return;
        }

        await inMemBus.SendAsync(new GetPurchasedItemsQuery(message.PurchaseId));

        State = $"Handled {message}";
    }

    public async Task HandleStepAsync(PurchasedItemsQueryResult message, CancellationToken cancellationToken)
    {
        PurchasedItems.AddRange(message.Items);

        if (PurchasedItems.Count < 1)
        {
            CompleteWorkflow();
            return;
        }

        var tasks = new Task[message.Items.Count];

        for (var i = 0; i < message.Items.Count; i++)
        {
            tasks[i] = inMemBus.SendAsync(new ValidatePurchasedItemCommand(message.PurchaseId, message.Items.ElementAt(i)));
        }

        await Task.WhenAll(tasks);

        State = $"Handled {message} - item count = ";
    }

    public async Task HandleStepAsync(PurchasedItemValidationSucceededEvent message, CancellationToken cancellationToken)
    {
        NumberOfSuccessfullyValidatedPurchasedItems++;

        if (NumberOfSuccessfullyValidatedPurchasedItems < PurchasedItems.Count)
        {
            return;
        }

        await inMemBus.SendAsync(new ShipItemsCommand(message.PurchaseId, ShippingId: Guid.NewGuid(), PurchasedItems));

        State = $"Handled {message}";
    }

    public async Task HandleStepAsync(PurchasedItemValidationFailedEvent message, CancellationToken cancellationToken)
    {
        await inMemBus.PublishAsync(new PurchaseFailedEvent(message.PurchaseId, message.Reason));

        CompleteWorkflow();

        State = $"Handled {message}";
    }

    public async Task HandleStepAsync(ItemsShippedEvent message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        testDataAsserter.Add(message.PurchaseId);

        CompleteWorkflow();

        State = $"Handled {message}";
    }

    private async Task DoAThing(string value)
    {
        await Task.CompletedTask;
        testDataAsserter.Add(value);
        CompleteWorkflow();
    }
}