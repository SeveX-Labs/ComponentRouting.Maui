using Microsoft.Maui.Controls;
using System;

namespace ComponentRouting.Maui.Abstraction
{
    public abstract class OverlayPresenter : AbsoluteLayout, Presenter
    {
        #region auto-properties

        protected Action? ActOnTappedOutside { get; set; }

        #endregion

        #region access methods

        public void Configure(Action actOnTappedOutside)
        {
            ActOnTappedOutside = actOnTappedOutside;
        }

        #endregion
    }
}
