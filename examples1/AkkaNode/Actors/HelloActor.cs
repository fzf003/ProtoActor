using Akka.Actor;
using Akka.Event;

public class HelloActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private int _helloCounter = 0;

    public HelloActor()
    {

         Receive<string>(message =>
         {
             _log.Info("{0} {1} {2}", message, _helloCounter++, this.Self.Path.ToString());
         });


         Receive<StreamRequestMessage>(stream =>
        {
             Console.WriteLine(stream);;
        });

        Receive<DeadLetter>(async deadleter=>{

            _log.Info("死信消息： {0}",deadleter.Message);
        });



        ReceiveAny(message =>
        {
            _log.Info("Received unknown message {0}", message);
            //throw new ArithmeticException("ArithmeticException");
            if (message.ToString().Length % 2 == 0)
            {
                //this.Self.Tell(PoisonPill.Instance);
            }

        });
    }

    protected override void PreStart()
    {
        _log.Info("HelloActor PreStart 启动");

        Context.System.EventStream.Subscribe<DeadLetter>(this.Self);
    
        base.PreStart();
    }

    override protected void PreRestart(Exception reason, object message)
    {
        _log.Info("HelloActor PreRestart 重启");
        base.PreRestart(reason, message);
    }

    override protected void PostRestart(Exception reason)
    {
        _log.Info("HelloActor PostRestart 重启完成");
        base.PostRestart(reason);
    }

    protected override void PostStop()
    {
        _log.Info("HelloActor 停止 PostStop");
         Context.System.EventStream.Unsubscribe<DeadLetter>(this.Self);
     
        base.PostStop();
    }

    protected override SupervisorStrategy SupervisorStrategy()
    {

        return new OneForOneStrategy(
           maxNrOfRetries: 2,
           withinTimeRange: TimeSpan.FromMicroseconds(1),
           decider: Decider.From(x =>
           {
               if (x is ArithmeticException) return Directive.Resume;
               if (x is NotSupportedException) return Directive.Stop;
               return Directive.Restart;
           }));
    }
}


public interface IHelloActor
{
    void SayHello(string message);
}

public class HelloActorProxy : IHelloActor
{
    private readonly IActorRef _actorRef;

    public HelloActorProxy(IActorRef actorRef)
    {
        _actorRef = actorRef;
    }

    public void SayHello(string message)
    {
        _actorRef.Tell(message);
    }
}