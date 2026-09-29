namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Where every run starts: the first launch, a give-up after a bomb and a finished cash-out. Clears the
    /// prior run and hands straight over to zone setup. Lives for one transition.
    /// </summary>
    public sealed class BootState : GameStateBase
    {
        public BootState(GameContext context) : base(context) { }

        public override void Enter()
        {
            Context.Run.ResetRun();
            Machine.Change<ZoneSetupState>();
        }
    }
}
