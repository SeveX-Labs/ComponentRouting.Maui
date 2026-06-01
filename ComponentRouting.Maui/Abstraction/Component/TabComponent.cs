using System.Threading.Tasks;

namespace ComponentRouting.Maui.Abstraction
{
    public abstract class TabComponent<TState> : AbstractComponent<TState, bool>
    {
        #region overrides

        protected override Task PresentInternal()
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}
