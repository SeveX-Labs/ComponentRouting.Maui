using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Presenters.Snackbars;

namespace ComponentRouting.Maui.Sample.Components.Snackbars;

public sealed class InfoSnackbarComponent : SnackbarComponent
{
    protected override Presenter CreatePresenter()
    {
        return new InfoSnackbarPresenter();
    }
}
