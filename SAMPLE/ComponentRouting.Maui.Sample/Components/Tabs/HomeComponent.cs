using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Components.Base;
using ComponentRouting.Maui.Sample.Components.Modals;
using ComponentRouting.Maui.Sample.Components.Overlays;
using ComponentRouting.Maui.Sample.Components.Pages;
using ComponentRouting.Maui.Sample.Components.Pushables;
using ComponentRouting.Maui.Sample.Components.Root;
using ComponentRouting.Maui.Sample.Components.Snackbars;
using ComponentRouting.Maui.Sample.Models;
using ComponentRouting.Maui.Sample.Presenters.Tabs;

namespace ComponentRouting.Maui.Sample.Components.Tabs;

public sealed class HomeComponent : SampleTabComponent<bool>
{
    private readonly ComponentFactory componentFactory;
    private readonly Router router;

    public HomeComponent(ComponentFactory componentFactory, Router router)
    {
        this.componentFactory = componentFactory;
        this.router = router;
    }

    protected override Task Configure(bool state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(bool state)
    {
        var page = (HomePage)Presenter!;
        page.Configure(
            OpenLogin,
            OpenDetails,
            StartWizard,
            ShowLoading,
            HideLoading,
            CloseAllPopups,
            ShowSnackbar,
            CountSnackbars,
            ShowMatrixRootOverlay,
            ShowMatrixRootSnackbar,
            OpenPushOverlayDemo,
            OpenModalOverlayDemo);
        page.SetFactoryStatus(ReferenceEquals(
            componentFactory.CreateComponent<SampleTabbedRootComponent>(),
            componentFactory.CreateComponent<SampleTabbedRootComponent>()));
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }

    private async Task OpenLogin()
    {
        var result = await router.PresentComponent<LoginComponent, LoginComponent.ComponentState, LoginResult>(
            new LoginComponent.ComponentState("Login page", "Complete this page to return a sample result."));
        ((HomePage)Presenter!).SetLastResult($"Login result: {result}");
        _ = router.PresentComponent<SampleTabbedRootComponent, bool, bool>(true);
    }

    private async Task OpenDetails()
    {
        var result = await router.PresentComponent<DetailsComponent, DetailsComponent.ComponentState, bool>(
            new DetailsComponent.ComponentState("Modal details", "This modal returns true or false."));
        ((HomePage)Presenter!).SetLastResult($"Details result: {result}");
    }

    private async Task StartWizard()
    {
        var result = await router.PresentComponent<WizardStepComponent, WizardStepComponent.ComponentState, WizardResult>(
            new WizardStepComponent.ComponentState("Wizard step", "Step one preloads the confirmation component."));
        ((HomePage)Presenter!).SetLastResult($"Wizard result: {result}");
    }

    private Task ShowMatrixRootOverlay()
    {
        if (router.GetMountedOverlayComponents<LoadingPopupComponent>().Count == 0)
        {
            _ = router.PresentComponent<LoadingPopupComponent, LoadingPopupComponent.ComponentState, bool>(
                new LoadingPopupComponent.ComponentState(
                    "Overlay from root",
                    "This intentionally uses the current root tab overlay host."));
        }

        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private Task ShowMatrixRootSnackbar()
    {
        _ = router.PresentComponent<InfoSnackbarComponent, SnackbarConfiguration, bool>(
            new SnackbarConfiguration("Snackbar from root", false, 0));
        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private async Task OpenPushOverlayDemo()
    {
        var result = await router.PresentComponent<OverlayMatrixPushComponent, OverlayMatrixPushComponent.ComponentState, bool>(
            new OverlayMatrixPushComponent.ComponentState(
                "Push overlay demo",
                "Use this pushed page to compare overlay and snackbar bounds from a PushAsync surface.",
                "push page"));
        ((HomePage)Presenter!).SetLastResult($"Push overlay demo result: {result}");
        UpdateMountedCounts();
    }

    private async Task OpenModalOverlayDemo()
    {
        var result = await router.PresentComponent<OverlayMatrixModalComponent, OverlayMatrixModalComponent.ComponentState, bool>(
            new OverlayMatrixModalComponent.ComponentState(
                "Modal overlay demo",
                "Use this modal to compare overlay and snackbar bounds from a modal surface and a push inside it."));
        ((HomePage)Presenter!).SetLastResult($"Modal overlay demo result: {result}");
        UpdateMountedCounts();
    }

    private Task ShowLoading()
    {
        if (router.GetMountedOverlayComponents<LoadingPopupComponent>().Count == 0)
        {
            _ = router.PresentComponent<LoadingPopupComponent, LoadingPopupComponent.ComponentState, bool>(
                new LoadingPopupComponent.ComponentState("Loading popup", "Mounted lookup can hide this overlay."));
        }

        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private Task HideLoading()
    {
        router.GetMountedOverlayComponent<LoadingPopupComponent>()?.Unpresent();
        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private Task CloseAllPopups()
    {
        router.CloseAllPopups();
        ((HomePage)Presenter!).SetLastResult("Closed all mounted popups.");
        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private Task ShowSnackbar()
    {
        _ = router.PresentComponent<InfoSnackbarComponent, SnackbarConfiguration, bool>(
            new SnackbarConfiguration("Transient snackbar from ComponentRouting.Maui", false, 0));
        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private Task CountSnackbars()
    {
        var snackbars = router.GetMountedOverlayComponents<InfoSnackbarComponent>();
        var message = $"Mounted snackbar count: {snackbars.Count}";

        try
        {
            _ = router.GetMountedOverlayComponent<InfoSnackbarComponent>();
            message += ". Single mounted lookup did not throw.";
        }
        catch (InvalidOperationException ex)
        {
            message += $". Single mounted lookup threw: {ex.Message}";
        }

        ((HomePage)Presenter!).SetLastResult(message);
        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private void UpdateMountedCounts()
    {
        ((HomePage)Presenter!).SetMountedCounts(
            router.GetMountedOverlayComponents<LoadingPopupComponent>().Count,
            router.GetMountedOverlayComponents<InfoSnackbarComponent>().Count);
    }
}
