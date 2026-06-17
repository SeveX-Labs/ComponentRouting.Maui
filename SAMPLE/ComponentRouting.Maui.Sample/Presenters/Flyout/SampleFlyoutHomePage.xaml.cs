using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Sample.Presenters.Flyout;

public partial class SampleFlyoutHomePage : ContentPage, Presenter, OverlayHost
{
    private readonly Action openFlyout;
    private Func<Task>? startWizard;
    private Func<Task>? showLoading;
    private Func<Task>? hideLoading;
    private Func<Task>? closeAllPopups;
    private Func<Task>? showSnackbar;
    private Func<Task>? countSnackbars;

    public SampleFlyoutHomePage(Action openFlyout)
    {
        this.openFlyout = openFlyout;
        InitializeComponent();
    }

    public AbsoluteLayout? OverlayContainer => OverlayLayer;

    public void Configure(
        Func<Task> startWizard,
        Func<Task> showLoading,
        Func<Task> hideLoading,
        Func<Task> closeAllPopups,
        Func<Task> showSnackbar,
        Func<Task> countSnackbars)
    {
        this.startWizard = startWizard;
        this.showLoading = showLoading;
        this.hideLoading = hideLoading;
        this.closeAllPopups = closeAllPopups;
        this.showSnackbar = showSnackbar;
        this.countSnackbars = countSnackbars;
    }

    public void SetFactoryStatus(bool isSingleton)
    {
        FactoryStatusLabel.Text = isSingleton
            ? "SampleFlyoutRootComponent resolves as a singleton."
            : "SampleFlyoutRootComponent resolved as separate instances.";
    }

    public void SetMountedCounts(int loadingPopups, int snackbars)
    {
        MountedCountsLabel.Text = $"Mounted overlays: {loadingPopups}; snackbars: {snackbars}";
    }

    public void SetLastResult(string text)
    {
        LastResultLabel.Text = text;
    }

    private void HandleMenuClicked(object? sender, EventArgs e) => openFlyout();
    private async void HandleStartWizardClicked(object? sender, EventArgs e) => await Invoke(startWizard);
    private async void HandleShowLoadingClicked(object? sender, EventArgs e) => await Invoke(showLoading);
    private async void HandleHideLoadingClicked(object? sender, EventArgs e) => await Invoke(hideLoading);
    private async void HandleCloseAllPopupsClicked(object? sender, EventArgs e) => await Invoke(closeAllPopups);
    private async void HandleShowSnackbarClicked(object? sender, EventArgs e) => await Invoke(showSnackbar);
    private async void HandleCountSnackbarsClicked(object? sender, EventArgs e) => await Invoke(countSnackbars);

    private static async Task Invoke(Func<Task>? action)
    {
        if (action is not null)
            await action();
    }
}
