using UnityEngine;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// Every tween duration, distance, and punch strength the presenters use for cosmetic feedback, so
    /// there are no magic floats scattered across them — the same reasoning <see cref="WheelSpinConfig"/>
    /// applies to the spin itself, extended to the rest of the feel.
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Juice", fileName = "Juice_")]
    public sealed class JuiceConfig : ScriptableObject
    {
        [Header("Wheel — idle & zone transition")]
        [Range(0f, 0.5f)] [SerializeField] private float _tickPunchDuration = 0.09f;
        [Range(1f, 1.2f)] [SerializeField] private float _breatheScale = 1.04f;
        [Range(0f, 3f)] [SerializeField] private float _breatheDuration = 1.1f;
        [Range(0f, 1f)] [SerializeField] private float _zoneExitDuration = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _zoneEnterDuration = 0.45f;
        [Range(0f, 1500f)] [SerializeField] private float _zoneHiddenOffsetY = 900f;

        [Header("Wheel — spin settle & reveal")]
        [Range(0f, 1f)] [SerializeField] private float _settlePunchDuration = 0.28f;
        [Range(0, 20)] [SerializeField] private int _settlePunchVibrato = 6;
        [Range(0f, 2f)] [SerializeField] private float _settlePunchElasticity = 1f;
        [Range(1f, 2f)] [SerializeField] private float _highlightScale = 1.25f;
        [Range(0f, 1f)] [SerializeField] private float _highlightDuration = 0.18f;

        [Header("Bomb")]
        [Range(0f, 1f)] [SerializeField] private float _bombShakeDuration = 0.5f;
        [Range(0f, 100f)] [SerializeField] private float _bombShakeStrength = 34f;
        [Range(0, 40)] [SerializeField] private int _bombShakeVibrato = 22;
        [Range(0f, 180f)] [SerializeField] private float _bombShakeRandomness = 90f;
        [Range(0f, 1f)] [SerializeField] private float _flashInDuration = 0.06f;
        [Range(0f, 1f)] [SerializeField] private float _flashOutDuration = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float _flashPeakAlpha = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float _bombImpactHoldDuration = 0.4f;

        [Header("Bank")]
        [Range(0f, 1f)] [SerializeField] private float _bankFlyDuration = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _bankPunchScale = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float _bankPunchDuration = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float _bankCounterDuration = 0.4f;

        [Header("Zone map")]
        [Range(0f, 1f)] [SerializeField] private float _zoneScrollDuration = 0.45f;
        [Range(1f, 2f)] [SerializeField] private float _currentZoneTileScale = 1.12f;

        [Header("Rewards")]
        [Tooltip(
            "Per-unit worth (RewardDefinition.EstimatedValue) at or above which a landed reward earns the " +
            "glow burst on top of any safe/super zone clear it might also be.")]
        [Min(0)] [SerializeField] private int _bigRewardUnitValue = 60;

        public float TickPunchDuration
        {
            get { return _tickPunchDuration; }
        }
        public float BreatheScale
        {
            get { return _breatheScale; }
        }
        public float BreatheDuration
        {
            get { return _breatheDuration; }
        }
        public float ZoneExitDuration
        {
            get { return _zoneExitDuration; }
        }
        public float ZoneEnterDuration
        {
            get { return _zoneEnterDuration; }
        }
        public float ZoneHiddenOffsetY
        {
            get { return _zoneHiddenOffsetY; }
        }
        public float SettlePunchDuration
        {
            get { return _settlePunchDuration; }
        }
        public int SettlePunchVibrato
        {
            get { return _settlePunchVibrato; }
        }
        public float SettlePunchElasticity
        {
            get { return _settlePunchElasticity; }
        }
        public float HighlightScale
        {
            get { return _highlightScale; }
        }
        public float HighlightDuration
        {
            get { return _highlightDuration; }
        }
        public float BombShakeDuration
        {
            get { return _bombShakeDuration; }
        }
        public float BombShakeStrength
        {
            get { return _bombShakeStrength; }
        }
        public int BombShakeVibrato
        {
            get { return _bombShakeVibrato; }
        }
        public float BombShakeRandomness
        {
            get { return _bombShakeRandomness; }
        }
        public float FlashInDuration
        {
            get { return _flashInDuration; }
        }
        public float FlashOutDuration
        {
            get { return _flashOutDuration; }
        }
        public float FlashPeakAlpha
        {
            get { return _flashPeakAlpha; }
        }
        public float BombImpactHoldDuration
        {
            get { return _bombImpactHoldDuration; }
        }
        public float BankFlyDuration
        {
            get { return _bankFlyDuration; }
        }
        public float BankPunchScale
        {
            get { return _bankPunchScale; }
        }
        public float BankPunchDuration
        {
            get { return _bankPunchDuration; }
        }
        public float BankCounterDuration
        {
            get { return _bankCounterDuration; }
        }
        public float ZoneScrollDuration
        {
            get { return _zoneScrollDuration; }
        }
        public float CurrentZoneTileScale
        {
            get { return _currentZoneTileScale; }
        }
        public int BigRewardUnitValue
        {
            get { return _bigRewardUnitValue; }
        }
    }
}
