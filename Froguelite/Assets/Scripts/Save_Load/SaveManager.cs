using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

public class SaveManager : MonoBehaviour
{
    #region VARIABLES

    public static SaveManager Instance { get; private set; }

    // Events for other scripts to automatically update their variables when saving/loading
    public static event Action SaveData;
    public static event Action LoadData;
    public static event Action LoadZone;

    private ProfileData profileData = new ProfileData(); // always initialized

    public static int activeProfile { get; private set; }

    // File path
    private string folderPath;
    private readonly string fileNameEnd = "_savefile.json";
    private string fullPath;

    // Shared JSON settings (handles polymorphic objects, prevents circular loops)
    private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        Formatting = Newtonsoft.Json.Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    #endregion

    #region MONOBEHAVIOUR AND SETUP

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // persist across scenes
    }

    //private void OnApplicationQuit()
    //{
    //    // Only save if a profile has been selected
    //    if (fullPath != null)
    //    {
    //        WriteToFile();
    //    }
    //}

    #endregion

    #region STATIC SAVE AND LOAD FUNCTIONS

    // Save a variable into profile data
    public static void SaveForProfile<T>(SaveVariable variableName, T data)
    {
        CheckInstance();
        Instance.profileData.data[variableName] = new SaveValue<T> { value = data };
    }

    // Load a variable from profile data
    public static T LoadForProfile<T>(SaveVariable variableName)
    {
        CheckInstance();

        if (Instance.profileData.data.TryGetValue(variableName, out var val))
        {
            if (val is SaveValue<T> typed)
                return typed.value;

            throw new InvalidCastException(
                $"Stored value for {variableName} is {val.GetType()}, not {typeof(T)}"
            );
        }

        throw new KeyNotFoundException($"No data found for variable: {variableName}");
    }

    // Serialize and write profileData to file
    private static void SaveToFile()
    {
        CheckInstance();

        if (Instance == null || Instance.fullPath == null)
        {
            Debug.LogWarning("[SaveManager] No active profile set, cannot save data. Skipping for now - might be due to saving without passing through menu first.");
            return;
        }

        string json = JsonConvert.SerializeObject(Instance.profileData, jsonSettings);
        File.WriteAllText(Instance.fullPath, json);
        Debug.Log($"[SaveManager] Saved profile {activeProfile} to {Instance.fullPath}");
    }

    // Load profileData from file
    private static void LoadFromFile()
    {
        CheckInstance();

        if (File.Exists(Instance.fullPath))
        {
            string json = File.ReadAllText(Instance.fullPath);
            Instance.profileData = JsonConvert.DeserializeObject<ProfileData>(json, jsonSettings);

            if (Instance.profileData == null)
                Instance.profileData = new ProfileData();

            Debug.Log($"[SaveManager] Loaded profile {activeProfile} from {Instance.fullPath}");
        }
        else
        {
            Debug.LogWarning($"[SaveManager] No save file found at {Instance.fullPath}, creating new profile data.");
            Instance.profileData = new ProfileData();
        }
    }

    // Public save entry point
    // Fires SaveData event so all subscribed scripts update their variables automatically, then writes file
    public static void WriteToFile()
    {
        CheckInstance();

        if (SaveData != null)
        {
            foreach (Action subscriber in SaveData.GetInvocationList())
            {
                try { subscriber.Invoke(); }
                catch (Exception ex) { Debug.LogError($"[SaveManager] SaveData subscriber failed: {ex}"); }
            }
        }

        SaveToFile();
        Debug.Log("[SaveManager] Data saved to file.");
    }

    // Public load entry point for after scene generation (objects don't exist before generation)
    // Fires LoadData event so subscribers can update variables automatically
    public static void LoadDataAfterGeneration()
    {
        CheckInstance();

        //LoadFromFile();

        if (LoadData != null)
        {
            foreach (Action subscriber in LoadData.GetInvocationList())
            {
                try { subscriber.Invoke(); }
                catch (Exception ex) { Debug.LogError($"[SaveManager] LoadData subscriber failed: {ex}"); }
            }
        }

    }

    // Public load entry point for before scene generation (zones) (exist as DontDestroy)
    // Loads file, and then fires LoadZones event so subscribers can update variables automatically
    public static void LoadDataBeforeGeneration()
    {
        CheckInstance();

        LoadFromFile();

        if (LoadZone != null)
        {
            foreach (Action subscriber in LoadZone.GetInvocationList())
            {
                try { subscriber.Invoke(); }
                catch (Exception ex) { Debug.LogError($"[SaveManager] LoadZone subscriber failed: {ex}"); }
            }
        }

    }

    #endregion

    #region STATIC SETTERS AND GETTERS

    // Set active profile and load its data
    public static void SetActiveProfile(int profileNumber)
    {
        CheckInstance();

        activeProfile = profileNumber;

        // Setup file path for profile
        //Instance.folderPath = Application.persistentDataPath;
        //string fileName = "profile_" + activeProfile + Instance.fileNameEnd;
        //Instance.fullPath = Path.Combine(Instance.folderPath, fileName);
        Instance.fullPath = buildFilePath(profileNumber);
        Debug.Log($"[SaveManager] Active profile set to {activeProfile}, file path: {Instance.fullPath}");
    }

    public static string GetFileNameEnd()
    {
        CheckInstance();
        return Instance.fileNameEnd;
    }

    #endregion

    #region HELPERS

    // Ensures SaveManager instance exists before using static methods
    private static void CheckInstance()
    {
        if (Instance == null)
            throw new Exception("[SaveManager] Instance not initialized! Ensure SaveManager exists in the scene.");
    }

    private static string buildFilePath(int profileNumber)
    {
        CheckInstance();

        // Setup file path for profile
        if (Instance.folderPath == null)
        {
            Instance.folderPath = Application.persistentDataPath;
        }
        string fileName = "profile_" + profileNumber + Instance.fileNameEnd;
        return Path.Combine(Instance.folderPath, fileName);
    }

    public static void DeleteProfile(int profileNumber)
    {
        string deleteFilePath = buildFilePath(profileNumber);
        if (File.Exists(deleteFilePath))
        {
            File.Delete(deleteFilePath);
            Debug.Log("File deleted: " +  deleteFilePath);
        } else
        {
            Debug.LogWarning("File not found: " + deleteFilePath);
        }
    }

    #endregion
}


public enum SaveVariable
{
    CurrentHealth,
    MaxHealth,
    CurrentLevel,
    Lotus,
    Woodpeckers,
    CollectedPowerflies,
    PurchasedPowerFlies,
    GoldenFlies,
    RandomSeed,
    CurrentZone,
    CurrentSubZone,
    PlayerLevel
}