using System.Collections.Generic;
using System.Threading.Tasks;

namespace ComponentRouting.Maui;

public interface Router
{
    Component? CurrentTabComponent { get; }
    Component? MountedComponent { get; }
    List<Component> ComponentsStack { get; }
    
    TComponent? GetMountedComponent<TComponent>()
        where TComponent : Component;

    IReadOnlyList<TComponent> GetMountedComponents<TComponent>()
        where TComponent : Component;

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

    bool OnDeviceBackPressed();
}
