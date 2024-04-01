

public record SubscriberMessage(PID publisher,string Message);


public class ChannelSubscriber : IActor
{
    public static Props CreateProps(ILoggerFactory loggerFactory) => Props.FromProducer(() => new ChannelSubscriber(loggerFactory));
    private readonly ILogger _logger;
    private readonly ILoggerFactory loggerFactory;
    public ChannelSubscriber(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
        this._logger = loggerFactory.CreateLogger<ChannelSubscriber>();
    }
    public Task ReceiveAsync(IContext context)
    {
        return context.Message switch
        {
            Started started => InitAsync(context),

            SubscriberMessage subscriber=>SubscriberMessageAsync(subscriber,context),

            _ => Task.CompletedTask
        };
    }

    Task SubscriberMessageAsync(SubscriberMessage subscriber, IContext context)
    {
       this._logger.LogInformation($"{subscriber}");
       return Task.CompletedTask;
    }

    Task InitAsync(IContext context)
    {
        this._logger.LogInformation("HelloActor 启动....");

        return Task.CompletedTask;
    }
}