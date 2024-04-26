

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public interface ISubscribeHandler<T>
{
    public Task OnNext(MessageContext<T> context, CancellationToken cancellationToken);

    public void OnError(MessageContext<T> context);
}

public interface IMessageQueue<T> : IAsyncDisposable
{
    Task PublishAsync(T integrationEvent, CancellationToken cancellationToken = default);

    Task PublishAsync(T integrationEvent,Dictionary<string,string> headers, CancellationToken cancellationToken = default);
    
    Task SubscribeAsync(Func<MessageContext<T>, CancellationToken, Task> handler, Action<MessageContext<T>> OnError = default, CancellationToken cancellationToken = default);

    Task SubscribeAsync(ISubscribeHandler<T> handler, CancellationToken cancellationToken = default)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
        return SubscribeAsync(handler.OnNext, handler.OnError, cancellationToken);
    }
}

public class InMemoryMessageQueue<T> : IMessageQueue<T>
{
    private readonly Channel<MessageContext<T>> _channel = Channel.CreateUnbounded<MessageContext<T>>(new UnboundedChannelOptions
    {
        SingleWriter = false,
        SingleReader = true,
        AllowSynchronousContinuations = false
    });

    internal ChannelReader<MessageContext<T>> Reader => _channel.Reader;

    internal ChannelWriter<MessageContext<T>> Writer => _channel.Writer;

      public InMemoryMessageQueue()
    {
        Console.WriteLine("InMemoryMessageQueue Init....."+this.GetHashCode());
    }


    public async Task PublishAsync(T integrationEvent,Dictionary<string,string> headers, CancellationToken cancellationToken = default)
    {
        var context = MessageContext<T>.Create(integrationEvent,headers);
 
        await Writer.WriteAsync(context, cancellationToken);
    }

    public Task PublishAsync(T integrationEvent, CancellationToken cancellationToken = default)
    {
       return this.PublishAsync(integrationEvent, new Dictionary<string, string>(), cancellationToken);
    }

      

    IAsyncEnumerable<MessageContext<T>> ReadMessageStreamAsync(CancellationToken cancellationToken = default)
    {
        return Reader.ReadAllAsync(cancellationToken);
    }

    public Task SubscribeAsync(Func<MessageContext<T>, CancellationToken, Task> handler, Action<MessageContext<T>> OnError = default, CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            await foreach (var messagecontext in this.ReadMessageStreamAsync(cancellationToken).WithCancellation(cancellationToken))
            {
                
                try
                {
                    await handler(messagecontext, cancellationToken);
                }
                catch (System.Exception ex)
                {
                    messagecontext.SetError(ex);

                    if (OnError is not null)
                    {
                        OnError(messagecontext);
                    }
                }
            }
        });
    }
 
    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("InMemoryMessageQueue DisposeAsync....."+this.GetHashCode());
        if (_channel is not null)
        {
            if (_channel.Writer.TryComplete())
            {
                await _channel.Reader.Completion;
            }
        }
    }

  
}
 

public class MessageContext<T>
{
    public Exception Exception { get; internal set; }

    public T Message { get; internal set; }

    public bool HasError => Exception is not null;

    public IDictionary<string, string> Headers { get; internal set; }

    public MessageContext(T message, IDictionary<string, string> headers)
    {
        Message = message;
        Headers = headers;
    }

    public void SetError(Exception ex)
    {
        Exception = ex;
    }

    public void SetHeader(string key, string value)
    {
        Headers[key] = value;
    }

    public string GetHeader(string key)
    {
        return Headers[key];
    }

    public bool TryGetHeader(string key, out string value)
    {
        return Headers.TryGetValue(key, out value);
    }

    public static MessageContext<T> Create(T message)
    {
        return new MessageContext<T>(message, new Dictionary<string, string>());
    }
    public static MessageContext<T> Create(T message, Dictionary<string, string> headers)
    {
        return new MessageContext<T>(message, headers);
    }
}




public static class InMemoryMessageQueueExtension
{
    public static Task SubscribeAsync<T>(this IMessageQueue<T> messageQueue, ISubscribeHandler<T> handler, CancellationToken cancellationToken = default)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return messageQueue.SubscribeAsync(handler, cancellationToken);
    }

}


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryMessageQueue<T>(this IServiceCollection services)
    {
        //services.AddSingleton<IMessageQueue<T>, InMemoryMessageQueue<T>>();
        var assemblies = Assembly.GetExecutingAssembly();
        services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.Where(type => type.IsClass && !type.IsAbstract))
                .AsImplementedInterfaces()
                .AddClasses(classes => classes.AssignableTo(typeof(ISubscribeHandler<T>)))
                .AsSelfWithInterfaces()
                .WithTransientLifetime()
               // .AddClasses(classes => classes.AssignableTo(typeof(IMessageQueue<T>)))
               // .AsSelfWithInterfaces()
               // .WithSingletonLifetime()
               );

        return services;
    }
}

