using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Exceptions;
using ComponentRouting.Maui.Routing;
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

public sealed class OrderedCountingComponent : TestComponent
{
    private readonly IList<string> closeOrder;
    private readonly string name;

    public OrderedCountingComponent(string name, IList<string> closeOrder)
    {
        this.name = name;
        this.closeOrder = closeOrder;
    }

    public int UnpresentCount { get; private set; }

    public override bool Unpresent()
    {
        UnpresentCount++;
        closeOrder.Add(name);
        return base.Unpresent();
    }
}

public sealed class HostComponent : TestComponent
{
}

public sealed class ShutdownBlockingComponent : TestComponent, IRouterShutdownAwareComponent
{
    private readonly TaskCompletionSource<bool> entered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource<bool> release =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Entered => entered.Task;

    public void Release() => release.TrySetResult(true);

    public async ValueTask OnRouterShutdownAsync(RouterShutdownContext context)
    {
        entered.TrySetResult(true);
        await release.Task;
    }
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
    private readonly RouterRuntimeLifecycle runtimeLifecycle;
    private readonly RouterRuntimeComponentRegistry runtimeComponentRegistry = new();
    private int shutdownGeneration = -1;
    private Task? shutdownTask;

    public TestRouter()
        : this(new RouterRuntimeLifecycle())
    {
    }

    public TestRouter(RouterRuntimeLifecycle runtimeLifecycle)
    {
        this.runtimeLifecycle = runtimeLifecycle;
    }

    public Component? CurrentTabComponent => null;
    public Component? MountedComponent => null;
    public List<Component> ComponentsStack { get; } = new();
    public int PresentInvocationCount { get; private set; }
    public int TrackedRuntimeComponentCount => runtimeComponentRegistry.Count;

    public void TrackRuntimeComponentForTest(Component component)
    {
        runtimeComponentRegistry.Track(component);
    }

    public bool IsRuntimeComponentTrackedForTest(Component component)
    {
        return runtimeComponentRegistry.IsTracked(component);
    }

    public TComponent? GetMountedOverlayComponent<TComponent>(bool throwIfMultiple = false)
        where TComponent : Component => default;

    public IReadOnlyList<TComponent> GetMountedOverlayComponents<TComponent>()
        where TComponent : Component => Array.Empty<TComponent>();

    public void CloseAllPopups() { }

    public Task PreloadComponent<TComponent, TState, TResult>(TState input)
        where TComponent : RoutableComponent<TState, TResult> => runtimeLifecycle.IsShuttingDown
            ? Task.CompletedTask
            : Task.CompletedTask;

    public Task<TResult> PresentComponent<TComponent, TState, TResult>(TState input)
        where TComponent : RoutableComponent<TState, TResult>
    {
        if (runtimeLifecycle.IsShuttingDown)
            throw new RouterException(RouterError.RouterIsShuttingDown);

        PresentInvocationCount++;
        throw new NotSupportedException();
    }

    public Task UnpresentRootComponent() => Task.CompletedTask;
    public Task UnpresentComponentStack() => Task.CompletedTask;

    public Task DismissComponent<TComponent, TState, TResult>(bool animated = true)
        where TComponent : RoutableComponent<TState, TResult> => Task.CompletedTask;

    public void DispatchResume() { }
    public Task DispatchSleep() => Task.CompletedTask;
    public Task DispatchDestroy() => Task.CompletedTask;
    public Task ShutdownAsync(
        RouterShutdownOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new RouterShutdownOptions();
        var generation = runtimeLifecycle.BeginShutdown();
        var context = new RouterShutdownContext(
            generation,
            runtimeLifecycle.IsShuttingDown,
            runtimeLifecycle.ShutdownToken,
            options.Reason);

        if (shutdownTask is not null && shutdownGeneration == generation)
            return shutdownTask;

        shutdownGeneration = generation;
        shutdownTask = ShutdownInternalAsync(context);
        return shutdownTask;
    }

    public void BeginNewRuntime()
    {
        // Mirrors AbstractRouter.BeginNewRuntime: on reopen after shutdown, clean the stale runtime
        // registry so old singletons are not left IsTracked && pending.
        var wasShuttingDown = runtimeLifecycle.IsShuttingDown;
        runtimeLifecycle.BeginNewRuntime();

        if (wasShuttingDown)
            runtimeComponentRegistry.DisposeTrackedComponents();

        if (runtimeLifecycle.IsShuttingDown)
            return;

        shutdownGeneration = -1;
        shutdownTask = null;
    }

    public Task ResetRuntimeAsync(RouterRuntimeResetOptions? options = null)
    {
        // Mirrors AbstractRouter.ResetRuntimeAsync: a live reset clears tracked/pending
        // components and never touches the runtime lifecycle (no BeginShutdown / IsShuttingDown).
        runtimeComponentRegistry.DisposeTrackedComponents();
        return Task.CompletedTask;
    }

    public bool OnDeviceBackPressed() => false;

    private async Task ShutdownInternalAsync(RouterShutdownContext context)
    {
        var notifiedPresenters = new HashSet<IRouterShutdownAwarePresenter>(
            ReferenceEqualityComparer<IRouterShutdownAwarePresenter>.Instance);

        try
        {
            await runtimeComponentRegistry.InvokeShutdownHooksAsync(context, notifiedPresenters);
        }
        finally
        {
            // Mirrors AbstractRouter.ShutdownInternalAsync: skip the destructive cleanup if the
            // runtime was reopened (generation changed) while this shutdown was suspended.
            if (runtimeLifecycle.Generation == context.Generation)
                runtimeComponentRegistry.DisposeTrackedComponents();
        }
    }
}
