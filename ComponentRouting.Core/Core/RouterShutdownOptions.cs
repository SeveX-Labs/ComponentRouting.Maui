namespace ComponentRouting.Maui;

public sealed class RouterShutdownOptions
{
    public RouterShutdownReason Reason { get; init; } = RouterShutdownReason.WindowDestroying;
    public bool DisconnectMauiPageTree { get; init; } = true;
}
