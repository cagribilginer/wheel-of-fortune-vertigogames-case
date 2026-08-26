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
        [SerializeField] private Color _accentColor = Color.white;
        [SerializeField] private Color _glowColor = Color.white;
        [SerializeField] private AudioClip _spinLoop;
        [SerializeField] private AudioClip _tick;

        public Sprite BaseSprite => _baseSprite;
        public Sprite IndicatorSprite => _indicatorSprite;
        public Color AccentColor => _accentColor;
        public Color GlowColor => _glowColor;
        public AudioClip SpinLoop => _spinLoop;
        public AudioClip Tick => _tick;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_baseSprite == null)
                Debug.LogWarning($"[Vertigo] Theme '{name}' has no base sprite.", this);
            if (_indicatorSprite == null)
                Debug.LogWarning($"[Vertigo] Theme '{name}' has no indicator sprite.", this);
        }
#endif
    }
}
