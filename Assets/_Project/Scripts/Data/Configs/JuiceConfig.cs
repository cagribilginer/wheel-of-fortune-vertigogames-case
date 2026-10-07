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
        [Header("Display")]
        [Tooltip("Frame rate the game asks the device for. 60 for a smooth wheel; 30 saves battery on weak devices.")]
        [Range(30, 120)] [SerializeField] private int _targetFrameRate = 60;

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
        [SerializeField] private Color _bombFlashColor = new(1f, 0.2f, 0.2f, 1f);

        [Header("Button click")]
        [Tooltip("Every interactable button shares this punch; negative squeezes, positive pops.")]
        [Range(-0.5f, 0.5f)] [SerializeField] private float _buttonPunchScale = -0.18f;
        [Range(0f, 1f)] [SerializeField] private float _buttonPunchDuration = 0.28f;
        [Range(0, 20)] [SerializeField] private int _buttonPunchVibrato = 8;
        [Range(0f, 2f)] [SerializeField] private float _buttonPunchElasticity = 0.75f;

        [Header("Bank")]
        [Range(0f, 1f)] [SerializeField] private float _bankFlyDuration = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _bankPunchScale = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float _bankPunchDuration = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float _bankCounterDuration = 0.4f;
        [Tooltip("How many icons carry one reward into the bank; its amount is split between them.")]
        [Range(1, 5)] [SerializeField] private int _bankFlyIconsPerReward = 1;
        [Tooltip("Delay between the icons of one reward taking off.")]
        [Range(0f, 0.3f)] [SerializeField] private float _bankFlyStagger = 0.08f;

        [Header("Zone map")]
        [Range(0f, 1f)] [SerializeField] private float _zoneScrollDuration = 0.45f;
        [Range(1f, 2f)] [SerializeField] private float _currentZoneTileScale = 1.12f;
        [SerializeField] private Color _currentZoneTextColor = new(0.12f, 0.13f, 0.16f);
        [Tooltip("Opacity multiplier of the numbers of zones the player has already cleared.")]
        [Range(0f, 1f)] [SerializeField] private float _passedZoneAlpha = 0.5f;

        [Header("Popups")]
        [Range(0f, 1f)] [SerializeField] private float _popupBackdropAlpha = 0.82f;
        [Range(0f, 1f)] [SerializeField] private float _popupFadeDuration = 0.2f;
        [Range(0.5f, 1f)] [SerializeField] private float _popupClosedScale = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float _popupOpenDuration = 0.3f;

        [Header("Bomb popup")]
        [Range(0f, 1f)] [SerializeField] private float _bombBackdropAlpha = 0.86f;
        [Range(0f, 1f)] [SerializeField] private float _vignetteMinAlpha = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _vignettePeakAlpha = 0.9f;
        [Range(0f, 3f)] [SerializeField] private float _vignetteBreatheDuration = 0.85f;

        [Header("Collect popup")]
        [Range(0f, 3f)] [SerializeField] private float _claimHoldDuration = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float _cardPunchScale = 0.25f;
        [Range(0f, 1f)] [SerializeField] private float _cardPunchDuration = 0.35f;
        [Range(0f, 2f)] [SerializeField] private float _countUpDuration = 0.5f;

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
        public Color BombFlashColor
        {
            get { return _bombFlashColor; }
        }
        public float ButtonPunchScale
        {
            get { return _buttonPunchScale; }
        }
        public float ButtonPunchDuration
        {
            get { return _buttonPunchDuration; }
        }
        public int ButtonPunchVibrato
        {
            get { return _buttonPunchVibrato; }
        }
        public float ButtonPunchElasticity
        {
            get { return _buttonPunchElasticity; }
        }
        public int TargetFrameRate
        {
            get { return _targetFrameRate; }
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
        public int BankFlyIconsPerReward
        {
            get { return _bankFlyIconsPerReward; }
        }
        public float BankFlyStagger
        {
            get { return _bankFlyStagger; }
        }
        public float ZoneScrollDuration
        {
            get { return _zoneScrollDuration; }
        }
        public float CurrentZoneTileScale
        {
            get { return _currentZoneTileScale; }
        }
        public Color CurrentZoneTextColor
        {
            get { return _currentZoneTextColor; }
        }
        public float PassedZoneAlpha
        {
            get { return _passedZoneAlpha; }
        }
        public float PopupBackdropAlpha
        {
            get { return _popupBackdropAlpha; }
        }
        public float PopupFadeDuration
        {
            get { return _popupFadeDuration; }
        }
        public float PopupClosedScale
        {
            get { return _popupClosedScale; }
        }
        public float PopupOpenDuration
        {
            get { return _popupOpenDuration; }
        }
        public float BombBackdropAlpha
        {
            get { return _bombBackdropAlpha; }
        }
        public float VignetteMinAlpha
        {
            get { return _vignetteMinAlpha; }
        }
        public float VignettePeakAlpha
        {
            get { return _vignettePeakAlpha; }
        }
        public float VignetteBreatheDuration
        {
            get { return _vignetteBreatheDuration; }
        }
        public float ClaimHoldDuration
        {
            get { return _claimHoldDuration; }
        }
        public float CardPunchScale
        {
            get { return _cardPunchScale; }
        }
        public float CardPunchDuration
        {
            get { return _cardPunchDuration; }
        }
        public float CountUpDuration
        {
            get { return _countUpDuration; }
        }
    }
}
