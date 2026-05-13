using UnityEngine;

namespace TileAdventure.Services
{
    /// <summary>
    /// Persistent singleton audio manager. Lives in the Loading scene and survives
    /// scene transitions via DontDestroyOnLoad.
    ///
    /// Features:
    ///   - Background music looping (bg_music.ogg)
    ///   - SFX pool of 4 AudioSources for tap/match sounds (no clipping)
    ///   - Volume control (music and SFX separately)
    ///
    /// Audio clips are loaded from Resources on Start() if not assigned in Inspector.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _bgMusic;
        [SerializeField] private AudioClip _tapSfx;
        [SerializeField] private AudioClip _matchSfx;

        [Header("Volume")]
        [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 0.8f;

        private AudioSource _musicSource;
        private AudioSource[] _sfxSources;
        private int _sfxSourceCount = 4;
        private int _currentSfxIndex;

        private void Awake()
        {
            // Enforce singleton — destroy duplicates from scene reloads
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Music: single looping source
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.volume = _musicVolume;

            // SFX: pool of sources for overlapping sounds (rapid taps)
            _sfxSources = new AudioSource[_sfxSourceCount];
            for (int i = 0; i < _sfxSourceCount; i++)
            {
                _sfxSources[i] = gameObject.AddComponent<AudioSource>();
                _sfxSources[i].playOnAwake = false;
                _sfxSources[i].volume = _sfxVolume;
            }
        }

        private void Start()
        {
            LoadAudioClips();
            PlayMusic();
        }

        /// <summary>
        /// Load clips from Resources if not assigned in Inspector.
        /// Paths are relative to Resources/ (e.g., "bg_music" → Resources/bg_music.ogg).
        /// </summary>
        private void LoadAudioClips()
        {
            if (_bgMusic == null)
                _bgMusic = Resources.Load<AudioClip>("bg_music");
            if (_tapSfx == null)
                _tapSfx = Resources.Load<AudioClip>("tap");
            if (_matchSfx == null)
                _matchSfx = Resources.Load<AudioClip>("match");
        }

        /// <summary> Start looping background music. No-op if already playing. </summary>
        public void PlayMusic()
        {
            if (_musicSource != null && _bgMusic != null && !_musicSource.isPlaying)
            {
                _musicSource.clip = _bgMusic;
                _musicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (_musicSource != null)
                _musicSource.Stop();
        }

        /// <summary> Play tap sound effect. Uses round-robin source pool. </summary>
        public void PlayTap()
        {
            PlaySfx(_tapSfx);
        }

        /// <summary> Play match sound effect. Uses round-robin source pool. </summary>
        public void PlayMatch()
        {
            PlaySfx(_matchSfx);
        }

        /// <summary>
        /// Play a one-shot SFX on the next available pooled source (round-robin).
        /// PlayOneShot allows overlapping sounds on the same AudioSource.
        /// </summary>
        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;

            var source = _sfxSources[_currentSfxIndex];
            _currentSfxIndex = (_currentSfxIndex + 1) % _sfxSourceCount;
            source.PlayOneShot(clip, _sfxVolume);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            if (_musicSource != null)
                _musicSource.volume = _musicVolume;
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            foreach (var source in _sfxSources)
            {
                if (source != null)
                    source.volume = _sfxVolume;
            }
        }
    }
}
