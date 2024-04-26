


var hostBuilder = new HostBuilder()
    .ConfigureAppConfiguration((hostContext, configApp) =>
    {
        //configApp.AddJsonFile("appsettings.json");
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddAkka("MyActorSystem", (builder, sp) =>
        {
            builder.AddPetabridgeCmd(c=>c.Start())
                   .AddHoconFile("akka.hocon",HoconAddMode.Append);

            builder.WithActors((system, registry, resolver) =>
                {
                    var helloActor = system.ActorOf(Props.Create(() => new HelloActor()), "hello-actor");
                    system.EventStream.Subscribe(helloActor, typeof(Akka.Event.DeadLetter));
                    registry.Register<HelloActor>(helloActor);
                    
                }).WithActors((system, registry, resolver) =>
                {
                    var timerActorProps =resolver.Props<TimerActor>(); 
                    var timerActor = system.ActorOf(timerActorProps, "timer-actor");
                    registry.Register<TimerActor>(timerActor);
                });

        });

    }).UseConsoleLifetime().Build();

await  hostBuilder.RunAsync();