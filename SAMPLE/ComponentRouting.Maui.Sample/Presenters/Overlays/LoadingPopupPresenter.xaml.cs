namespace ComponentRouting.Maui.Sample.Presenters.Overlays;

public partial class LoadingPopupPresenter : Grid, Presenter
{
    private Action? close;

    public LoadingPopupPresenter()
    {
        InitializeComponent();
    }

    public void Initialize(string title, string message, Action close)
    {
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        this.close = close;
    }

    private void HandleCloseClicked(object? sender, EventArgs e)
    {
        close?.Invoke();
    }
}
