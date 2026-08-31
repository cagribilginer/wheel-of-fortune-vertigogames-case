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

        public void PlayReward() => _audio.PlayOneShot(_library != null ? _library.RewardChime : null);
        public void PlayBombImpact() => _audio.PlayOneShot(_library != null ? _library.BombExplosion : null);
        public void PlayPopupOpen() => _audio.PlayOneShot(_library != null ? _library.PopupOpen : null);
        public void PlayPopupClose() => _audio.PlayOneShot(_library != null ? _library.PopupClose : null);
    }
}
