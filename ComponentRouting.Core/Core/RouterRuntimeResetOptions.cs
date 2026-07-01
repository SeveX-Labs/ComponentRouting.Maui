namespace ComponentRouting.Maui;

/// <summary>
/// Options for <see cref="Router.ResetRuntimeAsync"/>, the live (in-process) runtime reset.
/// Deliberately distinct from <see cref="RouterShutdownOptions"/>: a live reset never
/// disconnects the MAUI page tree and never enters the shutting-down lifecycle state.
/// </summary>
public sealed class RouterRuntimeResetOptions
{
    public RouterRuntimeResetReason Reason { get; init; } = RouterRuntimeResetReason.Manual;
}
