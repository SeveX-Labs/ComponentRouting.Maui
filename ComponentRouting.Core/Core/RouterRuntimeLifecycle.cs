using System;
using System.Threading;

namespace ComponentRouting.Maui;

public sealed class RouterRuntimeLifecycle : IDisposable
{
    private readonly object gate = new();
    private CancellationTokenSource shutdownCts = new();
    private int generation;
    private bool isShuttingDown;

    public int Generation
    {
        get
        {
            lock (gate)
                return generation;
        }
    }

    public bool IsShuttingDown
    {
        get
        {
            lock (gate)
                return isShuttingDown;
        }
    }

    public CancellationToken ShutdownToken
    {
        get
        {
            lock (gate)
                return shutdownCts.Token;
        }
    }

    internal int BeginShutdown()
    {
        lock (gate)
        {
            if (isShuttingDown)
                return generation;

            generation++;
            isShuttingDown = true;
            shutdownCts.Cancel();
            return generation;
        }
    }

    public void Dispose()
    {
        lock (gate)
            shutdownCts.Dispose();
    }
}
