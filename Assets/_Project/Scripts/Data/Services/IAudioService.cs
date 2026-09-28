using UnityEngine;

namespace Vertigo.Wheel.Data.Services
{
    /// <summary>
    /// The one seam every SFX call goes through. It lives in Data rather than as a Core port because a call
    /// carries an <see cref="AudioClip"/>, which Core rejects; only presenters and views play sound.
    /// </summary>
    public interface IAudioService
    {
        void PlayOneShot(AudioClip clip, float volumeScale = 1f);
    }
}
