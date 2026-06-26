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

public sealed class TestPresenter : Presenter
{
}
