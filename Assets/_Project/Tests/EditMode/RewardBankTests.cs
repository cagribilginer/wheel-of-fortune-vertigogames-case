using System;
using System.Collections.Generic;
using NUnit.Framework;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Tests.EditMode
{
    [TestFixture]
    public sealed class RewardBankTests
    {
        private static readonly RewardId Pistol = new RewardId("pistol_points");
        private static readonly RewardId Rifle = new RewardId("rifle_points");
        private static readonly RewardId Armor = new RewardId("armor_points");

        private RewardBank _bank;

        [SetUp]
        public void SetUp() => _bank = new RewardBank();

        [Test]
        public void NewBank_IsEmpty()
        {
            Assert.That(_bank.IsEmpty, Is.True);
            Assert.That(_bank.Entries, Is.Empty);
            Assert.That(_bank.TotalValue, Is.Zero);
        }

        [Test]
        public void SameRewardTwice_StacksIntoOneRow()
        {
            _bank.Add(Pistol, 12);
            _bank.Add(Pistol, 30);

            Assert.That(_bank.DistinctRewardCount, Is.EqualTo(1));
            Assert.That(_bank.AmountOf(Pistol), Is.EqualTo(42));
        }

        [Test]
        public void DifferentRewards_KeepFirstAcquisitionOrder()
        {
            _bank.Add(Rifle, 1);
            _bank.Add(Pistol, 1);
            _bank.Add(Armor, 1);
            _bank.Add(Rifle, 5); // topping up must not move Rifle to the end

            var order = new List<RewardId>();
            foreach (BankEntry entry in _bank.Entries) order.Add(entry.Reward);

            CollectionAssert.AreEqual(new[] { Rifle, Pistol, Armor }, order);
            Assert.That(_bank.AmountOf(Rifle), Is.EqualTo(6));
        }

        [Test]
        public void Clear_EmptiesBankAndRaisesChanged()
        {
            _bank.Add(Pistol, 5);

            int raised = 0;
            _bank.Changed += () => raised++;

            _bank.Clear();

            Assert.That(_bank.IsEmpty, Is.True);
            Assert.That(_bank.AmountOf(Pistol), Is.Zero);
            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void ClearOnEmptyBank_DoesNotRaiseChanged()
        {
            int raised = 0;
            _bank.Changed += () => raised++;

            _bank.Clear();

            Assert.That(raised, Is.Zero);
        }

        [Test]
        public void Add_RaisesChangedEachTime()
        {
            int raised = 0;
            _bank.Changed += () => raised++;

            _bank.Add(Pistol, 1);
            _bank.Add(Pistol, 1);
            _bank.Add(Rifle, 1);

            Assert.That(raised, Is.EqualTo(3));
        }

        [Test]
        public void Entries_CannotBeMutatedByCaller() =>
            Assert.That(_bank.Entries, Is.Not.InstanceOf<List<BankEntry>>(),
                "Entries must be a read-only view, not the live backing list.");

        [Test]
        public void TotalValue_SumsAmountTimesUnitValue()
        {
            _bank.Add(Pistol, 10, unitValue: 3);
            _bank.Add(Rifle, 2, unitValue: 50);

            Assert.That(_bank.TotalValue, Is.EqualTo(10 * 3 + 2 * 50));
        }

        [Test]
        public void ToppingUp_KeepsTheOriginalUnitValue()
        {
            _bank.Add(Pistol, 1, unitValue: 10);
            _bank.Add(Pistol, 1, unitValue: 999);

            Assert.That(_bank.TotalValue, Is.EqualTo(20));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void NonPositiveAmount_Throws(int amount) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _bank.Add(Pistol, amount));

        [Test]
        public void EmptyRewardId_Throws() =>
            Assert.Throws<ArgumentException>(() => _bank.Add(RewardId.None, 1));

        [Test]
        public void NegativeUnitValue_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _bank.Add(Pistol, 1, unitValue: -1));
    }
}
