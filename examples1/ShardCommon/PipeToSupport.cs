public static class PipeToSupport
{

    public static Task ToPipe<T>(this Task<T> taskToPipe, Func<T, object> success = null, Func<Exception, object> failure = null)
    {
        return taskToPipe.ContinueWith(tresult =>
        {
            if (tresult.IsCanceled || tresult.IsFaulted)

                failure(tresult.Exception);

            else if (tresult.IsCompleted)

                success(tresult.Result);

        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}