using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using System;

public enum CombatSound
{
    TongueOut,
    TongueIn,
    TongueHit,
    BrushDestroyed,
    TotemMove,
    TotemImpact,
    BossDeath,
    Victory
}

public enum FlySlotsSound
{
    FlySlotsStart,
    FlySlotsLeverDown,
    FlySlotsLeverUp,
    FlySlotsCollect,
    FlySlotsInvalid,
}

public enum CollectibleSound
{
    PowerFlyCollect,
    LotusCollect,
    HeartCollect,
    WoodpeckerCollect,
    WoodpeckerUse,
    GoldenCollect,
}

public enum TravelSound
{
    LeafTravel,
    WaterTravel,
    BubbleTravel,
    PortalTravel,
    RoomClear,
}

public enum PlayerSound
{
    Dodge,
    PlayerHurt,
    PlayerDeath,
    UiClick,
}

public enum MusicType 
{
    Menu,
    Stump,
    ForestZone,
    SwampZone,
    Boss,
    SubBoss,
    Mystic,
    Portal,
    None,
}

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("Sound Effect Lists")]
    [SerializeField] private CombatAudioListItem[] combatAudioList;
    [SerializeField] private FlySlotsAudioListItem[] flySlotsAudioList;
    [SerializeField] private CollectibleAudioListItem[] collectibleAudioList;
    [SerializeField] private TravelAudioListItem[] travelAudioList;
    [SerializeField] private PlayerAudioListItem[] playerAudioList;
    
    [Header("Music")]
    [SerializeField] private MusicListItem[] musicList;
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource audioSourcePrefab;
    public static AudioManager Instance;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource musicOverrideSource;
    [SerializeField] private AudioSource ambianceSource;
    [SerializeField] private bool playMusicOnStart = false;
    
    private static float globalVolumeLevel = 1.0f;
    private static float clipVolumeLevel = 1.0f;
    private static List<AudioSource> activeAudioSources = new List<AudioSource>();
    private static Dictionary<System.Enum, AudioSource> indefiniteSounds = new Dictionary<System.Enum, AudioSource>();



    #region MONOBEHAVIOUR AND SETUP


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;

        if (playMusicOnStart)
        {
            PlayMusic(MusicType.Menu);
        }
    }


    #endregion


    #region PLAY SOUND


    // Plays a 2D sound effect at the specified volume and position
    public void PlaySound(System.Enum sound, float volume = 1, Vector3? position = null)
    {
        if (Instance == null || Instance.audioSourcePrefab == null)
        {
            Debug.LogWarning("AudioManager instance or audioSourcePrefab is null");
            return;
        }

        AudioClip[] clips = GetAudioClips(sound);
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"No audio clips found for sound type: {sound}");
            return;
        }

        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        
        // Instantiate new audio source from prefab
        AudioSource newAudioSource = Instantiate(Instance.audioSourcePrefab);
        
        // Set position (use world origin if not specified)
        newAudioSource.transform.position = position ?? Vector3.zero;
        
        // Configure for 2D sound (no spatial blend)
        newAudioSource.spatialBlend = 0f;
        
        // Set volume and play
        newAudioSource.volume = volume * globalVolumeLevel;
        newAudioSource.clip = randomClip;
        newAudioSource.Play();
        
        // Track active audio source
        activeAudioSources.Add(newAudioSource);
        
        // Destroy after clip finishes
        Instance.StartCoroutine(Instance.DestroyAudioSourceAfterPlay(newAudioSource, randomClip.length));
    }


    // Plays a 2D sound effect from a direct AudioClip reference
    public void PlaySound(AudioClip clip, float volume = 1, Vector3? position = null)
    {
        if (Instance == null || Instance.audioSourcePrefab == null)
        {
            Debug.LogWarning("AudioManager instance or audioSourcePrefab is null");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("AudioClip is null");
            return;
        }

        // Instantiate new audio source from prefab
        AudioSource newAudioSource = Instantiate(Instance.audioSourcePrefab);
        
        // Set position (use world origin if not specified)
        newAudioSource.transform.position = position ?? Vector3.zero;
        
        // Configure for 2D sound (no spatial blend)
        newAudioSource.spatialBlend = 0f;
        
        // Set volume and play
        newAudioSource.volume = volume * globalVolumeLevel;
        newAudioSource.clip = clip;
        newAudioSource.Play();
        
        // Track active audio source
        activeAudioSources.Add(newAudioSource);
        
        // Destroy after clip finishes
        Instance.StartCoroutine(Instance.DestroyAudioSourceAfterPlay(newAudioSource, clip.length));
    }


    // Plays a 3D sound effect at the specified position with given min and max distances
    public void PlaySound3D(System.Enum sound, Vector3 position, float volume = 1, float minDistance = 1f, float maxDistance = 50f)
    {
        if (Instance == null || Instance.audioSourcePrefab == null)
        {
            Debug.LogWarning("AudioManager instance or audioSourcePrefab is null");
            return;
        }

        AudioClip[] clips = GetAudioClips(sound);
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"No audio clips found for sound type: {sound}");
            return;
        }

        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        
        // Instantiate new audio source from prefab
        AudioSource newAudioSource = Instantiate(Instance.audioSourcePrefab);
        
        // Set position
        newAudioSource.transform.position = position;
        
        // Configure for 3D sound
        newAudioSource.spatialBlend = 1f; // Full 3D
        newAudioSource.rolloffMode = AudioRolloffMode.Linear;
        newAudioSource.minDistance = minDistance;
        newAudioSource.maxDistance = maxDistance;
        newAudioSource.dopplerLevel = 0f; // Disable doppler effect for gameplay sounds
        
        // Set volume and play
        newAudioSource.volume = volume * globalVolumeLevel;
        newAudioSource.clip = randomClip;
        newAudioSource.Play();
        
        // Track active audio source
        activeAudioSources.Add(newAudioSource);
        
        // Destroy after clip finishes
        Instance.StartCoroutine(Instance.DestroyAudioSourceAfterPlay(newAudioSource, randomClip.length));
    }


    #endregion


    #region HELPER METHODS


    // Gets audio clips for a given sound type
    private static AudioClip[] GetAudioClips(System.Enum sound)
    {
        // Check which type of enum it is and search the appropriate list
        if (sound is CombatSound combatSound)
        {
            foreach (var audioItem in Instance.combatAudioList)
            {
                if (audioItem.soundType == combatSound)
                {
                    return audioItem.soundsList;
                }
            }
        }
        else if (sound is FlySlotsSound flySlotsSound)
        {
            foreach (var audioItem in Instance.flySlotsAudioList)
            {
                if (audioItem.soundType == flySlotsSound)
                {
                    return audioItem.soundsList;
                }
            }
        }
        else if (sound is CollectibleSound collectibleSound)
        {
            foreach (var audioItem in Instance.collectibleAudioList)
            {
                if (audioItem.soundType == collectibleSound)
                {
                    return audioItem.soundsList;
                }
            }
        }
        else if (sound is TravelSound travelSound)
        {
            foreach (var audioItem in Instance.travelAudioList)
            {
                if (audioItem.soundType == travelSound)
                {
                    return audioItem.soundsList;
                }
            }
        }
        else if (sound is PlayerSound playerSound)
        {
            foreach (var audioItem in Instance.playerAudioList)
            {
                if (audioItem.soundType == playerSound)
                {
                    return audioItem.soundsList;
                }
            }
        }
        
        return null;
    }

    // Gets music item for a given music type
    private static MusicListItem? GetMusicItem(MusicType musicType)
    {
        foreach (var musicItem in Instance.musicList)
        {
            if (musicItem.musicType == musicType)
            {
                return musicItem;
            }
        }
        return null;
    }


    #endregion


    #region INDEFINITE SOUNDS


    // Plays a looping sound that continues indefinitely until stopped
    public void PlaySoundIndefinite(System.Enum sound, float volume = 1, Vector3? position = null)
    {
        if (Instance == null || Instance.audioSourcePrefab == null)
        {
            Debug.LogWarning("AudioManager instance or audioSourcePrefab is null");
            return;
        }

        // If this sound is already playing indefinitely, don't create another one
        if (indefiniteSounds.ContainsKey(sound) && indefiniteSounds[sound] != null)
        {
            Debug.LogWarning($"Sound {sound} is already playing indefinitely");
            return;
        }

        AudioClip[] clips = GetAudioClips(sound);
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"No audio clips found for sound type: {sound}");
            return;
        }

        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        
        // Instantiate new audio source from prefab
        AudioSource newAudioSource = Instantiate(Instance.audioSourcePrefab);
        
        // Set position (use world origin if not specified)
        newAudioSource.transform.position = position ?? Vector3.zero;
        
        // Configure for 2D sound (no spatial blend)
        newAudioSource.spatialBlend = 0f;
        
        // Set volume and configure looping
        newAudioSource.volume = volume * globalVolumeLevel;
        newAudioSource.clip = randomClip;
        newAudioSource.loop = true;
        newAudioSource.Play();
        
        // Track this indefinite sound
        indefiniteSounds[sound] = newAudioSource;
        activeAudioSources.Add(newAudioSource);
    }


    // Stops an indefinite looping sound and destroys its audio source
    public void StopIndefiniteSound(System.Enum sound)
    {
        if (!indefiniteSounds.ContainsKey(sound))
        {
            Debug.Log($"No indefinite sound found for sound type: {sound}, ignoring");
            return;
        }

        AudioSource source = indefiniteSounds[sound];
        if (source != null)
        {
            source.Stop();
            activeAudioSources.Remove(source);
            Destroy(source.gameObject);
        }

        indefiniteSounds.Remove(sound);
    }


    #endregion


    #region PLAY MUSIC


    // Plays background music of the specified type
    public void PlayMusic(MusicType musicType) 
    {
        if (Instance == null || Instance.musicSource == null)
        {
            Debug.LogWarning("AudioManager instance or musicSource is null");
            return;
        }

        // Handle MusicType.None - fade out and stop music
        if (musicType == MusicType.None)
        {
            if (Instance.musicSource.isPlaying)
            {
                LeanTween.value(Instance.gameObject, Instance.musicSource.volume, 0f, 1f)
                    .setOnUpdate((float val) => {
                        if (Instance.musicSource != null)
                        {
                            Instance.musicSource.volume = val;
                        }
                        // Fade out ambiance too if it's playing
                        if (Instance.ambianceSource != null && Instance.ambianceSource.isPlaying)
                        {
                            Instance.ambianceSource.volume = val;
                        }
                    })
                    .setOnComplete(() => {
                        if (Instance.musicSource != null)
                        {
                            Instance.musicSource.Stop();
                        }
                        if (Instance.ambianceSource != null)
                        {
                            Instance.ambianceSource.Stop();
                        }
                    });
            }
            return;
        }

        MusicListItem? musicItem = GetMusicItem(musicType);
        if (musicItem == null)
        {
            Debug.LogWarning($"No music found for music type: {musicType}");
            return;
        }

        AudioClip randomClip = musicItem.Value.music;
        AudioClip ambianceClip = musicItem.Value.optionalAmbiance;
        
        // If music is currently playing, fade it out first
        if (Instance.musicSource.isPlaying)
        {
            LeanTween.value(Instance.gameObject, Instance.musicSource.volume, 0f, 1f)
                .setOnUpdate((float val) => {
                    if (Instance.musicSource != null)
                    {
                        Instance.musicSource.volume = val;
                    }
                    // Fade out ambiance too if it's playing
                    if (Instance.ambianceSource != null && Instance.ambianceSource.isPlaying)
                    {
                        Instance.ambianceSource.volume = val;
                    }
                })
                .setOnComplete(() => {
                    if (Instance.musicSource != null)
                    {
                        Instance.musicSource.clip = randomClip;
                        Instance.musicSource.volume = globalVolumeLevel;
                        Instance.musicSource.Play();
                    }
                    // Play ambiance if available
                    if (Instance.ambianceSource != null && ambianceClip != null)
                    {
                        Instance.ambianceSource.clip = ambianceClip;
                        Instance.ambianceSource.volume = globalVolumeLevel;
                        Instance.ambianceSource.Play();
                    }
                    else if (Instance.ambianceSource != null)
                    {
                        // Stop ambiance if new music has none
                        Instance.ambianceSource.Stop();
                    }
                });
        }
        else
        {
            // No music playing, just start immediately
            Instance.musicSource.clip = randomClip;
            Instance.musicSource.volume = globalVolumeLevel;
            Instance.musicSource.Play();
            
            // Play ambiance if available
            if (Instance.ambianceSource != null && ambianceClip != null)
            {
                Instance.ambianceSource.clip = ambianceClip;
                Instance.ambianceSource.volume = globalVolumeLevel;
                Instance.ambianceSource.Play();
            }
            else if (Instance.ambianceSource != null)
            {
                // Stop ambiance if music has none
                Instance.ambianceSource.Stop();
            }
        }
    }


    // Plays override music, fading out the main music but keeping it playing
    public void PlayOverrideMusic(MusicType musicType)
    {
        if (Instance == null || Instance.musicOverrideSource == null)
        {
            Debug.LogWarning("AudioManager instance or musicOverrideSource is null");
            return;
        }

        MusicListItem? musicItem = GetMusicItem(musicType);
        if (musicItem == null)
        {
            Debug.LogWarning($"No music found for music type: {musicType}");
            return;
        }

        AudioClip randomClip = musicItem.Value.music;

        // Set up override music
        Instance.musicOverrideSource.clip = randomClip;
        Instance.musicOverrideSource.volume = 0f;
        Instance.musicOverrideSource.Play();

        // Fade in override music
        LeanTween.value(Instance.gameObject, 0f, globalVolumeLevel, 1f)
            .setOnUpdate((float val) => {
                if (Instance.musicOverrideSource != null)
                {
                    Instance.musicOverrideSource.volume = val;
                }
            });

        // Fade out main music (but keep it playing)
        if (Instance.musicSource != null && Instance.musicSource.isPlaying)
        {
            LeanTween.value(Instance.gameObject, Instance.musicSource.volume, 0f, 1f)
                .setOnUpdate((float val) => {
                    if (Instance.musicSource != null)
                    {
                        Instance.musicSource.volume = val;
                    }
                });
        }
    }


    // Clears override music and fades main music back in
    public void ClearOverrideMusic()
    {
        if (Instance == null)
        {
            Debug.LogWarning("AudioManager instance is null");
            return;
        }

        // Fade out and stop override music if it's playing
        if (Instance.musicOverrideSource != null && Instance.musicOverrideSource.isPlaying)
        {
            LeanTween.value(Instance.gameObject, Instance.musicOverrideSource.volume, 0f, 1f)
                .setOnUpdate((float val) => {
                    if (Instance.musicOverrideSource != null)
                    {
                        Instance.musicOverrideSource.volume = val;
                    }
                })
                .setOnComplete(() => {
                    if (Instance.musicOverrideSource != null)
                    {
                        Instance.musicOverrideSource.Stop();
                    }
                });
        }

        // Fade in main music if it's playing
        if (Instance.musicSource != null && Instance.musicSource.isPlaying)
        {
            LeanTween.value(Instance.gameObject, Instance.musicSource.volume, globalVolumeLevel, 1f)
                .setOnUpdate((float val) => {
                    if (Instance.musicSource != null)
                    {
                        Instance.musicSource.volume = val;
                    }
                });
        }
    }


    #endregion


    #region CLEANUP


    // Destroys a given audio source after its clip has finished playing
    private IEnumerator DestroyAudioSourceAfterPlay(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if (source != null)
        {
            activeAudioSources.Remove(source);
            Destroy(source.gameObject);
        }
    }


    #endregion


    #region SET AUDIO VOLUME


    // Sets the global volume level for all audio
    public void SetVolumeLevel(float volume)
    {
        if(volume > 1.0f)
        {
            Debug.LogWarning("Volume level is greater than 100%");
            return;
        }

        //Debug.Log("Volume level: " +  volume);

        //Change Global volume
        globalVolumeLevel = volume;
        
        // Retroactively adjust all active audio sources
        for (int i = activeAudioSources.Count - 1; i >= 0; i--)
        {
            if (activeAudioSources[i] != null)
            {
                // Preserve the clip-specific volume multiplier
                float clipVolume = activeAudioSources[i].volume / (globalVolumeLevel > 0 ? globalVolumeLevel : 1f);
                activeAudioSources[i].volume = clipVolume * globalVolumeLevel;
            }
            else
            {
                // Clean up null references
                activeAudioSources.RemoveAt(i);
            }
        }

        // Adjust music source
        if (Instance != null && Instance.musicSource != null)
        {
            Instance.musicSource.volume = globalVolumeLevel;
        }

        // Adjust music override source
        if (Instance != null && Instance.musicOverrideSource != null)
        {
            Instance.musicOverrideSource.volume = globalVolumeLevel;
        }
        
        // Adjust ambiance source
        if (Instance != null && Instance.ambianceSource != null)
        {
            Instance.ambianceSource.volume = globalVolumeLevel;
        }
    }


    #endregion


}

[Serializable]
public struct CombatAudioListItem
{
    public CombatSound soundType;
    public AudioClip[] soundsList;
}

[Serializable]
public struct FlySlotsAudioListItem
{
    public FlySlotsSound soundType;
    public AudioClip[] soundsList;
}

[Serializable]
public struct CollectibleAudioListItem
{
    public CollectibleSound soundType;
    public AudioClip[] soundsList;
}

[Serializable]
public struct TravelAudioListItem
{
    public TravelSound soundType;
    public AudioClip[] soundsList;
}

[Serializable]
public struct PlayerAudioListItem
{
    public PlayerSound soundType;
    public AudioClip[] soundsList;
}

[Serializable]
public struct MusicListItem
{
    public MusicType musicType;
    public AudioClip music;
    public AudioClip optionalAmbiance;
}