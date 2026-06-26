namespace ComponentRouting.Maui;

public sealed class RouterShutdownOptions
{
    public RouterShutdownReason Reason { get; init; } = RouterShutdownReason.WindowDestroying;
}
