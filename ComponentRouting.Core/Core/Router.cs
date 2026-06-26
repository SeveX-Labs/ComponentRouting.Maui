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

    bool OnDeviceBackPressed();
}
