# ComponentRouting.Maui

ComponentRouting.Maui is a .NET MAUI library for building UI flows around `Component`, `Presenter`, and `Router` types. It is inspired by component-based routing patterns: a component prepares state, owns or binds a presenter, and completes with a typed result when the routed UI finishes.

The library focuses on routing and composition rather than visual styling. Your MAUI pages, layouts, popups, snackbars, tabs, and root pages remain normal MAUI views that implement the marker `Presenter` interface.

## Features

- Typed routing through `Router.PresentComponent<TComponent, TState, TResult>(state)`.
- Component preloading through `Router.PreloadComponent<TComponent, TState, TResult>(state)`.
- Push and modal dismissal through `Router.DismissComponent<TComponent, TState, TResult>(animated)`.
- Mounted overlay and snackbar lookup through `GetMountedOverlayComponent<TComponent>()` and `GetMountedOverlayComponents<TComponent>()`.
- Popup cleanup through `Router.CloseAllPopups()`.
- Extensible routing through `AbstractRouter`, including protected `RootComponent` and `CanNavigateBack(...)`.
- Component creation through `ComponentFactory.CreateComponent<T>()`.
- Microsoft dependency injection registration, component scanning, platform chrome, and MAUI handler setup with `UseComponentRoutingMaui(...)`.
- Built-in component base types for root, page, modal, push, overlay, snackbar, tab, and flyout flows.
- Singleton registration for normal components and transient registration for overlays and snackbars.

## Requirements

- .NET 10 SDK.
- .NET MAUI workload for MAUI apps and the sample app.
- `Microsoft.Extensions.DependencyInjection`.
- `NGettext`, because the public localization interfaces expose `NGettext.ICatalog`.

The package targets:

- `net10.0-android`
- `net10.0-ios`
- `net10.0-maccatalyst`

Starting from version 2.0.0, ComponentRouting.Maui is packaged only for MAUI platform target frameworks. Plain `net10.0` consumers are no longer supported. Existing MAUI consumers should remain source-compatible, but projects must target a supported MAUI platform TFM.

Starting from version 3.0.0, mounted component lookup is explicitly scoped to overlays and snackbars. Replace `GetMountedComponent<TComponent>()` with `GetMountedOverlayComponent<TComponent>()`, and replace `GetMountedComponents<TComponent>()` with `GetMountedOverlayComponents<TComponent>()`. The generic component type must be an overlay component.

Starting from version 4.0.0, setup is unified under `MauiAppBuilder`. Call `UseComponentRoutingMaui(...)` from `MauiProgram.cs`; it registers ComponentRouting services, platform chrome services, and the MAUI handlers needed by platform-specific chrome behavior.

## Installation

Install the package from NuGet:

```bash
dotnet add package ComponentRouting.Maui --version 4.0.0
```

## Breaking Changes In 4.0.0

- Setup now uses `MauiAppBuilder.UseComponentRoutingMaui(...)`.
- The previous public service-collection setup extensions were removed from the public API.
- Platform chrome registration is part of `UseComponentRoutingMaui(...)` because ComponentRouting.Maui must register both DI services and MAUI handlers.

## Breaking Changes In 3.0.0

- `Router.GetMountedComponent<TComponent>()` was replaced by `Router.GetMountedOverlayComponent<TComponent>()`.
- `Router.GetMountedComponents<TComponent>()` was replaced by `Router.GetMountedOverlayComponents<TComponent>()`.
- Mounted lookup is now constrained to overlay components through the marker `Component` interface.
- `GetMountedOverlayComponent<TComponent>()` returns the latest matching mounted overlay by default when multiple matches exist. Pass `throwIfMultiple: true` to preserve strict single-match behavior.
- `AbstractRouter` exposes its routing state to subclasses instead of through the public `Router` interface. Router implementations should override `RootComponent` as `protected`.

## Basic Setup

Register ComponentRouting.Maui in `MauiProgram.cs` and include the assembly that contains your components and router:

```csharp
using ComponentRouting.Maui;
using ComponentRouting.Maui.Ioc;
using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Sample.Routing;
using ComponentRouting.Maui.Sample.Services;
using ComponentRouting.Maui.Service.Core;

builder.UseComponentRoutingMaui(
    assemblies: new[] { typeof(SampleRouter).Assembly },
    additionalManifestScopeNamePrefixes: null);

builder.Services.AddSingleton<SampleRouter>();
builder.Services.AddSingleton<Router>(sp => sp.GetRequiredService<SampleRouter>());
builder.Services.AddSingleton<CatalogProvider, SampleCatalogProvider>();
builder.Services.AddSingleton<SafeAreaInsetsService, SampleSafeAreaInsetsService>();
```

`UseComponentRoutingMaui(...)` scans exported types from the provided assemblies, registers concrete components, registers `ComponentFactory`, configures platform chrome, registers required MAUI handlers, and registers the first discovered concrete `CatalogProvider`, `LocaleProvider`, and `Router` when available. Pass `additionalManifestScopeNamePrefixes` when your component manifests are generated in additional namespace scopes that should be included during discovery. The sample registers its router and required services explicitly.

## Platform Chrome And Fullscreen Modals

`UseComponentRoutingMaui(...)` accepts an optional `configureChrome` callback for route-scoped platform chrome defaults. The callback configures the `ComponentChromeConfiguration` registered in DI. In 4.0.0 this setup lives on `MauiAppBuilder` because ComponentRouting.Maui must register DI services and platform-specific MAUI handlers in one place.

```csharp
using ComponentRouting.Maui;
using ComponentRouting.Maui.Chrome;
using ComponentRouting.Maui.Ioc;
using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Sample.Routing;
using ComponentRouting.Maui.Sample.Services;
using ComponentRouting.Maui.Service.Core;
using Microsoft.Maui.Graphics;

builder.UseComponentRoutingMaui(
    assemblies: new[] { typeof(SampleRouter).Assembly },
    additionalManifestScopeNamePrefixes: null,
    configureChrome: chrome =>
    {
        var normalChromeColor = Color.FromArgb("#334155");
        var normalChrome = new ComponentChromeOptions
        {
            StatusBarBackgroundColor = normalChromeColor,
            NavigationBarBackgroundColor = normalChromeColor,
            ActionBarBackgroundColor = normalChromeColor,
            WindowBackgroundColor = normalChromeColor,
            StatusBarForeground = ChromeForeground.LightContent,
            NavigationBarForeground = ChromeForeground.LightContent,
            ActionBarTextColor = Colors.White,
            EdgeToEdge = false,
            DecorFitsSystemWindows = true,
            DisplayCutoutMode = ComponentDisplayCutoutMode.Default
        };

        chrome.GlobalDefaults = normalChrome;
        chrome.PageDefaults = normalChrome;
        chrome.PushableDefaults = normalChrome;
        chrome.ModalDefaults = normalChrome;
        chrome.FullscreenModalDefaults = new ComponentChromeOptions
        {
            StatusBarBackgroundColor = Colors.Transparent,
            NavigationBarBackgroundColor = Colors.Transparent,
            WindowBackgroundColor = Colors.Transparent,
            StatusBarForeground = ChromeForeground.LightContent,
            NavigationBarForeground = ChromeForeground.LightContent,
            ActionBarTextColor = Colors.White,
            EdgeToEdge = true,
            DecorFitsSystemWindows = false,
            DisplayCutoutMode = ComponentDisplayCutoutMode.Always,
            StatusBarContrastEnforced = false,
            NavigationBarContrastEnforced = false
        };
    });

builder.Services.AddSingleton<SampleRouter>();
builder.Services.AddSingleton<Router>(sp => sp.GetRequiredService<SampleRouter>());
builder.Services.AddSingleton<CatalogProvider, SampleCatalogProvider>();
builder.Services.AddSingleton<SafeAreaInsetsService, SampleSafeAreaInsetsService>();
```

`ComponentChromeConfiguration` resolves options in this order: `LibraryDefaults`, `GlobalDefaults`, the presentation defaults for the current route (`PageDefaults`, `PushableDefaults`, `ModalDefaults`, or `FullscreenModalDefaults`), and finally any entry in `ComponentOverrides` for the component type. Each `ComponentChromeOptions` value is nullable. A higher-priority non-null value replaces the lower-priority value; a null value leaves the lower-priority value unchanged.

`ComponentChromeOptions` platform support:

| Option | Android | iOS | Notes |
| --- | --- | --- | --- |
| `StatusBarBackgroundColor` | Supported | No-op | Android exposes status bar color. iOS does not have a separate status bar background; the visible color comes from the view/controller content behind it. |
| `StatusBarForeground` | Supported | Supported | Android maps to system bar appearance flags. iOS is controller-based: root/flyout use the ComponentRouting status bar host, and modal `NavigationPage` routes with explicit `LightContent`/`DarkContent` use an internal navigation renderer. `Auto` is treated as no explicit request. |
| `NavigationBarBackgroundColor` | Supported | No-op | Android applies the system navigation bar color. iOS does not expose an equivalent bottom system navigation bar. |
| `NavigationBarForeground` | Supported | No-op | Android maps to navigation bar icon appearance. iOS has no equivalent bottom system navigation bar icon foreground. |
| `ActionBarBackgroundColor` | No-op | Supported | iOS applies it to MAUI `NavigationPage` navigation bar appearance. Android keeps this option available for app conventions but does not apply it to the activity window. |
| `ActionBarTextColor` | No-op | Supported | iOS applies it to MAUI navigation bar title/button text. Android keeps this option available for app conventions but does not apply it to the activity window. |
| `WindowBackgroundColor` | Supported | No-op/limited | Android applies it to the window and decor background. iOS intentionally avoids applying it to view controllers because it can cover routed content. |
| `DecorFitsSystemWindows` | Supported | No-op | Android controls decor fitting. iOS safe area is handled through MAUI `SafeAreaEdges`. |
| `DisplayCutoutMode` | Supported | No-op | Android-only display cutout layout behavior. |
| `StatusBarContrastEnforced` | Supported on Android versions that expose it | No-op | Android-only contrast enforcement. |
| `NavigationBarContrastEnforced` | Supported on Android versions that expose it | No-op | Android-only contrast enforcement. |
| Safe area behavior | Supported through router policy | Supported through router policy | `FullscreenModal` uses `SafeAreaEdges.None`; all other presentation kinds use `SafeAreaEdges.Container`. |
| `FullscreenModal` behavior | Supported | Supported | The router applies fullscreen modal presentation policy and fullscreen safe-area defaults without changing the component presenter layout. |

On iOS, status bar foreground is resolved by UIKit from the active view controller. ComponentRouting.Maui therefore uses different mechanisms for root/flyout routes and modal routes. Root/flyout routes are handled by the root status bar host. Modal navigation routes with explicit `StatusBarForeground` are handled by an internal `NavigationPage`/renderer pair so UIKit receives the requested `PreferredStatusBarStyle`.

Chrome options are intentionally all-null by default. If you do not pass `configureChrome`, every option remains unset unless you configure it elsewhere. This keeps ComponentRouting.Maui conservative: it will not change system bar colors, foregrounds, cutout handling, contrast enforcement, window background, or decor fitting unless you opt in.

### Default behavior

- No `configureChrome`: chrome options resolve as all-null, so platform chrome services are active but have no platform window changes to apply.
- `configureChrome`: the router resolves your defaults per presentation kind and applies them through the platform chrome service.

### Fullscreen modal routing

Use `FullscreenModalPageComponent<TState, TResult>` for modal pages that should resolve `FullscreenModalDefaults` and use the fullscreen safe-area policy.

```csharp
using ComponentRouting.Maui;
using ComponentRouting.Maui.Abstraction;

public sealed class FullscreenChromeDemoComponent
    : FullscreenModalPageComponent<FullscreenChromeDemoComponent.ComponentState, bool>
{
    public readonly record struct ComponentState(string Title, string Message);

    protected override Presenter CreatePresenter()
    {
        return new FullscreenChromeDemoPage();
    }

    protected override Task Configure(ComponentState state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(ComponentState state)
    {
        ((FullscreenChromeDemoPage)Presenter!).Initialize(
            state.Title,
            state.Message,
            () => CompletionSource?.TrySetResult(true));
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }
}
```

The router applies a safe-area policy to pages it presents. Fullscreen modals get `SafeAreaEdges.None`; all other presentation kinds get `SafeAreaEdges.Container`. This policy is applied to the mountable page, the component presenter when it is a `Page`, and the navigation page created for navigation components. The router does not traverse `ContentPage.Content`, root grids, borders, scroll views, or arbitrary controls inside the page. If your fullscreen content needs internal padding, apply it in the page layout.

With the platform chrome service, route-specific chrome replaces app-side window code such as `ApplyChromeToWindow`, app-level `AppChromeMode` switches, manual Android `Dialog.Window` discovery, and Android-specific reapply code in pages. Keep platform chrome in `MauiProgram.cs` and keep fullscreen modal presenters focused on their UI and completion callbacks.

## Router

Create a router by deriving from `AbstractRouter`:

```csharp
using ComponentRouting.Maui;
using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Routing;
using ComponentRouting.Maui.Sample.Components.Root;
using ComponentRouting.Maui.Service.Core;

public sealed class SampleRouter : AbstractRouter
{
    public SampleRouter(
        ComponentFactory componentFactory,
        CatalogProvider catalogProvider,
        SafeAreaInsetsService safeAreaInsetsService)
        : base(componentFactory, catalogProvider, safeAreaInsetsService)
    {
    }

    protected override RootComponent RootComponent =>
        ComponentFactory.CreateComponent<SampleModeRootComponent>();

    protected override bool CanNavigateBack(Component component)
    {
        return true;
    }
}
```

The router creates components through `ComponentFactory`, applies localization for `LocalizableComponent`, presents the component on the MAUI main thread, and resolves the component's typed result.

## Minimal Page Component

A page flow pairs a component with a MAUI page that implements `Presenter`:

```csharp
using ComponentRouting.Maui;
using ComponentRouting.Maui.Abstraction;
using Microsoft.Maui.Controls;

public sealed class LoginComponent
    : PageComponent<LoginComponent.ComponentState, LoginResult>
{
    public readonly record struct ComponentState(string Title, string Message);

    protected override Presenter CreatePresenter()
    {
        return new LoginPage();
    }

    protected override Task Configure(ComponentState state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(ComponentState state)
    {
        ((LoginPage)Presenter!).Initialize(
            state.Title,
            state.Message,
            () => CompletionSource?.TrySetResult(LoginResult.SignedIn),
            () => CompletionSource?.TrySetResult(LoginResult.Cancelled));

        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }
}

public enum LoginResult
{
    SignedIn,
    Cancelled
}

public partial class LoginPage : ContentPage, Presenter
{
    private Action? complete;
    private Action? cancel;

    public void Initialize(string title, string message, Action complete, Action cancel)
    {
        Title = title;
        this.complete = complete;
        this.cancel = cancel;
    }
}
```

Open the component through the injected `Router`:

```csharp
var result = await router.PresentComponent<LoginComponent, LoginComponent.ComponentState, LoginResult>(
    new LoginComponent.ComponentState("Login page", "Complete this page to return a result."));
```

The generic arguments keep the component state and result explicit at the call site.

## Component Types

- `RootComponent` mounts the app's root presenter.
- `PageComponent<TState, TResult>` replaces the current window page.
- `ModalPageComponent<TState, TResult>` presents a modal MAUI page.
- `PushableComponent<TState, TResult>` pushes a page onto the current navigation stack.
- `OverlayComponent<TState, TResult>` mounts a MAUI layout over an `OverlayHost` and implements the marker `Component` interface used by mounted overlay lookup.
- `SnackbarComponent` is an overlay specialized for `SnackbarConfiguration`.
- `TabComponent<TState>` represents a tab-bound component.
- `FlyoutComponent<TState>` is supported by the router as a flyout-bound component type.

## Overlays And Snackbars

Overlays and snackbars are registered as transient components, so each presentation gets a separate instance.

```csharp
_ = router.PresentComponent<LoadingPopupComponent, LoadingPopupComponent.ComponentState, bool>(
    new LoadingPopupComponent.ComponentState("Loading popup", "Mounted lookup can hide this overlay."));

router.GetMountedOverlayComponent<LoadingPopupComponent>()?.Unpresent();
```

Snackbars use `SnackbarConfiguration`:

```csharp
_ = router.PresentComponent<InfoSnackbarComponent, SnackbarConfiguration, bool>(
    new SnackbarConfiguration("Saved", mustCloseAutomatically: true, closureDelayMs: 3000));
```

Mounted lookup searches the router's overlay and snackbar history:

```csharp
var popup = router.GetMountedOverlayComponent<LoadingPopupComponent>();
var snackbars = router.GetMountedOverlayComponents<InfoSnackbarComponent>();
```

`GetMountedOverlayComponent<TComponent>()` returns `null` when no mounted instance exists and returns the latest matching mounted overlay or snackbar by default. Pass `throwIfMultiple: true` to throw `InvalidOperationException` when multiple matching instances exist. Use `GetMountedOverlayComponents<TComponent>()` when multiple overlays or snackbars can be present.

Close all mounted popup overlays without closing mounted snackbars:

```csharp
router.CloseAllPopups();
```

`CloseAllPopups()` is idempotent. Calling it when no popup overlays are mounted does nothing.

## Tabs And Flyout

The sample app starts with a mode root component that can open either a tabbed root flow or a flyout root flow. Tab routing binds existing tab presenters to `TabComponent<TState>` instances through `ComponentFactory.CreateComponent<T>()`.

`FlyoutComponent<TState>` is also a supported router component type, and the sample includes flyout home, customers, and settings components.

## Sample App

The sample app is in `SAMPLE/ComponentRouting.Maui.Sample`.

It demonstrates:

- root mode selection through `SampleModeRootComponent`;
- tabbed navigation through `SampleTabbedRootComponent`;
- flyout navigation through `SampleFlyoutRootComponent`;
- page routing with `LoginComponent`;
- modal routing with `DetailsComponent`, using platform chrome defaults and light status bar foreground;
- fullscreen modal routing with `FullscreenChromeDemoComponent` and fullscreen platform chrome defaults;
- route-specific light/dark status bar foreground through `ComponentChromeOptions` defaults and overrides;
- pushable wizard flow with `WizardStepComponent` and `WizardConfirmComponent`;
- overlay presentation with `LoadingPopupComponent`;
- closing mounted popup overlays with `CloseAllPopups()`;
- snackbar presentation with `InfoSnackbarComponent`;
- mounted overlay and snackbar lookup;
- builder setup through `UseComponentRoutingMaui(...)`, plus `SampleRouter`, `CatalogProvider`, and `SafeAreaInsetsService` registration.

The app creates an initial placeholder window page, then presents the root component when the window is created.

## Build And Test

```bash
dotnet restore ComponentRouting.Maui.sln
dotnet build ComponentRouting.Maui/ComponentRouting.Maui.csproj -f net10.0-android -c Release --no-restore
dotnet test ComponentRouting.Maui.Tests/ComponentRouting.Maui.Tests.csproj -c Release --no-restore
dotnet build SAMPLE/ComponentRouting.Maui.Sample/ComponentRouting.Maui.Sample.csproj -f net10.0-android -c Debug
```

The Android sample build requires the .NET MAUI workload and Android platform tooling.


## Changelog

See [CHANGELOG.md](CHANGELOG.md) for release notes and compatibility changes.

## Current Limitations

- Dependency injection integration is based on `Microsoft.Extensions.DependencyInjection`.
- Mounted component lookup is intentionally scoped to overlays and snackbars, not root, page, tab, flyout, modal, or push stack components.

## Local development with ProjectReference

This library is intended to be consumed primarily as a NuGet package.

For local development, debugging, or testing changes before publishing a new package, you can also clone the repository and reference the project directly with a `ProjectReference` from a consuming .NET MAUI app.

This mode is optional and should be treated as a development-only workflow. Normal consumers should use the NuGet package.

### Enable ProjectReference mode locally

To enable local `ProjectReference` mode, create a file named:

```text
Directory.Build.local.props
```

in the same directory as:

```text
Directory.Build.props
```

Do not commit this file. It is meant to contain local machine/developer settings only.

Recommended local configuration:

```xml
<Project>
	<PropertyGroup>
		<UseAsProjectReference>true</UseAsProjectReference>
		<OverrideAndroidSpecificVersion>36.0</OverrideAndroidSpecificVersion>
		<!-- Optional, only if the consuming app requires a specific MacCatalyst platform version. -->
		<!-- <OverrideMacCatalystSpecificVersion>26.0</OverrideMacCatalystSpecificVersion> -->
	</PropertyGroup>
</Project>
```

### What this does

By default, the project uses package-oriented, generic .NET MAUI platform TFMs, for example:

```text
net10.0-ios
net10.0-android
```

When `UseAsProjectReference` is enabled, the project can adjust its target frameworks to match the platform-specific target required by a consuming app.

For example, with:

```xml
<OverrideAndroidSpecificVersion>36.0</OverrideAndroidSpecificVersion>
```

the Android target becomes:

```text
net10.0-android36.0
```

This is useful when a consuming app targets a specific Android platform version and the library is referenced directly as a project instead of as a NuGet package.

If `UseAsProjectReference=true` is set and `OverrideAndroidSpecificVersion` is not provided, the project is configured to fall back to Android `36.0` for ProjectReference mode. Setting the value explicitly is still recommended because it makes the consuming setup easier to read.

### Optional iOS override

If a consuming app requires a specific iOS platform version, use:

```xml
<OverrideIosSpecificVersion>26.0</OverrideIosSpecificVersion>
```

In that case, the iOS target becomes:

```text
net10.0-ios26.0
```

If `OverrideIosSpecificVersion` is not set, the iOS target remains generic:

```text
net10.0-ios
```

### Optional MacCatalyst override

Projects that include MacCatalyst also support:

```xml
<OverrideMacCatalystSpecificVersion>26.0</OverrideMacCatalystSpecificVersion>
```

When set together with `UseAsProjectReference=true`, this changes the MacCatalyst target to:

```text
net10.0-maccatalyst26.0
```

### Important notes

- `Directory.Build.local.props` is for local development only.
- Do not commit `Directory.Build.local.props`.
- Normal NuGet builds and CI builds should run without this local file.
- When the local file is not present, `UseAsProjectReference` defaults to `false`.
- When `UseAsProjectReference` is `false`, the project uses its normal package-oriented target frameworks.
- If the project supports MacCatalyst, `OverrideMacCatalystSpecificVersion` can be used in the same way as the Android and iOS overrides.
- If you switch between package mode and project-reference mode, clean `bin` and `obj` folders before rebuilding.
- Restore and build should be performed in the same mode. If restore runs with local overrides enabled, build should use the same overrides.
- If Rider keeps building against an old Android/iOS target after switching modes, reload all projects. If the problem persists, use **File > Invalidate Caches...** and reopen the solution.

## Packing and testing the NuGet package locally

When creating or testing the NuGet package, make sure the local ProjectReference overrides are disabled. Otherwise the package can be produced with development-specific target frameworks.

Before packing, temporarily rename the local props file if it exists:

```bash
mv Directory.Build.local.props Directory.Build.local.props.disabled
```

Then clean generated folders from the repository root:

```bash
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
```

Pack the library by calling `dotnet pack` directly on the library `.csproj`, not on the solution root:

```bash
dotnet pack ComponentRouting.Maui/ComponentRouting.Maui.csproj \
  -c Release \
  -o ./local-nuget
```

Packing the concrete library project avoids unintentionally building sample apps, tests, or other projects in the solution.

After packing, you can re-enable your local development settings:

```bash
mv Directory.Build.local.props.disabled Directory.Build.local.props
```

To inspect the generated package contents:

```bash
unzip -l ./local-nuget/ComponentRouting.Maui.4.0.0.nupkg | grep "lib/"
```

With .NET MAUI/.NET 10, it is normal for the generated `.nupkg` to contain platform-normalized asset folders such as:

```text
lib/net10.0-android36.0/
lib/net10.0-ios26.0/
```

even when the project file declares generic TFMs such as `net10.0-android` or `net10.0-ios`. Those platform versions are resolved by the installed .NET SDK/workloads during build/pack.

To test the package without publishing it, add `./local-nuget` as a local NuGet source in a consuming app and use the normal `PackageReference` workflow. This is the best way to verify the package as a real consumer would use it.

## License

ComponentRouting.Maui is released under the [MIT License](LICENSE).
