using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LibSugar.Future;

public enum FutureState : byte { Pending = 1, Ready = 2, Failed = 3, Canceled = 255 }

[AsyncMethodBuilder(typeof(FutureMethodBuilder<>))]
public readonly struct Future<T> : INotifyCompletion
{
    internal Future(T result)
    {
        state = FutureState.Ready;
        this.result = result;
        obj = null;
    }

    internal Future(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            state = FutureState.Canceled;
            result = default!;
            obj = null;
        }
        else
        {
            state = FutureState.Failed;
            result = default!;
            obj = exception;
        }
    }

    internal Future(Pending<T> pending)
    {
        state = FutureState.Pending;
        result = default!;
        obj = pending;
    }

    private readonly FutureState state;
    private readonly T result;
    private readonly object? obj;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Future<T> GetAwaiter() => this;

    #region Getter

    public FutureState State
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state is FutureState.Pending ? GetPending().State : state;
    }

    public bool IsCompleted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state switch {
            FutureState.Pending                     => GetPending().IsCompleted,
            FutureState.Ready or FutureState.Failed => true,
            _                                       => false,
        };
    }

    public bool IsCompletedSuccessfully
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state switch {
            FutureState.Pending => GetPending().IsCompletedSuccessfully,
            FutureState.Ready   => true,
            _                   => false,
        };
    }

    public bool IsFaulted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state switch {
            FutureState.Pending => GetPending().IsFaulted,
            FutureState.Failed  => true,
            _                   => false,
        };
    }

    public bool IsCanceled
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state switch {
            FutureState.Pending  => GetPending().IsCanceled,
            FutureState.Canceled => true,
            _                    => false,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetResult() => state switch {
        FutureState.Pending  => GetPending().GetResult(),
        FutureState.Failed   => throw Unsafe.As<Exception>(obj)!,
        FutureState.Ready    => result,
        FutureState.Canceled => throw new OperationCanceledException(),
        _                    => throw new InvalidOperationException(),
    };

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pending<T> GetPending() => Unsafe.As<Pending<T>>(obj)!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation)
    {
        if (state is FutureState.Pending) GetPending().OnCompleted(continuation);
        else if (state is FutureState.Canceled) return;
        else continuation();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NoWait() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T WaitSync()
    {
        if (state == FutureState.Pending)
        {
            var pending = GetPending();
            if (pending.State == FutureState.Pending)
            {
                var thread = Thread.CurrentThread!;
                pending.OnCompleted(() => {
                    thread.Interrupt();
                });
                Thread.Sleep(Timeout.Infinite);
                return pending.GetResult();
            }
            else return pending.GetResult();
        }
        else return GetResult();
    }
}

[AsyncMethodBuilder(typeof(FutureMethodBuilder))]
public readonly struct Future : INotifyCompletion
{
    internal Future(byte _)
    {
        state = FutureState.Ready;
        obj = null;
    }

    internal Future(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            state = FutureState.Canceled;
            obj = null;
        }
        else
        {
            state = FutureState.Failed;
            obj = exception;
        }
    }

    internal Future(Pending pending)
    {
        state = FutureState.Pending;
        obj = pending;
    }

    private readonly FutureState state;
    private readonly object? obj;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Future GetAwaiter() => this;

    #region Getter

    public FutureState State
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state is FutureState.Pending ? GetPending().State : state;
    }

    public bool IsCompleted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state switch {
            FutureState.Pending                     => GetPending().IsCompleted,
            FutureState.Ready or FutureState.Failed => true,
            _                                       => false,
        };
    }

    public bool IsCompletedSuccessfully
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state switch {
            FutureState.Pending => GetPending().IsCompletedSuccessfully,
            FutureState.Ready   => true,
            _                   => false,
        };
    }

    public bool IsFaulted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state switch {
            FutureState.Pending => GetPending().IsFaulted,
            FutureState.Failed  => true,
            _                   => false,
        };
    }

    public bool IsCanceled
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state switch {
            FutureState.Pending  => GetPending().IsCanceled,
            FutureState.Canceled => true,
            _                    => false,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult()
    {
        switch (state)
        {
            case FutureState.Pending:
                GetPending().GetResult();
                break;
            case FutureState.Failed:
                throw Unsafe.As<Exception>(obj)!;
            case FutureState.Ready:
                break;
            case FutureState.Canceled:
                throw new OperationCanceledException();
            default:
                throw new InvalidOperationException();
        }
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pending GetPending() => Unsafe.As<Pending>(obj)!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation)
    {
        if (state is FutureState.Pending) GetPending().OnCompleted(continuation);
        else if (state is FutureState.Canceled) return;
        else continuation();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NoWait() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WaitSync()
    {
        if (state == FutureState.Pending)
        {
            var pending = GetPending();
            if (pending.State == FutureState.Pending)
            {
                var thread = Thread.CurrentThread!;
                pending.OnCompleted(() => {
                    thread.Interrupt();
                });
                try
                {
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (ThreadInterruptedException) { }

                pending.GetResult();
                return;
            }
            else
            {
                pending.GetResult();
                return;
            }
        }
        else
        {
            GetResult();
            return;
        }
    }

    public static Future Yield()
    {
        var promise = new Promise();
        FutureScheduler.Default.Scheduler(promise.Complete);
        return promise.AsFuture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Future Delay(int ms)
    {
        var promise = new Promise();
        var timer = new System.Timers.Timer(ms) {
            AutoReset = false,
        };
        timer.Elapsed += (_, _) => {
            FutureScheduler.Default.Scheduler(promise.Complete);
            timer.Dispose();
        };
        timer.Start();
        return promise.AsFuture;
    }
}
