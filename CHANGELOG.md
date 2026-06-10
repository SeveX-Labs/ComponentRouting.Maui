# Changelog

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
