using Microsoft.Maui.Controls;
using ComponentRouting.Maui.Abstraction.Core;

namespace ComponentRouting.Maui.Abstraction
{
    public abstract class PageComponent<TState, TResult> : AbstractComponent<TState, TResult>, NavigationComponent
    {
        #region NavigationComponent implementation

        public NavigationPage? Navigation { get; set; }

        #endregion

        #region abstract methods implementation

        public override void Dispose()
        {
            Navigation = null;
            base.Dispose();
        }

        #endregion

    }
}
