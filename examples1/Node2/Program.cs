
var system = BuilderSystem();

await system.Remote().StartAsync();
 
IRootContext context = new RootContext(system);

var loggerFactory = LoggerFactory.Create(c => c.AddConsole());
 
var Props= HelloActor.CreateProps(loggerFactory).WithContextDecorator(c=>new LoggingRootDecorator(c,"logger1"));

var HelloActorPid = context.SpawnNamed(Props, "helloactor");


Stack<string> _behaviors = new();
int _count = 1;


//for (; ; )
{
    context.Send(HelloActorPid, new PayLoadEvent("Hello,Actor!"));

    //context.Send(HelloActorPid, new RequestMessageEvent(null,"Hello,Requesr!!!"));

  //  Console.ReadKey();

}

await Task.Delay(-1);












static ActorSystem BuilderSystem(bool isRemote = true)
{
    ActorSystem system = new ActorSystem(GetSystemConfig());
    if (isRemote)
    {
        GrpcNetRemoteConfig grpcNetRemoteConfig = GrpcNetRemoteConfig.BindToLocalhost(8000);
        return system.WithRemote(grpcNetRemoteConfig);
    }
    return system;
}


static ActorSystemConfig GetSystemConfig()
{

    var actorSystemConfig = ActorSystemConfig
                    .Setup()
                    .WithMetrics()
                    .WithDeadLetterThrottleCount(3)
                    .WithDeadLetterThrottleInterval(TimeSpan.FromSeconds(1));
    //.WithConfigureRootContext(context => context.Headers);
    return actorSystemConfig;
}
