using System;
using System.Collections.Concurrent;
using System.Threading;

namespace LibSugar.Future;

public sealed class FutureScheduler : IDisposable
{
    public static readonly FutureScheduler Default = new();

    private readonly FutureWorker[] workers;
    internal readonly FutureWorker?[] idle_workers;

    internal readonly ConcurrentQueue<Action> task_queue = new();

    public FutureScheduler() : this(Environment.ProcessorCount) { }

    public FutureScheduler(int workersCount)
    {
        if (workersCount <= 0) throw new ArgumentOutOfRangeException(nameof(workersCount));
        workers = new FutureWorker[workersCount];
        idle_workers = new FutureWorker[workersCount];
        for (var i = 0; i < workersCount; i++)
            workers[i] = idle_workers[i] = new FutureWorker(this, i);
        StartMaintain();
    }

    #region OnException

    public event Action<Exception>? OnException;

    internal void EmitOnException(Exception e)
    {
        OnException?.Invoke(e);
    }

    #endregion

    public void Scheduler(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        task_queue.Enqueue(action);

        for (var i = 0; i < idle_workers.Length; i++)
        {
            var worker = Interlocked.Exchange(ref idle_workers[i], null);
            if (worker == null) continue;
            worker.Wake();
            return;
        }
    }

    internal volatile bool disposed;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (disposed) return;
        disposed = true;
        foreach (var worker in workers)
        {
            worker.Wake();
        }

        MaintainThread.Interrupt();
    }

    ~FutureScheduler() => Dispose();

    private Thread MaintainThread = null!;

    private void StartMaintain()
    {
        MaintainThread = new Thread(Maintain) {
            Name = $"Future Worker Maintainer",
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
        };
        MaintainThread.Start();
    }

    private void Maintain()
    {
        while (!disposed)
        {
            foreach (var worker in workers)
            {
                worker.Maintain();
            }

            try
            {
                Thread.Sleep(new TimeSpan(0, 0, 1, 0));
            }
            catch (ThreadInterruptedException) { }
        }
    }
}

internal sealed class FutureWorker
{
    public FutureWorker(FutureScheduler scheduler, int index)
    {
        Scheduler = scheduler;
        Index = index;
        CtorThread();
    }

    public readonly FutureScheduler Scheduler;
    public readonly int Index;
    public Thread Thread = null!;

    private void CtorThread()
    {
        Thread = new Thread(Worker) {
            Name = $"Future Worker ({Index})",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal,
        };
        Thread.Start();
    }

    private void Worker()
    {
        while (!Scheduler.disposed)
        {
            for (int c = 0; c < 1000; c++)
            {
                if (Scheduler.task_queue.TryDequeue(out var action))
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            ThreadPool.QueueUserWorkItem(_ => {
                                Scheduler.EmitOnException(e);
                            });
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    c = 0;
                }
            }

            Idle();
            try
            {
                Thread.Sleep(Timeout.Infinite);
            }
            catch (ThreadInterruptedException) { }
        }
    }

    public void Wake()
    {
        if (Thread.ThreadState.HasFlag(ThreadState.Stopped))
        {
            CtorThread();
        }
        else if (Thread.ThreadState.HasFlag(ThreadState.WaitSleepJoin))
        {
            Thread.Interrupt();
        }
    }

    public void Idle()
    {
        Interlocked.Exchange(ref Scheduler.idle_workers[Index], this);
    }

    public void Maintain()
    {
        if (Thread.ThreadState.HasFlag(ThreadState.Stopped))
        {
            CtorThread();
        }
    }
}
