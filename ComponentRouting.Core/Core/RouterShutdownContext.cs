using System.Threading;

namespace ComponentRouting.Maui;

public sealed class RouterShutdownContext
{
    public RouterShutdownContext(
        int generation,
        bool isShuttingDown,
        CancellationToken shutdownToken,
        RouterShutdownReason reason)
    {
        Generation = generation;
        IsShuttingDown = isShuttingDown;
        ShutdownToken = shutdownToken;
        Reason = reason;
    }

    public int Generation { get; }
    public bool IsShuttingDown { get; }
    public CancellationToken ShutdownToken { get; }
    public RouterShutdownReason Reason { get; }
}
