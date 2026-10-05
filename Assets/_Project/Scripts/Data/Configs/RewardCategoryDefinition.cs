using UnityEngine;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// What a kind of reward is allowed to do, authored as an asset: whether it stacks, how many of it one drop
    /// may carry, and whether it is a wallet currency. A new category is a new asset (right-click in the Project
    /// window), not a code change, and a <see cref="RewardDefinition"/> simply points at the one it belongs to.
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Rewards/Reward Category", fileName = "Category_")]
    public sealed class RewardCategoryDefinition : ScriptableObject
    {
        [Tooltip("Whether more than one of this reward can be granted at once. Unique drops (weapons, chests) are always one.")]
        [SerializeField] private bool _stackable = true;

        [Tooltip("Hard ceiling on a single drop's count after zone scaling; 0 means no ceiling.")]
        [Min(0)]
        [SerializeField] private int _maxAmountPerDrop;

        [Tooltip("Rewards of this category are wallet currencies: cash-out banks them into the persistent wallet.")]
        [SerializeField] private bool _isWalletCurrency;

        public bool Stackable
        {
            get { return _stackable; }
        }
        public int MaxAmountPerDrop
        {
            get { return _maxAmountPerDrop; }
        }
        public bool IsWalletCurrency
        {
            get { return _isWalletCurrency; }
        }
    }
}
