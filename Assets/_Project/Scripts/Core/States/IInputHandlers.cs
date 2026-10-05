namespace Vertigo.Wheel.Core.States
{
    // One small capability per player input. A state implements exactly the ones it accepts, so what a state
    // reacts to is stated in its declaration instead of being a list of overridden no-ops. The machine
    // forwards an input only to a state that declares the matching handler; anything else is ignored on
    // purpose, which is why double-tapping spin mid-spin cannot queue a second spin.

    /// <summary>The state accepts the SPIN button.</summary>
    public interface ISpinInputHandler
    {
        void OnSpinRequested();
    }

    /// <summary>The state accepts the EXIT button (walk away with the haul).</summary>
    public interface IExitInputHandler
    {
        void OnExitRequested();
    }

    /// <summary>The state accepts a confirmation.</summary>
    public interface IConfirmInputHandler
    {
        void OnConfirmed();
    }

    /// <summary>The state accepts a cancellation.</summary>
    public interface ICancelInputHandler
    {
        void OnCancelled();
    }

    /// <summary>The state accepts a restart (give up).</summary>
    public interface IRestartInputHandler
    {
        void OnRestartRequested();
    }

    /// <summary>The state accepts a paid revive.</summary>
    public interface IContinueInputHandler
    {
        void OnContinueRequested();
    }

    /// <summary>The state accepts a free ad revive.</summary>
    public interface IAdContinueInputHandler
    {
        void OnAdContinueRequested();
    }
}
