namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public class ValidatePurchasedItemCommandHandler(IInMemBus inMemBus) : IInMemBusMessageHandler<ValidatePurchasedItemCommand>
{
    public async Task HandleAsync(ValidatePurchasedItemCommand message, CancellationToken cancellationToken)
    {
        await Delayer.DelayAsync();

        // todo: for tests purposes have an extra field somehow that invalidates

        await inMemBus.PublishAsync(new PurchasedItemValidationSucceededEvent(message.PurchaseId, message.PurchasedItem.PurchasedItemId));
    }
}