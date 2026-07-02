# Changelog

## [5.2.2] - 2026-07-02

### Fixed

- Made iOS chrome application a true no-op when no chrome options are configured: `PlatformComponentChromeService.Apply(...)` now returns immediately for all-null options, so consumers that never opt into chrome no longer get the status bar host controller installed. This aligns iOS with the documented all-null-by-default contract.
- Hardened component stack cleanup by iterating a snapshot of the stack in `UnpresentComponentStack()`, so a component that completes its result and mutates the stack mid-iteration can no longer modify the collection being enumerated.

### Internal

- Observed and logged fire-and-forget task failures across the window/activity shutdown, device back-press, presentation, snackbar auto-dismiss, and chrome reapply paths, so exceptions are no longer swallowed silently. No timing or routing behavior changes.
- Routed best-effort cleanup `catch` blocks through internal diagnostics so their exceptions are logged outside DEBUG as well, without rethrowing or changing the cleanup flow.

### Compatibility

- Patch release with internal robustness and observability fixes only.
- No public API changes and no intentional behavior changes to routing, presentation, or dismissal.
- The only observable change is on iOS for consumers that never configure chrome: the status bar host controller is no longer installed for all-null options (now matching the no-op contract).

## [5.2.1] - 2026-07-02

### Fixed

- Prevented a stale asynchronous shutdown from running its destructive cleanup against a runtime that was already reopened by `BeginNewRuntime()` after window/activity recreation. `ShutdownInternalAsync(...)` now uses a generation guard, and `BeginNewRuntime()` cleans the previous runtime's registry-level state on reopen.
- Marshaled the MAUI-bound teardown to the main thread when invoked from a background thread: live reset (`ResetRuntimeAsync(...)`), root unpresent (`UnpresentRootComponent()`), shutdown-aware hooks, MAUI page-tree disconnect, and tracked component disposal.

### Internal

- Hardened `RouterRuntimeComponentRegistry` and `RouterComponentMountRegistry` with minimal internal locking for concurrent shutdown/restart paths.

### Compatibility

- Patch release with internal robustness fixes only.
- No public API changes and no intentional behavior changes.
- `BeginNewRuntime()`, `ShutdownAsync(...)`, `ResetRuntimeAsync(...)`, and the window/activity lifecycle paths keep their existing semantics.

## [5.2.0] - 2026-07-02

### Added

- Added `Router.ResetRuntimeAsync(...)` for live runtime resets such as signout, account switching, and authentication-expired flows, where the `Window` stays alive and a new root/login must be presented immediately.
- Added `RouterRuntimeResetOptions` and `RouterRuntimeResetReason`.

### Documentation

- Clarified the difference between final app/window shutdown (`ShutdownAsync(...)`) and live runtime reset (`ResetRuntimeAsync(...)`).
- Documented that signout/live reset should use `ResetRuntimeAsync(...)` instead of `ShutdownAsync(... DisconnectMauiPageTree = false)` followed by `BeginNewRuntime()`.

### Compatibility

- Minor release adding a new additive API. No public API was removed and no behavior changed.
- `ResetRuntimeAsync(...)` reuses the existing `UnpresentRootComponent()` teardown; it does not enter the shutting-down lifecycle, does not set `IsShuttingDown = true`, does not disconnect the MAUI page tree, and does not require `BeginNewRuntime()` afterward.
- Consumers that subclass `AbstractRouter` inherit `ResetRuntimeAsync(...)` and need no changes; types that implement the `Router` interface directly must add the new member.
- `ShutdownAsync(...)`, `BeginNewRuntime()`, and the `Window.Destroying` / Android `Activity.OnDestroy` shutdown paths are unchanged.

## [5.1.0] - 2026-07-01

### Added

- Added `RouterError.ComponentAlreadyPresented`.
- Added a runtime guard in `PresentComponent(...)` that blocks presenting the same singleton component while it already has an active or pending presentation. The guard fires when the component is already tracked and `HasPendingPresentation == true`.
- The guard prevents double presentation of the same singleton, reuse of the same presenter/page, `CompletionSource` overwrite, orphaned result tasks, and raw MAUI crashes from `PushAsync`/`PushModalAsync` of a page that is already in the stack.

### Changed

- `UnpresentRootComponent()` now also disposes and untracks any still-tracked or pending components through `RuntimeComponentRegistry.DisposeTrackedComponents()` as a final mop-up step.
- This makes root reset/signout consistent with a full application-context reset and prevents the new guard from reporting stale, already-dismissed components after a reset.
- The soft, DEBUG-only diagnostic for already-tracked/pending components introduced in 5.0.2 was replaced by the runtime guard.

### Compatibility

- This is a minor release for an intentional runtime fail-fast, not a patch release.
- Public API signatures are unchanged; only a new `RouterError` enum value was added.
- `DismissComponent(...)` semantics are unchanged.
- For `PushableComponent`, `DismissComponent(...)` on a top pushable remains a visual dismissal; the result must be completed by the component or consumer with `CompletionSource?.TrySetResult(...)`.
- Both orderings remain supported: `TrySetResult(...)` then `DismissComponent(...)`, and `DismissComponent(...)` then `TrySetResult(...)`.
- Modal and overlay semantics are unchanged; non-top pushable removal and owner navigation mount mapping are unchanged.

## [5.0.2] - 2026-07-01

### Fixed

- Router root reset and runtime shutdown now fully reset the internal pushable mount registry, including the finalized-component tracking set, by using `Clear()` instead of `ClearMounts()` in the full-reset paths. This prevents the finalization guard from retaining strong component references across resets.

### Changed

- Low-risk internal cleanup of `AbstractRouter`: removed dead code and stale comments.
- Extracted modal chrome DEBUG diagnostics into a dedicated `ModalChromeDiagnostics` helper.
- Extracted snackbar layout and safe-area logic into a dedicated `SnackbarLayoutApplier` helper.
- Added a soft, `Debug`-only diagnostic when `PresentComponent(...)` is called for a component instance that is already tracked with a pending presentation, to help detect re-presenting a pushable before completing its previous presentation.

### Documentation

- Documented the `PushableComponent` dismissal contract: `DismissComponent(...)` on a top pushable is a visual dismissal, and the logical result is completed separately with `CompletionSource?.TrySetResult(...)`.
- Clarified that both orderings are supported: `TrySetResult(...)` then `DismissComponent(...)`, and `DismissComponent(...)` then `TrySetResult(...)`.
- Documented that the same pushable should not be presented again before its previous presentation is completed, and that `ModalPageComponent` keeps a different, intentional semantic where completing the result also performs the modal's visual dismissal.

### Compatibility

- Patch release focused on cleanup, diagnostics, and robustness.
- No intentional breaking changes.
- `DismissComponent(...)` semantics are unchanged; modal and overlay behavior is unchanged.
- No significant new public API; only internal and test-support members were added (used by the new diagnostic and unit tests).

## [5.0.1] - 2026-07-01

### Fixed

- Added support for removing non-top pushable pages through `DismissComponent(...)`.
- Pushable pages that are no longer foreground are removed with `INavigation.RemovePage(page)` and are always removed without animation.
- The router now keeps an internal pushable mount context so a page can still be removed after the component has completed and disposed its presenter.
- The pushable mount context now stores the historical owner `INavigation` used for `PushAsync(page)`.
- `DismissComponent(...)` uses that owner navigation instead of the current foreground navigation, fixing nested root/modal navigation flows where a pushable page belongs to a previous modal navigation while a newer modal is foreground.

### Compatibility

- Patch release focused on more robust pushable page handling in nested navigation stacks.
- No public API changes.
- Modal components remain top-only.
- Overlay components are still not handled by `DismissComponent(...)`.

## [5.0.0] - 2026-06-27

### Added

- Added `Router.ShutdownAsync(...)` for explicit router runtime shutdown.
- Added `Router.BeginNewRuntime()` to reopen the router runtime after shutdown.
- Added `RouterRuntimeLifecycle` to track runtime generation and shutdown state.
- Added `RouterShutdownOptions`, `RouterShutdownReason`, and `RouterShutdownContext`.
- Added `RouterError.RouterIsShuttingDown` so navigation attempted during shutdown fails explicitly.
- Added tracked component disposal during shutdown.
- Added shutdown-aware hooks through `IRouterShutdownAwareComponent` and `IRouterShutdownAwarePresenter`.
- Added conservative MAUI page tree disconnect support during shutdown.
- Added opt-in MAUI `Window` lifecycle wiring through `window.UseComponentRoutingMauiLifecycle(router)`.
- Added opt-in Android native lifecycle wiring through `runtime.EnableAutomaticPlatformLifecycle()`.
- Added diagnostics for apps that enable automatic platform lifecycle but do not attach a `Window` with `UseComponentRoutingMauiLifecycle(...)`.

### Changed

- Router shutdown now blocks new navigation and platform chrome work while the runtime is shutting down.
- SampleApp and README now document the recommended runtime lifecycle pattern.

### Compatibility

- This is a major release for the new runtime lifecycle and shutdown model.
- No public APIs were removed in this release.
- `EnableAutomaticPlatformLifecycle()` is Android-specific and does not replace the cross-platform `UseComponentRoutingMauiLifecycle(...)` window helper.

## [4.0.2] - 2026-06-26

### Fixed

- Android chrome modal window discovery is now best-effort during app teardown and handler disconnect.
- `AndroidModalWindowDiscoveryService` now skips destroyed fragment managers and fragments that are not attached, detached, removing, or missing context/activity.
- Access to `Fragment.ChildFragmentManager` is guarded and handles `Java.Lang.IllegalStateException`, preventing chrome apply from crashing when a fragment can no longer be inspected during shutdown.

### Compatibility

- No routing, modal, or normal chrome behavior changes are intended outside Android teardown safety.

## [4.0.1] - 2026-06-22

### Changed

- Removed temporary overlay surface diagnostics from ComponentRouting.Maui and the SampleApp.
- Renamed the internal Android overlay platform surface provider to reflect root, modal, and fullscreen modal support.
- Added a SampleApp fullscreen modal overlay/snackbar demo.

### Compatibility

- No breaking changes.

## [4.0.0] - 2026-06-20

### Added

- New `MauiAppBuilder.UseComponentRoutingMaui(...)` setup API.
- Platform chrome setup now registers both DI services and MAUI platform handlers.
- iOS modal `StatusBarForeground` support through an internal status-bar-aware navigation page and renderer.
- Overlay platform surface hosts for Android and iOS root, modal, and fullscreen modal surfaces.
- Modal-aware overlay and snackbar hosting so overlays can mount over the active visual surface instead of only the current page content.
- Snackbar safe-area behavior for platform-hosted overlays.
- README platform support matrix for Android and iOS chrome behavior.
- Updated sample showing platform chrome, normal modal, and fullscreen modal behavior.
- SampleApp overlay matrix covering root, push, modal, and push-inside-modal overlay/snackbar scenarios.
- SampleApp mutable popup demo for updating and unpresenting an already mounted popup through mounted overlay lookup.

### Changed

- ComponentRouting.Maui setup is now centered on `MauiAppBuilder`.
- README and SAMPLE were updated to the 4.0.0 setup flow.
- Mounted overlay lookup is now history-based and works independently from the visual tree or platform host used to mount the overlay.

### Fixed

- Legacy empty overlay hosts no longer block input.
- `GetMountedOverlayComponent<T>()` and `GetMountedOverlayComponents<T>()` work for overlays mounted on platform hosts as well as legacy hosts.
- Legacy dismiss keeps overlay container input and visibility state coherent after removing the last child.
- Overlay diagnostics now report the actual host kind selected for a presentation.

### Breaking Changes

- `AddComponentRoutingMaui(...)` is no longer the public setup entry point.
- `AddComponentRoutingMauiPlatformChrome()` is no longer a public setup entry point.
- `UseComponentRoutingMauiPlatformChrome()` has been replaced by the unified `UseComponentRoutingMaui(...)`.
- Applications must migrate setup from `builder.Services...` to `builder.UseComponentRoutingMaui(...)`.

### Removed

- Temporary experimental API name `UseComponentRoutingMauiStatusBarModalExperiment()`.
- Temporary iOS modal diagnostics/probe code.
- App-specific chrome fallback logic.

## 3.0.0

### Breaking Changes

- Replaced `Router.GetMountedComponent<TComponent>()` with `Router.GetMountedOverlayComponent<TComponent>()`.
- Replaced `Router.GetMountedComponents<TComponent>()` with `Router.GetMountedOverlayComponents<TComponent>()`.
- Mounted lookup is now constrained to overlay components through the marker `OverlayComponent` interface.
- `AbstractRouter` exposes its routing state to subclasses instead of through the public `Router` interface.

### Added

- Added `Router.CloseAllPopups()` to close all mounted popup overlays without closing mounted snackbars.

## 2.0.0

### Breaking Changes

- Removed the plain `net10.0` NuGet asset.
- The package now ships only MAUI platform-specific assets:
  - `net10.0-android`
  - `net10.0-ios`
  - `net10.0-maccatalyst`
- Plain `net10.0` consumers that could restore 1.0.1 will no longer be compatible.

### Added

- Added `ComponentRouting.Core` as an internal implementation assembly bundled inside the `ComponentRouting.Maui` NuGet package.
- Added runnable `net10.0` unit tests for MAUI-independent routing/core logic.
- Added package content verification to ensure `ComponentRouting.Core.dll` is included for all MAUI target frameworks.

### Changed

- `ComponentRouting.Maui` now uses MAUI platform target frameworks with `<UseMaui>true</UseMaui>`.
- Improved multi-target NuGet packaging reliability.
- Restored Sample app solution build participation.

### Compatibility Notes

- Existing MAUI platform consumers should remain source-compatible.
- `ComponentRouting.Core` is bundled inside the `ComponentRouting.Maui` package and does not need to be installed manually as a separate NuGet package.
