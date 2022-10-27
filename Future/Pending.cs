using System;
using System.Runtime.CompilerServices;

namespace LibSugar.Future;

internal class Pending<T>
{
    private FutureState state = FutureState.Pending;
    private T result = default!;
    private Exception? exception;
    private event Action? Continuation;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void OnCompleted(Action continuation)
    {
        if (IsCompleted) continuation();
        Continuation += continuation;
    }

    #region Getter

    internal FutureState State
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T GetResult() => state switch {
        FutureState.Failed   => throw exception!,
        FutureState.Ready    => result,
        FutureState.Canceled => throw new OperationCanceledException(),
        _                    => throw new InvalidOperationException(),
    };
    
    internal bool IsCompleted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state is FutureState.Ready or FutureState.Failed;
    }

    internal bool IsCompletedSuccessfully
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state is FutureState.Ready;
    }

    internal bool IsFaulted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state is FutureState.Failed;
    }

    internal bool IsCanceled
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state is FutureState.Canceled;
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetException(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            state = FutureState.Canceled;
        }
        else
        {
            state = FutureState.Failed;
            this.exception = exception;
            Continuation?.Invoke();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetResult(T result)
    {
        state = FutureState.Ready;
        this.result = result;
        Continuation?.Invoke();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetCanceled()
    {
        state = FutureState.Canceled;
    }
}

internal class Pending
{
    private FutureState state = FutureState.Pending;
    private Exception? exception;
    private event Action? Continuation;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void OnCompleted(Action continuation)
    {
        if (IsCompleted) continuation();
        Continuation += continuation;
    }

    #region Getter

    internal FutureState State
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void GetResult()
    {
        switch (state)
        {
            case FutureState.Ready:  return;
            case FutureState.Failed:   throw exception!;
            case FutureState.Canceled: throw new OperationCanceledException();
            default:                   throw new InvalidOperationException();
        }
    }

    internal bool IsCompleted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state is FutureState.Ready or FutureState.Failed;
    }

    internal bool IsCompletedSuccessfully
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state is FutureState.Ready;
    }

    internal bool IsFaulted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state is FutureState.Failed;
    }

    internal bool IsCanceled
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => state is FutureState.Canceled;
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetException(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            state = FutureState.Canceled;
        }
        else
        {
            state = FutureState.Failed;
            this.exception = exception;
            Continuation?.Invoke();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetResult()
    {
        state = FutureState.Ready;
        Continuation?.Invoke();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetCanceled()
    {
        state = FutureState.Canceled;
    }
}
