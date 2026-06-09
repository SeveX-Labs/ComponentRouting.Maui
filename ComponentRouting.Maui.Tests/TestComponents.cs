using ComponentRouting.Maui.Provider.Core;
using NGettext;

namespace ComponentRouting.Maui.Tests;

public class TestComponent : Component
{
    public virtual Presenter? Presenter { get; }
    public void Dispose() { }
    public virtual void Resume() { }
    public virtual bool Unpresent() => true;
}

public sealed class DerivedTestComponent : TestComponent
{
}

public sealed class CountingComponent : TestComponent
{
    public int UnpresentCount { get; private set; }

    public override bool Unpresent()
    {
        UnpresentCount++;
        return base.Unpresent();
    }
}

public sealed class HostComponent : TestComponent
{
}

public class GenericComponent<T> : TestComponent
{
}

public class IntermediateGenericComponent : GenericComponent<string>
{
}

public sealed class DerivedGenericComponent : IntermediateGenericComponent
{
}

public sealed class TestCatalogProvider : CatalogProvider
{
    public Task<ICatalog> GetLocalCatalog() => throw new NotSupportedException();
    public Task<string> GetCatalogTwoLetterIsoLanguageName(bool skipFallbackValidation = false) => throw new NotSupportedException();
    public void Dispose() { }
}

public sealed class TestLocaleProvider : LocaleProvider
{
    public Task<string> GetTwoLetterIsoLanguageName() => throw new NotSupportedException();
    public Task<string> GetTwoLetterIsoFallbackLanguageName() => throw new NotSupportedException();
}

public sealed class TestRouter : Router
{
    public Component? CurrentTabComponent => null;
    public Component? MountedComponent => null;
    public List<Component> ComponentsStack { get; } = new();

    public TComponent? GetMountedComponent<TComponent>()
        where TComponent : Component => default;

    public IReadOnlyList<TComponent> GetMountedComponents<TComponent>()
        where TComponent : Component => Array.Empty<TComponent>();

    public Task PreloadComponent<TComponent, TState, TResult>(TState input)
        where TComponent : RoutableComponent<TState, TResult> => Task.CompletedTask;

    public Task<TResult> PresentComponent<TComponent, TState, TResult>(TState input)
        where TComponent : RoutableComponent<TState, TResult> => throw new NotSupportedException();

    public Task UnpresentRootComponent() => Task.CompletedTask;
    public Task UnpresentComponentStack() => Task.CompletedTask;

    public Task DismissComponent<TComponent, TState, TResult>(bool animated = true)
        where TComponent : RoutableComponent<TState, TResult> => Task.CompletedTask;

    public void DispatchResume() { }
    public Task DispatchSleep() => Task.CompletedTask;
    public Task DispatchDestroy() => Task.CompletedTask;
    public bool OnDeviceBackPressed() => false;
}
