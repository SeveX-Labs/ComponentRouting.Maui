using ComponentRouting.Maui.Abstraction;

namespace ComponentRouting.Maui.Sample.Presenters.Snackbars;

public partial class InfoSnackbarPresenter : SnackbarPresenter
{
    public InfoSnackbarPresenter()
    {
        InitializeComponent();
        GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => ActOnTappedOutside?.Invoke())
        });
    }

    public override Task Initialize(string text)
    {
        MessageLabel.Text = text;
        return Task.CompletedTask;
    }
}
