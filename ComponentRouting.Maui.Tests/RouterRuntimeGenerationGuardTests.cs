using Xunit;

namespace ComponentRouting.Maui.Tests;

// These tests validate the B4 generation-guard semantics through TestRouter, which faithfully mirrors
// AbstractRouter's runtime-lifecycle choreography over the real RouterRuntimeLifecycle and
// RouterRuntimeComponentRegistry. Limit: the real AbstractRouter path (page-tree disconnect, mount
// registry, MAUI navigation) is not exercised here because it is MAUI-bound; the mirror covers the
// runtime-registry cleanup + generation guard that this fix is about.
public class RouterRuntimeGenerationGuardTests
{
    [Fact]
    public async Task BeginNewRuntime_afterBlockedShutdown_cleansOldStateAndSurvivesLateShutdown()
    {
        var router = new TestRouter();
        var oldComponent = new ShutdownBlockingComponent();
        router.TrackRuntimeComponentForTest(oldComponent);
        Assert.Equal(1, router.TrackedRuntimeComponentCount);

        // Fire-and-forget shutdown; it suspends inside the shutdown-aware hook.
        var shutdownTask = router.ShutdownAsync();
        await oldComponent.Entered;

        // Reopen the runtime while the old shutdown is still suspended.
        router.BeginNewRuntime();

        // BeginNewRuntime cleaned the stale runtime state -> the old singleton is no longer tracked,
        // so re-presenting the same singleton root/login would not be blocked by ComponentAlreadyPresented.
        Assert.False(router.IsRuntimeComponentTrackedForTest(oldComponent));
        Assert.Equal(0, router.TrackedRuntimeComponentCount);

        // The new runtime tracks a fresh component.
        var newComponent = new TestComponent();
        router.TrackRuntimeComponentForTest(newComponent);
        Assert.Equal(1, router.TrackedRuntimeComponentCount);

        // Let the old shutdown resume.
        oldComponent.Release();
        await shutdownTask;

        // The stale shutdown must NOT have disposed/cleared the new runtime (generation guard).
        Assert.True(router.IsRuntimeComponentTrackedForTest(newComponent));
        Assert.Equal(1, router.TrackedRuntimeComponentCount);
    }

    [Fact]
    public async Task Shutdown_withoutReopen_cleansTrackedComponentsAsBefore()
    {
        var router = new TestRouter();
        var component = new ShutdownBlockingComponent();
        router.TrackRuntimeComponentForTest(component);

        var shutdownTask = router.ShutdownAsync();
        await component.Entered;
        component.Release();
        await shutdownTask;

        // Generation unchanged -> normal cleanup happens.
        Assert.False(router.IsRuntimeComponentTrackedForTest(component));
        Assert.Equal(0, router.TrackedRuntimeComponentCount);
    }

    [Fact]
    public void BeginNewRuntime_withoutActiveShutdown_isNoOpAndKeepsTrackedComponents()
    {
        var router = new TestRouter();
        var component = new TestComponent();
        router.TrackRuntimeComponentForTest(component);

        // Not shutting down -> BeginNewRuntime must be a no-op and must not dispose active components.
        router.BeginNewRuntime();

        Assert.True(router.IsRuntimeComponentTrackedForTest(component));
        Assert.Equal(1, router.TrackedRuntimeComponentCount);
    }
}
