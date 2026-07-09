using ComponentRouting.Maui.Routing;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class RouterComponentMountRegistryTests
{
    [Fact]
    public void TryResolve_returns_exactly_tracked_mount()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();
        var mount = new object();

        registry.Track(component, mount);

        Assert.True(registry.TryResolve(component, out var trackedComponent, out var trackedMount));
        Assert.Same(component, trackedComponent);
        Assert.Same(mount, trackedMount);
    }

    [Fact]
    public void TryResolve_returns_latest_component_for_requested_component_type()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var trackedComponent = new TransientLookupComponent();
        var requestedComponent = new TransientLookupComponent();
        var mount = new object();

        registry.Track(trackedComponent, mount);

        Assert.True(registry.TryResolve(requestedComponent, out var resolvedComponent, out var trackedMount));
        Assert.Same(trackedComponent, resolvedComponent);
        Assert.Same(mount, trackedMount);
    }

    [Fact]
    public void TryResolve_returns_tracked_mount_with_owner()
    {
        var registry = new RouterComponentMountRegistry<RouterComponentMount<object, object>>();
        var component = new TestComponent();
        var page = new object();
        var ownerNavigation = new object();
        var mount = new RouterComponentMount<object, object>(page, ownerNavigation);

        registry.Track(component, mount);

        Assert.True(registry.TryResolve(component, out var trackedComponent, out var trackedMount));
        Assert.Same(component, trackedComponent);
        Assert.Same(page, trackedMount.Mount);
        Assert.Same(ownerNavigation, trackedMount.Owner);
    }

    [Fact]
    public void TryBeginFinalize_is_idempotent_until_component_is_tracked_again()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();
        var mount = new object();

        Assert.True(registry.TryBeginFinalize(component));
        Assert.False(registry.TryBeginFinalize(component));

        registry.Track(component, mount);

        Assert.True(registry.TryBeginFinalize(component));
    }

    [Fact]
    public void Remove_deletes_mount_for_component()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();
        var mount = new object();

        registry.Track(component, mount);
        registry.Remove(component);

        Assert.False(registry.TryResolve(component, out _, out _));
    }

    [Fact]
    public void ClearMounts_keeps_finalize_state()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();
        var mount = new object();

        registry.Track(component, mount);
        Assert.True(registry.TryBeginFinalize(component));

        registry.ClearMounts();

        Assert.False(registry.TryResolve(component, out _, out _));
        Assert.False(registry.TryBeginFinalize(component));
    }

    [Fact]
    public void Clear_resets_finalize_state()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();
        var mount = new object();

        registry.Track(component, mount);
        Assert.True(registry.TryBeginFinalize(component));

        registry.Clear();

        Assert.False(registry.TryResolve(component, out _, out _));
        Assert.True(registry.TryBeginFinalize(component));
    }

    // --- Solution A: finalizingComponents is an in-flight guard (EndFinalize reopens it). ---
    // These model the router's finalize gate: in FinalizeComponent, TryBeginFinalize==true is exactly the
    // condition under which component.Dispose() runs, and EndFinalize is called in its finally.

    [Fact]
    public void TryBeginFinalize_reopens_after_EndFinalize()
    {
        // Test 1: true -> false (before EndFinalize) -> true (after EndFinalize).
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();

        Assert.True(registry.TryBeginFinalize(component));   // finalize begins
        Assert.False(registry.TryBeginFinalize(component));  // still in-flight -> blocked
        registry.EndFinalize(component);                     // finalize completes (FinalizeComponent finally)
        Assert.True(registry.TryBeginFinalize(component));   // a NEW finalize may begin
    }

    [Fact]
    public void Singleton_modal_finalized_twice_disposes_each_time()
    {
        // Test 2: a reused singleton MODAL is never re-Track'd; before the fix its 2nd finalize was blocked
        // (no 2nd Dispose = the bug). With EndFinalize the gate reopens, so Dispose runs on every close.
        var registry = new RouterComponentMountRegistry<object>();
        var singletonModal = new TestComponent();

        // 1st open/close
        Assert.True(registry.TryBeginFinalize(singletonModal));  // -> Dispose runs
        registry.EndFinalize(singletonModal);

        // 2nd open/close: SAME instance, NO Track in between (modal path)
        Assert.True(registry.TryBeginFinalize(singletonModal));  // -> Dispose runs AGAIN
        registry.EndFinalize(singletonModal);
    }

    [Fact]
    public void Transient_modal_finalizes_each_fresh_instance()
    {
        // Test 3: transient modal = a fresh instance per present; each finalizes independently.
        var registry = new RouterComponentMountRegistry<object>();
        var firstInstance = new TestComponent();
        var secondInstance = new TestComponent();

        Assert.True(registry.TryBeginFinalize(firstInstance));   // -> Dispose #1
        registry.EndFinalize(firstInstance);

        Assert.True(registry.TryBeginFinalize(secondInstance));  // different reference -> Dispose #2
        registry.EndFinalize(secondInstance);
    }

    [Fact]
    public void Singleton_pushable_finalized_twice_is_not_regressed()
    {
        // Test 4: pushables Track on each present (which also clears the guard); EndFinalize keeps that intact.
        var registry = new RouterComponentMountRegistry<object>();
        var singletonPushable = new TestComponent();
        var mount = new object();

        registry.Track(singletonPushable, mount);                  // present 1
        Assert.True(registry.TryBeginFinalize(singletonPushable)); // -> Dispose #1
        registry.EndFinalize(singletonPushable);

        registry.Track(singletonPushable, mount);                  // present 2
        Assert.True(registry.TryBeginFinalize(singletonPushable)); // -> Dispose #2
        registry.EndFinalize(singletonPushable);
    }

    [Fact]
    public void Concurrent_or_reentrant_finalize_disposes_only_once_while_in_flight()
    {
        // Test 5: while a finalize is in flight (before EndFinalize), a concurrent/reentrant finalize of the
        // same instance is blocked -> a single Dispose. After EndFinalize a future finalize is allowed again.
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();

        Assert.True(registry.TryBeginFinalize(component));   // finalize begins -> the ONLY Dispose
        Assert.False(registry.TryBeginFinalize(component));  // concurrent/reentrant -> no 2nd Dispose
        Assert.False(registry.TryBeginFinalize(component));  // still blocked while in flight

        registry.EndFinalize(component);
        Assert.True(registry.TryBeginFinalize(component));   // finalization done -> future finalize allowed
    }

    private sealed class TransientLookupComponent : TestComponent
    {
    }
}
