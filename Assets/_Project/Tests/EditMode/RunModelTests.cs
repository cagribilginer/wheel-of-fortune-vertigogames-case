using System;
using NUnit.Framework;
using Vertigo.Wheel.Core.Rewards;
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
        private Wallet _wallet;
        private RunModel _run;

        [SetUp]
        public void SetUp()
        {
            _save = new InMemorySaveService();
            _wallet = new Wallet(_save);
            _run = new RunModel(new ZoneClassifier(), _wallet, TestWheels.Gold, TestWheels.Cash);
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
        public void WalletAddPastIntMax_SaturatesInsteadOfWrapping()
        {
            _wallet.Add(TestWheels.Gold, int.MaxValue);
            _wallet.Add(TestWheels.Gold, 1);

            Assert.That(_wallet.BalanceOf(TestWheels.Gold), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Grant_WithABombOutcome_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _run.Grant(new SpinOutcome(0, SliceKind.Bomb, Vertigo.Wheel.Core.Rewards.RewardId.None, 0)));
        }

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

            _run.Detonate();

            Assert.That(_run.Bank.IsEmpty, Is.True);
            Assert.That(_run.Phase, Is.EqualTo(RunPhase.GameOver));
        }

        /// <summary>
        /// A bomb must never touch the wallet — otherwise it could lock the player out of the very
        /// continue that is meant to answer it.
        /// </summary>
        [Test]
        public void Detonate_LeavesTheWalletIntact()
        {
            _wallet.Add(TestWheels.Gold, 300);
            _wallet.Add(TestWheels.Cash, 75);

            _run.Detonate();

            Assert.That(_wallet.BalanceOf(TestWheels.Gold), Is.EqualTo(300));
            Assert.That(_wallet.BalanceOf(TestWheels.Cash), Is.EqualTo(75));
        }

        [Test]
        public void CashOut_ConvertsBankedGoldIntoTheWallet()
        {
            _run.Grant(new SpinOutcome(0, SliceKind.Reward, TestWheels.Gold, 180));
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 5));

            _run.CashOut();

            Assert.That(_wallet.BalanceOf(TestWheels.Gold), Is.EqualTo(180));
            Assert.That(_run.Phase, Is.EqualTo(RunPhase.CashOut));
        }

        [Test]
        public void CashOut_WithNoBankedGold_LeavesTheWalletAlone()
        {
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 5));
            _run.CashOut();

            Assert.That(_wallet.BalanceOf(TestWheels.Gold), Is.Zero);
        }

        [Test]
        public void CashOut_CreditsEveryCurrencyTheRunWasGiven()
        {
            var gems = new RewardId("gems");
            var run = new RunModel(new ZoneClassifier(), _wallet, TestWheels.Gold, TestWheels.Cash, new[] { gems });

            run.Grant(new SpinOutcome(1, SliceKind.Reward, gems, 7));
            run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 999));
            run.CashOut();

            Assert.That(_wallet.BalanceOf(gems), Is.EqualTo(7), "A third currency must not vanish at cash-out.");
            Assert.That(_wallet.BalanceOf(TestWheels.Pistol), Is.Zero);
        }

        [Test]
        public void CashOut_OnlyBanksGoldAndCash()
        {
            // Pistol is neither of the two wallet currencies, so it must survive the run's end without
            // ever touching the wallet — the rest of the haul (weapons, cosmetics, chests) stays behind.
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 999));
            _run.CashOut();

            Assert.That(_wallet.BalanceOf(TestWheels.Pistol), Is.Zero,
                "Pistol Points are not a wallet currency, so cashing out must not bank them.");
        }

        /// <summary>
        /// Regression for a real report: gold climbed after "Claim &amp; Leave" but cash stayed flat. The two
        /// currencies share every step of this path (<see cref="RunModel.CashOut"/>, <see cref="Wallet"/>),
        /// so a bank holding both must credit both — nothing here is allowed to special-case gold over cash.
        /// </summary>
        [Test]
        public void CashOut_CreditsBothWalletCurrenciesFromTheSameBank()
        {
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Gold, 40));
            _run.Grant(new SpinOutcome(2, SliceKind.Reward, TestWheels.Cash, 50));

            _run.CashOut();

            Assert.That(_run.Balances.Gold, Is.EqualTo(40), "Gold should be credited.");
            Assert.That(_run.Balances.Cash, Is.EqualTo(50), "Cash should be credited too.");
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
            _wallet.Add(TestWheels.Gold, 90);
            _run.ResetRun();

            Assert.That(_wallet.BalanceOf(TestWheels.Gold), Is.EqualTo(90));
        }

        [Test]
        public void ApplyGoldRevive_ResumesTheSameZoneAndKeepsTheHaul()
        {
            _run.AdvanceZone();                                   // now on zone 2
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 10));
            _run.Detonate();                                      // bomb clears the bank, snapshots the haul

            _run.ApplyGoldRevive();

            Assert.That(_run.CurrentZone, Is.EqualTo(2));
            Assert.That(_run.Bank.AmountOf(TestWheels.Pistol), Is.EqualTo(10), "the snapshotted haul is restored");
            Assert.That(_run.Phase, Is.EqualTo(RunPhase.Idle));
            Assert.That(_run.GoldRevivesUsedThisRun, Is.EqualTo(1));
            Assert.That(_run.AdRevivesUsedThisRun, Is.Zero);
        }

        [Test]
        public void GoldRevive_CanBeAppliedRepeatedly_EachBumpsTheCount()
        {
            _run.Detonate();
            _run.ApplyGoldRevive();
            _run.Detonate();
            _run.ApplyGoldRevive();

            Assert.That(_run.GoldRevivesUsedThisRun, Is.EqualTo(2));
        }

        [Test]
        public void ApplyAdRevive_BumpsTheAdCountAndRestoresTheHaul()
        {
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 5));
            _run.Detonate();

            _run.ApplyAdRevive();

            Assert.That(_run.Bank.AmountOf(TestWheels.Pistol), Is.EqualTo(5));
            Assert.That(_run.AdRevivesUsedThisRun, Is.EqualTo(1));
            Assert.That(_run.GoldRevivesUsedThisRun, Is.Zero);
        }

        /// <summary>
        /// The regression this guards: "gold"/"cash" shown anywhere in the UI must always mean the same
        /// wallet balance. Before this, one screen computed its own haul-weighted score under the "cash"
        /// label while another showed the actual currency amount — same name, two different numbers.
        /// </summary>
        [Test]
        public void Balances_PairsEachCurrencyWithItsOwnBalance()
        {
            _wallet.Add(TestWheels.Gold, 100);
            _wallet.Add(TestWheels.Cash, 25);

            Assert.That(_run.Balances.Gold, Is.EqualTo(100));
            Assert.That(_run.Balances.Cash, Is.EqualTo(25));
        }

        [Test]
        public void CanLeave_TracksWhetherTheBankHasAnything()
        {
            _run.JumpToZone(5);
            Assert.That(_run.CanLeave, Is.False, "empty bank");

            _run.Grant(new SpinOutcome(5, SliceKind.Reward, TestWheels.Pistol, 10));

            Assert.That(_run.CanLeave, Is.True, "bank has a reward");
        }

        [Test]
        public void CanLeave_IsFalseWhileSpinningEvenWithAHaul()
        {
            _run.Grant(new SpinOutcome(1, SliceKind.Reward, TestWheels.Pistol, 10));
            _run.Phase = RunPhase.Spinning;

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
            Assert.Throws<ArgumentNullException>(
                () => new RunModel(null, _wallet, TestWheels.Gold, TestWheels.Cash));
            Assert.Throws<ArgumentNullException>(
                () => new RunModel(new ZoneClassifier(), null, TestWheels.Gold, TestWheels.Cash));
        }
    }
}
