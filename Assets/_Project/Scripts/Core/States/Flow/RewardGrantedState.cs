namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>Banks the reward, then advances a zone and rebuilds the wheel.</summary>
    public sealed class RewardGrantedState : GameStateBase
    {
        public RewardGrantedState(GameContext context) : base(context) { }

        public override void Enter()
        {
            Context.Run.Grant(Context.PendingOutcome, Context.PendingOutcome.UnitValue);

            Context.Presentation.PlayRewardGranted(Context.PendingOutcome, () =>
            {
                Context.Run.AdvanceZone();
                Machine.Change<ZoneSetupState>();
            });
        }
    }
}
