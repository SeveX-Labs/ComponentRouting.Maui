namespace ComponentRouting.Maui.Abstraction
{
    public abstract class ModalPageComponent<TState, TResult> : PageComponent<TState, TResult>
    {
        #region abstract methods implementation

        public override void Dispose()
        {
            Navigation = null;
            base.Dispose();
        }

        #endregion
    }
}
