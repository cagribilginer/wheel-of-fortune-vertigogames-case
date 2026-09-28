namespace Vertigo.Wheel.Core.States
{
    /// <summary>The spin and EXIT buttons.</summary>
    public interface IInputPresentation
    {
        /// <summary>Mirrors the current input legality onto the buttons. Never decides it.</summary>
        void SetInputState(InputState state);
    }
}
