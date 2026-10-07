using UnityEngine;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// The look of one wheel tier. Swapping a tier's entire visual identity is a single inspector drag.
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Wheel/Theme", fileName = "Theme_")]
    public sealed class WheelThemeConfig : ScriptableObject
    {
        [SerializeField] private Sprite _baseSprite;
        [SerializeField] private Sprite _indicatorSprite;
        [SerializeField] private Color _glowColor = Color.white;
        [SerializeField] private AudioClip _tick;

        [Header("Zone strip")]
        [Tooltip("Colour of a zone's number in the strip when that zone uses this theme.")]
        [SerializeField] private Color _stripNumberColor = Color.white;
        [Tooltip("Whether the number is bold, which marks the milestone (safe / super) zones.")]
        [SerializeField] private bool _isStripNumberBold;

        public Sprite BaseSprite
        {
            get { return _baseSprite; }
        }
        public Sprite IndicatorSprite
        {
            get { return _indicatorSprite; }
        }
        public Color GlowColor
        {
            get { return _glowColor; }
        }
        public AudioClip Tick
        {
            get { return _tick; }
        }
        public Color StripNumberColor
        {
            get { return _stripNumberColor; }
        }
        public bool IsStripNumberBold
        {
            get { return _isStripNumberBold; }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_baseSprite)
                Debug.LogWarning($"[Vertigo] Theme '{name}' has no base sprite.", this);
            if (!_indicatorSprite)
                Debug.LogWarning($"[Vertigo] Theme '{name}' has no indicator sprite.", this);
        }
#endif
    }
}
