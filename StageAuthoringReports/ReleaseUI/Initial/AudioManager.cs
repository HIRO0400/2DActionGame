using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField]
    private AudioSource seSource;


    [Header("Mixer")]
    [SerializeField]
    private AudioMixer audioMixer;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void PlaySE(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        seSource.PlayOneShot(clip);
    }


    public void SetSEVolume(float volume)
    {
        audioMixer.SetFloat(
            "SFXVolume",
            Mathf.Log10(volume) * 20
        );
    }
}