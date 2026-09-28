using UnityEngine;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// The handful of SFX clips that aren't tied to a wheel tier (compare <see cref="WheelThemeConfig"/>'s
    /// per-tier <c>Tick</c>) — one clip each, played the same way regardless of which zone or
    /// theme is active. A single asset rather than one per clip, for the same reason <c>RewardCatalog</c> is
    /// one asset: there is exactly one of these in the whole game.
    /// <para>
    /// <see cref="Services.AudioService"/> and <c>AudioPresenter</c> are null-safe against an unassigned
    /// slot, so a clip can be dropped in or swapped later as a pure content change, never a code change.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip _buttonClick;
        [SerializeField] private AudioClip _popupOpen;
        [SerializeField] private AudioClip _rewardChime;
        [SerializeField] private AudioClip _bankCollect;
        [SerializeField] private AudioClip _wheelTransition;
        [SerializeField] private AudioClip _bombExplosion;
        [SerializeField] private AudioClip _defeatAmbience;

        public AudioClip ButtonClick
        {
            get { return _buttonClick; }
        }
        public AudioClip PopupOpen
        {
            get { return _popupOpen; }
        }

        /// <summary>The bright sting when a reward is revealed at the wheel stop.</summary>
        public AudioClip RewardChime
        {
            get { return _rewardChime; }
        }

        /// <summary>The softer swoosh / coin-drop as a won reward flies into the bank panel.</summary>
        public AudioClip BankCollect
        {
            get { return _bankCollect; }
        }

        /// <summary>The mechanical slide as the wheel exits, re-themes and rides back in for the next zone.</summary>
        public AudioClip WheelTransition
        {
            get { return _wheelTransition; }
        }

        public AudioClip BombExplosion
        {
            get { return _bombExplosion; }
        }

        /// <summary>The tense drone that stings in under the bomb defeat / revive screen.</summary>
        public AudioClip DefeatAmbience
        {
            get { return _defeatAmbience; }
        }
    }
}
