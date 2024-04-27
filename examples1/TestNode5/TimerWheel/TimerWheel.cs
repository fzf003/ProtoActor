using System;

internal abstract class TimerWheel : IDisposable
    {
        public abstract void Dispose();
 
        public abstract TimerWheelTimer CreateTimer(TimeSpan timeout);

        public abstract void SubscribeForTimeouts(TimerWheelTimer timer);

        
        public static TimerWheel CreateTimerWheel(
            TimeSpan resolution,
            int buckets)
        {
            return new TimerWheelCore(resolution, buckets);
        }
    }