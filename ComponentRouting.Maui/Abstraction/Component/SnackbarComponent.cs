using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace ComponentRouting.Maui.Abstraction
{
    public class SnackbarConfiguration
    {
        #region auto-properties

        public string Text { get; }
        public bool MustCloseAutomatically { get; }
        public int ClosureDelayMs { get; }

        #endregion

        #region ctor(s)

        public SnackbarConfiguration(string text, bool mustCloseAutomatically, int closureDelayMs)
        {
            Text = text;
            MustCloseAutomatically = mustCloseAutomatically;
            ClosureDelayMs = closureDelayMs;
        }

        #endregion
    }

    public abstract class SnackbarComponent : OverlayComponent<SnackbarConfiguration, bool>
    {
        #region properties

        private SnackbarPresenter SnackbarPresenter => (Presenter as SnackbarPresenter);

        #endregion

        #region abstract methods implementation

        public override View? Backdrop => (Presenter as View);

        protected override Task Configure(SnackbarConfiguration state)
        {
            SnackbarPresenter.Configure(HandleTappedOutside);
            return Task.CompletedTask;
        }


        protected override Task Initialize(SnackbarConfiguration state)
        {
            return SnackbarPresenter.Initialize(state.Text);
        }

        protected override Task PresentInternal()
        {
            if (State.MustCloseAutomatically)
            {
                Task.Run(async () =>
                {
                    int delay = State.ClosureDelayMs > 0 ? State.ClosureDelayMs : 3000;

                    await Task.Delay(delay);

                    await MainThread.InvokeOnMainThreadAsync(() => CompletionSource?.TrySetResult(false));
                }).ForgetSafely("Snackbar auto-dismiss");
            }

            return Task.CompletedTask;
        }

        #endregion

        #region event handler

        private async void HandleTappedOutside()
        {
            try
            {
                if (CompletionSource is not null)
                    await MainThread.InvokeOnMainThreadAsync(() => CompletionSource.TrySetResult(false));
            }
            catch (Exception ex)
            {
                ComponentRoutingDiagnostics.WriteException(ex);
            }
        }

        #endregion
    }
}
