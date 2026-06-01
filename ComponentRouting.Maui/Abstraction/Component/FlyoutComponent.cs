using System.Threading.Tasks;

namespace ComponentRouting.Maui.Abstraction;

public abstract class FlyoutComponent<TState> : AbstractComponent<TState, bool>
{
    #region overrides

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }

    #endregion
}