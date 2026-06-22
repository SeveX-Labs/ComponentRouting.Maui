using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Components;
using ComponentRouting.Maui.Sample.Components.Overlays;
using ComponentRouting.Maui.Sample.Components.Pushables;
using ComponentRouting.Maui.Sample.Components.Snackbars;
using ComponentRouting.Maui.Sample.Presenters.Modals;

namespace ComponentRouting.Maui.Sample.Components.Modals;

public sealed class OverlayMatrixModalComponent : ModalPageComponent<OverlayMatrixModalComponent.ComponentState, bool>
{
    private readonly Router router;

    public OverlayMatrixModalComponent(Router router)
    {
        this.router = router;
    }

    public readonly record struct ComponentState(string Title, string Message);

    protected override Presenter CreatePresenter()
    {
        return new OverlayMatrixModalPage();
    }

    protected override Task Configure(ComponentState state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(ComponentState state)
    {
        ((OverlayMatrixModalPage)Presenter!).Initialize(
            state.Title,
            state.Message,
            ShowOverlay,
            ShowSnackbar,
            ShowMutablePopup,
            OpenPushInsideModal,
            CloseOverlay,
            Close);
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }

    private Task ShowOverlay()
    {
        OverlayMatrixTraceLog.Click("Modal", "Show overlay from modal", this);
        if (router.GetMountedOverlayComponents<LoadingPopupComponent>().Count == 0)
        {
            _ = router.PresentComponent<LoadingPopupComponent, LoadingPopupComponent.ComponentState, bool>(
                new LoadingPopupComponent.ComponentState(
                    "Overlay from modal",
                    "This intentionally uses the current modal page overlay host."));
        }

        return Task.CompletedTask;
    }

    private Task ShowSnackbar()
    {
        OverlayMatrixTraceLog.Click("Modal", "Show snackbar from modal", this);
        _ = router.PresentComponent<InfoSnackbarComponent, SnackbarConfiguration, bool>(
            new SnackbarConfiguration("Snackbar from modal", false, 0));
        return Task.CompletedTask;
    }

    private Task ShowMutablePopup()
    {
        OverlayMatrixTraceLog.Click("Modal", "Show mutable popup from modal", this);
        if (router.GetMountedOverlayComponents<MutablePopupComponent>().Count == 0)
        {
            _ = router.PresentComponent<MutablePopupComponent, MutablePopupComponent.ComponentState, bool>(
                new MutablePopupComponent.ComponentState(
                    "Mutable popup from modal",
                    "Initial text. Use the buttons inside this popup to update or unpresent it through mounted overlay lookup."));
        }

        return Task.CompletedTask;
    }

    private async Task OpenPushInsideModal()
    {
        OverlayMatrixTraceLog.Click("Modal", "Open push inside modal overlay demo", this);
        await router.PresentComponent<OverlayMatrixPushComponent, OverlayMatrixPushComponent.ComponentState, bool>(
            new OverlayMatrixPushComponent.ComponentState(
                "Push inside modal",
                "This push page is opened from a modal navigation stack.",
                "push inside modal"));
    }

    private Task CloseOverlay()
    {
        OverlayMatrixTraceLog.Click("Modal", "Close overlay from modal", this);
        router.GetMountedOverlayComponent<LoadingPopupComponent>()?.Unpresent();
        return Task.CompletedTask;
    }

    private void Close()
    {
        OverlayMatrixTraceLog.Click("Modal", "Close modal overlay demo", this);
        CompletionSource?.TrySetResult(true);
    }
}
