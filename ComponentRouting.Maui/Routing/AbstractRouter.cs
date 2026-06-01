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
using ComponentRouting.Maui.Exceptions;
using ComponentRouting.Maui.Extension;
using ComponentRouting.Maui.Model.Core;
using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Service.Core;

namespace ComponentRouting.Maui.Routing;

public abstract class AbstractRouter : Router
{
    #region nested classes

    protected class ComponentHistoryItem
    {
        public Type ParentComponentType { get; }
        public Component Component { get; }
        public DateTime Timestamp { get; }

        public ComponentHistoryItem(Type parentComponentType, Component component)
        {
            ParentComponentType = parentComponentType;
            Component = component;
            Timestamp = DateTime.Now;
        }

        public bool IsSnackbar()
        {
            return Component is SnackbarComponent;
        }

        public bool IsPopup()
        {
            return Component.IsSubclassOfRawGeneric(typeof(OverlayComponent<,>)) && !(Component is SnackbarComponent);
        }
    }

    #endregion

    #region auto-properties

    public Component? CurrentTabComponent { get; private set; }
    public Component? CurrentFlyoutComponent { get; private set; }
    public Component? MountedComponent { get; private set; }
    public List<Component> ComponentsStack { get; private set; }

    public abstract RootComponent RootComponent { get; }

    protected ComponentFactory ComponentFactory { get; }
    private CatalogProvider CatalogProvider { get; }
    protected SafeAreaInsetsService SafeAreaInsetsService { get; }

    protected View SafeAreaBottomPatch { get; set; }

    private List<ComponentHistoryItem> Snackbars { get; set; }
    private List<ComponentHistoryItem> Popups { get; set; }
    // private List<ComponentHistoryItem> Panels { get; set; }

    #endregion

    #region ctor(s)

    protected AbstractRouter(ComponentFactory componentFactory, CatalogProvider catalogProvider, SafeAreaInsetsService safeAreaInsetsService)
    {
        ComponentFactory = componentFactory;
        CatalogProvider = catalogProvider;
        SafeAreaInsetsService = safeAreaInsetsService;

        ComponentsStack = new List<Component>();
        Snackbars = new List<ComponentHistoryItem>();
        Popups = new List<ComponentHistoryItem>();
        // Panels = new List<ComponentHistoryItem>();
    }

    #endregion

    #region abstract methods

    protected abstract bool CanNavigateBack(Component component);

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

    public TComponent? GetMountedComponent<TComponent>()
        where TComponent : Component
    {
        var components = GetMountedComponents<TComponent>();

        if (components.Count == 0)
            return default;

        if (components.Count == 1)
            return components[0];

        throw new InvalidOperationException(
            $"Multiple mounted {typeof(TComponent).Name} instances were found. Use {nameof(GetMountedComponents)}<{typeof(TComponent).Name}>() instead.");
    }

    public IReadOnlyList<TComponent> GetMountedComponents<TComponent>()
        where TComponent : Component
    {
        var result = new List<TComponent>();
        var seen = new HashSet<Component>(ReferenceEqualityComparer.Instance);

        foreach (var component in Popups.Concat(Snackbars).Select(item => item.Component))
        {
            if (component is TComponent typedComponent && seen.Add(component))
                result.Add(typedComponent);
        }

        return result;
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
        if (Snackbars.Any())
        {
            var snackbarsClone = Snackbars.ToList();
            foreach (var snackbar in snackbarsClone)
                snackbar.Component.Unpresent();
        }
        Snackbars = new List<ComponentHistoryItem>();

        if (Popups.Any())
        {
            var popupsClone = Popups.ToList();
            foreach (var popup in popupsClone)
                popup.Component.Unpresent();
        }
        Popups = new List<ComponentHistoryItem>();

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
                _ = PopModalComponentInternal(component, true);
            }
            else if (component.IsSubclassOfRawGeneric(typeof(TabComponent<>)))
            {
                CurrentTabComponent = null;
            }
            else if (component.IsSubclassOfRawGeneric(typeof(FlyoutComponent<>)))
            {
                CurrentFlyoutComponent = null;
            }

            if (ComponentsStack.Contains(component))
                ComponentsStack.Remove(component);

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
                        Application.Current!.Windows[0].Page = navigationPage;
                    }
                    else
                    {
                        Application.Current!.Windows[0].Page = rootPage;
                    }

                    MountedComponent = rootComponent;
                    CurrentTabComponent = null;
                    CurrentFlyoutComponent = null;
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
            didPresent = true;
        }
        else if (component.IsSubclassOfRawGeneric(typeof(FlyoutComponent<>)))
        {
            CurrentFlyoutComponent = component;
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

            ComponentsStack.Add(component);
            await PushComponentInternal(component);
            didPresent = true;
        }
        else if (component.IsSubclassOfRawGeneric(typeof(ModalPageComponent<,>)))
        {
            if (component.Presenter is null)
                throw new RouterException(RouterError.MissingPresenter, component);

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
            didPresent = true;
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

    private ComponentHistoryItem? TryAndGetLatestHistorySnackbar(Type parentComponentType)
    {
        return GetLatestHistoryItem(Snackbars, parentComponentType);
    }

    private ComponentHistoryItem? TryAndGetLatestHistoryPopup(Type parentComponentType)
    {
        return GetLatestHistoryItem(Popups, parentComponentType);
    }

    private static ComponentHistoryItem? GetLatestHistoryItem(
        IReadOnlyList<ComponentHistoryItem> source,
        Type parentComponentType)
    {
        return source
            .Where(item => item.ParentComponentType == parentComponentType)
            .OrderByDescending(item => item.Timestamp)
            .FirstOrDefault();
    }

    private void DismissMostRecentHistoryItem(ComponentHistoryItem? snackbarItem, ComponentHistoryItem? popupItem, ComponentHistoryItem? panelItem)
    {
        var historyItems = new List<ComponentHistoryItem>();
        if (snackbarItem is not null) historyItems.Add(snackbarItem);
        if (popupItem is not null) historyItems.Add(popupItem);
        if (panelItem is not null) historyItems.Add(panelItem);

        if (historyItems.Any())
        {
            var mostRecentHistoryItem = historyItems
                .OrderByDescending(chi => chi.Timestamp)
                .First();

            mostRecentHistoryItem.Component.Unpresent();
        }
    }

    private async Task PushComponentInternal(Component component)
    {
        EnsureApplication(component);

        var window = Application.Current!.Windows.First();

        INavigation? navigation = GetCurrentNavigation();
        if (navigation is null)
            throw new RouterException(RouterError.NavigationNotAvailable, component);

        var modalStack = navigation.ModalStack;
        if (modalStack is not null && modalStack.LastOrDefault() is NavigationPage modalNavPage)
            navigation = modalNavPage.Navigation;

        if (component.Presenter is not Page page)
            throw new RouterException(RouterError.PresenterIsNotPage, component);

        await MainThread.InvokeOnMainThreadAsync(async () => await navigation.PushAsync(page));
    }

    private async Task PushModalComponentInternal(Component component)
    {
        EnsureApplication(component);

        var mountablePage = component.Presenter as Page;
        if (mountablePage is null)
            throw new RouterException(RouterError.PresenterIsNotPage, component);

        if (component is NavigationComponent navigationComponent)
        {
            navigationComponent.Navigation ??= CreateNavigationPage(mountablePage);
            mountablePage = navigationComponent.Navigation;
        }

        var navigation = GetCurrentNavigation(true);
        if (navigation is null)
            throw new RouterException(RouterError.NavigationNotAvailable, component);

        await MainThread.InvokeOnMainThreadAsync(async () => await navigation.PushModalAsync(mountablePage));
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

        if (ComponentsStack.Contains(component))
            ComponentsStack.Remove(component);

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

        if (ComponentsStack.Contains(component))
            ComponentsStack.Remove(component);

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

        if (!TryFindOverlayContainer(out var parentComponent, out var containerLayout))
        {
            Debug.WriteLine("Overlay presentation failed: no available OverlayHost container was found.");
            return false;
        }

        if (containerLayout.Children.Contains(layout))
        {
            Debug.WriteLine("Overlay presentation skipped: layout is already mounted.");
            return false;
        }

        // Android 11+ can report top insets that affect snackbar placement.
        if (component is SnackbarComponent { Presenter: SnackbarPresenter snackbarPresenter }
            && DeviceInfo.Platform == DevicePlatform.Android
            && DeviceInfo.Version.Major >= 11)
        {
            var insets = SafeAreaInsetsService.GetSafeAreaInsets();
            if (insets.Top > 0) snackbarPresenter.TranslationY = insets.Top;
        }

        var historyItem = new ComponentHistoryItem(parentComponent.GetType(), component);
        if (component is SnackbarComponent) Snackbars.Add(historyItem);
        else Popups.Add(historyItem);

        AbsoluteLayout.SetLayoutFlags(layout, AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutBounds(layout, new Rect(0, 0, 1, 1));
        layout.IsVisible = false;
        containerLayout.Children.Add(layout);
        layout.ZIndex = 10;
        layout.IsVisible = true;
        return true;
    }

    private bool TryFindOverlayContainer(out Component parentComponent, out AbsoluteLayout containerLayout)
    {
        if (TryGetOverlayContainer(Popups.LastOrDefault()?.Component, out parentComponent, out containerLayout))
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
        AbsoluteLayout? containerLayout = null;
        Component? parentComponent = null;

        if (component.Presenter is Layout layout)
        {
            if (Popups.Count > 0 &&
                Popups.Last().Component.Presenter is OverlayHost { OverlayContainer: not null } popupHost &&
                popupHost.OverlayContainer.Children.Contains(layout))
            {
                parentComponent = Popups.Last().Component;
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

                if (parentComponent is not null)
                {
                    if (component is SnackbarComponent)
                    {
                        var snackbarHistoryItem = Snackbars.FirstOrDefault(item => ReferenceEquals(item.Component, component));
                        if (snackbarHistoryItem is not null) Snackbars.Remove(snackbarHistoryItem);
                    }
                    else
                    {
                        var popupHistoryItem = Popups.FirstOrDefault(item => ReferenceEquals(item.Component, component));
                        if (popupHistoryItem is not null) Popups.Remove(popupHistoryItem);
                    }
                }
            }
        }
    }

    private NavigationPage CreateNavigationPage(Page rootPage)
    {
        var navigationPage = new NavigationPage(rootPage)
        {
            BarTextColor = Color.FromArgb("#FFFFFF")
        };

        if (Application.Current is not null &&
            Application.Current.Resources.TryGetValue("Trout", out var troutColorResource) &&
            Application.Current.Resources.TryGetValue("Charade", out var charadeColorResource))
        {
            var brush = new LinearGradientBrush()
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            var troutColor = (Color)troutColorResource;
            var charadeColor = (Color)charadeColorResource;
            brush.GradientStops.Add(new GradientStop(troutColor, 0));
            brush.GradientStops.Add(new GradientStop(charadeColor, 1));
            navigationPage.Background = brush;
        }

        return navigationPage;
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
        var result = new List<Component>();
        var seen = new HashSet<Component>(ReferenceEqualityComparer.Instance);

        AddResumeComponent(MountedComponent, result, seen);
        AddResumeComponent(CurrentTabComponent, result, seen);
        AddResumeComponent(CurrentFlyoutComponent, result, seen);
        AddResumeComponent(ComponentsStack.LastOrDefault(), result, seen);

        return result;
    }

    private static void AddResumeComponent(
        Component? component,
        ICollection<Component> result,
        ISet<Component> seen)
    {
        if (component is not null && seen.Add(component))
            result.Add(component);
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
