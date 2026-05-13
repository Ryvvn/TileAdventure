using UnityEngine;

namespace TileAdventure.Services
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioClip _bgMusic;
        [SerializeField] private AudioClip _tapSfx;
        [SerializeField] private AudioClip _matchSfx;
        [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 0.8f;

        private AudioSource _musicSource;
        private AudioSource[] _sfxSources;
        private int _sfxSourceCount = 4;
        private int _currentSfxIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.volume = _musicVolume;

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

        private void LoadAudioClips()
        {
            if (_bgMusic == null)
                _bgMusic = Resources.Load<AudioClip>("bg_music");
            if (_tapSfx == null)
                _tapSfx = Resources.Load<AudioClip>("tap");
            if (_matchSfx == null)
                _matchSfx = Resources.Load<AudioClip>("match");
        }

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

        public void PlayTap()
        {
            PlaySfx(_tapSfx);
        }

        public void PlayMatch()
        {
            PlaySfx(_matchSfx);
        }

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
