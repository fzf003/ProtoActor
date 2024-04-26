using Akka.Actor;
using Akka.Hosting;

public class TimerActor : ReceiveActor, IWithTimers
{
     IActorRef _helloActor;

    public TimerActor(IRequiredActor<HelloActor> helloActor)
    {
        _helloActor = helloActor.ActorRef;

        Context.Watch(_helloActor);

        Receive<string>(message =>
        {
            //Context.ActorSelection("akka://MyActorSystem/user/hello-actor")?.Tell(message);
            _helloActor.Tell(message);
        });

   

        Receive<Terminated>(Terminated=>{

          Console.WriteLine("停止:"+Terminated.ActorRef.Path);

          _helloActor = Context.ActorOf(Props.Create(() => new HelloActor()), "hello-actor");

           Context.Watch(_helloActor);

        });

    
    }

    protected override void PreStart()
    {
        
        Timers.StartPeriodicTimer("hello-key", new StreamRequestMessage(Guid.NewGuid().ToString("N")) , TimeSpan.FromSeconds(1));
    }

    public ITimerScheduler Timers { get; set; } = null!;
}