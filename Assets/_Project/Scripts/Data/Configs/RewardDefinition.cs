using UnityEngine;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// One authored reward: its stable id, its sprite, and what it is worth.
    /// <para>
    /// The core layer only ever sees <see cref="RewardId"/>; this asset is the single place a sprite is
    /// attached to one. Adding a reward is a right-click in the Project window and no code at all.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Rewards/Reward Definition", fileName = "Reward_")]
    public sealed class RewardDefinition : ScriptableObject
    {
        [Tooltip("Stable key used by the logic layer. Defaults to the asset filename.")]
        [SerializeField] private string _id;

        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private RewardCategory _category = RewardCategory.Points;

        [Tooltip("Amount granted at zone 1, before zone scaling.")]
        [Min(1)]
        [SerializeField] private int _defaultBaseAmount = 10;

        [Tooltip("Relative worth per unit. Only sizes the cash-out chest; never affects odds.")]
        [Min(0)]
        [SerializeField] private int _estimatedValue = 1;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public RewardId RewardId => new RewardId(Id);
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Sprite Icon => _icon;
        public RewardCategory Category => _category;
        public int DefaultBaseAmount => _defaultBaseAmount;
        public int EstimatedValue => _estimatedValue;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id)) _id = name;

            if (_icon == null)
                Debug.LogWarning($"[Vertigo] Reward '{name}' has no icon assigned.", this);
        }
#endif
    }
}
