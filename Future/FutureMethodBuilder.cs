using System;
using System.Runtime.CompilerServices;

namespace LibSugar.Future;

public struct FutureMethodBuilder<T>
{
    private Future<T> future;
    private Pending<T>? pending;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FutureMethodBuilder<T> Create() => new();

    public Future<T> Task
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => future;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
        => default(AsyncValueTaskMethodBuilder<T>).Start(ref stateMachine);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception)
    {
        if (pending == null) future = new Future<T>(exception);
        else pending.SetException(exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult(T result)
    {
        if (pending == null) future = new Future<T>(result);
        else pending.SetResult(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine
    )
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (pending == null)
        {
            pending = new Pending<T>();
            future = new Future<T>(pending);
        }

        awaiter.OnCompleted(new StateMachineContinuation<TStateMachine>(stateMachine).Continuation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine
    )
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.OnCompleted(new StateMachineContinuation<TStateMachine>(stateMachine).Continuation);
    }

    class StateMachineContinuation<S> where S : IAsyncStateMachine
    {
        S StateMachine;

        public StateMachineContinuation(S stateMachine)
        {
            StateMachine = stateMachine;
        }

        public void Continuation()
        {
            StateMachine.MoveNext();
        }
    }
}

public struct FutureMethodBuilder
{
    private Future future;
    private Pending? pending;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FutureMethodBuilder Create() => new();

    public Future Task
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => future;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
        => default(AsyncValueTaskMethodBuilder).Start(ref stateMachine);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception)
    {
        if (pending == null) future = new Future(exception);
        else pending.SetException(exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult()
    {
        if (pending == null) future = new Future(0);
        else pending.SetResult();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine
    )
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (pending == null)
        {
            pending = new Pending();
            future = new Future(pending);
        }

        awaiter.OnCompleted(new StateMachineContinuation<TStateMachine>(stateMachine).Continuation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine
    )
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.OnCompleted(new StateMachineContinuation<TStateMachine>(stateMachine).Continuation);
    }

    class StateMachineContinuation<S> where S : IAsyncStateMachine
    {
        S StateMachine;

        public StateMachineContinuation(S stateMachine)
        {
            StateMachine = stateMachine;
        }

        public void Continuation()
        {
            StateMachine.MoveNext();
        }
    }
}
