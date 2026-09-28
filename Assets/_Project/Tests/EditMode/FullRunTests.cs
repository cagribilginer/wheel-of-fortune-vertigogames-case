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
    /// End-to-end runs through the real flow. These are the highest-value tests in the suite: a regression
    /// anywhere in classification, blueprint selection, scaling, resolution, banking or transitions shows
    /// up here even when every unit test still passes.
    /// </summary>
    [TestFixture]
    public sealed class FullRunTests
    {
        private InMemorySaveService _save;
        private Wallet _wallet;
        private RunModel _run;
        private StubBlueprintProvider _blueprints;
        private InstantPresentation _view;
        private GameStateMachine _machine;

        private void Build(IRandomProvider random, int bombWeight = 1, InstantPresentation view = null)
        {
            _save = new InMemorySaveService();
            _wallet = new Wallet(_save);
            _run = new RunModel(new ZoneClassifier(), _wallet, TestWheels.Gold, TestWheels.Cash);

            _blueprints = new StubBlueprintProvider(bombIndex: 0) { BombWeight = bombWeight };
            var factory = new ZoneWheelFactory(new ZoneClassifier(), _blueprints, new LinearRewardScaling());

            _view = view ?? new InstantPresentation();

            var context = new GameContext(
                _run, factory, new SpinService(new WeightedSliceResolver(random)),
                new ContinueService(_wallet, TestWheels.Gold, ContinueSettings.Default), _view);

            _machine = GameFlow.Build(context);
            GameFlow.Start(_machine);
        }

        /// <summary>
        /// A fixed seed must always produce the same run. If this drifts, something in the chain changed
        /// its arithmetic or its ordering.
        /// </summary>
        [Test]
        public void SeededRun_IsReproducible()
        {
            int FirstRun()
            {
                Build(new SystemRandomProvider(12345));
                while (!_machine.IsIn<GameOverState>()) _machine.RequestSpin();
                return _run.CurrentZone;
            }

            int a = FirstRun();
            int b = FirstRun();

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void SeededRun_EndsOnABombWithAnEmptyBank()
        {
            Build(new SystemRandomProvider(12345));

            while (!_machine.IsIn<GameOverState>()) _machine.RequestSpin();

            Assert.That(_view.BombsPlayed, Is.EqualTo(1));
            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_view.GameOverVisible, Is.True);
        }

        /// <summary>
        /// Sixty zones with the bomb disabled: exercises both safe intervals, both super zones, band
        /// recycling past thirty, and deep-zone scaling in one pass.
        /// </summary>
        [Test]
        public void SixtyZoneRun_ClearsBothSuperZonesWithoutOverflow()
        {
            Build(new SystemRandomProvider(7), bombWeight: 0);

            var superZonesSeen = new System.Collections.Generic.List<int>();
            var safeZonesSeen = 0;

            while (_run.CurrentZone < 60)
            {
                if (_run.CurrentZoneType == ZoneType.Super) superZonesSeen.Add(_run.CurrentZone);
                else if (_run.CurrentZoneType == ZoneType.Safe) safeZonesSeen++;

                Assert.That(_view.LastWheel.SliceCount, Is.EqualTo(8), $"zone {_run.CurrentZone}");
                _machine.RequestSpin();
            }

            CollectionAssert.AreEqual(new[] { 30 }, superZonesSeen);
            Assert.That(safeZonesSeen, Is.EqualTo(11),
                "Zone 1, then 5..55 step 5, excluding 30 which is super.");
            Assert.That(_run.CurrentZone, Is.EqualTo(60));
            Assert.That(_view.BombsPlayed, Is.Zero);
        }

        [Test]
        public void DeepRun_DoesNotOverflowRewardAmounts()
        {
            Build(new SystemRandomProvider(3), bombWeight: 0);

            while (_run.CurrentZone < 200) _machine.RequestSpin();

            for (int i = 0; i < _view.LastWheel.SliceCount; i++)
                Assert.That(_view.LastWheel[i].Amount, Is.GreaterThanOrEqualTo(0),
                    "A negative amount means the scaling cast wrapped.");

            Assert.That(_run.Bank.DistinctRewardCount, Is.GreaterThan(0));
        }

        /// <summary>
        /// The full survival loop: reach a safe zone, walk away, and have the gold land in the wallet
        /// where a future continue can spend it.
        /// </summary>
        [Test]
        public void CashingOutOnASuperZone_FundsAFutureContinue()
        {
            Build(new SystemRandomProvider(11), bombWeight: 0);

            while (_run.CurrentZone < 30) _machine.RequestSpin();

            Assert.That(_run.CurrentZoneType, Is.EqualTo(ZoneType.Super));
            _machine.RequestSpin();                        // bank the golden reward
            Assert.That(_run.CurrentZone, Is.EqualTo(31));

            // Zone 35 is the next safe zone where leaving is legal.
            while (_run.CurrentZoneType != ZoneType.Safe) _machine.RequestSpin();

            long bankedGold = _run.Bank.AmountOf(TestWheels.Gold);
            Assert.That(bankedGold, Is.GreaterThan(0), "The super zone should have paid gold.");

            _machine.RequestLeave();
            Assert.That(_view.CashOutVisible, Is.True);

            _machine.Confirm();

            Assert.That(_wallet.BalanceOf(TestWheels.Gold), Is.EqualTo(bankedGold));
            Assert.That(_run.CurrentZone, Is.EqualTo(1));
            Assert.That(_run.Bank.IsEmpty, Is.True);
        }

        [Test]
        public void SecondConfirmDuringTheClaim_CreditsTheWalletOnce()
        {
            var view = new DeferredClaimPresentation();
            OpenCashOutWithGold(view, gold: 100);

            _machine.Confirm();
            _machine.Confirm();
            view.FinishClaim();

            Assert.That(_wallet.BalanceOf(TestWheels.Gold), Is.EqualTo(100));
            Assert.That(view.ClaimsStarted, Is.EqualTo(1));
            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_machine.IsIn<IdleState>(), Is.True);
        }

        [Test]
        public void CancelDuringTheClaim_IsIgnored()
        {
            var view = new DeferredClaimPresentation();
            OpenCashOutWithGold(view, gold: 100);

            _machine.Confirm();
            _machine.Cancel();

            Assert.That(_machine.IsIn<CashOutState>(), Is.True, "A claim that already paid out can't be cancelled.");

            view.FinishClaim();

            Assert.That(_wallet.BalanceOf(TestWheels.Gold), Is.EqualTo(100));
            Assert.That(_run.CurrentZone, Is.EqualTo(1));
            Assert.That(_run.Bank.IsEmpty, Is.True);
        }

        // Zone 1 is safe, so banking gold there makes leaving legal straight away.
        private void OpenCashOutWithGold(InstantPresentation view, int gold)
        {
            Build(new SystemRandomProvider(1), bombWeight: 0, view: view);
            _run.Bank.Add(TestWheels.Gold, gold);

            _machine.RequestLeave();
            Assert.That(_machine.IsIn<CashOutState>(), Is.True);
        }

        /// <summary>Holds the claim celebration open until the test finishes it, like the real 0.8s tween.</summary>
        private sealed class DeferredClaimPresentation : InstantPresentation
        {
            private System.Action _pendingClaim;

            public int ClaimsStarted { get; private set; }

            public override void ClaimCashOut(int playerGold, int playerCash, System.Action onComplete)
            {
                ClaimsStarted++;
                _pendingClaim = onComplete;
            }

            public void FinishClaim()
            {
                System.Action finish = _pendingClaim;
                _pendingClaim = null;
                finish?.Invoke();
            }
        }

        [Test]
        public void BombNeverFiresOnASafeOrSuperZone()
        {
            Build(new SystemRandomProvider(999));

            for (int i = 0; i < 400 && !_machine.IsIn<GameOverState>(); i++)
            {
                ZoneType type = _run.CurrentZoneType;
                int before = _view.BombsPlayed;

                _machine.RequestSpin();

                if (type != ZoneType.Normal)
                    Assert.That(_view.BombsPlayed, Is.EqualTo(before),
                        $"A bomb fired on a {type} zone, which must be risk-free.");
            }
        }
    }
}
