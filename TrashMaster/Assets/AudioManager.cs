using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public SoundEffect[] soundEffects;

    private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

    private void Awake()
    {
        // Only initialize local components, no access to GameManager
        InitializeAudioSources();
    }

    // Access GameManager in Start instead of Awake if needed
    private void Start()
    {
        // Safe to access GameManager here if needed
    }

    private void InitializeAudioSources()
    {
        // Create audio sources for each sound effect
        if (soundEffects == null) return;

        foreach (SoundEffect sound in soundEffects)
        {
            if (sound.clip == null) continue;

            GameObject soundObject = new GameObject("Sound_" + sound.name);
            soundObject.transform.parent = transform;

            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.clip = sound.clip;
            audioSource.volume = sound.volume;
            audioSource.playOnAwake = false;

            audioSources.Add(sound.name, audioSource);
        }
    }

    public void PlaySound(string soundName)
    {
        if (audioSources.ContainsKey(soundName))
        {
            audioSources[soundName].Play();
        }
        else
        {
            Debug.LogWarning("Sound not found: " + soundName);
        }
    }

    public void StopSound(string soundName)
    {
        if (audioSources.ContainsKey(soundName))
        {
            audioSources[soundName].Stop();
        }
    }

    public void SetVolume(string soundName, float volume)
    {
        if (audioSources.ContainsKey(soundName))
        {
            audioSources[soundName].volume = volume;
        }
    }
}