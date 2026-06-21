using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Sample.Presenters.Modals;

public partial class OverlayMatrixModalPage : ContentPage, Presenter, OverlayHost
{
    private Func<Task>? showOverlay;
    private Func<Task>? showSnackbar;
    private Func<Task>? openPushInsideModal;
    private Func<Task>? closeOverlay;
    private Action? close;

    public OverlayMatrixModalPage()
    {
        InitializeComponent();
    }

    public AbsoluteLayout? OverlayContainer => OverlayLayer;

    public void Initialize(
        string title,
        string message,
        Func<Task> showOverlay,
        Func<Task> showSnackbar,
        Func<Task> openPushInsideModal,
        Func<Task> closeOverlay,
        Action close)
    {
        Title = title;
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        this.showOverlay = showOverlay;
        this.showSnackbar = showSnackbar;
        this.openPushInsideModal = openPushInsideModal;
        this.closeOverlay = closeOverlay;
        this.close = close;
    }

    private async void HandleShowOverlayClicked(object? sender, EventArgs e) => await Invoke(showOverlay);
    private async void HandleShowSnackbarClicked(object? sender, EventArgs e) => await Invoke(showSnackbar);
    private async void HandleOpenPushInsideModalClicked(object? sender, EventArgs e) => await Invoke(openPushInsideModal);
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
