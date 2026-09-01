using System;
using NUnit.Framework;
using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Tests.EditMode
{
    [TestFixture]
    public sealed class ContinueServiceTests
    {
        private InMemorySaveService _save;
        private GoldWallet _wallet;
        private ContinueService _service;

        [SetUp]
        public void SetUp()
        {
            _save = new InMemorySaveService();
            _wallet = new GoldWallet(_save);
            _service = new ContinueService(_wallet, ContinueSettings.Default);
        }

        [TestCase(1, 0, 60)]
        [TestCase(17, 0, 220)]
        [TestCase(30, 0, 350)]
        [TestCase(17, 1, 440)]   // one gold revive already taken this run -> doubled
        [TestCase(17, 2, 880)]   // -> quadrupled
        public void Cost_RisesWithDepthAndDoublesPerGoldRevive(int zone, int goldRevivesUsed, int expected) =>
            Assert.That(_service.CostFor(zone, goldRevivesUsed), Is.EqualTo(expected));

        [Test]
        public void Cost_OnNonPositiveZone_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.CostFor(0, 0));

        [Test]
        public void UnaffordableGoldRevive_IsNotOffered()
        {
            _wallet.Add(10);
            Assert.That(_service.IsGoldReviveOffered(zoneReached: 17, goldRevivesUsedThisRun: 0), Is.False);
        }

        [Test]
        public void AffordableGoldRevive_IsOffered()
        {
            _wallet.Add(220);
            Assert.That(_service.IsGoldReviveOffered(zoneReached: 17, goldRevivesUsedThisRun: 0), Is.True);
        }

        [Test]
        public void GoldRevive_HasNoPerRunCap()
        {
            _wallet.Add(100_000);

            Assert.That(_service.IsGoldReviveOffered(17, goldRevivesUsedThisRun: 1), Is.True);
            Assert.That(_service.IsGoldReviveOffered(17, goldRevivesUsedThisRun: 4), Is.True);
        }

        [Test]
        public void GoldRevive_IsRefusedOnceTheDoubledPriceIsUnaffordable()
        {
            _wallet.Add(700);   // covers 220 and 440, not the 880 third revive

            Assert.That(_service.IsGoldReviveOffered(17, 0), Is.True);
            Assert.That(_service.IsGoldReviveOffered(17, 1), Is.True);
            Assert.That(_service.IsGoldReviveOffered(17, 2), Is.False);
        }

        [Test]
        public void AdRevive_IsOfferedOnceThenCapped()
        {
            Assert.That(_service.IsAdReviveOffered(adRevivesUsedThisRun: 0), Is.True);
            Assert.That(_service.IsAdReviveOffered(adRevivesUsedThisRun: 1), Is.False);
        }

        [Test]
        public void Purchase_DebitsExactlyTheCost_IncludingTheDoubledSecond()
        {
            _wallet.Add(1_000);

            Assert.That(_service.TryPurchase(17, 0), Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(1_000 - 220));

            Assert.That(_service.TryPurchase(17, 1), Is.True, "a second gold revive is allowed");
            Assert.That(_wallet.Balance, Is.EqualTo(1_000 - 220 - 440), "at the doubled price");
        }

        [Test]
        public void FailedPurchase_LeavesTheWalletUntouched()
        {
            _wallet.Add(10);

            Assert.That(_service.TryPurchase(17, 0), Is.False);
            Assert.That(_wallet.Balance, Is.EqualTo(10));
        }

        [Test]
        public void Wallet_PersistsThroughTheSaveService()
        {
            _wallet.Add(120);

            var reloaded = new GoldWallet(_save);
            Assert.That(reloaded.Balance, Is.EqualTo(120));
        }

        [Test]
        public void WalletReset_ZeroesTheBalance()
        {
            _wallet.Add(120);
            _wallet.Reset();

            Assert.That(_wallet.Balance, Is.Zero);
        }

        [Test]
        public void Wallet_RaisesChangedWithTheNewBalance()
        {
            int observed = -1;
            _wallet.Changed += balance => observed = balance;

            _wallet.Add(75);

            Assert.That(observed, Is.EqualTo(75));
        }

        [Test]
        public void Wallet_RejectsNegativeCredit() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.Add(-1));
    }
}
