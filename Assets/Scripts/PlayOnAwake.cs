using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayOnAwake : MonoBehaviour
{
    public AudioSource sfxSource;
    // Start is called before the first frame update
    public void Play()
    {
        sfxSource.PlayOneShot(sfxSource.clip);
        Destroy(gameObject, sfxSource.clip.length);
    }
}

