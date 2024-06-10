namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public class ShippingWorkflow(IInMemBus inMemBus) : InMemBusWorkflow<ShipItemsCommand>,
    IInMemBusWorkflowStep<ItemShippingPreparedEvent>
{
    private ShipItemsCommand? originalMessage;
    private readonly List<ItemShippingPreparedEvent> shippingPreparedEvents = [];

    public override async Task HandleStartAsync(ShipItemsCommand message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        originalMessage = message;

        foreach (var purchasedItem in message.Items)
        {
            inMemBus.Send(new PrepareToShipCommand(message.PurchaseId, message.ShippingId, purchasedItem));
        }
    }

    public async Task HandleStepAsync(ItemShippingPreparedEvent message, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        shippingPreparedEvents.Add(message);

        if (originalMessage != null && shippingPreparedEvents.Count < originalMessage.Items.Count)
        {
            return;
        }

        inMemBus.Publish(new ItemsShippedEvent(message.PurchaseId));

        CompleteWorkflow();
    }
}