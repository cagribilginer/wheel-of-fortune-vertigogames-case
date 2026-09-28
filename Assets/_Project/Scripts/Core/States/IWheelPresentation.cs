namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// Everything the flow needs the screen to do, without a single Unity type. Each animating call takes a
    /// completion callback, so the machine advances only when the screen is done and a test double can finish at once.
    /// </summary>
    public interface IWheelPresentation
        : IZonePresentation, IInputPresentation, ISpinPresentation, IGameOverPresentation, ICashOutPresentation
    {
    }
}
