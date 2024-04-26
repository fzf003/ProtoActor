using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class SubscribeHostedService<T> : IHostedService
{
    readonly IMessageQueue<T> _messageQueue;
    readonly ISubscribeHandler<T> _subscribeHandler;
    readonly IServiceProvider _serviceProvider;
    readonly ILogger<SubscribeHostedService<T>> _logger;

    public SubscribeHostedService(IMessageQueue<T> messageQueue, ISubscribeHandler<T> subscribeHandler, IServiceProvider serviceProvider, ILogger<SubscribeHostedService<T>> logger)
    {
        if (messageQueue == null || subscribeHandler == null || serviceProvider == null || logger == null)
        {
            throw new ArgumentNullException();
        }

        _messageQueue = messageQueue;
        _subscribeHandler = subscribeHandler;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting subscription...");
            _ = _messageQueue.SubscribeAsync(_subscribeHandler, cancellationToken);
            _logger.LogInformation("Subscription started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start subscription.");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Stopping subscription...");
            if (_messageQueue is not null)
            {
                await _messageQueue.DisposeAsync().ConfigureAwait(false);
            }
            _logger.LogInformation("Subscription stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop subscription.");
     
            throw;
        }
    }
}


