using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Tests.EditMode
{
    /// <summary>
    /// The "unique drops don't stack or scale" rule: consumables and currencies are the only categories
    /// whose amounts grow with zone depth; everything else is a single item.
    /// </summary>
    [TestFixture]
    public sealed class RewardStackingRulesTests
    {
        private RewardDefinition _reward;

        [TearDown]
        public void TearDown()
        {
            if (_reward != null) Object.DestroyImmediate(_reward);
        }

        [TestCase(RewardCategory.Consumable, true)]
        [TestCase(RewardCategory.Currency, true)]
        [TestCase(RewardCategory.Weapon, false)]
        [TestCase(RewardCategory.Cosmetic, false)]
        [TestCase(RewardCategory.Points, false)]
        [TestCase(RewardCategory.Chest, false)]
        public void IsStackable_FollowsCategory(RewardCategory category, bool expected)
        {
            Assert.That(Make(category, baseAmount: 5).IsStackable, Is.EqualTo(expected));
        }

        [Test]
        public void OnValidate_ForcesANonStackableBaseAmountBackToOne()
        {
            RewardDefinition weapon = Make(RewardCategory.Weapon, baseAmount: 12);
            Invoke(weapon, "OnValidate");

            Assert.That(weapon.DefaultBaseAmount, Is.EqualTo(1));
        }

        [Test]
        public void OnValidate_LeavesAStackableBaseAmountAlone()
        {
            RewardDefinition cash = Make(RewardCategory.Currency, baseAmount: 50);
            Invoke(cash, "OnValidate");

            Assert.That(cash.DefaultBaseAmount, Is.EqualTo(50));
        }

        [Test]
        public void WheelSliceEntry_ClampsANonStackableRewardToOne_EvenWithAnOverride()
        {
            WheelSliceEntry entry = MakeEntry(Make(RewardCategory.Weapon, baseAmount: 8), baseAmountOverride: 40);

            Assert.That(entry.ResolveBaseAmount(), Is.EqualTo(1));

            SliceBlueprint blueprint = entry.ToBlueprint();
            Assert.That(blueprint.Scalable, Is.False);
            Assert.That(blueprint.BaseAmount, Is.EqualTo(1));
        }

        [Test]
        public void WheelSliceEntry_KeepsAStackableRewardScalable()
        {
            WheelSliceEntry entry = MakeEntry(Make(RewardCategory.Consumable, baseAmount: 3), baseAmountOverride: 0);

            Assert.That(entry.ResolveBaseAmount(), Is.EqualTo(3));
            Assert.That(entry.ToBlueprint().Scalable, Is.True);
        }

        private RewardDefinition Make(RewardCategory category, int baseAmount)
        {
            _reward = ScriptableObject.CreateInstance<RewardDefinition>();
            _reward.name = "Reward_Test";

            var so = new SerializedObject(_reward);
            so.FindProperty("_category").enumValueIndex = (int)category;
            so.FindProperty("_defaultBaseAmount").intValue = baseAmount;
            so.ApplyModifiedPropertiesWithoutUndo();

            return _reward;
        }

        private static WheelSliceEntry MakeEntry(RewardDefinition reward, int baseAmountOverride)
        {
            var entry = new WheelSliceEntry();
            SetField(entry, "_kind", SliceKind.Reward);
            SetField(entry, "_reward", reward);
            SetField(entry, "_baseAmountOverride", baseAmountOverride);
            SetField(entry, "_weight", 1);
            return entry;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);

        private static void Invoke(object target, string name) =>
            target.GetType()
                .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
    }
}
