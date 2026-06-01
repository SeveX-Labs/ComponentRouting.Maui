using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace ComponentRouting.Maui.Abstraction
{
    public abstract class SnackbarPresenter : AbsoluteLayout, Presenter
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

        #region abstract methods

        public abstract Task Initialize(string text);

        #endregion

        #region event handlers

        protected void HandleTapped(Object sender, EventArgs e)
        {
            ActOnTappedOutside?.Invoke();
        }

        #endregion

    }
}

