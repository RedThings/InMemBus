using InMemBus.MemoryBus;
using InMemBus.Saga;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InMemBus.MessageHandling;

internal class MessageHandler<TMessage>(
    ILogger<MessageHandler<TMessage>> logger,
    InMemBusConfiguration memBusConfiguration,
    SagasConfiguration sagasConfiguration,
    ISagaManager sagaManager,
    IInMemBus inMemBus)
    where TMessage : class
{
    public async Task HandleAsync(IServiceProvider currentScopeServiceProvider, Message message, CancellationToken cancellationToken)
    {
        var messageType = typeof(TMessage);
        var sagaConfigurations = sagasConfiguration.GetSagaConfigurations<TMessage>();
        var sagaTaskFuncs = new List<Func<Task>>(sagaConfigurations.Count);
        var sagaAvailableOrCanSkipOverRequeue = sagaConfigurations.Count < 1;

        foreach (var sagaConfiguration in sagaConfigurations)
        {
            var step = sagaConfiguration.GetStepForMessage(typeof(TMessage));

            if (step.IsStarting)
            {
                var sagaCreationResult = sagaManager.TryCreateNewSaga<TMessage>(currentScopeServiceProvider, message.Payload, step);
                sagaAvailableOrCanSkipOverRequeue = sagaCreationResult.Success;

                if (!sagaAvailableOrCanSkipOverRequeue)
                {
                    break;
                }

                sagaTaskFuncs.Add(async () =>
                {
                    await sagaCreationResult.Saga.HandleStartAsync((TMessage) message.Payload, cancellationToken).ConfigureAwait(false);

                    sagaCreationResult.ProcessSuccessfullyHandledMessage();
                });
            }
            else
            {
                var sagaAccessResult = sagaManager.TryGetSagaForStep<TMessage>(message.Payload, step);
                sagaAvailableOrCanSkipOverRequeue = sagaAccessResult.Success;

                if (!sagaAvailableOrCanSkipOverRequeue)
                {
                    break;
                }

                sagaTaskFuncs.Add(async () =>
                {
                    await sagaAccessResult.SagaStep.HandleStepAsync((TMessage) message.Payload, cancellationToken).ConfigureAwait(false);

                    sagaAccessResult.ProcessSuccessfullyHandledMessage();
                });
            }
        }

        if (!sagaAvailableOrCanSkipOverRequeue)
        {
            // TODO: poison message handling - for example, if you get the scatter-gather calc wrong, this message will end up being replayed endlessly
            logger.LogInformation("Message {type} is being handled, but the handling of the message has resulted in an unavailable saga - no handling will "
                                  + "take place and the message will be requeued.", messageType);

            sagaTaskFuncs.Clear();
            inMemBus.Requeue(message);

            return;
        }

        var plainMessageHandlers = currentScopeServiceProvider.GetServices<IInMemBusMessageHandler<TMessage>>().ToArray();

        var tasks = new List<Task>(plainMessageHandlers.Length + sagaConfigurations.Count);

        foreach (var sagaTaskFunc in sagaTaskFuncs)
        {
            var sagaTask = sagaTaskFunc.Invoke();
            tasks.Add(HandleWithRetriesAsync(messageType, () => sagaTask));
        }

        foreach (var inMemBusMessageHandler in plainMessageHandlers)
        {
            tasks.Add(HandleWithRetriesAsync(messageType, () => inMemBusMessageHandler.HandleAsync((TMessage) message.Payload, cancellationToken)));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task HandleWithRetriesAsync(Type messageType, Func<Task> taskFunc)
    {
        var retryPattern = memBusConfiguration.GetRetryPattern(messageType);

        var maxAttempts = retryPattern.Length + 1;
        var currentAttempt = 0;

        while (true)
        {
            try
            {
                await taskFunc.Invoke().ConfigureAwait(false);
                break;
            }
            catch (Exception ex)
            {
                if (currentAttempt + 1 == maxAttempts)
                {
                    logger.LogError(ex, "Exception during attempt {attempt} of handling message {type}. Max attempts reached.", currentAttempt, messageType);
                    break;
                }

                var nextDelay = retryPattern[currentAttempt];

                logger.LogError(ex, "Exception during attempt {attempt} of handling message {type}. Next delay is {delay}ms.", currentAttempt, messageType, nextDelay);

                currentAttempt++;

                await Task.Delay(nextDelay).ConfigureAwait(false);
            }
        }
    }
}