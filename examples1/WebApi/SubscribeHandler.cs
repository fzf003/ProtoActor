public class ConsumerHandler : ISubscribeHandler<string>, ISubscribeHandler<int>, ISubscribeHandler<Message>
{
    readonly IDBFactory _dBFactory;
    public ConsumerHandler(IDBFactory dBFactory )
    {
        this._dBFactory = dBFactory;
    }
    public void OnError(MessageContext<string> context)
    {
        Console.WriteLine("OnError:" + context.Message + "--" + context.Exception.Message);
    }

    public Task OnNext(MessageContext<string> context, CancellationToken cancellationToken)
    {
        Console.WriteLine("OnNext:" + context.Message);
        return Task.CompletedTask;
    }


    public void OnError(MessageContext<int> context)
    {
        Console.WriteLine("OnError:" + context.Message + "--" + context.Exception.Message);
    }



    public Task OnNext(MessageContext<int> context, CancellationToken cancellationToken)
    {
        Console.WriteLine("SubscribeHandler--" + this.GetHashCode());
        Console.WriteLine("OnNext:" + context.Message);
        throw new Exception("OOP");
        return Task.CompletedTask;
    }

    public Task OnNext(MessageContext<Message> context, CancellationToken cancellationToken)
    {
        this._dBFactory.CreateDB();
        Console.WriteLine("OnNext:" + context.Message);
        throw new Exception("time out!!");
        return Task.CompletedTask;
    }

    public void OnError(MessageContext<Message> context)
    {
        this._dBFactory.CreateDB();
        if (context.GetHeader("Route") == "Add")
        {
            Console.WriteLine("Router Add :OnError:" + context.Message + "--" + context.Exception.Message);
        }
    }
}

public interface IDBFactory
{
    public void CreateDB();
}

public class DatabaseFactory : IDBFactory
{
    public void CreateDB()
    {
        Console.WriteLine("CreateDB");
    }
}


public class CommandHandler : ICommandHandle<Message>,ICommandHandle<string>
{
    public Task HandleAsync(Message command, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("CommandHandler--" + this.GetHashCode());
        Console.WriteLine("HandleAsync:" + command.message);
        return Task.CompletedTask;
    }

    public Task HandleAsync(string command, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("String CommandHandler--" + this.GetHashCode());
        Console.WriteLine("String HandleAsync:" + command);
        return Task.CompletedTask;
    }
}