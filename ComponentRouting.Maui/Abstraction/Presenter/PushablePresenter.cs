using Microsoft.Maui.Controls;
using System;

namespace ComponentRouting.Maui.Abstraction
{
    public abstract class PushablePresenter : ContentPage, Presenter
    {
        #region delegate

        private Action? ActOnBackTapped { get; set; }

        #endregion

        #region ctor(s)

        public PushablePresenter()
        {
        }

        #endregion

        #region access methods

        public void Configure(Action? actOnBackTapped)
        {
            ActOnBackTapped = actOnBackTapped;
        }

        #endregion

        #region event handlers

        protected virtual void HandleBackTapped(object? sender, EventArgs e)
        {
            if (ActOnBackTapped is not null) ActOnBackTapped();
        }

        #endregion

    }
}
