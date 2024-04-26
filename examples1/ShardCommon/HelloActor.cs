



namespace ShardCommon;

public record PayLoadEvent(string Message);

public record RequestMessageEvent(PID request, string Message);

public record ResponseMessageEvent(string Message);



class ProcessActor : IActor
{
    public static Props CreateProps(ILoggerFactory loggerFactory) => Props.FromProducer(() => new ProcessActor(loggerFactory))
                                                                          .WithChildSupervisorStrategy(new OneForOneStrategy((pid, reason) => SupervisorDirective.Resume, 3, TimeSpan.FromMinutes(5)));
    private readonly ILogger _logger;
    public ProcessActor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ProcessActor>();
    }
    public Task ReceiveAsync(IContext context)
    {
        return context.Message switch
        {
            Started started => InitAsync(context),
            RequestMessageEvent requestMessageEvent => ProcessRequestMessageEventAsync(requestMessageEvent, context),
            Stopped stopped => StoppedAsync(context),
            _ => Task.CompletedTask
        };
    }


    Task StoppedAsync(IContext context)
    {
        this._logger.LogInformation("ProcessActor 停止....");
        return Task.CompletedTask;
    }

    Task ProcessRequestMessageEventAsync(RequestMessageEvent requestMessageEvent, IContext context)
    {
        this._logger.LogInformation($"ProcessActor 收到消息:{requestMessageEvent}");

        //throw new Exception("ProcessActor 出现异常....");

        if (requestMessageEvent.request is not null)
        {
            context.Send(requestMessageEvent.request, new ResponseMessageEvent($"ProcessActor 收到消息并处理完成:{requestMessageEvent.Message}"));

            return Task.CompletedTask;
        }

        context.Respond(new ResponseMessageEvent($"ProcessActor 收到消息并处理完成:{requestMessageEvent.Message}"));

        return Task.CompletedTask;
    }

    Task InitAsync(IContext context)
    {

        this._logger.LogInformation("ProcessActor 启动....");

        return Task.CompletedTask;
    }

}


public class LoggingRootDecorator : ActorContextDecorator
{

    private readonly string _loggerName;
    public LoggingRootDecorator(IContext context, string name) : base(context)
    {
        this._loggerName = name;
    }

    public override async Task<T> RequestAsync<T>(PID target, object message, CancellationToken ct)
    {
        Console.WriteLine($"{_loggerName} : Enter RequestAsync");
        var res = await base.RequestAsync<T>(target, message, ct);
        Console.WriteLine($"{_loggerName} : Exit RequestAsync");

        return res;
    }
}


public class HelloActor : IActor
{

    public static Props CreateProps(ILoggerFactory loggerFactory) => Props.FromProducer(() => new HelloActor(loggerFactory));

    private readonly ILogger _logger;
    private readonly ILoggerFactory loggerFactory;

    PID? processactorPid = default;



    public HelloActor(ILoggerFactory _loggerFactory)
    {
        this.loggerFactory = _loggerFactory;

        _logger = _loggerFactory.CreateLogger<HelloActor>();
    }
    public Task ReceiveAsync(IContext context)
    {
        return context.Message switch
        {
            Started started => InitAsync(context),
            PayLoadEvent payLoadEvent => ProcessPayLoadEventAsync(payLoadEvent, context),
            RequestMessageEvent requestMessageEvent => ProcessRequestMessageEventAsync(requestMessageEvent, context),
            ReceiveTimeout receiveTimeout => ReceiveTimeOutAsync(receiveTimeout, context),
            _ => Task.CompletedTask
        };
    }

    Task ReceiveTimeOutAsync(ReceiveTimeout receiveTimeout, IContext context)
    {
        Console.WriteLine(context.Message + "接受超时!");

        return Task.CompletedTask;
    }

    Task ProcessRequestMessageEventAsync(RequestMessageEvent requestMessageEvent, IContext context)
    {

        this._logger.LogInformation($"HelloActor 收到消息:self:{context.Self.Id} Sender:{context.Sender?.Id} Self Address:{context.Self.Address} Sender Address: {context.Sender?.Address}");
        if (context.Sender is not null)
        {
            context.Respond(new ResponseMessageEvent($"HelloActor 收到消息:self:{context.Self.Id} Sender:{context.Sender?.Id} Self Address:{context.Self.Address} Sender Address: {context.Sender?.Address}"));
        }
        else
        {

            this._logger.LogInformation($"HelloActor 收到消息:{requestMessageEvent}无回复消息！！");
        }
        /* return context.RequestAsync<ResponseMessageEvent>(processactorPid, requestMessageEvent)
                            .ToPipe(success: response =>
                            {
                                this._logger.LogInformation($"返回:HelloActor 收到消息:{requestMessageEvent} 并处理完成:{response}");
                                return response;
                            }, failure: exception =>
                            {
                                this._logger.LogError(exception, "ProcessActor 出现异常....");
                                return exception;
                            });*/

        return Task.CompletedTask;
    }

    Task ProcessPayLoadEventAsync(PayLoadEvent payLoadEvent, IContext context)
    {
        this._logger.LogInformation($"HelloActor 收到消息:{payLoadEvent}");

        return Task.CompletedTask;
    }

    Task InitAsync(IContext context)
    {

        processactorPid = context.SpawnNamed(ProcessActor.CreateProps(this.loggerFactory), "processactor-1");

        this._logger.LogInformation("HelloActor 启动....");

        //context.SetReceiveTimeout(TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }
}