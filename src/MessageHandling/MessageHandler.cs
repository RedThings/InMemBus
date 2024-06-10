using InMemBus.MemoryBus;
using InMemBus.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InMemBus.MessageHandling;

internal class MessageHandler<TMessage>(
    ILogger<MessageHandler<TMessage>> logger,
    InMemBusConfiguration memBusConfiguration,
    WorkflowsConfiguration workflowsConfiguration,
    IWorkflowManager workflowManager,
    IInMemBus inMemBus)
    where TMessage : class
{
    public async Task HandleAsync(IServiceProvider currentScopeServiceProvider, Message message, CancellationToken cancellationToken)
    {
        var messageType = typeof(TMessage);
        var workflowConfigurations = workflowsConfiguration.GetWorkflowConfigurations<TMessage>();
        var workflowTaskFuncs = new List<Func<Task>>(workflowConfigurations.Count);
        var workflowAvailableOrCanSkipOverRequeue = workflowConfigurations.Count < 1;

        foreach (var workflowConfiguration in workflowConfigurations)
        {
            var step = workflowConfiguration.GetStepForMessage(typeof(TMessage));

            if (step.IsStarting)
            {
                var workflowCreationResult = workflowManager.TryCreateNewWorkflow<TMessage>(currentScopeServiceProvider, message.Payload, step);
                workflowAvailableOrCanSkipOverRequeue = workflowCreationResult.Success;

                if (!workflowAvailableOrCanSkipOverRequeue)
                {
                    break;
                }

                workflowTaskFuncs.Add(async () =>
                {
                    await workflowCreationResult.Workflow.HandleStartAsync((TMessage) message.Payload, cancellationToken).ConfigureAwait(false);

                    workflowCreationResult.ProcessSuccessfullyHandledMessage();
                });
            }
            else
            {
                var workflowAccessResult = workflowManager.TryGetWorkflowForStep<TMessage>(message.Payload, step);
                workflowAvailableOrCanSkipOverRequeue = workflowAccessResult.Success;

                if (!workflowAvailableOrCanSkipOverRequeue)
                {
                    break;
                }

                workflowTaskFuncs.Add(async () =>
                {
                    await workflowAccessResult.WorkflowStep.HandleStepAsync((TMessage) message.Payload, cancellationToken).ConfigureAwait(false);

                    workflowAccessResult.ProcessSuccessfullyHandledMessage();
                });
            }
        }

        if (!workflowAvailableOrCanSkipOverRequeue)
        {
            // TODO: poison message handling - for example, if you get the scatter-gather calc wrong, this message will end up being replayed endlessly
            logger.LogInformation("Message {type} is being handled, but the handling of the message has resulted in an unavailable workflow - no handling will "
                                  + "take place and the message will be requeued.", messageType);

            workflowTaskFuncs.Clear();
            inMemBus.Requeue(message);

            return;
        }

        var plainMessageHandlers = currentScopeServiceProvider.GetServices<IInMemBusMessageHandler<TMessage>>().ToArray();

        var tasks = new List<Task>(plainMessageHandlers.Length + workflowConfigurations.Count);

        foreach (var workflowTaskFunc in workflowTaskFuncs)
        {
            var workflowTask = workflowTaskFunc.Invoke();
            tasks.Add(HandleWithRetriesAsync(messageType, () => workflowTask));
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