using System.Linq;
using NUnit.Framework;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.Core.States.Flow;
using Vertigo.Wheel.Core.Zones;
using Vertigo.Wheel.Tests.EditMode.Doubles;

namespace Vertigo.Wheel.Tests.EditMode
{
    /// <summary>
    /// Drives the real flow with an instant presentation and a scripted resolver, so every assertion is
    /// about the shipping state machine rather than a simplified stand-in.
    /// </summary>
    [TestFixture]
    public sealed class GameStateMachineTests
    {
        private const int BOMB_SLOT = 0;
        private const int REWARD_SLOT = 3;

        private InMemorySaveService _save;
        private Wallet _wallet;
        private RunModel _run;
        private FixedSliceResolver _resolver;
        private InstantPresentation _view;
        private GameStateMachine _machine;

        [SetUp]
        public void SetUp()
        {
            _save = new InMemorySaveService();
            _wallet = new Wallet(_save);
            _run = new RunModel(new ZoneClassifier(), _wallet, TestWheels.Currencies);

            var blueprints = new StubBlueprintProvider(BOMB_SLOT);
            var factory = new ZoneWheelFactory(new ZoneClassifier(), blueprints, new LinearRewardScaling());

            _resolver = new FixedSliceResolver(REWARD_SLOT);
            _view = new InstantPresentation();

            var context = new GameContext(
                _run, factory, new SpinService(_resolver),
                new ContinueService(_wallet, TestWheels.Gold, TestWheels.Continue), _view);

            _machine = GameFlow.Build(context);
            GameFlow.Start(_machine);
        }

        [Test]
        public void BootFallsThroughToIdleOnZoneOne()
        {
            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_run.CurrentZone, Is.EqualTo(1));
            Assert.That(_view.LastZoneShown, Is.EqualTo(1));
        }

        [Test]
        public void SpinFromIdle_GrantsTheRewardAndAdvancesOneZone()
        {
            _machine.RequestSpin();

            Assert.That(_run.CurrentZone, Is.EqualTo(2));
            // Zone 1 is a normal zone: the stub pays Pistol x10 there, unscaled at zone 1.
            Assert.That(_run.Bank.AmountOf(TestWheels.Pistol), Is.EqualTo(10));
            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_view.SpinsPlayed, Is.EqualTo(1));
        }

        [Test]
        public void TheAnimationIsToldTheSlotTheLogicChose()
        {
            _machine.RequestSpin();
            Assert.That(_view.LastSlotIndex, Is.EqualTo(REWARD_SLOT));
        }

        [Test]
        public void BombOutcome_ClearsTheBankAndOpensGameOver()
        {
            _machine.RequestSpin();               // bank a reward first
            _resolver.LandOn(BOMB_SLOT);
            _machine.RequestSpin();

            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_machine.IsIn<GameOverState>(), Is.True);
            Assert.That(_view.GameOverVisible, Is.True);
            Assert.That(_view.BombsPlayed, Is.EqualTo(1));
        }

        [Test]
        public void RestartAfterBomb_ReturnsToZoneOneWithAnEmptyBank()
        {
            AdvanceToZone(2);                             // bank something first, so the bomb has a haul to take
            _resolver.LandOn(BOMB_SLOT);
            _machine.RequestSpin();
            _machine.RequestRestart();

            Assert.That(_run.CurrentZone, Is.EqualTo(1));
            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_view.GameOverVisible, Is.False);
        }

        [Test]
        public void LeaveWithAnEmptyBank_IsIgnored()
        {
            // Fresh on zone 1 with nothing banked: there is nothing to cash out.
            _machine.RequestExit();

            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_view.CashOutVisible, Is.False);
        }

        [Test]
        public void LeaveOnANormalZoneWithAHaul_IsIgnored()
        {
            _machine.RequestSpin();                        // zone 1 -> 2, banks a reward

            Assert.That(_run.CurrentZoneType, Is.EqualTo(ZoneType.Normal));
            _machine.RequestExit();

            Assert.That(_machine.IsIn<IdleState>(), Is.True, "Normal zones may not be cashed out from.");
            Assert.That(_view.CashOutVisible, Is.False);
        }

        [Test]
        public void LeaveOnASafeZoneWithAHaul_OpensTheCashOutSummary()
        {
            AdvanceToZone(5);                              // zone 5 is safe

            Assert.That(_run.CurrentZoneType, Is.EqualTo(ZoneType.Safe));
            _machine.RequestExit();

            Assert.That(_machine.IsIn<CashOutState>(), Is.True);
            Assert.That(_view.CashOutVisible, Is.True);
        }

        [Test]
        public void TheCashOutSummary_ListsOnlyWhatTheClaimKeeps()
        {
            AdvanceToZone(5);                              // the run banked Pistol/Rifle points on the way
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Gold, 30));

            _machine.RequestExit();

            Assert.That(_view.CashOutHaul.Select(e => e.Reward), Is.EqualTo(new[] { TestWheels.Gold }),
                "Weapons, chests and points are gone when the run ends, so the claim screen must not list them.");
            Assert.That(_view.CashOutHaul[0].Amount, Is.EqualTo(30));
        }

        [Test]
        public void ConfirmingCashOut_ClaimsTheHaulAndStartsAFreshRun()
        {
            AdvanceToZone(5);
            _machine.RequestExit();
            _machine.Confirm();

            Assert.That(_run.CurrentZone, Is.EqualTo(1));
            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_view.CashOutVisible, Is.False);
            Assert.That(_machine.IsIn<IdleState>(), Is.True);
        }

        [Test]
        public void CancellingCashOut_ReturnsToTheSameZoneWithTheHaulIntact()
        {
            AdvanceToZone(5);                              // zone 5 is safe, so leaving is legal here
            int zoneBefore = _run.CurrentZone;
            int bankedBefore = _run.Bank.Entries.Count;

            _machine.RequestExit();
            _machine.Cancel();

            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_view.CashOutVisible, Is.False);
            Assert.That(_run.CurrentZone, Is.EqualTo(zoneBefore));
            Assert.That(_run.Bank.Entries.Count, Is.EqualTo(bankedBefore));
            Assert.That(bankedBefore, Is.GreaterThan(0));
        }

        [Test]
        public void ExitIsOfferedOnlyOnSafeOrSuperZonesWithAHaul()
        {
            Assert.That(_view.CanLeave, Is.False, "zone 1, empty bank");

            _machine.RequestSpin();                        // zone 1 -> 2, banks a reward
            Assert.That(_run.CurrentZoneType, Is.EqualTo(ZoneType.Normal));
            Assert.That(_view.CanLeave, Is.False, "zone 2 is Normal, even with a haul");

            AdvanceToZone(5);
            Assert.That(_run.CurrentZoneType, Is.EqualTo(ZoneType.Safe));
            Assert.That(_view.CanLeave, Is.True, "zone 5 is Safe, and the haul survived getting there");
        }

        [Test]
        public void ContinueIsNotOfferedWithAnEmptyWallet()
        {
            AdvanceToZone(2);                             // bank something first, so the bomb has a haul to take
            _resolver.LandOn(BOMB_SLOT);
            _machine.RequestSpin();

            Assert.That(_machine.IsIn<GameOverState>(), Is.True);
            Assert.That(_view.ContinueOffered, Is.False);
        }

        [Test]
        public void ContinueResumesTheSameZoneWithTheHaulIntact()
        {
            _wallet.Add(TestWheels.Gold, 10_000);

            _machine.RequestSpin();                       // zone 1 -> 2, banks a reward
            int zoneBefore = _run.CurrentZone;
            int bankedBefore = _run.Bank.Entries.Count;

            _resolver.LandOn(BOMB_SLOT);
            _machine.RequestSpin();                       // bomb clears the bank

            Assert.That(_view.ContinueOffered, Is.True);
            int walletBefore = _wallet.BalanceOf(TestWheels.Gold);

            _machine.RequestContinue();

            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_run.CurrentZone, Is.EqualTo(zoneBefore), "Continue must resume the same zone.");
            Assert.That(_wallet.BalanceOf(TestWheels.Gold), Is.LessThan(walletBefore), "The continue must have been paid for.");
            Assert.That(_run.GoldRevivesUsedThisRun, Is.EqualTo(1));
            Assert.That(bankedBefore, Is.GreaterThan(0));
            Assert.That(_run.Bank.Entries.Count, Is.EqualTo(bankedBefore), "the continue restores the lost haul");
        }

        [Test]
        public void GoldRevive_CanBeUsedMoreThanOncePerRun_AtADoublingPrice()
        {
            _wallet.Add(TestWheels.Gold, 100_000);
            AdvanceToZone(2);

            _resolver.LandOn(BOMB_SLOT);
            _machine.RequestSpin();                       // bomb -> game over
            Assert.That(_view.ContinueOffered, Is.True);
            int firstCost = _view.ContinueCostShown;

            _machine.RequestContinue();                   // gold revive #1
            Assert.That(_machine.IsIn<IdleState>(), Is.True);

            _machine.RequestSpin();                       // bombs again, same zone
            Assert.That(_machine.IsIn<GameOverState>(), Is.True);
            Assert.That(_view.ContinueOffered, Is.True, "gold revive has no per-run cap");
            Assert.That(_view.ContinueCostShown, Is.EqualTo(firstCost * 2), "the second gold revive costs double");

            _machine.RequestContinue();                   // gold revive #2
            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_run.GoldRevivesUsedThisRun, Is.EqualTo(2));
        }

        [Test]
        public void AdRevive_IsOfferedOnlyOncePerRun()
        {
            AdvanceToZone(2);

            _resolver.LandOn(BOMB_SLOT);
            _machine.RequestSpin();                       // bomb -> game over
            Assert.That(_view.AdReviveOffered, Is.True);

            _machine.RequestAdContinue();                 // free revive
            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_run.AdRevivesUsedThisRun, Is.EqualTo(1));

            _machine.RequestSpin();                       // bombs again, same zone
            Assert.That(_machine.IsIn<GameOverState>(), Is.True);
            Assert.That(_view.AdReviveOffered, Is.False, "the ad revive is one per run");
        }

        [Test]
        public void SuperZoneUsesTheGoldenWheelAndAllowsLeaving()
        {
            AdvanceToZone(30);

            Assert.That(_run.CurrentZoneType, Is.EqualTo(ZoneType.Super));
            Assert.That(_view.LastWheel.Tier, Is.EqualTo(WheelTier.Golden));
            Assert.That(_view.LastWheel.BombCount, Is.Zero);
            Assert.That(_view.CanLeave, Is.True);
        }

        /// <summary>Input is only accepted in Idle, so a second tap cannot queue a spin.</summary>
        [Test]
        public void InputIsRejectedOutsideIdle()
        {
            var blocking = new BlockingPresentation();
            var context = new GameContext(
                new RunModel(new ZoneClassifier(), _wallet, TestWheels.Currencies),
                new ZoneWheelFactory(new ZoneClassifier(), new StubBlueprintProvider(BOMB_SLOT), new LinearRewardScaling()),
                new SpinService(new FixedSliceResolver(REWARD_SLOT)),
                new ContinueService(_wallet, TestWheels.Gold, TestWheels.Continue),
                blocking);

            GameStateMachine machine = GameFlow.Build(context);
            GameFlow.Start(machine);

            machine.RequestSpin();
            Assert.That(machine.IsIn<SpinningState>(), Is.True);

            machine.RequestSpin();
            machine.RequestExit();

            Assert.That(machine.IsIn<SpinningState>(), Is.True, "No input may be honoured mid-spin.");
            Assert.That(blocking.SpinCalls, Is.EqualTo(1), "A second spin must not have been started.");
        }

        private void AdvanceToZone(int target)
        {
            _resolver.LandOn(REWARD_SLOT);
            while (_run.CurrentZone < target) _machine.RequestSpin();
        }

        /// <summary>Never completes its spin, holding the machine in SpinningState.</summary>
        private sealed class BlockingPresentation : InstantPresentation
        {
            public int SpinCalls { get; private set; }

            public override void PlaySpin(int slotIndex, System.Action onComplete)
            {
                SpinCalls++;
            }
        }
    }
}
