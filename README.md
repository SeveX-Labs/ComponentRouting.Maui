# ComponentRouting.Maui

ComponentRouting.Maui is a .NET MAUI library for building UI flows around `Component`, `Presenter`, and `Router` types. It is inspired by component-based routing patterns: a component prepares state, owns or binds a presenter, and completes with a typed result when the routed UI finishes.

The library focuses on routing and composition rather than visual styling. Your MAUI pages, layouts, popups, snackbars, tabs, and root pages remain normal MAUI views that implement the marker `Presenter` interface.

## Features

- Typed routing through `Router.PresentComponent<TComponent, TState, TResult>(state)`.
- Component preloading through `Router.PreloadComponent<TComponent, TState, TResult>(state)`.
- Push and modal dismissal through `Router.DismissComponent<TComponent, TState, TResult>(animated)`.
- Mounted overlay and snackbar lookup through `GetMountedComponent<TComponent>()` and `GetMountedComponents<TComponent>()`.
- Extensible routing through `AbstractRouter`, including `RootComponent` and `CanNavigateBack(...)`.
- Component creation through `ComponentFactory.CreateComponent<T>()`.
- Microsoft dependency injection registration and component scanning with `AddComponentRoutingMaui(...)`.
- Built-in component base types for root, page, modal, push, overlay, snackbar, tab, and flyout flows.
- Singleton registration for normal components and transient registration for overlays and snackbars.

## Requirements

- .NET 10 SDK.
- .NET MAUI workload for MAUI apps and the sample app.
- `Microsoft.Extensions.DependencyInjection`.
- `NGettext`, because the public localization interfaces expose `NGettext.ICatalog`.

The package targets:

- `net10.0`

ComponentRouting.Maui is designed for .NET MAUI applications. Android, iOS, and MacCatalyst apps can consume the `net10.0` assembly from their platform-specific targets because the library contains shared routing and presenter logic, not platform-specific handlers, resources, or implementations.

## Installation

Install the package from NuGet:

```bash
dotnet add package ComponentRouting.Maui --version 1.0.0
```

## Basic Setup

Register ComponentRouting.Maui in `MauiProgram.cs` and include the assembly that contains your components and router:

```csharp
using ComponentRouting.Maui;
using ComponentRouting.Maui.Ioc;
using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Sample.Routing;
using ComponentRouting.Maui.Sample.Services;
using ComponentRouting.Maui.Service.Core;

builder.Services.AddComponentRoutingMaui(typeof(SampleRouter).Assembly);
builder.Services.AddSingleton<SampleRouter>();
builder.Services.AddSingleton<Router>(sp => sp.GetRequiredService<SampleRouter>());
builder.Services.AddSingleton<CatalogProvider, SampleCatalogProvider>();
builder.Services.AddSingleton<SafeAreaInsetsService, SampleSafeAreaInsetsService>();
```

`AddComponentRoutingMaui(...)` scans exported types from the provided assemblies, registers concrete components, registers `ComponentFactory`, and registers the first discovered concrete `CatalogProvider`, `LocaleProvider`, and `Router` when available. The sample registers its router and required services explicitly.

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
    
    public override RootComponent RootComponent =>
        ComponentFactory.CreateComponent<SampleTabbedRootComponent>();

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
- `OverlayComponent<TState, TResult>` mounts a MAUI layout over an `OverlayHost`.
- `SnackbarComponent` is an overlay specialized for `SnackbarConfiguration`.
- `TabComponent<TState>` represents a tab-bound component.
- `FlyoutComponent<TState>` is supported by the router as a flyout-bound component type.

## Overlays And Snackbars

Overlays and snackbars are registered as transient components, so each presentation gets a separate instance.

```csharp
_ = router.PresentComponent<LoadingPopupComponent, LoadingPopupComponent.ComponentState, bool>(
    new LoadingPopupComponent.ComponentState("Loading popup", "Mounted lookup can hide this overlay."));

router.GetMountedComponent<LoadingPopupComponent>()?.Unpresent();
```

Snackbars use `SnackbarConfiguration`:

```csharp
_ = router.PresentComponent<InfoSnackbarComponent, SnackbarConfiguration, bool>(
    new SnackbarConfiguration("Saved", mustCloseAutomatically: true, closureDelayMs: 3000));
```

Mounted lookup searches the router's overlay and snackbar history:

```csharp
var popup = router.GetMountedComponent<LoadingPopupComponent>();
var snackbars = router.GetMountedComponents<InfoSnackbarComponent>();
```

`GetMountedComponent<TComponent>()` returns `null` when no mounted instance exists, returns the single match when exactly one is mounted, and throws `InvalidOperationException` when multiple matching instances exist. Use `GetMountedComponents<TComponent>()` when multiple overlays or snackbars can be present.

## Tabs And Flyout

The sample app shows tab routing with a `RootComponent` that creates a tabbed root page and binds existing tab presenters to `TabComponent<TState>` instances through `ComponentFactory.CreateComponent<T>()`.

`FlyoutComponent<TState>` is also a supported router component type. This repository does not currently include a flyout sample.

## Sample App

The sample app is in `SAMPLE/ComponentRouting.Maui.Sample`.

It demonstrates:

- root and tabbed navigation through `SampleTabbedRootComponent`;
- page routing with `LoginComponent`;
- modal routing with `DetailsComponent`;
- pushable wizard flow with `WizardStepComponent` and `WizardConfirmComponent`;
- overlay presentation with `LoadingPopupComponent`;
- snackbar presentation with `InfoSnackbarComponent`;
- mounted overlay and snackbar lookup;
- DI registration through `AddComponentRoutingMaui(...)`, `SampleRouter`, `CatalogProvider`, and `SafeAreaInsetsService`.

The app creates an initial placeholder window page, then presents the root component when the window is created.

## Build And Test

```bash
dotnet restore ComponentRouting.Maui.sln
dotnet build ComponentRouting.Maui/ComponentRouting.Maui.csproj -f net10.0 -c Release --no-restore
dotnet test ComponentRouting.Maui.Tests/ComponentRouting.Maui.Tests.csproj -c Release --no-restore
dotnet build SAMPLE/ComponentRouting.Maui.Sample/ComponentRouting.Maui.Sample.csproj -f net10.0-android -c Debug
```

The Android sample build requires the .NET MAUI workload and Android platform tooling.

## Packaging

Create NuGet packages locally:

```bash
dotnet pack ComponentRouting.Maui/ComponentRouting.Maui.csproj -c Release -o artifacts
```

This produces the package and symbol package:

- `artifacts/ComponentRouting.Maui.1.0.0.nupkg`
- `artifacts/ComponentRouting.Maui.1.0.0.snupkg`

Publishing is intentionally manual or release-workflow driven. Do not commit NuGet API keys to the repository.

## Current Limitations

- Dependency injection integration is based on `Microsoft.Extensions.DependencyInjection`.
- Mounted component lookup is intentionally scoped to overlays and snackbars, not root, page, tab, flyout, modal, or push stack components.

## License

ComponentRouting.Maui is released under the [MIT License](LICENSE).
