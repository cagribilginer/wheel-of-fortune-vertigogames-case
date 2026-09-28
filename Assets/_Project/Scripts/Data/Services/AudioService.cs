using UnityEngine;

namespace Vertigo.Wheel.Data.Services
{
    /// <summary>
    /// Pools a handful of <see cref="AudioSource"/>s on a runtime-only GameObject it creates itself — no
    /// scene node, no Inspector wiring, the same "self-contained service <c>GameInstaller</c> just news up"
    /// shape as <c>Wallet</c> or <c>ContinueService</c>. Round-robins one-shots across the pool so
    /// overlapping cues (a tick under a reward chime) never cut each other off.
    /// </summary>
    public sealed class AudioService : IAudioService
    {
        private const int POOL_SIZE = 6;

        private readonly AudioSource[] _sfxPool;
        private int _nextSfx;

        public AudioService(Transform parent = null)
        {
            var root = new GameObject("audio_service");
            if (parent != null) root.transform.SetParent(parent, worldPositionStays: false);

            _sfxPool = new AudioSource[POOL_SIZE];
            for (int i = 0; i < POOL_SIZE; i++)
            {
                var source = new GameObject($"sfx_{i}").AddComponent<AudioSource>();
                source.transform.SetParent(root.transform, worldPositionStays: false);
                source.playOnAwake = false;
                source.spatialBlend = 0f; // a 2D UI game has no listener position for 3D attenuation to key off
                _sfxPool[i] = source;
            }
        }

        public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            AudioSource source = _sfxPool[_nextSfx];
            _nextSfx = (_nextSfx + 1) % _sfxPool.Length;
            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }
    }
}
