using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Tests.EditMode
{
    /// <summary>
    /// The "unique drops don't stack or scale" rule: a category asset decides whether its rewards stack, how many
    /// one drop may carry and whether they are wallet currencies. Consumables and currencies are the only kinds
    /// whose amounts grow with zone depth; everything else is a single item.
    /// </summary>
    [TestFixture]
    public sealed class RewardStackingRulesTests
    {
        private const string CATEGORY_FOLDER = "Assets/_Project/Configs/Categories/";
        private const int SHARD_CEILING = 5;

        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
                if (created != null) Object.DestroyImmediate(created);
            _created.Clear();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void IsStackable_FollowsTheCategory(bool stackable)
        {
            Assert.That(Make(Category(stackable, 0), baseAmount: 5).IsStackable, Is.EqualTo(stackable));
        }

        [Test]
        public void OnValidate_ForcesANonStackableBaseAmountBackToOne()
        {
            RewardDefinition weapon = Make(Category(stackable: false), baseAmount: 12);
            Invoke(weapon, "OnValidate");

            Assert.That(weapon.DefaultBaseAmount, Is.EqualTo(1));
        }

        [Test]
        public void OnValidate_LeavesAStackableBaseAmountAlone()
        {
            RewardDefinition cash = Make(Category(stackable: true, isWalletCurrency: true), baseAmount: 50);
            Invoke(cash, "OnValidate");

            Assert.That(cash.DefaultBaseAmount, Is.EqualTo(50));
        }

        [Test]
        public void OnValidate_ClampsABaseAmountToTheCategoryCeiling()
        {
            RewardDefinition shard = Make(Category(stackable: true, maxAmountPerDrop: SHARD_CEILING), baseAmount: 10);
            Invoke(shard, "OnValidate");

            Assert.That(shard.DefaultBaseAmount, Is.EqualTo(SHARD_CEILING));
        }

        [Test]
        public void WheelSliceEntry_ClampsAnOverrideToTheCategoryCeiling_SoTheBlueprintBuilds()
        {
            WheelSliceEntry entry = MakeEntry(Make(Category(true, SHARD_CEILING), baseAmount: 1), baseAmountOverride: 12);

            Assert.That(entry.ResolveBaseAmount(), Is.EqualTo(SHARD_CEILING));
            Assert.That(entry.ToBlueprint().BaseAmount, Is.EqualTo(SHARD_CEILING));
        }

        [Test]
        public void WheelSliceEntry_ClampsANonStackableRewardToOne_EvenWithAnOverride()
        {
            WheelSliceEntry entry = MakeEntry(Make(Category(stackable: false), baseAmount: 8), baseAmountOverride: 40);

            Assert.That(entry.ResolveBaseAmount(), Is.EqualTo(1));

            SliceBlueprint blueprint = entry.ToBlueprint();
            Assert.That(blueprint.IsScalable, Is.False);
            Assert.That(blueprint.BaseAmount, Is.EqualTo(1));
        }

        [Test]
        public void WheelSliceEntry_KeepsAStackableRewardScalable()
        {
            WheelSliceEntry entry = MakeEntry(Make(Category(stackable: true), baseAmount: 3), baseAmountOverride: 0);

            Assert.That(entry.ResolveBaseAmount(), Is.EqualTo(3));
            Assert.That(entry.ToBlueprint().IsScalable, Is.True);
        }

        [Test]
        public void WheelSliceEntry_CarriesTheCategoryCeilingIntoTheBlueprint()
        {
            WheelSliceEntry entry = MakeEntry(Make(Category(true, SHARD_CEILING), baseAmount: 1), baseAmountOverride: 0);

            SliceBlueprint blueprint = entry.ToBlueprint();
            Assert.That(blueprint.IsScalable, Is.True);
            Assert.That(blueprint.MaxAmount, Is.EqualTo(SHARD_CEILING));
            Assert.That(blueprint.ToSlice(99, new LinearRewardScaling()).Amount, Is.EqualTo(SHARD_CEILING));
        }

        [Test]
        public void ARewardWithoutACategory_FailsByName()
        {
            var reward = ScriptableObject.CreateInstance<RewardDefinition>();
            reward.name = "Reward_NoCategory";
            _created.Add(reward);

            var error = Assert.Throws<System.InvalidOperationException>(() => { var _ = reward.IsStackable; });
            StringAssert.Contains("Reward_NoCategory", error.Message);
        }

        /// <summary>The shipped category assets carry the design brief's rules, so a retune there is a deliberate edit.</summary>
        [TestCase("Points", true, SHARD_CEILING, false)]
        [TestCase("Weapon", false, 0, false)]
        [TestCase("Consumable", true, 0, false)]
        [TestCase("Cosmetic", false, 0, false)]
        [TestCase("Currency", true, 0, true)]
        [TestCase("Chest", false, 0, false)]
        public void TheShippedCategoryAssets_CarryTheBriefsRules(string name, bool stackable, int ceiling, bool currency)
        {
            var category = AssetDatabase.LoadAssetAtPath<RewardCategoryDefinition>($"{CATEGORY_FOLDER}Category_{name}.asset");

            Assert.That(category, Is.Not.Null, $"Category_{name}.asset is missing.");
            Assert.That(category.IsStackable, Is.EqualTo(stackable));
            Assert.That(category.MaxAmountPerDrop, Is.EqualTo(ceiling));
            Assert.That(category.IsWalletCurrency, Is.EqualTo(currency));
        }

        private RewardCategoryDefinition Category(bool stackable, int maxAmountPerDrop = 0, bool isWalletCurrency = false)
        {
            var category = ScriptableObject.CreateInstance<RewardCategoryDefinition>();
            category.name = "Category_Test";
            _created.Add(category);

            var so = new SerializedObject(category);
            so.FindProperty("_isStackable").boolValue = stackable;
            so.FindProperty("_maxAmountPerDrop").intValue = maxAmountPerDrop;
            so.FindProperty("_isWalletCurrency").boolValue = isWalletCurrency;
            so.ApplyModifiedPropertiesWithoutUndo();

            return category;
        }

        private RewardDefinition Make(RewardCategoryDefinition category, int baseAmount)
        {
            var reward = ScriptableObject.CreateInstance<RewardDefinition>();
            reward.name = "Reward_Test";
            _created.Add(reward);

            var so = new SerializedObject(reward);
            so.FindProperty("_category").objectReferenceValue = category;
            so.FindProperty("_defaultBaseAmount").intValue = baseAmount;
            so.ApplyModifiedPropertiesWithoutUndo();

            return reward;
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

        private static void SetField(object target, string name, object value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void Invoke(object target, string name)
        {
            target.GetType()
                .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
        }
    }
}
