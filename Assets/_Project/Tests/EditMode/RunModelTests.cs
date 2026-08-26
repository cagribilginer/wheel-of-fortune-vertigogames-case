using System;
using NUnit.Framework;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;
using Vertigo.Wheel.Tests.EditMode.Doubles;

namespace Vertigo.Wheel.Tests.EditMode
{
    [TestFixture]
    public sealed class RunModelTests
    {
        private InMemorySaveService _save;
        private GoldWallet _wallet;
        private RunModel _run;

        [SetUp]
        public void SetUp()
        {
            _save = new InMemorySaveService();
            _wallet = new GoldWallet(_save);
            _run = new RunModel(new ZoneClassifier(), _wallet, TestWheels.Gold);
        }

        [Test]
        public void NewRun_StartsOnZoneOneWithAnEmptyBank()
        {
            Assert.That(_run.CurrentZone, Is.EqualTo(1));
            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_run.Phase, Is.EqualTo(RunPhase.Idle));
        }

        [Test]
        public void Grant_BanksTheReward()
        {
            _run.Grant(new SpinOutcome(3, SliceKind.Reward, TestWheels.Pistol, 25));

            Assert.That(_run.Bank.AmountOf(TestWheels.Pistol), Is.EqualTo(25));
        }

        [Test]
        public void Grant_WithABombOutcome_Throws() =>
            Assert.Throws<InvalidOperationException>(() =>
                _run.Grant(new SpinOutcome(0, SliceKind.Bomb, Vertigo.Wheel.Core.Rewards.RewardId.None, 0)));

        [Test]
        public void AdvanceZone_IncrementsAndNotifies()
        {
            int observed = 0;
            _run.ZoneChanged += zone => observed = zone;

            _run.AdvanceZone();

            Assert.That(_run.CurrentZone, Is.EqualTo(2));
            Assert.That(observed, Is.EqualTo(2));
        }

        [Test]
        public void Detonate_ClearsTheBankAndEndsTheRun()
        {
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 40));

            RunEndReason? reason = null;
            _run.RunEnded += r => reason = r;

            _run.Detonate();

            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_run.Phase, Is.EqualTo(RunPhase.GameOver));
            Assert.That(reason, Is.EqualTo(RunEndReason.Bomb));
        }

        /// <summary>
        /// A bomb must never touch the wallet — otherwise it could lock the player out of the very
        /// continue that is meant to answer it.
        /// </summary>
        [Test]
        public void Detonate_LeavesTheGoldWalletIntact()
        {
            _wallet.Add(300);
            _run.Detonate();

            Assert.That(_wallet.Balance, Is.EqualTo(300));
        }

        [Test]
        public void CashOut_ConvertsBankedGoldIntoTheWallet()
        {
            _run.Grant(new SpinOutcome(0, SliceKind.Reward, TestWheels.Gold, 180));
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 5));

            _run.CashOut();

            Assert.That(_wallet.Balance, Is.EqualTo(180));
            Assert.That(_run.Phase, Is.EqualTo(RunPhase.CashOut));
        }

        [Test]
        public void CashOut_WithNoBankedGold_LeavesTheWalletAlone()
        {
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 5));
            _run.CashOut();

            Assert.That(_wallet.Balance, Is.Zero);
        }

        [Test]
        public void GiveUp_ForfeitsTheHaul()
        {
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 60));

            RunEndReason? reason = null;
            _run.RunEnded += r => reason = r;

            _run.GiveUp();

            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(reason, Is.EqualTo(RunEndReason.GaveUp));
        }

        [Test]
        public void ResetRun_ReturnsToZoneOneWithAnEmptyBank()
        {
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 10));
            _run.AdvanceZone();
            _run.AdvanceZone();
            _run.Detonate();

            _run.ResetRun();

            Assert.That(_run.CurrentZone, Is.EqualTo(1));
            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_run.Phase, Is.EqualTo(RunPhase.Idle));
            Assert.That(_run.ContinuesUsedThisRun, Is.Zero);
        }

        [Test]
        public void ResetRun_PreservesTheWallet()
        {
            _wallet.Add(90);
            _run.ResetRun();

            Assert.That(_wallet.Balance, Is.EqualTo(90));
        }

        [Test]
        public void ApplyContinue_ResumesTheSameZoneAndKeepsTheHaul()
        {
            _run.AdvanceZone();                                   // now on zone 2
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 10));
            _run.Phase = RunPhase.GameOver;

            _run.ApplyContinue();

            Assert.That(_run.CurrentZone, Is.EqualTo(2));
            Assert.That(_run.Bank.AmountOf(TestWheels.Pistol), Is.EqualTo(10));
            Assert.That(_run.Phase, Is.EqualTo(RunPhase.Idle));
            Assert.That(_run.ContinuesUsedThisRun, Is.EqualTo(1));
        }

        [TestCase(1, false)]
        [TestCase(5, true)]
        [TestCase(30, true)]
        public void CanLeave_TracksTheCurrentZoneType(int zone, bool expected)
        {
            for (int i = 1; i < zone; i++) _run.AdvanceZone();

            Assert.That(_run.CurrentZone, Is.EqualTo(zone));
            Assert.That(_run.CanLeave, Is.EqualTo(expected));
        }

        [Test]
        public void CanLeave_IsFalseWhileSpinningEvenOnASafeZone()
        {
            for (int i = 1; i < 5; i++) _run.AdvanceZone();
            _run.Phase = RunPhase.Spinning;

            Assert.That(_run.CurrentZoneType, Is.EqualTo(ZoneType.Safe));
            Assert.That(_run.CanLeave, Is.False);
        }

        [Test]
        public void PhaseChanged_FiresOnlyOnActualChanges()
        {
            int raised = 0;
            _run.PhaseChanged += _ => raised++;

            _run.Phase = RunPhase.Spinning;
            _run.Phase = RunPhase.Spinning;
            _run.Phase = RunPhase.Idle;

            Assert.That(raised, Is.EqualTo(2));
        }

        [Test]
        public void NullDependencies_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new RunModel(null, _wallet, TestWheels.Gold));
            Assert.Throws<ArgumentNullException>(() => new RunModel(new ZoneClassifier(), null, TestWheels.Gold));
        }
    }
}
