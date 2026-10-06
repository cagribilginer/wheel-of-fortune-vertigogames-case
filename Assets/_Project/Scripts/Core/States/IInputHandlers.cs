namespace Vertigo.Wheel.Core.States
{
    // One capability per player input; a state implements the ones it accepts and the machine ignores the rest
    // on purpose, so double-tapping spin mid-spin cannot queue a second spin.

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
