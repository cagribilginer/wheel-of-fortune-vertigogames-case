using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.Data.Services;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The non-tier SFX: reward chime, bomb impact, popup open and the like. The per-zone wheel tick is played by
    /// <see cref="WheelPresenter"/>, which owns the theme. Fire-and-forget: nothing waits on a sound.
    /// </summary>
    public sealed class AudioPresenter
    {
        private const float BANK_COLLECT_VOLUME = 0.8f;

        private readonly IAudioService _audio;
        private readonly AudioLibrary _library;

        public AudioPresenter(IAudioService audio, AudioLibrary library)
        {
            _audio = audio;
            _library = library;
        }

        /// <summary>Reward revealed at the wheel stop.</summary>
        public void PlayReward()
        {
            _audio.PlayOneShot(_library ? _library.RewardChime : null);
        }

        /// <summary>The reward tile landing in the bank panel — a quieter collect swoosh, not the reveal sting.</summary>
        public void PlayBankCollect()
        {
            _audio.PlayOneShot(_library ? _library.BankCollect : null, BANK_COLLECT_VOLUME);
        }

        /// <summary>The wheel sliding out/in between zones (covers tier swaps — every one rides a transition).</summary>
        public void PlayWheelTransition()
        {
            _audio.PlayOneShot(_library ? _library.WheelTransition : null);
        }

        /// <summary>The cash-out "rewards claimed" flourish. Reuses the reward chime — it is the game's one
        /// positive sting and there is no dedicated victory clip in the pack.</summary>
        public void PlayClaim()
        {
            _audio.PlayOneShot(_library ? _library.RewardChime : null);
        }

        /// <summary>The shared click on every interactable button (handed to each <c>UIButtonPunchMono</c> by the installer).</summary>
        public void PlayButtonClick()
        {
            _audio.PlayOneShot(_library ? _library.ButtonClick : null);
        }

        public void PlayBombImpact()
        {
            _audio.PlayOneShot(_library ? _library.BombExplosion : null);
        }
        public void PlayDefeatAmbience()
        {
            _audio.PlayOneShot(_library ? _library.DefeatAmbience : null);
        }
        public void PlayPopupOpen()
        {
            _audio.PlayOneShot(_library ? _library.PopupOpen : null);
        }
    }
}
