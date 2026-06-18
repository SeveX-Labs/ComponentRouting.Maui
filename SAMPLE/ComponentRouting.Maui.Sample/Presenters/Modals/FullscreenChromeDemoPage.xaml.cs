namespace ComponentRouting.Maui.Sample.Presenters.Modals;

public partial class FullscreenChromeDemoPage : ContentPage, Presenter
{
    private Action? complete;

    public FullscreenChromeDemoPage()
    {
        InitializeComponent();
    }

    public void Initialize(string title, string message, Action complete)
    {
        Title = title;
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        this.complete = complete;
    }

    private void HandleCloseClicked(object? sender, EventArgs e)
    {
        complete?.Invoke();
    }
}
