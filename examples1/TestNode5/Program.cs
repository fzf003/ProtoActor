
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
public class Program
{
    public static async Task Main(string[] args)
    {
        var (username, age) = MakeUserInfo;

          var factory = LoggerFactory.Create(builder => builder.AddConsole());

            var connectionStringSettings = "Data Source =10.32.3.13; Initial Catalog = HD; Integrated Security = False; User ID = sa; Password = sczs_dev2020; Max Pool Size = 1000; Connect Timeout = 3000";

            var provider = new CarSqlTableDependencyProvider(connectionStringSettings: connectionStringSettings, scheduler: Scheduler.Default, logger: factory.CreateLogger<Program>(), lifetimeScope: LifetimeScope.UniqueScope);

            var changestream = provider.SubscribeToEntityChanges();
            
 
            changestream.WhenEntityRecordChanges
                        .Subscribe(x =>
                        {
                            Console.WriteLine(x.Entity+"---"+x.EntityOldValues);
                        });
 

        //CreateObservable().Subscribe(Console.WriteLine);
 
       await Task.Delay(-1);  //Console.ReadKey();
    }

    static IObservable<string> CreateObservable()
    {
        return Observable.Create<string>(async observer =>
        {
            await foreach (var item in GetStream(CancellationToken.None).WithCancellation(CancellationToken.None))
            {
                observer.OnNext(item);
            }
            observer.OnCompleted();
        });
    }

    static async IAsyncEnumerable<string> GetStream(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            yield return Guid.NewGuid().ToString("N");
        }
    }

    static async Task<string> Create()
    {
        await Task.Delay(100);
        return Guid.NewGuid().ToString("N");
    }



    static async Task NewMethod()
    {
        var pipe = new Pipe();
        var writer = pipe.Writer;
        var reader = pipe.Reader;
        CancellationToken cancellationToken = new CancellationToken();


        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(cancellationToken);
                ReadOnlySequence<byte> buffer = result.Buffer;
                if (!buffer.IsSingleSegment)
                {
                    return;
                }
                string receivedMsg = Encoding.UTF8.GetString(buffer);
                Console.WriteLine("接受:" + receivedMsg);
                reader.AdvanceTo(buffer.End);
                Process currentProcess = Process.GetCurrentProcess();
                long memoryUsed = currentProcess.WorkingSet64;
                Console.WriteLine($"Memory used: {memoryUsed / 1024 / 1024} mb");
            }
        });

        int buffer = 512;

        for (; ; )
        {

            Memory<byte> memory = writer.GetMemory(buffer);

            Console.WriteLine(memory.Span.Length);

            var bytes = JsonSerializer.SerializeToUtf8Bytes(Guid.NewGuid().ToString("N"));
            bytes.CopyTo(memory.Span);

            writer.Advance(bytes.Length);

            await writer.FlushAsync();

            await Task.Delay(1000 * 1);
            //Console.ReadKey();
        }
    }


    private static IServiceProvider BuilderServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddInMemoryMessageQueue<int>()
                .AddInMemoryMessageQueue<Message>();
        var provider = services.BuildServiceProvider();
        return provider;
    }


    #region Name
    static (string username, int age) MakeUserInfo => ("张三", 18);
    #endregion


}