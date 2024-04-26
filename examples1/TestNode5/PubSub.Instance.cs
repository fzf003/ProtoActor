using System;
using System.Threading;
using System.Threading.Tasks;

public record Message(string message);


public class PubSub : InMemoryMessageQueue<Message>
{

}


public class PubSub1 : InMemoryMessageQueue<int>
{

}

public class ConsumerHandler : ISubscribeHandler<Message>, ISubscribeHandler<int>
{
    public void OnError(MessageContext<Message> context)
    {
        Console.WriteLine("OnError:" + context.Message + "--" + context.Exception.Message);
    }

    public void OnError(MessageContext<int> context)
    {
        Console.WriteLine("OnError:" + context.Message + "--" + context.Exception.Message);
    }

    public Task OnNext(MessageContext<Message> context, CancellationToken cancellationToken)
    {
        Console.WriteLine("OnNext:" + context.Message.message);
        return Task.CompletedTask;
    }

    public Task OnNext(MessageContext<int> context, CancellationToken cancellationToken)
    {
         Console.WriteLine("SubscribeHandler--"+this.GetHashCode());
        Console.WriteLine("OnNext:" + context.Message);
        throw new Exception("OOP");
        return Task.CompletedTask;
    }
}
