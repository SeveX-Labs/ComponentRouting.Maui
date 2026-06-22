using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Components;
using ComponentRouting.Maui.Sample.Components.Overlays;
using ComponentRouting.Maui.Sample.Components.Snackbars;
using ComponentRouting.Maui.Sample.Presenters.Modals;

namespace ComponentRouting.Maui.Sample.Components.Modals;

public sealed class OverlayMatrixFullscreenModalComponent
    : FullscreenModalPageComponent<OverlayMatrixFullscreenModalComponent.ComponentState, bool>
{
    private readonly Router router;

    public OverlayMatrixFullscreenModalComponent(Router router)
    {
        this.router = router;
    }

    public readonly record struct ComponentState(string Title, string Message);

    protected override Presenter CreatePresenter()
    {
        return new OverlayMatrixFullscreenModalPage();
    }

    protected override Task Configure(ComponentState state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(ComponentState state)
    {
        ((OverlayMatrixFullscreenModalPage)Presenter!).Initialize(
            state.Title,
            state.Message,
            ShowOverlay,
            ShowSnackbar,
            ShowMutablePopup,
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
        if (router.GetMountedOverlayComponents<LoadingPopupComponent>().Count == 0)
        {
            _ = router.PresentComponent<LoadingPopupComponent, LoadingPopupComponent.ComponentState, bool>(
                new LoadingPopupComponent.ComponentState(
                    "Overlay from fullscreen modal",
                    "This should mount on the fullscreen modal platform surface."));
        }

        return Task.CompletedTask;
    }

    private Task ShowSnackbar()
    {
        _ = router.PresentComponent<InfoSnackbarComponent, SnackbarConfiguration, bool>(
            new SnackbarConfiguration("Snackbar from fullscreen modal", false, 0));
        return Task.CompletedTask;
    }

    private Task ShowMutablePopup()
    {
        if (router.GetMountedOverlayComponents<MutablePopupComponent>().Count == 0)
        {
            _ = router.PresentComponent<MutablePopupComponent, MutablePopupComponent.ComponentState, bool>(
                new MutablePopupComponent.ComponentState(
                    "Mutable popup from fullscreen modal",
                    "Initial text. Use the buttons inside this popup to update or unpresent it through mounted overlay lookup."));
        }

        return Task.CompletedTask;
    }

    private Task CloseOverlay()
    {
        router.GetMountedOverlayComponent<LoadingPopupComponent>()?.Unpresent();
        return Task.CompletedTask;
    }

    private void Close()
    {
        CompletionSource?.TrySetResult(true);
    }
}
