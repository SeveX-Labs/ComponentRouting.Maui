using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ComponentRouting.Maui;

public interface Router
{
    TComponent? GetMountedOverlayComponent<TComponent>(bool throwIfMultiple = false)
        where TComponent : Component;

    IReadOnlyList<TComponent> GetMountedOverlayComponents<TComponent>()
        where TComponent : Component;

    void CloseAllPopups();

    public Task PreloadComponent<TComponent, TState, TResult>(TState input)
        where TComponent : RoutableComponent<TState, TResult>;

    Task<TResult> PresentComponent<TComponent, TState, TResult>(TState input)
        where TComponent : RoutableComponent<TState, TResult>;

    /// <summary>
    /// Low-level root teardown: unpresents and disposes the currently mounted root/tab/flyout tree,
    /// the pending pushables/modals, and the popups/snackbars, then clears the router tracking state.
    /// </summary>
    /// <remarks>
    /// For a live in-process reset (signout, account switch, auth expiry) prefer
    /// <see cref="ResetRuntimeAsync"/>, which performs this teardown as part of a coherent live reset.
    /// <see cref="ResetRuntimeAsync"/> is NOT a semantic alias of this virtual method: it runs the
    /// teardown directly and does NOT invoke a subclass override of <see cref="UnpresentRootComponent"/>.
    /// Overriding this method is therefore not the recommended place for app-specific reset behavior;
    /// put such behavior at the call site of <see cref="ResetRuntimeAsync"/> instead.
    /// </remarks>
    Task UnpresentRootComponent();

    /// <summary>
    /// Legacy low-level API that calls <c>Unpresent()</c> on each component currently in the push stack.
    /// It does NOT represent a full router reset: it does not clear the stack, the runtime/mount
    /// registries, overlay ownership, or history.
    /// </summary>
    /// <remarks>
    /// For a live runtime reset use <see cref="ResetRuntimeAsync"/>; to close a specific component use
    /// <see cref="DismissComponent{TComponent, TState, TResult}"/>; to close popups use
    /// <see cref="CloseAllPopups"/>.
    /// </remarks>
    [Obsolete("Use ResetRuntimeAsync, DismissComponent, or CloseAllPopups depending on the intended cleanup scope. UnpresentComponentStack is a legacy low-level API and does not represent a full router reset.", false)]
    Task UnpresentComponentStack();

    Task DismissComponent<TComponent, TState, TResult>(bool animated = true)
        where TComponent : RoutableComponent<TState, TResult>;

    void DispatchResume();

    Task DispatchSleep();

    Task DispatchDestroy();

    Task ShutdownAsync(
        RouterShutdownOptions? options = null,
        CancellationToken cancellationToken = default);

    void BeginNewRuntime();

    /// <summary>
    /// Live (in-process) reset of the router runtime for scenarios such as signout / account switch,
    /// where the Window stays alive and a new root/login must be presented immediately afterward.
    /// Unlike <see cref="ShutdownAsync"/> it does not enter the shutting-down lifecycle state,
    /// does not disconnect the MAUI page tree, and does not require <see cref="BeginNewRuntime"/> after it.
    /// </summary>
    Task ResetRuntimeAsync(RouterRuntimeResetOptions? options = null);

    bool OnDeviceBackPressed();
}
