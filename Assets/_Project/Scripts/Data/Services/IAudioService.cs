using UnityEngine;

namespace Vertigo.Wheel.Data.Services
{
    /// <summary>
    /// The one seam every SFX/music call in the game goes through.
    /// <para>
    /// Lives in Data rather than as a Core port, unlike <see cref="Core.Run.ISaveService"/> or
    /// <see cref="Core.Spin.IRandomProvider"/> — an audio call inherently carries an
    /// <see cref="AudioClip"/>, and Core's <c>noEngineReferences</c> rejects any Unity type on sight.
    /// Nothing in Core needs to trigger audio anyway: every call site is a Presenter or a View, the same
    /// layer that already owns VFX.
    /// </para>
    /// </summary>
    public interface IAudioService
    {
        void PlayOneShot(AudioClip clip, float volumeScale = 1f);
        void PlayMusicLoop(AudioClip clip);
        void StopMusic();

        float MasterVolume { get; }
        float SfxVolume { get; }
        float MusicVolume { get; }
        bool Muted { get; }

        void SetMasterVolume(float volume01);
        void SetSfxVolume(float volume01);
        void SetMusicVolume(float volume01);
        void SetMuted(bool muted);
    }
}
