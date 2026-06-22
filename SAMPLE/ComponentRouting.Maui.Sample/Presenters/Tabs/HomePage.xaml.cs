using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Sample.Presenters.Tabs;

public partial class HomePage : ContentPage, Presenter, OverlayHost
{
    private Func<Task>? openLogin;
    private Func<Task>? openDetails;
    private Func<Task>? startWizard;
    private Func<Task>? showLoading;
    private Func<Task>? hideLoading;
    private Func<Task>? closeAllPopups;
    private Func<Task>? showSnackbar;
    private Func<Task>? countSnackbars;
    private Func<Task>? showMatrixRootOverlay;
    private Func<Task>? showMatrixRootSnackbar;
    private Func<Task>? openPushOverlayDemo;
    private Func<Task>? openModalOverlayDemo;
    private Func<Task>? openFullscreenModalOverlayDemo;
    private Func<Task>? showMutablePopup;

    public HomePage()
    {
        InitializeComponent();
    }

    public AbsoluteLayout? OverlayContainer => OverlayLayer;

    public void Configure(
        Func<Task> openLogin,
        Func<Task> openDetails,
        Func<Task> startWizard,
        Func<Task> showLoading,
        Func<Task> hideLoading,
        Func<Task> closeAllPopups,
        Func<Task> showSnackbar,
        Func<Task> countSnackbars,
        Func<Task> showMatrixRootOverlay,
        Func<Task> showMatrixRootSnackbar,
        Func<Task> openPushOverlayDemo,
        Func<Task> openModalOverlayDemo,
        Func<Task> openFullscreenModalOverlayDemo,
        Func<Task> showMutablePopup)
    {
        this.openLogin = openLogin;
        this.openDetails = openDetails;
        this.startWizard = startWizard;
        this.showLoading = showLoading;
        this.hideLoading = hideLoading;
        this.closeAllPopups = closeAllPopups;
        this.showSnackbar = showSnackbar;
        this.countSnackbars = countSnackbars;
        this.showMatrixRootOverlay = showMatrixRootOverlay;
        this.showMatrixRootSnackbar = showMatrixRootSnackbar;
        this.openPushOverlayDemo = openPushOverlayDemo;
        this.openModalOverlayDemo = openModalOverlayDemo;
        this.openFullscreenModalOverlayDemo = openFullscreenModalOverlayDemo;
        this.showMutablePopup = showMutablePopup;
    }

    public void SetFactoryStatus(bool isSingleton)
    {
        FactoryStatusLabel.Text = isSingleton
            ? "SampleTabbedRootComponent resolves as a singleton."
            : "SampleTabbedRootComponent resolved as separate instances.";
    }

    public void SetMountedCounts(int loadingPopups, int snackbars)
    {
        MountedCountsLabel.Text = $"Mounted overlays: {loadingPopups}; snackbars: {snackbars}";
    }

    public void SetLastResult(string text)
    {
        LastResultLabel.Text = text;
    }

    private async void HandleOpenLoginClicked(object? sender, EventArgs e) => await Invoke(openLogin);
    private async void HandleOpenDetailsClicked(object? sender, EventArgs e) => await Invoke(openDetails);
    private async void HandleStartWizardClicked(object? sender, EventArgs e) => await Invoke(startWizard);
    private async void HandleShowLoadingClicked(object? sender, EventArgs e) => await Invoke(showLoading);
    private async void HandleHideLoadingClicked(object? sender, EventArgs e) => await Invoke(hideLoading);
    private async void HandleCloseAllPopupsClicked(object? sender, EventArgs e) => await Invoke(closeAllPopups);
    private async void HandleShowSnackbarClicked(object? sender, EventArgs e) => await Invoke(showSnackbar);
    private async void HandleCountSnackbarsClicked(object? sender, EventArgs e) => await Invoke(countSnackbars);
    private async void HandleShowMatrixRootOverlayClicked(object? sender, EventArgs e) => await Invoke(showMatrixRootOverlay);
    private async void HandleShowMatrixRootSnackbarClicked(object? sender, EventArgs e) => await Invoke(showMatrixRootSnackbar);
    private async void HandleOpenPushOverlayDemoClicked(object? sender, EventArgs e) => await Invoke(openPushOverlayDemo);
    private async void HandleOpenModalOverlayDemoClicked(object? sender, EventArgs e) => await Invoke(openModalOverlayDemo);
    private async void HandleOpenFullscreenModalOverlayDemoClicked(object? sender, EventArgs e) => await Invoke(openFullscreenModalOverlayDemo);
    private async void HandleShowMutablePopupClicked(object? sender, EventArgs e) => await Invoke(showMutablePopup);

    private static async Task Invoke(Func<Task>? action)
    {
        if (action is not null)
            await action();
    }
}
