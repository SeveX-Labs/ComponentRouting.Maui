using ComponentRouting.Maui.Abstraction;

namespace ComponentRouting.Maui.Sample.Components.Base;

public abstract class SampleTabComponent<TState> : TabComponent<TState>
{
    public Presenter? PassedPresenter { get; private set; }

    public new Presenter? Presenter => PassedPresenter;

    public void InsertPresenter(Presenter presenter)
    {
        PassedPresenter = presenter;
    }

    protected override Presenter CreatePresenter()
    {
        return PassedPresenter
               ?? throw new InvalidOperationException(
                   $"{GetType().Name} requires an existing presenter. Call {nameof(InsertPresenter)} before Prepare.");
    }
}
