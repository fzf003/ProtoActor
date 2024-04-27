using System;
using System.Threading.Tasks;

internal abstract class TimerWheelTimer
{
    /// <summary>
    /// Timeout of the timer.
    /// </summary>
    public abstract TimeSpan Timeout { get; }

    /// <summary>
    /// Starts the timer based on the Timeout configuration.
    /// </summary>
    public abstract Task StartTimerAsync();

    /// <summary>
    /// Cancels the timer.
    /// </summary>
    public abstract bool CancelTimer();

    /// <summary>
    /// Fire the associated timeout callback.
    /// </summary>
    public abstract bool FireTimeout();
}

internal class ApplyTimerWheelTimer : TimerWheelTimer
{
    public override TimeSpan Timeout { get; }

    public override Task StartTimerAsync()
    {
        Console.WriteLine("开始计时");
        return Task.CompletedTask;
    }

    public override bool CancelTimer()
    {
        Console.WriteLine("取消计时");
        return true;
    }

    public override bool FireTimeout()
    {
        Console.WriteLine("计时结束");
        return true;
    }
}