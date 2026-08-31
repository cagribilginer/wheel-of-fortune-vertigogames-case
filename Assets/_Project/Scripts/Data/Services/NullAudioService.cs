using UnityEngine;

namespace Vertigo.Wheel.Data.Services
{
    /// <summary>
    /// No-op fallback so any call site can reach <see cref="AudioHub.Service"/> safely before
    /// <c>GameInstaller.Awake()</c> has assigned the real one, and so nothing outside Play Mode — an Edit
    /// Mode test, the scene builder — ever has to null-check before calling into audio.
    /// </summary>
    public sealed class NullAudioService : IAudioService
    {
        public static readonly NullAudioService Instance = new NullAudioService();

        private NullAudioService() { }

        public void PlayOneShot(AudioClip clip, float volumeScale = 1f) { }
        public void PlayMusicLoop(AudioClip clip) { }
        public void StopMusic() { }

        public float MasterVolume => 1f;
        public float SfxVolume => 1f;
        public float MusicVolume => 1f;
        public bool Muted => false;

        public void SetMasterVolume(float volume01) { }
        public void SetSfxVolume(float volume01) { }
        public void SetMusicVolume(float volume01) { }
        public void SetMuted(bool muted) { }
    }
}
