# Changelog

## [4.0.0] - 2026-06-20

### Added

- New `MauiAppBuilder.UseComponentRoutingMaui(...)` setup API.
- Platform chrome setup now registers both DI services and MAUI platform handlers.
- iOS modal `StatusBarForeground` support through an internal status-bar-aware navigation page and renderer.
- README platform support matrix for Android and iOS chrome behavior.
- Updated sample showing platform chrome, normal modal, and fullscreen modal behavior.

### Changed

- ComponentRouting.Maui setup is now centered on `MauiAppBuilder`.
- README and SAMPLE were updated to the 4.0.0 setup flow.

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
