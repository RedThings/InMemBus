namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public class ShippingWorkflow(IInMemBus inMemBus) : InMemBusWorkflow<ShipItemsCommand>,
    IInMemBusWorkflowStep<ItemShippingPreparedEvent>
{
    private ShipItemsCommand? originalMessage;
    private readonly List<ItemShippingPreparedEvent> shippingPreparedEvents = [];

    public override async Task HandleStartAsync(ShipItemsCommand message, CancellationToken cancellationToken)
    {
        originalMessage = message;

        var tasks = new Task[message.Items.Count];

        for (var i = 0; i < message.Items.Count; i++)
        {
            tasks[i] = inMemBus.SendAsync(new PrepareToShipCommand(message.PurchaseId, message.ShippingId, message.Items.ElementAt(i)));
        }

        await Task.WhenAll(tasks);
    }

    public async Task HandleStepAsync(ItemShippingPreparedEvent message, CancellationToken cancellationToken)
    {
        shippingPreparedEvents.Add(message);

        if (originalMessage != null && shippingPreparedEvents.Count < originalMessage.Items.Count)
        {
            return;
        }

        await inMemBus.PublishAsync(new ItemsShippedEvent(message.PurchaseId));

        CompleteWorkflow();
    }
}