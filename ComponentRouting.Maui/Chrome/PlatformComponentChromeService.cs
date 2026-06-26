using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ComponentRouting.Maui.Abstraction.Core;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace ComponentRouting.Maui.Chrome;

public sealed class PlatformComponentChromeService : ComponentChromeService
{
    private readonly RouterRuntimeLifecycle? runtimeLifecycle;

#if ANDROID
    private readonly AndroidModalWindowDiscoveryService discovery;
    private readonly AndroidWindowChromeApplier applier;
    private readonly object lifecycleGate = new();
    // Fase 3 intentionally keeps handlers for the MAUI page lifetime; monitor before making this service default.
    private readonly HashSet<LifecycleRegistrationKey> lifecycleRegistrations = new();

    public PlatformComponentChromeService(
        AndroidModalWindowDiscoveryService discovery,
        AndroidWindowChromeApplier applier,
        RouterRuntimeLifecycle? runtimeLifecycle = null)
    {
        this.discovery = discovery;
        this.applier = applier;
        this.runtimeLifecycle = runtimeLifecycle;
    }
#elif IOS
    private readonly IosWindowChromeApplier iosApplier;
    private readonly IosStatusBarStyleCoordinator iosStatusBarStyleCoordinator;
    private readonly IosStatusBarHostControllerService iosStatusBarHostControllerService;

    internal PlatformComponentChromeService(
        IosWindowChromeApplier iosApplier,
        IosStatusBarStyleCoordinator iosStatusBarStyleCoordinator,
        IosStatusBarHostControllerService iosStatusBarHostControllerService,
        RouterRuntimeLifecycle? runtimeLifecycle = null)
    {
        this.iosApplier = iosApplier;
        this.iosStatusBarStyleCoordinator = iosStatusBarStyleCoordinator;
        this.iosStatusBarHostControllerService = iosStatusBarHostControllerService;
        this.runtimeLifecycle = runtimeLifecycle;
    }
#else
    public PlatformComponentChromeService(RouterRuntimeLifecycle? runtimeLifecycle = null)
    {
        this.runtimeLifecycle = runtimeLifecycle;
    }
#endif

    public void Apply(ComponentChromeContext context)
    {
        if (IsShuttingDown)
            return;

#if IOS
        ApplyIos(context);
#else
        if (!context.Options.HasAnyConfiguredValue)
            return;

#if ANDROID
        ApplyAndroid(context);
#endif
#endif
    }

    public void RegisterLifecycle(ComponentChromeContext context)
    {
        if (IsShuttingDown)
            return;

        if (!context.Options.HasAnyConfiguredValue)
            return;

#if ANDROID
        RegisterAndroidLifecycle(context);
#endif
    }

#if ANDROID
    private void ApplyAndroid(ComponentChromeContext context)
    {
        if (IsShuttingDown)
            return;

        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!IsShuttingDown)
                    ApplyAndroid(context);
            });
            return;
        }

        if (IsShuttingDown)
            return;

        if (context.PresentationKind is ComponentPresentationKind.Modal or ComponentPresentationKind.FullscreenModal)
        {
            foreach (var candidate in discovery.FindModalDialogWindows(context.Component, context.MountablePage))
                applier.Apply(candidate.Window, context.Options);

            return;
        }

        var activityWindow = Platform.CurrentActivity?.Window;
        if (activityWindow is not null)
            applier.Apply(activityWindow, context.Options);
    }

    private void RegisterAndroidLifecycle(ComponentChromeContext context)
    {
        if (IsShuttingDown)
            return;

        var seenPages = new HashSet<int>();
        RegisterAndroidLifecycle(context, context.MountablePage, seenPages);
        RegisterAndroidLifecycle(context, context.Component.Presenter as Page, seenPages);

        if (context.Component is NavigationComponent { Navigation: not null } navigationComponent)
            RegisterAndroidLifecycle(context, navigationComponent.Navigation, seenPages);
    }

    private void RegisterAndroidLifecycle(
        ComponentChromeContext context,
        Page? page,
        HashSet<int> seenPages)
    {
        if (page is null)
            return;

        var pageHash = RuntimeHelpers.GetHashCode(page);
        if (!seenPages.Add(pageHash))
            return;

        var registrationKey = new LifecycleRegistrationKey(
            pageHash,
            RuntimeHelpers.GetHashCode(context.Component));

        lock (lifecycleGate)
        {
            if (!lifecycleRegistrations.Add(registrationKey))
                return;
        }

        page.HandlerChanged += (_, _) => Apply(context);
        page.Loaded += (_, _) => Apply(context);
        page.Appearing += (_, _) => ScheduleAppearingReapply(context);
    }

    private void ScheduleAppearingReapply(ComponentChromeContext context)
    {
        if (IsShuttingDown)
            return;

        _ = ReapplyAfterDelay(context, 16);
        _ = ReapplyAfterDelay(context, 100);
        _ = ReapplyAfterDelay(context, 300);
    }

    private async Task ReapplyAfterDelay(ComponentChromeContext context, int delayMilliseconds)
    {
        await Task.Delay(delayMilliseconds).ConfigureAwait(false);
        if (IsShuttingDown)
            return;

        Apply(context);
    }

    private readonly record struct LifecycleRegistrationKey(int PageHash, int ComponentHash);
#endif

#if IOS
    private void ApplyIos(ComponentChromeContext context)
    {
        if (IsShuttingDown)
            return;

        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!IsShuttingDown)
                    ApplyIos(context);
            });
            return;
        }

        if (IsShuttingDown)
            return;

        var window = iosStatusBarHostControllerService.ResolveWindow();
        iosStatusBarHostControllerService.EnsureInstalled(window);
        iosStatusBarStyleCoordinator.Apply(window, context.Options.StatusBarForeground);
        iosStatusBarHostControllerService.RequestStatusBarUpdate(window);

        iosApplier.Apply(context);
    }
#endif

    private bool IsShuttingDown => runtimeLifecycle?.IsShuttingDown == true;
}
