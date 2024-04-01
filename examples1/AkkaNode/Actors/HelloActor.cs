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
           _log.Info("{0} {1}", message, _helloCounter++);
        });
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