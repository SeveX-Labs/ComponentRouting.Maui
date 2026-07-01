using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ComponentRouting.Maui;

public interface Router
{
    TComponent? GetMountedOverlayComponent<TComponent>(bool throwIfMultiple = false)
        where TComponent : Component;

    IReadOnlyList<TComponent> GetMountedOverlayComponents<TComponent>()
        where TComponent : Component;

    void CloseAllPopups();

    public Task PreloadComponent<TComponent, TState, TResult>(TState input)
        where TComponent : RoutableComponent<TState, TResult>;

    Task<TResult> PresentComponent<TComponent, TState, TResult>(TState input)
        where TComponent : RoutableComponent<TState, TResult>;

    Task UnpresentRootComponent();

    Task UnpresentComponentStack();

    Task DismissComponent<TComponent, TState, TResult>(bool animated = true)
        where TComponent : RoutableComponent<TState, TResult>;

    void DispatchResume();

    Task DispatchSleep();

    Task DispatchDestroy();

    Task ShutdownAsync(
        RouterShutdownOptions? options = null,
        CancellationToken cancellationToken = default);

    void BeginNewRuntime();

    /// <summary>
    /// Live (in-process) reset of the router runtime for scenarios such as signout / account switch,
    /// where the Window stays alive and a new root/login must be presented immediately afterward.
    /// Unlike <see cref="ShutdownAsync"/> it does not enter the shutting-down lifecycle state,
    /// does not disconnect the MAUI page tree, and does not require <see cref="BeginNewRuntime"/> after it.
    /// </summary>
    Task ResetRuntimeAsync(RouterRuntimeResetOptions? options = null);

    bool OnDeviceBackPressed();
}
