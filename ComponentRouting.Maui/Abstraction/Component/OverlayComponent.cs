
using Microsoft.Maui.Controls;

namespace ComponentRouting.Maui.Abstraction
{
    public abstract class OverlayComponent<TState, TResult> : AbstractComponent<TState, TResult>
    {
        #region properties

        public abstract View? Backdrop { get; }

        #endregion
    }
}
