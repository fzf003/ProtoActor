// See https://aka.ms/new-console-template for more information
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using ShardCommon;
using static Proto.Remote.GrpcNet.GrpcNetRemoteConfig;

var system = new ActorSystem().WithRemote(BindToLocalhost());
await system.Remote().StartAsync();
var loggerFactory = LoggerFactory.Create(c => c.AddConsole());

var subscriber = system.Root.SpawnNamed(ChannelSubscriber.CreateProps(loggerFactory), "Subscriber");

var publisher = PID.FromAddress("localhost:8000", "helloactor");

bool replay = false;

for (; ; )
{
    if (replay)
    {

        var response = await system.Root.RequestAsync<ResponseMessageEvent>(publisher, new RequestMessageEvent(subscriber, "Client Say:Hello,Actor!"));
        Console.WriteLine(response);
    }
    else
    {
        system.Root.Send(publisher, new RequestMessageEvent(subscriber, "Client Say:Hello,Actor! No Replay!!"));

    }
    Console.ReadKey();
}


