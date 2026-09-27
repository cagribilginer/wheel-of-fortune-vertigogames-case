using System;
using UnityEngine;
using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Data.Services
{
    /// <summary>
    /// Pools a handful of <see cref="AudioSource"/>s on a runtime-only GameObject it creates itself — no
    /// scene node, no Inspector wiring, the same "self-contained service <c>GameInstaller</c> just news up"
    /// shape as <see cref="Wallet"/> or <c>ContinueService</c>. One more source is reserved for the
    /// music bus, so a future looped track (e.g. a per-tier spin loop) has somewhere to play that isn't the
    /// SFX pool.
    /// <para>
    /// Volumes persist through the same <see cref="ISaveService"/> seam the wallet uses, as three ints
    /// (0-100) plus a mute flag — <see cref="ISaveService"/> only speaks ints, and a UI game has no reason
    /// for finer resolution than a percent.
    /// </para>
    /// </summary>
    public sealed class AudioService : IAudioService
    {
        private const string MASTER_KEY = "vertigo.wheel.audio.master";
        private const string SFX_KEY = "vertigo.wheel.audio.sfx";
        private const string MUSIC_KEY = "vertigo.wheel.audio.music";
        private const string MUTED_KEY = "vertigo.wheel.audio.muted";
        private const int POOL_SIZE = 6;

        private readonly ISaveService _save;
        private readonly AudioSource[] _sfxPool;
        private readonly AudioSource _musicSource;
        private int _nextSfx;

        private float _masterVolume;
        private float _sfxVolume;
        private float _musicVolume;
        private bool _muted;

        public AudioService(ISaveService save, Transform parent = null)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));

            _masterVolume = ReadVolume(MASTER_KEY);
            _sfxVolume = ReadVolume(SFX_KEY);
            _musicVolume = ReadVolume(MUSIC_KEY);
            _muted = _save.GetInt(MUTED_KEY, 0) != 0;

            var root = new GameObject("audio_service");
            if (parent != null) root.transform.SetParent(parent, worldPositionStays: false);

            _sfxPool = new AudioSource[POOL_SIZE];
            for (int i = 0; i < POOL_SIZE; i++)
                _sfxPool[i] = CreateSource(root.transform, $"sfx_{i}", loop: false);

            _musicSource = CreateSource(root.transform, "music", loop: true);
        }

        public float MasterVolume
        {
            get { return _masterVolume; }
        }
        public float SfxVolume
        {
            get { return _sfxVolume; }
        }
        public float MusicVolume
        {
            get { return _musicVolume; }
        }
        public bool Muted
        {
            get { return _muted; }
        }

        public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _muted) return;

            AudioSource source = _sfxPool[_nextSfx];
            _nextSfx = (_nextSfx + 1) % _sfxPool.Length;
            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * _masterVolume * _sfxVolume);
        }

        public void PlayMusicLoop(AudioClip clip)
        {
            if (clip == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            _musicSource.clip = clip;
            _musicSource.volume = _masterVolume * _musicVolume;
            if (!_muted) _musicSource.Play();
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        public void SetMasterVolume(float volume01)
        {
            _masterVolume = Persist(MASTER_KEY, volume01);
            ApplyMusicVolume();
        }

        public void SetSfxVolume(float volume01)
        {
            _sfxVolume = Persist(SFX_KEY, volume01);
        }

        public void SetMusicVolume(float volume01)
        {
            _musicVolume = Persist(MUSIC_KEY, volume01);
            ApplyMusicVolume();
        }

        public void SetMuted(bool muted)
        {
            _muted = muted;
            _save.SetInt(MUTED_KEY, muted ? 1 : 0);
            _save.Save();

            if (_muted) _musicSource.Pause();
            else if (_musicSource.clip != null) _musicSource.UnPause();
        }

        private void ApplyMusicVolume()
        {
            _musicSource.volume = _masterVolume * _musicVolume;
        }

        private static AudioSource CreateSource(Transform parent, string name, bool loop)
        {
            var source = new GameObject(name).AddComponent<AudioSource>();
            source.transform.SetParent(parent, worldPositionStays: false);
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // a 2D UI game has no listener position for 3D attenuation to key off
            return source;
        }

        private float ReadVolume(string key)
        {
            return Mathf.Clamp01(_save.GetInt(key, 100) / 100f);
        }

        private float Persist(string key, float volume01)
        {
            float clamped = Mathf.Clamp01(volume01);
            _save.SetInt(key, Mathf.RoundToInt(clamped * 100f));
            _save.Save();
            return clamped;
        }
    }
}
