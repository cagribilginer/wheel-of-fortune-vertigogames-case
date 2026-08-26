using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Builds the wheel for the current zone and lets the screen re-theme and scroll before input opens.
    /// </summary>
    public sealed class ZoneSetupState : GameStateBase
    {
        public ZoneSetupState(GameContext context) : base(context) { }

        public override void Enter()
        {
            int zone = Context.Run.CurrentZone;
            ZoneType zoneType = Context.Run.CurrentZoneType;

            Context.CurrentWheel = Context.WheelFactory.Build(zone, zoneType);
            Context.Presentation.ShowZone(zone, zoneType, Context.CurrentWheel, () => Machine.Change<IdleState>());
        }
    }
}
