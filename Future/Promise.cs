using System;

namespace LibSugar.Future;

public class Promise<T>
{
    private Future<T> future;
    private Pending<T> pending;

    public Promise()
    {
        pending = new();
        future = new(pending);
    }

    public Future<T> AsFuture => future;

    public void Complete(T result)
    {
        pending.SetResult(result);
    }

    public void Failure(Exception e)
    {
        pending.SetException(e);
    }

    public void Cancel()
    {
        pending.SetCanceled();
    }
}

public class Promise
{
    private Future future;
    private Pending pending;

    public Promise()
    {
        pending = new();
        future = new(pending);
    }

    public Future AsFuture => future;

    public void Complete()
    {
        pending.SetResult();
    }

    public void Failure(Exception e)
    {
        pending.SetException(e);
    }

    public void Cancel()
    {
        pending.SetCanceled();
    }
}
