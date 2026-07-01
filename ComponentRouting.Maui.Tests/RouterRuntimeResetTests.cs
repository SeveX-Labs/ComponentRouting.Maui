using Xunit;

namespace ComponentRouting.Maui.Tests;

public class RouterRuntimeResetTests
{
    [Fact]
    public void RouterRuntimeResetOptions_default_reason_is_manual()
    {
        var options = new RouterRuntimeResetOptions();

        Assert.Equal(RouterRuntimeResetReason.Manual, options.Reason);
    }

    [Fact]
    public void RouterRuntimeResetOptions_reason_can_be_set()
    {
        var options = new RouterRuntimeResetOptions { Reason = RouterRuntimeResetReason.Signout };

        Assert.Equal(RouterRuntimeResetReason.Signout, options.Reason);
    }

    [Fact]
    public async Task ResetRuntimeAsync_does_not_mark_runtime_shutting_down()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        var token = runtime.ShutdownToken;

        await router.ResetRuntimeAsync();

        Assert.False(runtime.IsShuttingDown);
        Assert.Equal(0, runtime.Generation);
        Assert.False(token.IsCancellationRequested);
        Assert.False(runtime.ShutdownToken.IsCancellationRequested);
    }

    [Fact]
    public async Task ResetRuntimeAsync_allows_presenting_without_begin_new_runtime()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);

        await router.ResetRuntimeAsync();

        // Reaching PresentComponent's body (NotSupportedException) proves the shutting-down
        // guard (RouterIsShuttingDown) was NOT hit, i.e. the router is presentable with no BeginNewRuntime.
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            router.PresentComponent<TestRoutableComponent, object, object>(new object()));

        Assert.Equal(1, router.PresentInvocationCount);
    }

    [Fact]
    public async Task ResetRuntimeAsync_clears_tracked_components()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        router.TrackRuntimeComponentForTest(new TestComponent());
        router.TrackRuntimeComponentForTest(new TestComponent());
        Assert.Equal(2, router.TrackedRuntimeComponentCount);

        await router.ResetRuntimeAsync();

        Assert.Equal(0, router.TrackedRuntimeComponentCount);
    }

    [Fact]
    public async Task ResetRuntimeAsync_is_idempotent()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);
        router.TrackRuntimeComponentForTest(new TestComponent());

        await router.ResetRuntimeAsync();
        await router.ResetRuntimeAsync();

        Assert.Equal(0, router.TrackedRuntimeComponentCount);
        Assert.False(runtime.IsShuttingDown);
        Assert.Equal(0, runtime.Generation);
    }

    [Fact]
    public async Task ShutdownAsync_after_reset_still_marks_runtime_shutting_down()
    {
        using var runtime = new RouterRuntimeLifecycle();
        var router = new TestRouter(runtime);

        await router.ResetRuntimeAsync();
        await router.ShutdownAsync();

        Assert.True(runtime.IsShuttingDown);
        Assert.Equal(1, runtime.Generation);
        Assert.True(runtime.ShutdownToken.IsCancellationRequested);
    }
}
