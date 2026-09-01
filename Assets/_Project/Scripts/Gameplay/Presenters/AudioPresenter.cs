using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.Data.Services;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The non-tier SFX: reward chime, bomb impact, and popup open/close. Tier-specific audio (the wheel's
    /// tick) is played directly by <see cref="WheelPresenter"/>, which already owns the per-zone
    /// <c>WheelThemeConfig</c> this class has no reason to duplicate. Fire-and-forget, same as
    /// <see cref="VfxPresenter"/> — nothing in the flow waits on a sound finishing.
    /// </summary>
    public sealed class AudioPresenter
    {
        private readonly IAudioService _audio;
        private readonly AudioLibrary _library;

        public AudioPresenter(IAudioService audio, AudioLibrary library)
        {
            _audio = audio;
            _library = library;
        }

        /// <summary>Reward revealed at the wheel stop.</summary>
        public void PlayReward() => _audio.PlayOneShot(_library != null ? _library.RewardChime : null);

        /// <summary>The reward tile landing in the bank panel — a quieter collect swoosh, not the reveal sting.</summary>
        public void PlayBankCollect() => _audio.PlayOneShot(_library != null ? _library.BankCollect : null, 0.8f);

        /// <summary>The wheel sliding out/in between zones (covers tier swaps — every one rides a transition).</summary>
        public void PlayWheelTransition() => _audio.PlayOneShot(_library != null ? _library.WheelTransition : null);

        /// <summary>The cash-out "rewards claimed" flourish. Reuses the reward chime — it is the game's one
        /// positive sting and there is no dedicated victory clip in the pack.</summary>
        public void PlayClaim() => _audio.PlayOneShot(_library != null ? _library.RewardChime : null);
        public void PlayBombImpact() => _audio.PlayOneShot(_library != null ? _library.BombExplosion : null);
        public void PlayDefeatAmbience() => _audio.PlayOneShot(_library != null ? _library.DefeatAmbience : null);
        public void PlayPopupOpen() => _audio.PlayOneShot(_library != null ? _library.PopupOpen : null);
        public void PlayPopupClose() => _audio.PlayOneShot(_library != null ? _library.PopupClose : null);
    }
}
