

using System.Threading.Channels;

public record UserInfo(string UserName, string Sex, DateTime DueAfter)
{
    public static UserInfo CreateUser(string UserName, string Sex, DateTime DueAfter)
    {
        return new UserInfo(UserName, Sex, DueAfter);
    }
}



public record InPutMessage(Guid Id, DateTime? Expires, string Headers, string Payload)
{
    public static InPutMessage Create(Guid Id, DateTime? Expires, string Headers, string Payload)
    {
        return new InPutMessage(Id, Expires, Headers, Payload);
    }

    public bool IsExpires()
    {
        return this.Expires < DateTime.Now;
    }
}

public record OutPutMessage(Guid Id, DateTime? Expires, IDictionary<string, string> Headers, string Payload)
{
    public static OutPutMessage Create(Guid Id, DateTime? Expires, IDictionary<string, string> Headers, string Payload)
    {
        return new OutPutMessage(Id, Expires, Headers, Payload);
    }
}



public interface ISubscribeHandler<T>
{
     public Task OnNext(T message);

     public void OnError(Exception ex, T message);
}


public class TestSubscribeHandler : ISubscribeHandler<InPutMessage>
{
    public void OnError(Exception ex, InPutMessage message)
    {
        
    }

    public async Task OnNext(InPutMessage message)
    {
        Console.WriteLine(message);
        await Task.CompletedTask;
    }
}




public abstract class PubSubService
{
    readonly Channel<InPutMessage> ProcessChannel;

    public PubSubService() : this(Channel.CreateUnbounded<InPutMessage>(new UnboundedChannelOptions
    {
        SingleWriter = true,
        SingleReader = false,
        AllowSynchronousContinuations = false
    }))
    {

    }

    public PubSubService(Channel<InPutMessage> channel)
    {
        ProcessChannel = channel;
    }

    public async Task SendAsync(InPutMessage incomingMessage, CancellationToken cancellationToken = default)
    {
        await ProcessChannel.Writer.WriteAsync(incomingMessage, cancellationToken);
    }

    protected ChannelReader<InPutMessage> OutPutReader => ProcessChannel.Reader;

    protected ChannelWriter<InPutMessage> InputWriter => ProcessChannel.Writer;

    public abstract void Subscribe(Func<InPutMessage, Task> func, Action<Exception, InPutMessage> OnError = null, CancellationToken cancellationToken = default);

}


public class UserPubSubService : PubSubService
{


    public override void Subscribe(Func<InPutMessage, Task> func, Action<Exception, InPutMessage> OnError = null, CancellationToken cancellationToken = default)
    {
        Task.Run(async () =>
       {
           await foreach (var item in this.OutPutReader.ReadAllAsync(cancellationToken).WithCancellation(cancellationToken))
           {
               try
               {
                   await func(item).ConfigureAwait(false);
                   
                   if(cancellationToken.IsCancellationRequested)
                   {
                       break;
                   }
               }
               catch (OperationCanceledException)
               {
                   //取消令牌
               }
               catch (Exception ex)
               {
                   if (OnError is not null)
                   {
                       OnError(ex, item);
                   }
               }
               finally
               {

               }
           }
       }, cancellationToken);
    }
}

public static class PubSubServiceExtensions
{
    public static PubSubService Subscribe(this PubSubService pubSubService, ISubscribeHandler<InPutMessage> subscribeHandler, CancellationToken cancellationToken = default)
    {
        pubSubService.Subscribe(subscribeHandler.OnNext, subscribeHandler.OnError, cancellationToken);
        return pubSubService;
    }

    
 }


/*
  UserPubSubService userPubSubService = new UserPubSubService();

var OnError = (Exception ex, InPutMessage message) =>
{
    Console.WriteLine(ex.Message);
};

var Process = (InPutMessage message) =>
{
    Console.WriteLine(message);

    return Task.CompletedTask;
};

userPubSubService.Subscribe(Process, OnError);



JsonSerializerOptions jsonSerializer = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

for (; ; )
{
    var jsonString = JsonSerializer.Serialize(UserInfo.CreateUser(Guid.NewGuid().ToString("N"), "男", DateTime.Now), jsonSerializer);
    var headers = new Dictionary<string, string>
    {
        {"Title", "发送信息"}
    };
    var serialized = JsonSerializer.Serialize(headers, jsonSerializer);
    var dic= JsonSerializer.Deserialize<IDictionary<string, string>>(serialized, jsonSerializer);
    foreach(var item in dic)
    {
        Console.WriteLine(item.Key+"--"+item.Value);
    }

    var message = InPutMessage.Create(Id: Guid.NewGuid(), Expires: DateTime.Now.AddHours(-1), Headers: serialized, jsonString: jsonString);
    Console.WriteLine(message.IsExpires());
    await userPubSubService.SendAsync(message).ConfigureAwait(false);
*/