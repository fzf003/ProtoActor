
using Akka.Actor;
using Akka.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Petabridge.Cmd.Host;

var hostBuilder = new HostBuilder()
    .ConfigureAppConfiguration((hostContext, configApp) =>
    {
        //configApp.AddJsonFile("appsettings.json");
    })
    .ConfigureServices((hostContext, services) =>
    {


        services.AddAkka("MyActorSystem", (builder, sp) =>
        {
                builder.AddPetabridgeCmd(c=>c.Start());
            builder.WithActors((system, registry, resolver) =>
                {
                    var helloActor = system.ActorOf(Props.Create(() => new HelloActor()), "hello-actor");
                    registry.Register<HelloActor>(helloActor);
                }).WithActors((system, registry, resolver) =>
            {
                var timerActorProps =
                    resolver.Props<TimerActor>(); 
                var timerActor = system.ActorOf(timerActorProps, "timer-actor");
                registry.Register<TimerActor>(timerActor);
            });

        });

    });

await hostBuilder.Build().RunAsync();