namespace Rustline.Presentation
{
    public enum FrontendState { Title, TitleOptions, Credits, Playing, Paused, PauseOptions, Transition }

    /// <summary>Scene-local navigation policy. Loading is a terminal state until scene teardown.</summary>
    public sealed class FrontendStateMachine
    {
        public FrontendState State { get; private set; }
        public bool IsPaused => State == FrontendState.Paused || State == FrontendState.PauseOptions;
        public bool IsOptions => State == FrontendState.TitleOptions || State == FrontendState.PauseOptions;
        public bool IsModal => State != FrontendState.Playing;
        public FrontendStateMachine(bool title) => State = title ? FrontendState.Title : FrontendState.Playing;

        public bool OpenOptions() => Move(State == FrontendState.Title ? FrontendState.TitleOptions :
            FrontendState.PauseOptions, State == FrontendState.Title || State == FrontendState.Paused);
        public bool OpenCredits() => Move(FrontendState.Credits, State == FrontendState.Title);
        public bool Pause() => Move(FrontendState.Paused, State == FrontendState.Playing);
        public bool BeginTransition() => Move(FrontendState.Transition,
            State == FrontendState.Title || State == FrontendState.Paused);

        public bool Back()
        {
            switch (State)
            {
                case FrontendState.Playing: return Pause();
                case FrontendState.Paused: return Move(FrontendState.Playing, true);
                case FrontendState.PauseOptions: return Move(FrontendState.Paused, true);
                case FrontendState.TitleOptions:
                case FrontendState.Credits: return Move(FrontendState.Title, true);
                default: return false;
            }
        }

        private bool Move(FrontendState next, bool valid)
        {
            if (!valid || State == next) return false;
            State = next;
            return true;
        }
    }
}
