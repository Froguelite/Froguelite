using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using System;

public enum SoundType
{
    Tongue,
    DamagePlayer,
    DamageEnemy,
    ItemPickup,
    Death,
    EnemyDeath,
    PowerUpPickup,
    Music

}
[RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioList[] audioList;
    private static AudioManager instance;
    private AudioSource audioSource;
    private static float globalVolumeLevel = 1.0f;
    private static float clipVolumeLevel = 1.0f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        instance = this;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        PlaySound(SoundType.Music);
    }

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        AudioClip[] clips = instance.audioList[(int)sound].Sounds;
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        clipVolumeLevel = volume;
        instance.audioSource.PlayOneShot(randomClip, clipVolumeLevel * globalVolumeLevel);

        //instance.audioSource.PlayOneShot(instance.audioList[(int)sound], volume);
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        string[] names = Enum.GetNames(typeof(SoundType));
        Array.Resize(ref audioList, names.Length);
        for (int i = 0; i < audioList.Length; i++)
        {
            audioList[i].name = names[i];
        }
    }
#endif

    #region SET AUDIO VOLUME
    public static void SetVolumeLevel(float volume)
    {
        if(volume > 1.0f)
        {
            Debug.LogWarning("Volume level is greater than 100%");
            return;
        }

        //Debug.Log("Volume level: " +  volume);

        //Change Global volume and apply change
        globalVolumeLevel = volume;
        instance.audioSource.volume = clipVolumeLevel * globalVolumeLevel;
    }

    #endregion

}

[Serializable]
public struct AudioList
{
    public AudioClip[] Sounds { get => sounds; }
    [HideInInspector] public string name; 
    [SerializeField]private AudioClip[] sounds;
}