using System.Collections;
using System.Collections.Generic;
using TiltFive.Logging;
using Unity.VisualScripting;
using UnityEngine;

namespace Code.Scripts
{
    public class SoundManager : MonoBehaviour
    {
        public int maxSimultaneousSounds = 10;
        public AudioClip effectVolumeRefSound;
        public AudioClip backgroundMusic;
        public float effectVolumeRefWaitTime = 0.2f;
        
        private readonly Queue<AudioSource> _availableSources = new();
        private AudioSource _musicAudioSource;
        
        private float _effectVolume;
        
        // Static instance of SoundManager, accessible from anywhere
        public static SoundManager Instance { get; private set; }

        private void Awake()
        {
            // Check for existing instances and destroy this instance if we've already got a one
            if (Instance != null && Instance != this)
            {
                Log.Warn("Destroying duplicate SoundManager");
                Destroy(gameObject);
                return;
            }

            // Set this instance as the Singleton instance
            Instance = this;
            
            PrepareAndStartMusicAudioSource();
            
            // Persist across scenes
            DontDestroyOnLoad(gameObject);
        }
        
        private float _lastEffectSetTime;
        private bool _valueHasBeenSet;

        private void PrepareAndStartMusicAudioSource()
        {
            // Prepare the music audio source
            _musicAudioSource = transform.AddComponent<AudioSource>();
            _musicAudioSource.loop = true;
            _musicAudioSource.volume = 1;
            _musicAudioSource.clip = backgroundMusic;
            _musicAudioSource.PlayDelayed(1);

            AllocateInstances();
        }
        
        public float MusicVolume
        {
            set => _musicAudioSource.volume = value;
            get => _musicAudioSource.volume;
        }
        
        public float EffectVolume
        {
            set {
                _effectVolume = value;
                
                // Debounce the changes, so we only play the reference sound after a period of quiescence
                _lastEffectSetTime = Time.time;
                StartCoroutine(PlayEffectReferenceCoroutine());
            }
            get => _effectVolume;
        }
        
        private IEnumerator PlayEffectReferenceCoroutine()
        {
            yield return new WaitForSeconds(effectVolumeRefWaitTime);
            if (Time.time - _lastEffectSetTime >= effectVolumeRefWaitTime)
            {
                PlaySound(effectVolumeRefSound, 1);
            }
        }

        private void AllocateInstances()
        {
            while (_availableSources.Count < maxSimultaneousSounds)
            {
                var soundObj = new GameObject("SoundManager_AudioSource_Instance");
                var source = soundObj.AddComponent<AudioSource>();
                soundObj.transform.SetParent(transform);
                _availableSources.Enqueue(source);
            }
        }
        
        public void PlaySound(AudioClip clip, float clipVolume)
        {
            // Early exit if no clip is provided
            if (!clip) return;
            
            // Obtain an available source, or exit if we've exhausted them
            if (!_availableSources.TryDequeue(out var source)) return;
            source.clip = clip;
            source.volume = clipVolume * _effectVolume;
            source.Play();
            StartCoroutine(ReturnSourceWhenFinished(source));
        }

        private IEnumerator ReturnSourceWhenFinished(AudioSource source)
        {
            yield return new WaitForSeconds(source.clip.length);
            source.Stop();
            _availableSources.Enqueue(source);
        }
    }
}