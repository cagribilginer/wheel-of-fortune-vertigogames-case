using UnityEngine;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// The SFX clips not tied to a wheel tier, one clip each. A single asset because the game has exactly one;
    /// AudioService and AudioPresenter tolerate an unassigned slot.
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
