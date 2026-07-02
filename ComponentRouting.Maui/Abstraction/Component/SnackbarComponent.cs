using System;
using System.Threading;
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

        private CancellationTokenSource? autoDismissCancellationTokenSource;

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
                // Cancel any previous timer for this instance, then start a cancellable one so a
                // manual/programmatic dismiss stops the pending auto-dismiss instead of firing late.
                CancelAutoDismissTimer();

                var cancellationTokenSource = new CancellationTokenSource();
                autoDismissCancellationTokenSource = cancellationTokenSource;

                RunAutoDismissTimerAsync(cancellationTokenSource.Token)
                    .ForgetSafely("Snackbar auto-dismiss");
            }

            return Task.CompletedTask;
        }

        private async Task RunAutoDismissTimerAsync(CancellationToken cancellationToken)
        {
            var delay = State.ClosureDelayMs > 0 ? State.ClosureDelayMs : 3000;

            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // The snackbar was dismissed/disposed before the delay elapsed: skip the late completion.
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() => CompletionSource?.TrySetResult(false));
        }

        public override bool Unpresent()
        {
            CancelAutoDismissTimer();
            return base.Unpresent();
        }

        public override void Dispose()
        {
            CancelAutoDismissTimer();
            base.Dispose();
        }

        private void CancelAutoDismissTimer()
        {
            var cancellationTokenSource = Interlocked.Exchange(ref autoDismissCancellationTokenSource, null);
            if (cancellationTokenSource is null)
                return;

            try
            {
                cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed: nothing to cancel.
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }

        #endregion

        #region event handler

        private async void HandleTappedOutside()
        {
            CancelAutoDismissTimer();

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
