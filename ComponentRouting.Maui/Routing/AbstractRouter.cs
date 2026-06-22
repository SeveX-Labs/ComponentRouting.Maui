using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Abstraction.Core;
using ComponentRouting.Maui.Chrome;
using ComponentRouting.Maui.Exceptions;
using ComponentRouting.Maui.Extension;
using ComponentRouting.Maui.Model.Core;
using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Service.Core;

namespace ComponentRouting.Maui.Routing;

public abstract class AbstractRouter : Router
{
    #region auto-properties

    protected Component? CurrentTabComponent { get; private set; }
    protected Component? CurrentFlyoutComponent { get; private set; }
    protected Component? MountedComponent { get; private set; }
    protected List<Component> ComponentsStack { get; private set; }

    protected abstract RootComponent RootComponent { get; }

    protected ComponentFactory ComponentFactory { get; }
    private CatalogProvider CatalogProvider { get; }
    protected SafeAreaInsetsService SafeAreaInsetsService { get; }
    private ComponentChromeService? ChromeService { get; }
    private ComponentChromeOptionsResolver? ChromeOptionsResolver { get; }

    protected View SafeAreaBottomPatch { get; set; }

    private ComponentHistory History { get; }
    private OverlaySurfaceResolver OverlaySurfaceResolver { get; }
    private OverlaySurfaceOwnershipRegistry OverlaySurfaceOwnership { get; }
    // private List<ComponentHistoryItem> Panels { get; set; }

    #endregion

    #region ctor(s)

    protected AbstractRouter(ComponentFactory componentFactory, CatalogProvider catalogProvider, SafeAreaInsetsService safeAreaInsetsService)
        : this(componentFactory, catalogProvider, safeAreaInsetsService, null, null)
    {
    }

    protected AbstractRouter(
        ComponentFactory componentFactory,
        CatalogProvider catalogProvider,
        SafeAreaInsetsService safeAreaInsetsService,
        ComponentChromeService? chromeService,
        ComponentChromeOptionsResolver? chromeOptionsResolver)
    {
        ComponentFactory = componentFactory;
        CatalogProvider = catalogProvider;
        SafeAreaInsetsService = safeAreaInsetsService;
        ChromeService = chromeService;
        ChromeOptionsResolver = chromeOptionsResolver;

        ComponentsStack = new List<Component>();
        History = new ComponentHistory();
        OverlaySurfaceResolver = new OverlaySurfaceResolver();
        OverlaySurfaceOwnership = new OverlaySurfaceOwnershipRegistry();
        // Panels = new List<ComponentHistoryItem>();
    }

    #endregion

    #region abstract methods

    protected abstract bool CanNavigateBack(Component component);

    protected virtual ComponentPresentationKind GetPresentationKind(Component component)
    {
        if (component.IsSubclassOfRawGeneric(typeof(FullscreenModalPageComponent<,>)))
            return ComponentPresentationKind.FullscreenModal;

        if (component.IsSubclassOfRawGeneric(typeof(ModalPageComponent<,>)))
            return ComponentPresentationKind.Modal;

        if (component.IsSubclassOfRawGeneric(typeof(PushableComponent<,>)))
            return ComponentPresentationKind.Pushable;

        return ComponentPresentationKind.Page;
    }

    #endregion

    #region Router implementation

    public async Task PreloadComponent<TComponent, TState, TResult>(TState input)
        where TComponent : RoutableComponent<TState, TResult>
    {
        if (Application.Current is null)
            return;

        var component = ComponentFactory.CreateComponent<TComponent>();
        await PrepareComponent(component, input);
    }

    public async Task<TResult> PresentComponent<TComponent, TState, TResult>(TState input)
        where TComponent : RoutableComponent<TState, TResult>
    {
        if (Application.Current is null)
            throw new RouterException(RouterError.ApplicationCurrentIsNull);

        var component = ComponentFactory.CreateComponent<TComponent>();
        await PrepareComponent(component, input);

        var didPresent = await MainThread.InvokeOnMainThreadAsync(async () => await PresentComponent(component, input));
        if (!didPresent)
            throw new RouterException(RouterError.ComponentPresentationFailed, component);

        try
        {
            var result = await component.Present();
            await HandleComponentResult(component);
            return result;
        }
        catch (RouterException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            throw new RouterException(RouterError.PresentMethodInvokeFailed, component, ex);
        }
    }

    public TComponent? GetMountedOverlayComponent<TComponent>(bool throwIfMultiple = false)
        where TComponent : Component
    {
        return History.GetMountedOverlayComponent<TComponent>(throwIfMultiple);
    }

    public IReadOnlyList<TComponent> GetMountedOverlayComponents<TComponent>()
        where TComponent : Component
    {
        return History.GetMountedOverlayComponents<TComponent>();
    }

    public void CloseAllPopups()
    {
        History.CloseAllPopups(ClosePopupComponent);
    }

    public virtual async Task DismissComponent<TComponent, TState, TResult>(bool animated = true)
        where TComponent : RoutableComponent<TState, TResult>
    {
        if (Application.Current is null)
            throw new RouterException(RouterError.ApplicationCurrentIsNull);

        var component = ComponentFactory.CreateComponent<TComponent>();
        if (component.IsSubclassOfRawGeneric(typeof(PushableComponent<,>)))
        {
            await PopComponentInternal(component, animated);
        }
        else if (component.IsSubclassOfRawGeneric(typeof(ModalPageComponent<,>)))
        {
            _ = await PopModalComponentInternal(component, animated);
        }
        else
        {
            throw new RouterException(RouterError.ComponentDismissalNotSupported, component);
        }
    }

    public async Task DispatchSleep()
    {
        await DispatchLifecycleEvent(component => component.HandleAppSleepAsync());
    }

    public async Task DispatchDestroy()
    {
        await DispatchLifecycleEvent(component => component.HandleAppDestroyAsync());
    }

    public bool OnDeviceBackPressed()
    {
        bool resolved = IsDeviceBackPressedResolved();
        _ = HandleDeviceBackPressedInternal();
        return resolved;
    }

    public void DispatchResume()
    {
        foreach (var component in GetResumeComponents())
            component.Resume();
    }

    public virtual Task UnpresentRootComponent()
    {
        if (History.Snackbars.Any())
        {
            foreach (var snackbar in History.ClearSnackbars())
                snackbar.Unpresent();
        }

        if (History.Popups.Any())
        {
            foreach (var popup in History.ClearPopups())
                popup.Unpresent();
        }

        // if (Panels.Any()) _ = Panels.Select(p => { p.Component.Unpresent(); return p; }).ToList();
        // Panels = new List<ComponentHistoryItem>();

        if (ComponentsStack.Any())
        {
            var componentsClone = ComponentsStack.ToList();
            foreach (var component in componentsClone)
                component.Unpresent();
        }
        ComponentsStack = new List<Component>();

        CurrentTabComponent?.Unpresent();
        CurrentFlyoutComponent?.Unpresent();
        MountedComponent?.Unpresent();

        CurrentTabComponent = null;
        CurrentFlyoutComponent = null;
        MountedComponent = null;
        OverlaySurfaceOwnership.Clear("UnpresentRootComponent");

        return Task.CompletedTask;
    }

    public virtual Task UnpresentComponentStack()
    {
        if (ComponentsStack.Any())
        {
            foreach (var component in ComponentsStack)
            {
                try
                {
                    component.Unpresent();
                }
                catch (Exception ex)
                {
                    // ignored
                    Debug.WriteLine(ex);
                }
            }
        }

        return Task.CompletedTask;
    }

    #endregion

    #region access methods

    protected virtual async Task HandleComponentResult(Component component)
    {
        try
        {
            if (component.IsSubclassOfRawGeneric(typeof(OverlayComponent<,>)))
            {
                DismissOverlayComponent(component);
            }
            else if (component.IsSubclassOfRawGeneric(typeof(ModalPageComponent<,>)))
            {
                await PopModalComponentInternal(component, true);
            }
            else if (component.IsSubclassOfRawGeneric(typeof(TabComponent<>)))
            {
                OverlaySurfaceOwnership.Remove(component, "HandleComponentResultTab");
                CurrentTabComponent = null;
            }
            else if (component.IsSubclassOfRawGeneric(typeof(FlyoutComponent<>)))
            {
                OverlaySurfaceOwnership.Remove(component, "HandleComponentResultFlyout");
                CurrentFlyoutComponent = null;
            }

            RemoveFromComponentsStack(component);

            component.Dispose();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    protected virtual async Task<bool> PresentComponent<TState>(Component component, TState input)
    {
        EnsureApplication(component);

        bool didPresent = false;
        var presentationKind = GetPresentationKind(component);

        ApplyComponentSafeAreaPolicy(component, presentationKind);
        RegisterComponentChromeLifecycle("BeforePresentComponent", component, presentationKind);
        ApplyComponentChrome("BeforePresentComponent", component, presentationKind);

        if (component is RootComponent rootComponent)
        {
            try
            {
                NavigationPage? navigationPage = rootComponent.Navigation;
                if (navigationPage is null && rootComponent.Presenter is Page rootPage)
                {
                    if (rootPage is not FlyoutPage && rootPage is not TabbedPage && rootPage is not NavigationPage)
                    {
                        navigationPage = CreateNavigationPage(rootPage);
                        rootComponent.Navigation = navigationPage;
                        ApplyComponentSafeAreaPolicy(component, presentationKind, navigationPage);
                        ApplyComponentChrome("AfterCreateRootNavigationPage", component, presentationKind, navigationPage);
                        Application.Current!.Windows[0].Page = navigationPage;
                    }
                    else
                    {
                        Application.Current!.Windows[0].Page = rootPage;
                    }

                    MountedComponent = rootComponent;
                    CurrentTabComponent = null;
                    CurrentFlyoutComponent = null;
                    OverlaySurfaceOwnership.Set(rootComponent, OverlaySurfaceKind.Root, "RootMounted");
                    didPresent = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        else if (component.IsSubclassOfRawGeneric(typeof(TabComponent<>)))
        {
            CurrentTabComponent = component;
            OverlaySurfaceOwnership.Set(component, OverlaySurfaceKind.Root, "TabMounted");
            didPresent = true;
        }
        else if (component.IsSubclassOfRawGeneric(typeof(FlyoutComponent<>)))
        {
            CurrentFlyoutComponent = component;
            OverlaySurfaceOwnership.Set(component, OverlaySurfaceKind.Root, "FlyoutMounted");
            didPresent = true;
        }
        else if (component.IsSubclassOfRawGeneric(typeof(OverlayComponent<,>)))
        {
            didPresent = PresentOverlayComponent(component);
        }
        else if (component.IsSubclassOfRawGeneric(typeof(PushableComponent<,>)))
        {
            if (component.Presenter is null)
                throw new RouterException(RouterError.MissingPresenter, component);

            OverlaySurfaceOwnership.Set(component, ResolveNextStackSurface(), "PushableInherited");
            ComponentsStack.Add(component);
            await PushComponentInternal(component);
            didPresent = true;
        }
        else if (component.IsSubclassOfRawGeneric(typeof(ModalPageComponent<,>)))
        {
            if (component.Presenter is null)
                throw new RouterException(RouterError.MissingPresenter, component);

            OverlaySurfaceOwnership.Set(component, ToOverlaySurfaceKind(presentationKind), presentationKind == ComponentPresentationKind.FullscreenModal ? "FullscreenModalPresented" : "ModalPresented");
            ComponentsStack.Add(component);
            await PushModalComponentInternal(component);
            didPresent = true;
        }
        else if (component.IsSubclassOfRawGeneric(typeof(PageComponent<,>)))
        {
            if (component.Presenter is null)
                throw new RouterException(RouterError.MissingPresenter, component);

            var page = (Page)component.Presenter;
            if (component is NavigationComponent navigationComponent)
            {
                NavigationPage? navigationPage = navigationComponent.Navigation;
                if (navigationPage is null)
                {
                    navigationPage = CreateNavigationPage(page);
                    navigationComponent.Navigation = navigationPage;
                    ApplyComponentSafeAreaPolicy(component, presentationKind, navigationPage);
                    ApplyComponentChrome("AfterCreateNavigationPage", component, presentationKind, navigationPage);
                }

                Application.Current!.Windows[0].Page = navigationPage;
            }
            else
            {
                Application.Current!.Windows[0].Page = page;
            }

            MountedComponent = component;
            CurrentTabComponent = null;
            CurrentFlyoutComponent = null;
            OverlaySurfaceOwnership.Set(component, OverlaySurfaceKind.Root, "PageMounted");
            didPresent = true;
        }

        if (didPresent)
        {
            PrepareLegacyOverlayHost(component);
            ApplyComponentSafeAreaPolicy(component, presentationKind);
            RegisterComponentChromeLifecycle("AfterPresentComponent", component, presentationKind);
            ApplyComponentChrome("AfterPresentComponent", component, presentationKind);
        }

        return didPresent;
    }

    protected virtual async Task HandleDeviceBackPressedInternal()
    {
        if (ComponentsStack.Any())
        {
            var component = ComponentsStack.Last();

            var historySnackbar = TryAndGetLatestHistorySnackbar(component.GetType());
            // var historyPanel = TryAndGetLatestHistoryPanel(component.GetType());
            ComponentHistoryItem? historyPanel = null;
            var historyPopup = TryAndGetLatestHistoryPopup(component.GetType());

            if (historyPanel is not null || historyPopup is not null || historySnackbar is not null)
            {
                DismissMostRecentHistoryItem(historySnackbar, historyPopup, historyPanel);
            }
            else
            {
                bool canContinue = CanNavigateBack(component);
                if (!canContinue) return;

                if (component.IsSubclassOfRawGeneric(typeof(PushableComponent<,>)))
                {
                    await PopComponentInternal(component, true);
                    component.Unpresent();
                }
                else
                {
                    component.Unpresent();
                }
            }
        }
        else if (MountedComponent is not null)
        {
            var historySnackbar = TryAndGetLatestHistorySnackbar(MountedComponent.GetType());
            // var historyPanel = TryAndGetLatestHistoryPanel(MountedComponent.GetType());
            ComponentHistoryItem? historyPanel = null;
            var historyPopup = TryAndGetLatestHistoryPopup(MountedComponent.GetType());

            if (historyPanel is not null || historyPopup is not null || historySnackbar is not null)
            {
                DismissMostRecentHistoryItem(historySnackbar, historyPopup, historyPanel);
            }
        }
    }

    protected async Task<TResult> PresentComponentInternal<TState, TResult>(RoutableComponent<TState, TResult> component)
    {
        return await component.Present();
    }

    public Component? GetCurrentTabComponent() => CurrentTabComponent;
    public Component? GetCurrentFlyoutComponent() => CurrentFlyoutComponent;

    #endregion

    #region helper methods

    private static void EnsureApplication(Component? component)
    {
        if (Application.Current is null)
            throw new RouterException(RouterError.ApplicationCurrentIsNull, component);

        if (!Application.Current.Windows.Any())
            throw new RouterException(RouterError.NoApplicationWindows, component);

        var window = Application.Current.Windows.First();
        if (window.Page is null)
            throw new RouterException(RouterError.WindowPageIsNull, component);
    }

    private async Task PrepareComponent<TState, TResult>(
        RoutableComponent<TState, TResult> component,
        TState input)
    {
        await component.Prepare(input);

        if (component is LocalizableComponent localizableComponent)
        {
            var catalog = await CatalogProvider.GetLocalCatalog();
            await localizableComponent.ApplyLocalization(catalog);
        }
    }

    private static void PrepareLegacyOverlayHost(Component component)
    {
        if (component.Presenter is not OverlayHost { OverlayContainer: not null } overlayHost)
            return;

        OverlaySurfaceHost.PrepareLegacyContainer(overlayHost.OverlayContainer);
    }

    private ComponentHistoryItem? TryAndGetLatestHistorySnackbar(Type parentComponentType)
    {
        return History.TryGetLatestSnackbar(parentComponentType);
    }

    private ComponentHistoryItem? TryAndGetLatestHistoryPopup(Type parentComponentType)
    {
        return History.TryGetLatestPopup(parentComponentType);
    }

    private void DismissMostRecentHistoryItem(ComponentHistoryItem? snackbarItem, ComponentHistoryItem? popupItem, ComponentHistoryItem? panelItem)
    {
        History.DismissMostRecent(snackbarItem, popupItem, panelItem);
    }

    private void ClosePopupComponent(Component component)
    {
        component.Unpresent();
        DismissOverlayComponent(component);
    }

    private async Task PushComponentInternal(Component component)
    {
        EnsureApplication(component);
        var presentationKind = GetPresentationKind(component);

        var window = Application.Current!.Windows.First();

        INavigation? navigation = GetCurrentNavigation();
        if (navigation is null)
            throw new RouterException(RouterError.NavigationNotAvailable, component);

        var modalStack = navigation.ModalStack;
        if (modalStack is not null && modalStack.LastOrDefault() is NavigationPage modalNavPage)
            navigation = modalNavPage.Navigation;

        if (component.Presenter is not Page page)
            throw new RouterException(RouterError.PresenterIsNotPage, component);

        ApplyComponentSafeAreaPolicy(component, presentationKind, page);
        ApplyComponentChrome("BeforePushAsync", component, presentationKind, page, navigation);
        await MainThread.InvokeOnMainThreadAsync(async () => await navigation.PushAsync(page));
        ApplyComponentSafeAreaPolicy(component, presentationKind, page);
        ApplyComponentChrome("AfterPushAsync", component, presentationKind, page, navigation);
    }

    private async Task PushModalComponentInternal(Component component)
    {
        EnsureApplication(component);
        var presentationKind = GetPresentationKind(component);

        LogModalChromeDiagnostics("BeforePushModalInternal", component, null, null);
        ApplyComponentSafeAreaPolicy(component, presentationKind);
        ApplyComponentChrome("BeforePushModalInternal", component, presentationKind);

        var mountablePage = component.Presenter as Page;
        if (mountablePage is null)
            throw new RouterException(RouterError.PresenterIsNotPage, component);

        if (component is NavigationComponent navigationComponent)
        {
            if (navigationComponent.Navigation is null)
            {
                var chromeOptions = ResolveChromeOptions(component, presentationKind);
                navigationComponent.Navigation = CreateNavigationPage(
                    mountablePage,
                    presentationKind,
                    chromeOptions,
                    useStatusBarNavigationPage: true);
                LogModalChromeDiagnostics("AfterCreateNavigationPage", component, navigationComponent.Navigation, null);
                ApplyComponentSafeAreaPolicy(component, presentationKind, navigationComponent.Navigation);
                ApplyComponentChrome("AfterCreateModalNavigationPage", component, presentationKind, navigationComponent.Navigation);
            }
            mountablePage = navigationComponent.Navigation;
        }

        var navigation = GetCurrentNavigation(true);
        if (navigation is null)
            throw new RouterException(RouterError.NavigationNotAvailable, component);

        LogModalChromeDiagnostics("BeforePushModalAsync", component, mountablePage, navigation);
        ApplyComponentSafeAreaPolicy(component, presentationKind, mountablePage);
        ApplyComponentModalPresentationPolicy(component, presentationKind, mountablePage);
        ApplyComponentChrome("BeforePushModalAsync", component, presentationKind, mountablePage, navigation);
        await MainThread.InvokeOnMainThreadAsync(async () => await navigation.PushModalAsync(mountablePage));
        LogModalChromeDiagnostics("AfterPushModalAsync", component, mountablePage, navigation);
        ApplyComponentSafeAreaPolicy(component, presentationKind, mountablePage);
        RegisterComponentChromeLifecycle("AfterPushModalAsync", component, presentationKind, mountablePage, navigation);
        ApplyComponentChrome("AfterPushModalAsync", component, presentationKind, mountablePage, navigation);
    }

    private static void ApplyComponentSafeAreaPolicy(
        Component component,
        ComponentPresentationKind presentationKind,
        Page? mountablePage = null)
    {
        ComponentSafeAreaPolicyApplier.Apply(component, presentationKind, mountablePage);
    }

    private static void ApplyComponentModalPresentationPolicy(
        Component component,
        ComponentPresentationKind presentationKind,
        Page? mountablePage = null)
    {
        ComponentModalPresentationPolicyApplier.Apply(component, presentationKind, mountablePage);
    }

    private ComponentChromeOptions ResolveChromeOptions(Component component, ComponentPresentationKind presentationKind)
    {
        return ChromeOptionsResolver?.Resolve(component, presentationKind) ?? new ComponentChromeOptions();
    }

    private void ApplyComponentChrome(
        string source,
        Component component,
        ComponentPresentationKind presentationKind,
        Page? mountablePage = null,
        INavigation? navigation = null)
    {
        if (ChromeService is null)
            return;

        try
        {
            ChromeService.Apply(new ComponentChromeContext(
                source,
                component,
                presentationKind,
                ResolveChromeOptions(component, presentationKind),
                mountablePage,
                navigation));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void RegisterComponentChromeLifecycle(
        string source,
        Component component,
        ComponentPresentationKind presentationKind,
        Page? mountablePage = null,
        INavigation? navigation = null)
    {
        if (ChromeService is null)
            return;

        try
        {
            ChromeService.RegisterLifecycle(new ComponentChromeContext(
                source,
                component,
                presentationKind,
                ResolveChromeOptions(component, presentationKind),
                mountablePage,
                navigation));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    [Conditional("DEBUG")]
    private static void LogModalChromeDiagnostics(
        string source,
        Component component,
        Page? mountablePage,
        INavigation? navigation)
    {
        try
        {
            var presenterPage = component.Presenter as Page;
            var navigationPage = component is NavigationComponent navigationComponent
                ? navigationComponent.Navigation
                : mountablePage as NavigationPage;

            WriteModalChromeDiagnostics(
                $"source=AbstractRouter.Modal.{source} " +
                $"component={component.GetType().FullName} " +
                $"presenter={component.Presenter?.GetType().FullName ?? "null"} " +
                $"presenterHash={presenterPage?.GetHashCode().ToString("X8") ?? "null"} " +
                $"presenterHandler={presenterPage?.Handler?.GetType().FullName ?? "null"} " +
                $"presenterPlatformView={presenterPage?.Handler?.PlatformView?.GetType().FullName ?? "null"} " +
                $"presenterWindowHash={presenterPage?.Window?.GetHashCode().ToString("X8") ?? "null"} " +
                $"mountablePage={mountablePage?.GetType().FullName ?? "null"} " +
                $"mountableHash={mountablePage?.GetHashCode().ToString("X8") ?? "null"} " +
                $"mountableHandler={mountablePage?.Handler?.GetType().FullName ?? "null"} " +
                $"mountablePlatformView={mountablePage?.Handler?.PlatformView?.GetType().FullName ?? "null"} " +
                $"mountableWindowHash={mountablePage?.Window?.GetHashCode().ToString("X8") ?? "null"} " +
                $"navigationPageHash={navigationPage?.GetHashCode().ToString("X8") ?? "null"} " +
                $"navigationPageRoot={navigationPage?.RootPage?.GetType().FullName ?? "null"} " +
                $"navigationPageHandler={navigationPage?.Handler?.GetType().FullName ?? "null"} " +
                $"navigationPagePlatformView={navigationPage?.Handler?.PlatformView?.GetType().FullName ?? "null"} " +
                $"navigationPageWindowHash={navigationPage?.Window?.GetHashCode().ToString("X8") ?? "null"} " +
                $"navigationPageBarBackground={MauiColorDescription(navigationPage?.BarBackgroundColor)} " +
                $"navigationPageBackground={navigationPage?.Background?.GetType().FullName ?? "null"} " +
                $"navigationNull={navigation is null} " +
                $"navigationModalStackCount={navigation?.ModalStack.Count.ToString() ?? "null"} " +
                $"navigationStackCount={navigation?.NavigationStack.Count.ToString() ?? "null"}");
        }
        catch (Exception ex)
        {
            WriteModalChromeDiagnostics($"source=AbstractRouter.Modal.{source} failed={ex}");
        }
    }

    [Conditional("DEBUG")]
    private static void WriteModalChromeDiagnostics(string message)
    {
#if ANDROID
        Android.Util.Log.Debug("ComponentRouting.ModalChrome", message);
#else
        Debug.WriteLine($"[ComponentRouting.ModalChrome] {message}");
#endif
    }

    private static string MauiColorDescription(Color? color)
    {
        if (color is null)
            return "null";

        return $"#{(int)Math.Round(color.Alpha * 255):X2}{(int)Math.Round(color.Red * 255):X2}{(int)Math.Round(color.Green * 255):X2}{(int)Math.Round(color.Blue * 255):X2}";
    }

    private async Task PopComponentInternal(Component component, bool animated)
    {
        EnsureApplication(component);

        INavigation? navigation = GetCurrentNavigation();
        if (navigation is null)
            throw new RouterException(RouterError.NavigationNotAvailable, component);

        // If a modal NavigationPage is open, push into that navigation stack.
        var modalStack = navigation.ModalStack;
        if (modalStack.Any() && modalStack.Last() is NavigationPage modalNavPage)
            navigation = modalNavPage.Navigation;

        var stack = navigation.NavigationStack;
        if (!stack.Any())
            throw new RouterException(RouterError.EmptyNavigationStack, component);

        if (stack.Last() != component.Presenter)
            throw new RouterException(RouterError.PresenterIsNotOnTopNavigationStack, component);

        RemoveFromComponentsStack(component);

        await navigation.PopAsync(animated);
    }

    private async Task<bool> PopModalComponentInternal(Component component, bool animated)
    {
        EnsureApplication(component);

        var navigation = GetCurrentNavigation(true);
        if (navigation is null)
            throw new RouterException(RouterError.NavigationNotAvailable, component);

        var modalStack = navigation.ModalStack;
        if (!modalStack.Any())
            throw new RouterException(RouterError.PresenterIsNotOnTopModalStack, component);

        var last = modalStack.Last();
        var ok =
            last == component.Presenter ||
            (last is NavigationPage navPage && navPage.RootPage == component.Presenter);

        if (!ok)
            throw new RouterException(RouterError.PresenterIsNotOnTopModalStack, component);

        RemoveFromComponentsStack(component);

        await navigation.PopModalAsync(animated);
        return true;
    }

    private bool PresentOverlayComponent(Component component)
    {
        if (component.Presenter is not Layout layout)
        {
            Debug.WriteLine("Overlay presentation failed: presenter is null or not a Layout.");
            return false;
        }

        var ownerComponent = ResolveOverlayOwnerComponent();
        var ownerSurfaceKind = ResolveOverlayOwnerSurface(ownerComponent);
        var hasActiveNativeModal = HasActiveNativeModal();
        if (!OverlaySurfaceResolver.TryResolveOverlaySurface(
                History.Popups.LastOrDefault()?.Component,
                ComponentsStack.LastOrDefault(),
                MountedComponent,
                ownerSurfaceKind,
                hasActiveNativeModal,
                GetOverlayPlatformSurfaceProvider(),
                out var surfaceHost))
        {
            Debug.WriteLine("Overlay presentation failed: no available OverlayHost container was found.");
            return false;
        }

        if (surfaceHost.Contains(layout))
        {
            Debug.WriteLine("Overlay presentation skipped: layout is already mounted.");
            return false;
        }

        // Android 11+ can report top insets that affect snackbar placement.
        /*
        if (component is SnackbarComponent { Presenter: SnackbarPresenter snackbarPresenter }
            && DeviceInfo.Platform == DevicePlatform.Android
            && DeviceInfo.Version.Major >= 11)
        {
            var insets = SafeAreaInsetsService.GetSafeAreaInsets();
            if (insets.Top > 0) snackbarPresenter.TranslationY = insets.Top;
        }
        */

        ApplySnackbarDefaultLayout(component);

        var surfaceMount = TryMountOverlaySurface(surfaceHost, layout);
        if (surfaceMount is null)
        {
            return false;
        }
        ApplySnackbarPlatformSafeArea(component, surfaceMount.SurfaceHost);

        OverlaySurfaceOwnership.Set(component, ownerSurfaceKind, component is SnackbarComponent ? "SnackbarPresented" : "OverlayPresented");
        if (component is SnackbarComponent)
        {
            History.AddSnackbar(
                surfaceMount.SurfaceHost.ParentComponent.GetType(),
                component,
                surfaceMount.SurfaceHandle);
        }
        else
        {
            History.AddPopup(
                surfaceMount.SurfaceHost.ParentComponent.GetType(),
                component,
                surfaceMount.SurfaceHandle);
        }

        return true;
    }

    private void ApplySnackbarDefaultLayout(Component component)
    {
        if (component is not SnackbarComponent { Presenter: SnackbarPresenter snackbarPresenter })
            return;

        snackbarPresenter.HorizontalOptions = LayoutOptions.Fill;
        snackbarPresenter.VerticalOptions = LayoutOptions.Fill;

        foreach (var child in snackbarPresenter.Children)
        {
            if (child is not View childView)
                continue;

            var flags = AbsoluteLayout.GetLayoutFlags(childView);
            var bounds = AbsoluteLayout.GetLayoutBounds(childView);

            if (flags != AbsoluteLayoutFlags.None ||
                bounds.Width != AbsoluteLayout.AutoSize ||
                bounds.Height != AbsoluteLayout.AutoSize)
            {
                continue;
            }

            AbsoluteLayout.SetLayoutFlags(
                childView,
                AbsoluteLayoutFlags.WidthProportional);
            AbsoluteLayout.SetLayoutBounds(
                childView,
                new Rect(0, 0, 1, AbsoluteLayout.AutoSize));
            childView.HorizontalOptions = LayoutOptions.Fill;
            childView.VerticalOptions = LayoutOptions.Start;
        }
    }

    private void ApplySnackbarPlatformSafeArea(Component component, OverlaySurfaceHost surfaceHost)
    {
        if (component is not SnackbarComponent { Presenter: SnackbarPresenter snackbarPresenter })
            return;

        var topInset = GetSnackbarTopInset(surfaceHost);
        if (topInset <= 0)
        {
            return;
        }

        foreach (var child in snackbarPresenter.Children)
        {
            if (child is not View childView)
                continue;

            var flags = AbsoluteLayout.GetLayoutFlags(childView);
            var bounds = AbsoluteLayout.GetLayoutBounds(childView);
            if (!flags.HasFlag(AbsoluteLayoutFlags.WidthProportional) ||
                bounds.Y != 0 ||
                bounds.Height != AbsoluteLayout.AutoSize)
            {
                continue;
            }

            var margin = childView.Margin;
            childView.Margin = new Thickness(
                margin.Left,
                Math.Max(margin.Top, topInset),
                margin.Right,
                margin.Bottom);
            snackbarPresenter.InvalidateMeasure();
        }
    }

    private double GetSnackbarTopInset(OverlaySurfaceHost surfaceHost)
    {
        if (!surfaceHost.IsPlatformHost)
            return 0;

        var configuredTopInset = SafeAreaInsetsService.GetSafeAreaInsets(true).Top;
#if ANDROID
        return Math.Max(configuredTopInset, GetAndroidStatusBarInset());
#else
        return configuredTopInset;
#endif
    }

#if ANDROID
    private static double GetAndroidStatusBarInset()
    {
        var activity = Platform.CurrentActivity;
        var density = DeviceDisplay.MainDisplayInfo.Density;
        if (density <= 0)
            density = 1;

        var resourceId = activity?.Resources?.GetIdentifier("status_bar_height", "dimen", "android") ?? 0;
        if (resourceId > 0 && activity?.Resources is { } resources)
            return resources.GetDimensionPixelSize(resourceId) / density;

        return 0;
    }
#endif

    private OverlaySurfaceMount? TryMountOverlaySurface(OverlaySurfaceHost surfaceHost, Layout layout)
    {
        try
        {
            var handle = surfaceHost.Mount(layout);
            return new OverlaySurfaceMount(surfaceHost, handle);
        }
        catch (Exception) when (surfaceHost.IsPlatformHost)
        {
        }

        if (!OverlaySurfaceResolver.TryResolveLegacyOverlayHost(
                History.Popups.LastOrDefault()?.Component,
                ComponentsStack.LastOrDefault(),
                MountedComponent,
                out var legacySurfaceHost))
        {
            Debug.WriteLine("Overlay presentation failed: platform surface failed and no legacy OverlayHost container was found.");
            return null;
        }

        if (legacySurfaceHost.Contains(layout))
        {
            Debug.WriteLine("Overlay presentation skipped: layout is already mounted on fallback legacy host.");
            return null;
        }

        var legacyHandle = legacySurfaceHost.Mount(layout);
        return new OverlaySurfaceMount(legacySurfaceHost, legacyHandle);
    }

    private sealed record OverlaySurfaceMount(
        OverlaySurfaceHost SurfaceHost,
        OverlaySurfaceHandle SurfaceHandle);

    private IOverlayPlatformSurfaceProvider? GetOverlayPlatformSurfaceProvider()
    {
        var serviceProvider = Application.Current?
            .Windows
            .FirstOrDefault()?
            .Page?
            .Handler?
            .MauiContext?
            .Services;

        return serviceProvider?.GetService(typeof(IOverlayPlatformSurfaceProvider)) as IOverlayPlatformSurfaceProvider;
    }

    private Component? ResolveOverlayOwnerComponent()
    {
        return History.Popups.LastOrDefault()?.Component ??
               ComponentsStack.LastOrDefault() ??
               MountedComponent;
    }

    private OverlaySurfaceKind ResolveOverlayOwnerSurface(Component? ownerComponent)
    {
        if (ownerComponent is not null &&
            OverlaySurfaceOwnership.TryGet(ownerComponent, out var ownerSurfaceKind))
        {
            return ownerSurfaceKind;
        }

        return OverlaySurfaceOwnership.GetInheritedSurface(ownerComponent);
    }

    private OverlaySurfaceKind ResolveNextStackSurface()
    {
        return OverlaySurfaceOwnership.GetInheritedSurface(ComponentsStack.LastOrDefault() ?? MountedComponent);
    }

    private static OverlaySurfaceKind ToOverlaySurfaceKind(ComponentPresentationKind presentationKind)
    {
        return presentationKind == ComponentPresentationKind.FullscreenModal
            ? OverlaySurfaceKind.FullscreenModal
            : OverlaySurfaceKind.Modal;
    }

    private void RemoveFromComponentsStack(Component component)
    {
        if (ComponentsStack.Contains(component))
            ComponentsStack.Remove(component);

        OverlaySurfaceOwnership.Remove(component, "RemoveFromComponentsStack");
    }

    private bool HasActiveNativeModal()
    {
        return GetCurrentNavigation(useGlobalNavigation: true)?.ModalStack.Any() == true;
    }

    private bool TryFindOverlayContainer(out Component parentComponent, out AbsoluteLayout containerLayout)
    {
        if (TryGetOverlayContainer(History.Popups.LastOrDefault()?.Component, out parentComponent, out containerLayout))
            return true;

        if (TryGetOverlayContainer(ComponentsStack.LastOrDefault(), out parentComponent, out containerLayout))
            return true;

        if (TryGetOverlayContainer(MountedComponent, out parentComponent, out containerLayout))
            return true;

        parentComponent = null!;
        containerLayout = null!;
        return false;
    }

    private static bool TryGetOverlayContainer(
        Component? component,
        out Component parentComponent,
        out AbsoluteLayout containerLayout)
    {
        if (component?.Presenter is OverlayHost { OverlayContainer: not null } overlayHost)
        {
            parentComponent = component;
            containerLayout = overlayHost.OverlayContainer;
            return true;
        }

        parentComponent = null!;
        containerLayout = null!;
        return false;
    }

    private void DismissOverlayComponent(Component component)
    {
        var historyItem = History.TryGetItem(component);

        if (historyItem?.OverlaySurfaceHandle is not null)
        {
            historyItem.OverlaySurfaceHandle.Unmount();
            History.Remove(component);
            OverlaySurfaceOwnership.Remove(component, "DismissOverlayComponentHandle");
            return;
        }

        AbsoluteLayout? containerLayout = null;
        Component? parentComponent = null;

        if (component.Presenter is Layout layout)
        {
            if (History.Popups.Count > 0 &&
                History.Popups.Last().Component.Presenter is OverlayHost { OverlayContainer: not null } popupHost &&
                popupHost.OverlayContainer.Children.Contains(layout))
            {
                parentComponent = History.Popups.Last().Component;
                containerLayout = popupHost.OverlayContainer;
            }

            if (parentComponent is null || containerLayout is null)
            {
                if (ComponentsStack.Count > 0
                    && ComponentsStack.Last().Presenter is OverlayHost { OverlayContainer: not null } overlayHost
                    && overlayHost.OverlayContainer.Children.Contains(layout))
                {
                    parentComponent = ComponentsStack.Last();
                    containerLayout = overlayHost.OverlayContainer;
                }
            }

            if (parentComponent is null || containerLayout is null)
            {
                if (MountedComponent?.Presenter is OverlayHost { OverlayContainer: not null } overlayHost2
                    && overlayHost2.OverlayContainer.Children.Contains(layout))
                {
                    parentComponent = MountedComponent;
                    containerLayout = overlayHost2.OverlayContainer;
                }
            }

            if (containerLayout is not null)
            {
                layout.IsVisible = false;
                if (containerLayout.Children.Contains(layout))
                {
                    containerLayout.Children.Remove(layout);
                }
                OverlaySurfaceHost.PrepareLegacyContainer(containerLayout);

                if (parentComponent is not null)
                {
                    History.Remove(component);
                    OverlaySurfaceOwnership.Remove(component, "DismissOverlayComponentLegacy");
                }
            }
        }
    }

    private NavigationPage CreateNavigationPage(
        Page rootPage,
        ComponentPresentationKind presentationKind = ComponentPresentationKind.Page,
        ComponentChromeOptions? chromeOptions = null,
        bool useStatusBarNavigationPage = false)
    {
        var navigationPage = CreateNavigationPageInstance(
            rootPage,
            presentationKind,
            chromeOptions,
            useStatusBarNavigationPage);

        return navigationPage;
    }

    private static NavigationPage CreateNavigationPageInstance(
        Page rootPage,
        ComponentPresentationKind presentationKind,
        ComponentChromeOptions? chromeOptions,
        bool useStatusBarNavigationPage)
    {
#if IOS
        if (useStatusBarNavigationPage &&
            chromeOptions?.StatusBarForeground is ChromeForeground.LightContent or ChromeForeground.DarkContent)
        {
            return new ComponentRoutingStatusBarNavigationPage(
                rootPage,
                chromeOptions.StatusBarForeground.Value,
                presentationKind);
        }
#endif

        return new NavigationPage(rootPage);
    }

    private bool IsDeviceBackPressedResolved()
    {
        bool resolved = false;
        if (ComponentsStack.Any())
        {
            resolved = true;
        }
        else if (MountedComponent is not null)
        {
            var historySnackbar = TryAndGetLatestHistorySnackbar(MountedComponent.GetType());
            // var historyPanel = TryAndGetLatestHistoryPanel(MountedComponent.GetType());
            ComponentHistoryItem? historyPanel = null;
            var historyPopup = TryAndGetLatestHistoryPopup(MountedComponent.GetType());
            resolved = historySnackbar is not null || historyPanel is not null || historyPopup is not null;
        }

        return resolved;
    }

    private INavigation? GetCurrentNavigation(bool useGlobalNavigation = false)
    {
        INavigation? navigation = null;

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is null) return null;

        if (window.Page is NavigationPage navigationPage) navigation = navigationPage.Navigation;
        if (navigation is null && window.Page is FlyoutPage flyoutPage)
        {
            if (flyoutPage.Detail is NavigationPage currentFlyoutPage)
                navigation = currentFlyoutPage.Navigation;
        }
        if (navigation is null && window.Page is TabbedPage tabbedPage)
        {
            if (tabbedPage.CurrentPage is NavigationPage currentTabPage)
                navigation = currentTabPage.Navigation;
        }

        if (navigation is null && useGlobalNavigation)
        {
            var windowNavigation = window.Navigation;
            var mainPageNavigation = window.Page?.Navigation;
            var flyoutNavigation = window.Page is FlyoutPage flyoutPage2 ? flyoutPage2.Navigation : null;
            var tabbedNavigation = window.Page is TabbedPage tabbedPage2 ? tabbedPage2.Navigation : null;

            return tabbedNavigation ?? flyoutNavigation ?? mainPageNavigation ?? windowNavigation;
        }

        return navigation;
    }

    private IList<AppLifecycleAwareComponent> GetLifecycleAwareComponents()
    {
        var result = new List<AppLifecycleAwareComponent>();

        foreach (var component in ComponentsStack.AsEnumerable().Reverse())
        {
            if (component is AppLifecycleAwareComponent lifecycleAwareComponent)
                result.Add(lifecycleAwareComponent);
        }

        if (MountedComponent is AppLifecycleAwareComponent mountedLifecycleAwareComponent &&
            !result.Contains(mountedLifecycleAwareComponent))
        {
            result.Add(mountedLifecycleAwareComponent);
        }

        if (CurrentTabComponent is AppLifecycleAwareComponent tabLifecycleAwareComponent &&
            !result.Contains(tabLifecycleAwareComponent))
        {
            result.Add(tabLifecycleAwareComponent);
        }

        if (CurrentFlyoutComponent is AppLifecycleAwareComponent flyoutLifecycleAwareComponent &&
            !result.Contains(flyoutLifecycleAwareComponent))
        {
            result.Add(flyoutLifecycleAwareComponent);
        }

        return result;
    }

    private IList<Component> GetResumeComponents()
    {
        return ComponentHistory.GetResumeComponents(
            MountedComponent,
            CurrentTabComponent,
            CurrentFlyoutComponent,
            ComponentsStack.LastOrDefault()).ToList();
    }

    private async Task DispatchLifecycleEvent(Func<AppLifecycleAwareComponent, Task> dispatch)
    {
        var lifecycleAwareComponents = GetLifecycleAwareComponents();

        foreach (var component in lifecycleAwareComponents)
        {
            try
            {
                await dispatch(component);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }

    #endregion
}
