using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using ComponentRouting.Maui.Abstraction.Core;

namespace ComponentRouting.Maui.Abstraction
{

    public abstract class RootComponent : AbstractComponent<bool, bool>, NavigationComponent
    {
        #region NavigationComponent implementation

        public NavigationPage? Navigation { get; set; }

        #endregion

        #region abstract methods implementation

        protected override Task PresentInternal()
        {
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            Navigation = null;
            base.Dispose();
        }

        #endregion
    }
}
