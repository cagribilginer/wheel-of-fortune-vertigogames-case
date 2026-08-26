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

        [TestCase(1, 60)]
        [TestCase(17, 220)]
        [TestCase(30, 350)]
        public void Cost_RisesWithDepth(int zone, int expected) =>
            Assert.That(_service.CostFor(zone), Is.EqualTo(expected));

        [Test]
        public void Cost_OnNonPositiveZone_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.CostFor(0));

        [Test]
        public void UnaffordableContinue_IsNotOffered()
        {
            _wallet.Add(10);
            Assert.That(_service.IsOffered(zoneReached: 17, continuesUsedThisRun: 0), Is.False);
        }

        [Test]
        public void AffordableContinue_IsOffered()
        {
            _wallet.Add(220);
            Assert.That(_service.IsOffered(zoneReached: 17, continuesUsedThisRun: 0), Is.True);
        }

        [Test]
        public void SecondContinueInOneRun_IsRefused()
        {
            _wallet.Add(10_000);
            Assert.That(_service.IsOffered(17, continuesUsedThisRun: 1), Is.False);
        }

        [Test]
        public void Purchase_DebitsExactlyTheCost()
        {
            _wallet.Add(500);

            Assert.That(_service.TryPurchase(17, 0), Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(500 - 220));
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
