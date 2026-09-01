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
        private const int BombSlot = 0;
        private const int RewardSlot = 3;

        private InMemorySaveService _save;
        private GoldWallet _wallet;
        private RunModel _run;
        private FixedSliceResolver _resolver;
        private InstantPresentation _view;
        private GameStateMachine _machine;

        [SetUp]
        public void SetUp()
        {
            _save = new InMemorySaveService();
            _wallet = new GoldWallet(_save);
            _run = new RunModel(new ZoneClassifier(), _wallet, TestWheels.Gold);

            var blueprints = new StubBlueprintProvider(BombSlot);
            var factory = new ZoneWheelFactory(new ZoneClassifier(), blueprints, new LinearRewardScaling());

            _resolver = new FixedSliceResolver(RewardSlot);
            _view = new InstantPresentation();

            var context = new GameContext(
                _run, factory, new SpinService(_resolver),
                new ContinueService(_wallet, ContinueSettings.Default), _view);

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
            // Zone 1 is a safe zone: the stub pays Rifle x20 there, unscaled at zone 1.
            Assert.That(_run.Bank.AmountOf(TestWheels.Rifle), Is.EqualTo(20));
            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_view.SpinsPlayed, Is.EqualTo(1));
        }

        [Test]
        public void TheAnimationIsToldTheSlotTheLogicChose()
        {
            _machine.RequestSpin();
            Assert.That(_view.LastSlotIndex, Is.EqualTo(RewardSlot));
        }

        [Test]
        public void BombOutcome_ClearsTheBankAndOpensGameOver()
        {
            _machine.RequestSpin();               // bank a reward first
            _resolver.LandOn(BombSlot);
            _machine.RequestSpin();

            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_machine.IsIn<GameOverState>(), Is.True);
            Assert.That(_view.GameOverVisible, Is.True);
            Assert.That(_view.BombsPlayed, Is.EqualTo(1));
        }

        [Test]
        public void RestartAfterBomb_ReturnsToZoneOneWithAnEmptyBank()
        {
            AdvanceToZone(2);                             // zone 1 is safe; the bomb only bites from zone 2
            _resolver.LandOn(BombSlot);
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
            _machine.RequestLeave();

            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_view.CashOutVisible, Is.False);
        }

        [Test]
        public void LeaveOnANormalZoneWithAHaul_OpensTheCashOutSummary()
        {
            _machine.RequestSpin();                        // zone 1 -> 2, banks a reward

            Assert.That(_run.CurrentZoneType, Is.EqualTo(ZoneType.Normal));
            _machine.RequestLeave();

            Assert.That(_machine.IsIn<CashOutState>(), Is.True);
            Assert.That(_view.CashOutVisible, Is.True);
            Assert.That(_view.CashOutHaul, Is.Not.Empty, "The summary must list the haul before it is cleared.");
        }

        [Test]
        public void ConfirmingCashOut_ClaimsTheHaulAndStartsAFreshRun()
        {
            AdvanceToZone(5);
            _machine.RequestLeave();
            _machine.Confirm();

            Assert.That(_run.CurrentZone, Is.EqualTo(1));
            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_view.CashOutVisible, Is.False);
            Assert.That(_machine.IsIn<IdleState>(), Is.True);
        }

        [Test]
        public void CancellingCashOut_ReturnsToTheSameZoneWithTheHaulIntact()
        {
            _machine.RequestSpin();                        // zone 1 -> 2, banks a reward
            int zoneBefore = _run.CurrentZone;
            long bankedBefore = _run.Bank.TotalValue;

            _machine.RequestLeave();
            _machine.Cancel();

            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_view.CashOutVisible, Is.False);
            Assert.That(_run.CurrentZone, Is.EqualTo(zoneBefore));
            Assert.That(_run.Bank.TotalValue, Is.EqualTo(bankedBefore));
            Assert.That(bankedBefore, Is.GreaterThan(0));
        }

        [Test]
        public void ExitIsOfferedWheneverTheBankHasSomething()
        {
            Assert.That(_view.CanLeave, Is.False, "zone 1, empty bank");

            _machine.RequestSpin();                        // zone 1 -> 2, banks a reward
            Assert.That(_view.CanLeave, Is.True, "zone 2, has a haul");

            _machine.RequestSpin();                        // zone 2 -> 3
            Assert.That(_view.CanLeave, Is.True, "zone 3, still has a haul");
        }

        [Test]
        public void GiveUp_ForfeitsTheHaulAndRestarts()
        {
            _machine.RequestSpin();
            _machine.RequestGiveUp();

            Assert.That(_machine.IsIn<GiveUpConfirmState>(), Is.True);
            Assert.That(_view.GiveUpConfirmVisible, Is.True);

            _machine.Confirm();

            Assert.That(_run.CurrentZone, Is.EqualTo(1));
            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_view.GiveUpConfirmVisible, Is.False);
        }

        [Test]
        public void CancellingGiveUp_ReturnsToIdleWithTheHaulIntact()
        {
            _machine.RequestSpin();
            _machine.RequestGiveUp();
            _machine.Cancel();

            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_run.Bank.IsEmpty, Is.False, "the haul survives a cancelled give-up");
            Assert.That(_run.CurrentZone, Is.EqualTo(2));
        }

        [Test]
        public void ContinueIsNotOfferedWithAnEmptyWallet()
        {
            AdvanceToZone(2);                             // zone 1 is safe; the bomb only bites from zone 2
            _resolver.LandOn(BombSlot);
            _machine.RequestSpin();

            Assert.That(_machine.IsIn<GameOverState>(), Is.True);
            Assert.That(_view.ContinueOffered, Is.False);
        }

        [Test]
        public void ContinueResumesTheSameZoneWithTheHaulIntact()
        {
            _wallet.Add(10_000);

            _machine.RequestSpin();                       // zone 1 -> 2, banks a reward
            int zoneBefore = _run.CurrentZone;
            long bankedBefore = _run.Bank.TotalValue;

            _resolver.LandOn(BombSlot);
            _machine.RequestSpin();                       // bomb clears the bank

            Assert.That(_view.ContinueOffered, Is.True);
            int walletBefore = _wallet.Balance;

            _machine.RequestContinue();

            Assert.That(_machine.IsIn<IdleState>(), Is.True);
            Assert.That(_run.CurrentZone, Is.EqualTo(zoneBefore), "Continue must resume the same zone.");
            Assert.That(_wallet.Balance, Is.LessThan(walletBefore), "The continue must have been paid for.");
            Assert.That(_run.ContinuesUsedThisRun, Is.EqualTo(1));
            Assert.That(bankedBefore, Is.GreaterThan(0));
            Assert.That(_run.Bank.TotalValue, Is.EqualTo(bankedBefore), "the continue restores the lost haul");
        }

        [Test]
        public void GoldRevive_CanBeUsedMoreThanOncePerRun_AtADoublingPrice()
        {
            _wallet.Add(100_000);
            AdvanceToZone(2);

            _resolver.LandOn(BombSlot);
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

            _resolver.LandOn(BombSlot);
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
                new RunModel(new ZoneClassifier(), _wallet, TestWheels.Gold),
                new ZoneWheelFactory(new ZoneClassifier(), new StubBlueprintProvider(BombSlot), new LinearRewardScaling()),
                new SpinService(new FixedSliceResolver(RewardSlot)),
                new ContinueService(_wallet, ContinueSettings.Default),
                blocking);

            GameStateMachine machine = GameFlow.Build(context);
            GameFlow.Start(machine);

            machine.RequestSpin();
            Assert.That(machine.IsIn<SpinningState>(), Is.True);

            machine.RequestSpin();
            machine.RequestLeave();
            machine.RequestGiveUp();

            Assert.That(machine.IsIn<SpinningState>(), Is.True, "No input may be honoured mid-spin.");
            Assert.That(blocking.SpinCalls, Is.EqualTo(1), "A second spin must not have been started.");
        }

        private void AdvanceToZone(int target)
        {
            _resolver.LandOn(RewardSlot);
            while (_run.CurrentZone < target) _machine.RequestSpin();
        }

        /// <summary>Never completes its spin, holding the machine in SpinningState.</summary>
        private sealed class BlockingPresentation : InstantPresentation
        {
            public int SpinCalls { get; private set; }

            public override void PlaySpin(int slotIndex, System.Action onComplete) => SpinCalls++;
        }
    }
}
