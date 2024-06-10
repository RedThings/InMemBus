namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public class PrepareToShipCommandHandler(IInMemBus inMemBus) : IInMemBusMessageHandler<PrepareToShipCommand>
{
    public async Task HandleAsync(PrepareToShipCommand message, CancellationToken cancellationToken)
    {
        await Delayer.DelayAsync();

        inMemBus.Publish(new ItemShippingPreparedEvent(message.PurchaseId, message.ShippingId, message.Item.PurchasedItemId));
    }
}