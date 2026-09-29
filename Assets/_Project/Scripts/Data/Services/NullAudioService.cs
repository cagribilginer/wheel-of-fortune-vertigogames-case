using UnityEngine;

namespace Vertigo.Wheel.Data.Services
{
    /// <summary>
    /// No-op fallback so <see cref="AudioHub"/> is safe to call before <c>GameInstaller.Awake()</c> has
    /// assigned the real service, and so nothing outside Play Mode — an Edit Mode test, the scene builder —
    /// ever has to null-check before calling into audio.
    /// </summary>
    public sealed class NullAudioService : IAudioService
    {
        public static readonly NullAudioService Instance = new();

        private NullAudioService() { }

        public void PlayOneShot(AudioClip clip, float volumeScale = 1f) { }
    }
}
