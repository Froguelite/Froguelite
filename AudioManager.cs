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

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    public static void PlaySound(SoundType sound, float volume = 1)
    {
        AudioClip[] clips = instance.audioList[(int)sound].Sounds;
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        instance.audioSource.PlayOneShot(randomClip, volume);

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
}

[Serializable]
public struct AudioList
{
    public AudioClip[] Sounds { get => sounds; }
    [HideInInspector] public string name; 
    [SerializeField]private AudioClip[] sounds;
}