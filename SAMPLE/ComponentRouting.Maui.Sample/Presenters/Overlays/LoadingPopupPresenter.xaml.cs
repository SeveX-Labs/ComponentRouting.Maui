namespace ComponentRouting.Maui.Sample.Presenters.Overlays;

public partial class LoadingPopupPresenter : Grid, Presenter
{
    public LoadingPopupPresenter()
    {
        InitializeComponent();
    }

    public void Initialize(string title, string message)
    {
        TitleLabel.Text = title;
        MessageLabel.Text = message;
    }
}
