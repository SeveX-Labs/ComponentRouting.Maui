using Xunit;

namespace ComponentRouting.Maui.Tests;

// Phase-2 tests: the unified runtime-state cleanup shared by live reset, shutdown and runtime reopen,
// plus the OnRuntimeResetAsync hook. Validated through TestRouter, which mirrors AbstractRouter's
// runtime-lifecycle choreography over the real RouterRuntimeLifecycle and RouterRuntimeComponentRegistry.
// Limit: History / overlay ownership / mounted-tab-flyout references are MAUI-bound and not modeled in
// the Core double; the mirror covers the component stack + registry + hook + generation-guard coupling
// (the regression-prone coordination). The full MAUI cleanup is exercised only by the platform builds.
public class RouterRuntimeStateCleanupTests
{
    [Fact]
    public async Task ResetRuntimeAsync_clears_tracked_components_and_stack()
    {
        var router = new TestRouter();
        router.TrackRuntimeComponentForTest(new TestComponent());
        router.ComponentsStack.Add(new TestComponent());

        await router.ResetRuntimeAsync();

        Assert.Equal(0, router.TrackedRuntimeComponentCount);
        Assert.Empty(router.ComponentsStack);
        Assert.False(router.IsShuttingDownForTest);
    }

    [Fact]
    public async Task Shutdown_then_BeginNewRuntime_clears_tracked_and_stack()
    {
        var router = new TestRouter();
        router.TrackRuntimeComponentForTest(new TestComponent());
        router.ComponentsStack.Add(new TestComponent());

        var blocking = new ShutdownBlockingComponent();
        router.TrackRuntimeComponentForTest(blocking);

        var shutdownTask = router.ShutdownAsync();
        await blocking.Entered;

        // Reopen the runtime while the shutdown is still suspended in the hook.
        router.BeginNewRuntime();

        // Phase 2: BeginNewRuntime now clears the component stack too, not only the registry.
        Assert.Equal(0, router.TrackedRuntimeComponentCount);
        Assert.Empty(router.ComponentsStack);

        // The reopened runtime accepts fresh tracking.
        var fresh = new TestComponent();
        router.TrackRuntimeComponentForTest(fresh);
        Assert.Equal(1, router.TrackedRuntimeComponentCount);

        blocking.Release();
        await shutdownTask;

        // The stale shutdown must not clean the reopened runtime (B4 preserved).
        Assert.True(router.IsRuntimeComponentTrackedForTest(fresh));
        Assert.Equal(1, router.TrackedRuntimeComponentCount);
    }

    [Fact]
    public async Task Shutdown_without_reopen_clears_tracked_and_stack()
    {
        var router = new TestRouter();
        router.ComponentsStack.Add(new TestComponent());

        var blocking = new ShutdownBlockingComponent();
        router.TrackRuntimeComponentForTest(blocking);

        var shutdownTask = router.ShutdownAsync();
        await blocking.Entered;
        blocking.Release();
        await shutdownTask;

        // Generation unchanged -> full cleanup (registry + stack).
        Assert.Equal(0, router.TrackedRuntimeComponentCount);
        Assert.Empty(router.ComponentsStack);
    }

    [Fact]
    public void BeginNewRuntime_without_shutdown_is_noop_and_keeps_active_state()
    {
        var router = new TestRouter();
        var tracked = new TestComponent();
        router.TrackRuntimeComponentForTest(tracked);
        router.ComponentsStack.Add(new TestComponent());

        // Not shutting down -> must be a no-op and must not clear active state.
        router.BeginNewRuntime();

        Assert.True(router.IsRuntimeComponentTrackedForTest(tracked));
        Assert.Equal(1, router.TrackedRuntimeComponentCount);
        Assert.Single(router.ComponentsStack);
    }

    [Fact]
    public async Task ResetRuntimeAsync_invokes_reset_hook_once_after_cleanup()
    {
        var router = new TestRouter();
        router.TrackRuntimeComponentForTest(new TestComponent());

        var calls = 0;
        var trackedWhenHookRan = -1;
        router.OnRuntimeResetForTest = _ =>
        {
            calls++;
            trackedWhenHookRan = router.TrackedRuntimeComponentCount;
            return Task.CompletedTask;
        };

        await router.ResetRuntimeAsync();

        Assert.Equal(1, calls);
        Assert.Equal(0, trackedWhenHookRan); // the hook runs AFTER the cleanup
    }

    [Fact]
    public async Task ResetRuntimeAsync_propagates_reset_hook_exception()
    {
        var router = new TestRouter();
        router.OnRuntimeResetForTest = _ => throw new InvalidOperationException("boom");

        await Assert.ThrowsAsync<InvalidOperationException>(() => router.ResetRuntimeAsync());
    }

    [Fact]
    public async Task Shutdown_does_not_invoke_reset_hook()
    {
        var router = new TestRouter();
        var hookInvoked = false;
        router.OnRuntimeResetForTest = _ =>
        {
            hookInvoked = true;
            return Task.CompletedTask;
        };

        var blocking = new ShutdownBlockingComponent();
        router.TrackRuntimeComponentForTest(blocking);

        var shutdownTask = router.ShutdownAsync();
        await blocking.Entered;
        blocking.Release();
        await shutdownTask;

        Assert.False(hookInvoked);
    }
}
