using UnityEngine;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// The handful of SFX clips that aren't tied to a wheel tier (compare <see cref="WheelThemeConfig"/>'s
    /// per-tier <c>Tick</c>/<c>SpinLoop</c>) — one clip each, played the same way regardless of which zone or
    /// theme is active. A single asset rather than one per clip, for the same reason <c>RewardCatalog</c> is
    /// one asset: there is exactly one of these in the whole game.
    /// <para>
    /// No <c>demo_content</c> audio ships with this project (see the README's design-decisions section), so
    /// every field here starts empty. <see cref="Services.AudioService"/> and <c>AudioPresenter</c> are both
    /// null-safe against that — dropping clips in later is a pure content change, never a code change.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip _buttonClick;
        [SerializeField] private AudioClip _popupOpen;
        [SerializeField] private AudioClip _popupClose;
        [SerializeField] private AudioClip _rewardChime;
        [SerializeField] private AudioClip _bombExplosion;
        [SerializeField] private AudioClip _defeatAmbience;

        public AudioClip ButtonClick => _buttonClick;
        public AudioClip PopupOpen => _popupOpen;
        public AudioClip PopupClose => _popupClose;
        public AudioClip RewardChime => _rewardChime;
        public AudioClip BombExplosion => _bombExplosion;

        /// <summary>The tense drone that stings in under the bomb defeat / revive screen.</summary>
        public AudioClip DefeatAmbience => _defeatAmbience;
    }
}
