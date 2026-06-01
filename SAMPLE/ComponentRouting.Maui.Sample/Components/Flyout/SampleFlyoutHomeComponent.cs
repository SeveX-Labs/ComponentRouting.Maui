using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Components.Base;
using ComponentRouting.Maui.Sample.Components.Overlays;
using ComponentRouting.Maui.Sample.Components.Pushables;
using ComponentRouting.Maui.Sample.Components.Root;
using ComponentRouting.Maui.Sample.Components.Snackbars;
using ComponentRouting.Maui.Sample.Models;
using ComponentRouting.Maui.Sample.Presenters.Flyout;

namespace ComponentRouting.Maui.Sample.Components.Flyout;

public sealed class SampleFlyoutHomeComponent : SampleFlyoutComponent<bool>
{
    private readonly ComponentFactory componentFactory;
    private readonly Router router;

    public SampleFlyoutHomeComponent(ComponentFactory componentFactory, Router router)
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
        var page = (SampleFlyoutHomePage)Presenter!;
        page.Configure(
            StartWizard,
            ShowLoading,
            HideLoading,
            ShowSnackbar,
            CountSnackbars);
        page.SetFactoryStatus(ReferenceEquals(
            componentFactory.CreateComponent<SampleFlyoutRootComponent>(),
            componentFactory.CreateComponent<SampleFlyoutRootComponent>()));
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }

    private async Task StartWizard()
    {
        var result = await router.PresentComponent<WizardStepComponent, WizardStepComponent.ComponentState, WizardResult>(
            new WizardStepComponent.ComponentState("Flyout wizard step", "Step one preloads the confirmation component."));
        ((SampleFlyoutHomePage)Presenter!).SetLastResult($"Wizard result: {result}");
    }

    private Task ShowLoading()
    {
        if (router.GetMountedComponents<LoadingPopupComponent>().Count == 0)
        {
            _ = router.PresentComponent<LoadingPopupComponent, LoadingPopupComponent.ComponentState, bool>(
                new LoadingPopupComponent.ComponentState("Flyout loading popup", "Mounted lookup can hide this overlay."));
        }

        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private Task HideLoading()
    {
        router.GetMountedComponent<LoadingPopupComponent>()?.Unpresent();
        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private Task ShowSnackbar()
    {
        _ = router.PresentComponent<InfoSnackbarComponent, SnackbarConfiguration, bool>(
            new SnackbarConfiguration("Transient snackbar from flyout root", false, 0));
        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private Task CountSnackbars()
    {
        var snackbars = router.GetMountedComponents<InfoSnackbarComponent>();
        var message = $"Mounted snackbar count: {snackbars.Count}";

        try
        {
            _ = router.GetMountedComponent<InfoSnackbarComponent>();
            message += ". Single mounted lookup did not throw.";
        }
        catch (InvalidOperationException ex)
        {
            message += $". Single mounted lookup threw: {ex.Message}";
        }

        ((SampleFlyoutHomePage)Presenter!).SetLastResult(message);
        UpdateMountedCounts();
        return Task.CompletedTask;
    }

    private void UpdateMountedCounts()
    {
        ((SampleFlyoutHomePage)Presenter!).SetMountedCounts(
            router.GetMountedComponents<LoadingPopupComponent>().Count,
            router.GetMountedComponents<InfoSnackbarComponent>().Count);
    }
}
