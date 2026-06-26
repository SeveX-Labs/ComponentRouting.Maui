using ComponentRouting.Maui.Exceptions;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class RouterRuntimeLifecycleTests
{
    [Fact]
    public void Initial_state_is_not_shutting_down()
    {
        using var runtime = new RouterRuntimeLifecycle();

        Assert.False(runtime.IsShuttingDown);
        Assert.Equal(0, runtime.Generation);
        Assert.False(runtime.ShutdownToken.IsCancellationRequested);
    }

    [Fact]
    public void RouterShutdownOptions_disconnects_maui_page_tree_by_default()
    {
        var options = new RouterShutdownOptions();

        Assert.True(options.DisconnectMauiPageTree);
    }

    [Fact]
    public void RouterShutdownOptions_can_disable_maui_page_tree_disconnect()
    {
        var options = new RouterShutdownOptions { DisconnectMauiPageTree = false };

        Assert.False(options.DisconnectMauiPageTree);
    }

    [Fact]
    public void ShutdownAsync_marks_runtime_shutting_down_synchronously()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var token = runtime.ShutdownToken;

        var shutdownTask = router.ShutdownAsync();

        Assert.True(shutdownTask.IsCompleted);
        Assert.True(runtime.IsShuttingDown);
        Assert.Equal(1, runtime.Generation);
        Assert.True(token.IsCancellationRequested);
        Assert.True(runtime.ShutdownToken.IsCancellationRequested);
    }

    [Fact]
    public void ShutdownAsync_is_idempotent()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);

        var firstTask = router.ShutdownAsync();
        var firstGeneration = runtime.Generation;
        var secondTask = router.ShutdownAsync();

        Assert.Same(firstTask, secondTask);
        Assert.Equal(1, firstGeneration);
        Assert.Equal(firstGeneration, runtime.Generation);
        Assert.True(runtime.IsShuttingDown);
    }

    [Fact]
    public async Task BeginNewRuntime_after_shutdown_reopens_runtime()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        await router.ShutdownAsync();

        router.BeginNewRuntime();

        Assert.False(runtime.IsShuttingDown);
        Assert.Equal(2, runtime.Generation);
        Assert.False(runtime.ShutdownToken.IsCancellationRequested);
    }

    [Fact]
    public async Task PresentComponent_is_not_blocked_after_begin_new_runtime()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        await router.ShutdownAsync();
        router.BeginNewRuntime();

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            router.PresentComponent<TestRoutableComponent, object, object>(new object()));

        Assert.Equal(1, router.PresentInvocationCount);
    }

    [Fact]
    public async Task BeginNewRuntime_after_shutdown_increments_generation_once()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);

        await router.ShutdownAsync();
        var shutdownGeneration = runtime.Generation;
        router.BeginNewRuntime();
        var newRuntimeGeneration = runtime.Generation;
        router.BeginNewRuntime();

        Assert.Equal(1, shutdownGeneration);
        Assert.Equal(2, newRuntimeGeneration);
        Assert.Equal(newRuntimeGeneration, runtime.Generation);
    }

    [Fact]
    public async Task BeginNewRuntime_replaces_shutdown_token()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var initialToken = runtime.ShutdownToken;
        await router.ShutdownAsync();
        var shutdownToken = runtime.ShutdownToken;

        router.BeginNewRuntime();
        var newRuntimeToken = runtime.ShutdownToken;

        Assert.True(initialToken.IsCancellationRequested);
        Assert.True(shutdownToken.IsCancellationRequested);
        Assert.False(newRuntimeToken.IsCancellationRequested);
        Assert.NotEqual(shutdownToken, newRuntimeToken);
    }

    [Fact]
    public void BeginNewRuntime_when_not_shutting_down_is_noop()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var token = runtime.ShutdownToken;

        router.BeginNewRuntime();

        Assert.False(runtime.IsShuttingDown);
        Assert.Equal(0, runtime.Generation);
        Assert.Equal(token, runtime.ShutdownToken);
        Assert.False(runtime.ShutdownToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Shutdown_begin_new_runtime_shutdown_again_runs_second_shutdown()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var firstComponent = new ConfigurableRoutableComponent();
        router.TrackRuntimeComponentForTest(firstComponent);

        await router.ShutdownAsync();
        router.BeginNewRuntime();

        var secondComponent = new ConfigurableRoutableComponent();
        router.TrackRuntimeComponentForTest(secondComponent);
        await router.ShutdownAsync();

        Assert.Equal(3, runtime.Generation);
        Assert.True(runtime.IsShuttingDown);
        Assert.Equal(1, firstComponent.DisposeCount);
        Assert.Equal(1, secondComponent.DisposeCount);
    }

    [Fact]
    public async Task PresentComponent_is_blocked_after_shutdown()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        await router.ShutdownAsync();

        var ex = await Assert.ThrowsAsync<RouterException>(() =>
            router.PresentComponent<TestRoutableComponent, object, object>(new object()));

        Assert.Equal(RouterError.RouterIsShuttingDown, ex.Error);
        Assert.Equal(0, router.PresentInvocationCount);
    }

    [Fact]
    public async Task PresentComponent_existing_behavior_is_unchanged_before_shutdown()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            router.PresentComponent<TestRoutableComponent, object, object>(new object()));

        Assert.Equal(1, router.PresentInvocationCount);
    }

    [Fact]
    public async Task ShutdownAsync_disposes_tracked_runtime_component_and_clears_registry()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var component = new ConfigurableRoutableComponent();
        router.TrackRuntimeComponentForTest(component);

        await router.ShutdownAsync();

        Assert.Equal(1, component.DisposeCount);
        Assert.Equal(0, router.TrackedRuntimeComponentCount);
    }

    [Fact]
    public async Task ShutdownAsync_disposes_same_tracked_component_once()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var component = new ConfigurableRoutableComponent();
        router.TrackRuntimeComponentForTest(component);
        router.TrackRuntimeComponentForTest(component);

        await router.ShutdownAsync();

        Assert.Equal(1, component.DisposeCount);
        Assert.Equal(0, router.TrackedRuntimeComponentCount);
    }

    [Fact]
    public async Task ShutdownAsync_idempotence_does_not_dispose_tracked_components_twice()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var component = new ConfigurableRoutableComponent();
        router.TrackRuntimeComponentForTest(component);

        await router.ShutdownAsync();
        await router.ShutdownAsync();

        Assert.Equal(1, component.DisposeCount);
        Assert.Equal(0, router.TrackedRuntimeComponentCount);
    }

    [Fact]
    public async Task Disposed_runtime_component_configures_again_when_prepared_later()
    {
        var component = new ConfigurableRoutableComponent();
        await component.Prepare(new object());
        await component.Prepare(new object());

        Assert.Equal(1, component.ConfigureCount);
        Assert.Equal(2, component.InitializeCount);

        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        router.TrackRuntimeComponentForTest(component);
        await router.ShutdownAsync();

        await component.Prepare(new object());

        Assert.Equal(2, component.ConfigureCount);
        Assert.Equal(3, component.InitializeCount);
    }

    [Fact]
    public async Task Tracked_runtime_component_is_not_disposed_without_shutdown()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var component = new ConfigurableRoutableComponent();
        router.TrackRuntimeComponentForTest(component);

        await component.Prepare(new object());

        Assert.False(runtime.IsShuttingDown);
        Assert.Equal(0, component.DisposeCount);
        Assert.Equal(1, component.ConfigureCount);
        Assert.Equal(1, router.TrackedRuntimeComponentCount);
    }

    [Fact]
    public async Task ShutdownAsync_calls_shutdown_aware_component_before_dispose()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var events = new List<string>();
        var component = new ShutdownAwareRoutableComponent(events);
        router.TrackRuntimeComponentForTest(component);

        await router.ShutdownAsync();

        Assert.Equal(1, component.ShutdownHookCount);
        Assert.Equal(1, component.DisposeCount);
        Assert.Equal(new[] { "component-hook", "component-dispose" }, events);
    }

    [Fact]
    public async Task ShutdownAsync_calls_all_shutdown_hooks_before_disposing_components()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var events = new List<string>();
        var firstComponent = new ShutdownAwareRoutableComponent(events);
        var secondComponent = new ShutdownAwareRoutableComponent(events);
        router.TrackRuntimeComponentForTest(firstComponent);
        router.TrackRuntimeComponentForTest(secondComponent);

        await router.ShutdownAsync();

        Assert.Equal(new[]
        {
            "component-hook",
            "component-hook",
            "component-dispose",
            "component-dispose"
        }, events);
    }

    [Fact]
    public async Task ShutdownAsync_passes_shutdown_context_to_component_hook()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var component = new ShutdownAwareRoutableComponent();
        var shutdownToken = runtime.ShutdownToken;
        router.TrackRuntimeComponentForTest(component);

        await router.ShutdownAsync(new RouterShutdownOptions { Reason = RouterShutdownReason.ApplicationShutdown });

        Assert.NotNull(component.LastShutdownContext);
        Assert.True(component.LastShutdownContext.IsShuttingDown);
        Assert.Equal(1, component.LastShutdownContext.Generation);
        Assert.Equal(RouterShutdownReason.ApplicationShutdown, component.LastShutdownContext.Reason);
        Assert.True(component.LastShutdownContext.ShutdownToken.IsCancellationRequested);
        Assert.True(shutdownToken.IsCancellationRequested);
    }

    [Fact]
    public async Task ShutdownAsync_calls_shutdown_aware_component_once_when_tracked_multiple_times()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var component = new ShutdownAwareRoutableComponent();
        router.TrackRuntimeComponentForTest(component);
        router.TrackRuntimeComponentForTest(component);

        await router.ShutdownAsync();

        Assert.Equal(1, component.ShutdownHookCount);
        Assert.Equal(1, component.DisposeCount);
    }

    [Fact]
    public async Task ShutdownAsync_idempotence_does_not_call_shutdown_hook_twice()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var component = new ShutdownAwareRoutableComponent();
        router.TrackRuntimeComponentForTest(component);

        await router.ShutdownAsync();
        await router.ShutdownAsync();

        Assert.Equal(1, component.ShutdownHookCount);
        Assert.Equal(1, component.DisposeCount);
    }

    [Fact]
    public async Task ShutdownAsync_calls_presenter_shutdown_hook_before_component_dispose()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var events = new List<string>();
        var presenter = new ShutdownAwarePresenter(events);
        var component = new ShutdownAwareRoutableComponent(events, presenter);
        router.TrackRuntimeComponentForTest(component);

        await router.ShutdownAsync();

        Assert.Equal(1, component.ShutdownHookCount);
        Assert.Equal(1, presenter.ShutdownHookCount);
        Assert.Equal(1, component.DisposeCount);
        Assert.Equal(new[] { "component-hook", "presenter-hook", "component-dispose" }, events);
        Assert.NotNull(presenter.LastShutdownContext);
        Assert.True(presenter.LastShutdownContext.IsShuttingDown);
    }

    [Fact]
    public async Task ShutdownAsync_still_disposes_non_shutdown_aware_component()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var component = new ConfigurableRoutableComponent();
        router.TrackRuntimeComponentForTest(component);

        await router.ShutdownAsync();

        Assert.Equal(1, component.DisposeCount);
    }

    [Fact]
    public async Task ShutdownAsync_hook_exception_is_best_effort_and_component_is_still_disposed()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var component = new ThrowingShutdownAwareRoutableComponent();
        router.TrackRuntimeComponentForTest(component);

        await router.ShutdownAsync();

        Assert.Equal(1, component.ShutdownHookCount);
        Assert.Equal(1, component.DisposeCount);
        Assert.Equal(0, router.TrackedRuntimeComponentCount);
    }
}

public sealed class TestRoutableComponent : TestComponent, RoutableComponent<object, object>
{
    public Task<Presenter> Prepare(object state) => throw new NotSupportedException();
    public Task<object> Present() => throw new NotSupportedException();
}

public sealed class ConfigurableRoutableComponent : Component, RoutableComponent<object, object>
{
    private bool wasLayoutConfigured;

    public Presenter? Presenter { get; private set; }
    public int ConfigureCount { get; private set; }
    public int InitializeCount { get; private set; }
    public int DisposeCount { get; private set; }

    public Task<Presenter> Prepare(object state)
    {
        if (!wasLayoutConfigured)
        {
            wasLayoutConfigured = true;
            ConfigureCount++;
            Presenter = new TestPresenter();
        }

        InitializeCount++;
        return Task.FromResult(Presenter!);
    }

    public Task<object> Present() => Task.FromResult<object>(new object());

    public void Dispose()
    {
        DisposeCount++;
        Presenter = null;
        wasLayoutConfigured = false;
    }

    public void Resume()
    {
    }

    public bool Unpresent() => true;
}

public sealed class ShutdownAwareRoutableComponent : Component, RoutableComponent<object, object>, IRouterShutdownAwareComponent
{
    private bool wasLayoutConfigured;
    private readonly IList<string> events;

    public ShutdownAwareRoutableComponent(IList<string>? events = null, Presenter? presenter = null)
    {
        this.events = events ?? new List<string>();
        Presenter = presenter;
    }

    public Presenter? Presenter { get; private set; }
    public int ConfigureCount { get; private set; }
    public int InitializeCount { get; private set; }
    public int ShutdownHookCount { get; private set; }
    public int DisposeCount { get; private set; }
    public RouterShutdownContext? LastShutdownContext { get; private set; }
    public IReadOnlyList<string> Events => events.ToList();

    public Task<Presenter> Prepare(object state)
    {
        if (!wasLayoutConfigured)
        {
            wasLayoutConfigured = true;
            ConfigureCount++;
            Presenter ??= new TestPresenter();
        }

        InitializeCount++;
        return Task.FromResult(Presenter!);
    }

    public Task<object> Present() => Task.FromResult<object>(new object());

    public ValueTask OnRouterShutdownAsync(RouterShutdownContext context)
    {
        ShutdownHookCount++;
        LastShutdownContext = context;
        events.Add("component-hook");
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        DisposeCount++;
        events.Add("component-dispose");
        Presenter = null;
        wasLayoutConfigured = false;
    }

    public void Resume()
    {
    }

    public bool Unpresent() => true;
}

public sealed class ThrowingShutdownAwareRoutableComponent : Component, RoutableComponent<object, object>, IRouterShutdownAwareComponent
{
    public Presenter? Presenter { get; }
    public int ShutdownHookCount { get; private set; }
    public int DisposeCount { get; private set; }

    public Task<Presenter> Prepare(object state) => Task.FromResult<Presenter>(new TestPresenter());
    public Task<object> Present() => Task.FromResult<object>(new object());

    public ValueTask OnRouterShutdownAsync(RouterShutdownContext context)
    {
        ShutdownHookCount++;
        throw new InvalidOperationException("Shutdown hook failed.");
    }

    public void Dispose()
    {
        DisposeCount++;
    }

    public void Resume()
    {
    }

    public bool Unpresent() => true;
}

public sealed class TestPresenter : Presenter
{
}

public sealed class ShutdownAwarePresenter : Presenter, IRouterShutdownAwarePresenter
{
    private readonly IList<string> events;

    public ShutdownAwarePresenter(IList<string> events)
    {
        this.events = events;
    }

    public int ShutdownHookCount { get; private set; }
    public RouterShutdownContext? LastShutdownContext { get; private set; }

    public ValueTask OnRouterShutdownAsync(RouterShutdownContext context)
    {
        ShutdownHookCount++;
        LastShutdownContext = context;
        events.Add("presenter-hook");
        return ValueTask.CompletedTask;
    }
}
