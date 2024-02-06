using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    //public Slider SFXvolumeSlider;
    //public Slider BGMvolumeSlider;

    public SoundScript[] bgmSounds, sfxSounds, bgSounds;
    public AudioSource bgmSource, sfxSource, bgSource;

    public GameObject sfx;
    public AudioMixer audioMixerGroup;

    public AudioMixerGroup musicMixerGroup, soundEffectsMixerGroup;

    public float masterVol;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            //audioMixerGroup.ClearFloat("MasterVolume");
            //audioMixerGroup.GetFloat("MasterVolume", out masterVol);
            //masterVol += 80f;
            //audioMixerGroup.SetFloat("MasterVolume", -80f + masterVol);
            //SFXvolumeSlider.value = sfxSource.volume;
            //BGMvolumeSlider.value = bgmSource.volume;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayUI()
    {
        PlaySFX("Click");
    }
    public void PlayBGM(string name)
    {
        SoundScript s = Array.Find(bgmSounds, x => x.name == name);

        if (s == null)
        {
            Debug.Log("Sound not found");
        }
        else
        {
            if (bgmSource != null)
            {
                if (!bgmSource.isPlaying)
                {
                    bgmSource.clip = s.clip;
                    bgmSource.PlayOneShot(s.clip);
                }
            }
        }
    }

    public void PlayBGMLoop(string name, bool stop)
    {
        SoundScript s = Array.Find(bgmSounds, x => x.name == name);

        if (s == null)
        {
            Debug.Log("Sound not found");
        }
        else
        {
            bgmSource.clip = s.clip;
            bgmSource.volume = (s.volume * 0.01f);

            if (bgmSource != null)
                if (!bgmSource.isPlaying)
                    bgmSource.Play();
            if (stop)
            {
                bgmSource.loop = false;
                bgmSource.Stop();
            }
            else
            {
                bgmSource.loop = true;
            }

        }
    }

    public void PlaySFX(string name, Vector3 position)
    {
        SoundScript s = Array.Find(sfxSounds, x => x.name == name);

        if (s == null)
        {
            Debug.Log("Sound not found : " + name);
        }
        else
        {
            GameObject go = Instantiate(sfx, position, Quaternion.identity);
            AudioSource tempSource = go.GetComponent<AudioSource>();
            tempSource.clip = s.clip;
            tempSource.volume = (s.volume * 0.01f);
            go.name = name + "_AudioClip";
            go.GetComponent<PlayOnAwake>().Play();
        }
    }
    public void PlaySFX(string name)
    {
        PlaySFX(name, Vector3.zero);
    }

    public void PlaySFXLoop(string name, bool stop)
    {
        SoundScript s = Array.Find(bgmSounds, x => x.name == name);

        if (s == null)
        {
            Debug.Log("Sound not found");
        }
        else
        {
            sfxSource.clip = s.clip;
            if (stop)
            {
                sfxSource.loop = false;
                sfxSource.Stop();
            }
            else
            {
                sfxSource.loop = true;
            }
            sfxSource.Play();
        }
    }

    public void PlayBG(string name)
    {
        SoundScript s = Array.Find(bgSounds, x => x.name == name);

        if (s == null)
        {
            Debug.Log("Sound not found");
        }
        else
        {
            bgSource.clip = s.clip;
            bgSource.PlayOneShot(s.clip);
        }
    }

    public void PlayBGLoop(string name, bool stop)
    {
        SoundScript s = Array.Find(bgSounds, x => x.name == name);

        if (s == null)
        {
            Debug.Log("Sound not found");
        }
        else
        {
            bgSource.clip = s.clip;
            if (stop)
            {
                bgSource.loop = false;
                bgSource.Stop();
            }
            else
            {
                bgSource.loop = true;
            }
            bgSource.Play();
        }
    }

    public void ToggleBGM()
    {
        bgmSource.mute = !bgmSource.mute;
        //bgSource.mute = !bgSource.mute;
    }

    public void ToggleSFX()
    {
        sfxSource.mute = !sfxSource.mute;
    }

    public void BGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    public void SFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
    public void StopBGM()
    {
        bgmSource.Stop();
    }
    public void MasterVolume(float volume)
    {
        if (volume == 0f)
            audioMixerGroup.SetFloat("MasterVolume", -80f);
        else
            audioMixerGroup.SetFloat("MasterVolume", -80f + Mathf.Log10(volume) * 50f);

        masterVol = volume;
    }

    public void UpdateMixerVolume()
    {
        //musicMixerGroup.audioMixer.SetFloat("MasterVolume", Mathf.Log10(AudioOptionsManager.masterVolume) * 20);
        //musicMixerGroup.audioMixer.SetFloat("MusicVolume", Mathf.Log10(AudioOptionsManager.musicVolume) * 20);
        //musicMixerGroup.audioMixer.SetFloat("SoundEffectsVolume", Mathf.Log10(AudioOptionsManager.soundEffectsVolume) * 20);
    }
}