using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Sample.Presenters.Modals;

public partial class OverlayMatrixFullscreenModalPage : ContentPage, Presenter, OverlayHost
{
    private Func<Task>? showOverlay;
    private Func<Task>? showSnackbar;
    private Func<Task>? showMutablePopup;
    private Func<Task>? closeOverlay;
    private Action? close;

    public OverlayMatrixFullscreenModalPage()
    {
        InitializeComponent();
    }

    public AbsoluteLayout? OverlayContainer => OverlayLayer;

    public void Initialize(
        string title,
        string message,
        Func<Task> showOverlay,
        Func<Task> showSnackbar,
        Func<Task> showMutablePopup,
        Func<Task> closeOverlay,
        Action close)
    {
        Title = title;
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        this.showOverlay = showOverlay;
        this.showSnackbar = showSnackbar;
        this.showMutablePopup = showMutablePopup;
        this.closeOverlay = closeOverlay;
        this.close = close;
    }

    private async void HandleShowOverlayClicked(object? sender, EventArgs e) => await Invoke(showOverlay);
    private async void HandleShowSnackbarClicked(object? sender, EventArgs e) => await Invoke(showSnackbar);
    private async void HandleShowMutablePopupClicked(object? sender, EventArgs e) => await Invoke(showMutablePopup);
    private async void HandleCloseOverlayClicked(object? sender, EventArgs e) => await Invoke(closeOverlay);

    private void HandleCloseClicked(object? sender, EventArgs e)
    {
        close?.Invoke();
    }

    private static async Task Invoke(Func<Task>? action)
    {
        if (action is not null)
            await action();
    }
}
