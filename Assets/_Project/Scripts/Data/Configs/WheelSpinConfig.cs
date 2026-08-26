using UnityEngine;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// Every timing number the spin presentation uses, so there are no magic floats in the presenter.
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Wheel Spin", fileName = "WheelSpin_")]
    public sealed class WheelSpinConfig : ScriptableObject
    {
        [Min(0.1f)] [SerializeField] private float _duration = 3.2f;
        [Min(1)] [SerializeField] private int _minTurns = 4;
        [Min(1)] [SerializeField] private int _maxTurns = 6;

        [Tooltip("A curve, not an Ease enum: OutQuart decelerates too gently and the last 300ms feels dead.")]
        [SerializeField]
        private AnimationCurve _spinEase = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.25f, 0.35f, 2f, 2f),
            new Keyframe(0.8f, 0.93f, 0.5f, 0.5f),
            new Keyframe(1f, 1f, 0f, 0f));

        [SerializeField] private float _settlePunchDegrees = 2.5f;
        [SerializeField] private float _tickPunchDegrees = 10f;
        [SerializeField] private float _revealDelay = 0.35f;

        public float Duration => _duration;
        public int MinTurns => _minTurns;
        public int MaxTurns => _maxTurns;
        public AnimationCurve SpinEase => _spinEase;
        public float SettlePunchDegrees => _settlePunchDegrees;
        public float TickPunchDegrees => _tickPunchDegrees;
        public float RevealDelay => _revealDelay;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_maxTurns < _minTurns)
            {
                _maxTurns = _minTurns;
                Debug.LogWarning($"[Vertigo] '{name}': max turns raised to match min turns.", this);
            }
        }
#endif
    }
}
