using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

public class SaveManager : MonoBehaviour
{

    #region VARIABLES

    public static SaveManager Instance { get; private set; }

    private ProfileData profileData;

    public static int activeProfile { get; private set; }

    private string folderPath;
    private string fileNameEnd = "savefile.json";
    private string fullPath;

    //Event for other scripts to subscribe to
    public static event Action SaveOnQuit;

    // Shared JSON settings (quick fix)
    private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All,
        Formatting = Newtonsoft.Json.Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore // Prevent issues with circular references (quick fix)
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
    }

    private void OnApplicationQuit()
    {
        //Fire the event before quitting
        SaveOnQuit?.Invoke();

        //Then save everything to file
        SaveToFile();
    }

    #endregion

    #region SAVE AND LOAD FUNCTIONS

    public void SaveForProfile<T>(SaveVariable variableName, T data)
    {
        profileData.data[variableName] = new SaveValue<T> { value = data };
    }

    public T LoadForProfile<T>(SaveVariable variableName)
    {
        if (profileData.data.TryGetValue(variableName, out var val))
        {
            if (val is SaveValue<T> typed)
            {
                return typed.value;
            }
            throw new InvalidCastException(
                $"Stored value for {variableName} is {val.GetType()}, not {typeof(T)}"
            );
        }

        throw new KeyNotFoundException($"No data found for variable: {variableName}");
    }

    public void SaveToFile()
    {
        string json = JsonConvert.SerializeObject(profileData, jsonSettings);
        File.WriteAllText(fullPath, json);
        Debug.Log($"[SaveManager] Saved profile {activeProfile} to {fullPath}");
    }

    private void LoadFromFile()
    {
        if (File.Exists(fullPath))
        {
            string json = File.ReadAllText(fullPath);
            profileData = JsonConvert.DeserializeObject<ProfileData>(json, jsonSettings);

            if (profileData == null)
                profileData = new ProfileData();

            Debug.Log($"[SaveManager] Loaded profile {activeProfile} from {fullPath}");
        }
        else
        {
            Debug.LogWarning($"[SaveManager] No save file found at {fullPath}, creating new profile data.");
            profileData = new ProfileData();
        }
    }

    #endregion

    #region SETTERS AND GETTERS

    public void SetActiveProfile(int profileNumber)
    {
        activeProfile = profileNumber;
        folderPath = Application.persistentDataPath;
        string fileName = "profile_" + activeProfile + fileNameEnd;
        fullPath = Path.Combine(folderPath, fileName);

        LoadFromFile();
    }

    #endregion
}

public enum SaveVariable
{
    PlayerHealth,
    PlayerPosition,
    CurrentLevel,
    Inventory
}