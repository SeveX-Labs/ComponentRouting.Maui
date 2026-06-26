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
}

public sealed class TestRoutableComponent : TestComponent, RoutableComponent<object, object>
{
    public Task<Presenter> Prepare(object state) => throw new NotSupportedException();
    public Task<object> Present() => throw new NotSupportedException();
}
