using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public class SaveManager : MonoBehaviour
{
    #region VARIABLES

    public static SaveManager Instance { get; private set; }

    private ProfileData profileData;

    public static int activeProfile { get; private set; } //Maybe require setting profile before save and load funcs?

    private string folderPath; //Path to save file
    private string fileNameEnd = "savefile.json"; //Name of save file

    private string fullPath; //Full path to save file

    #endregion

    #region MONOBEHAVIOUR AND SETUP


    // Awake, setup singleton
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }


    #endregion

    #region SAVE AND LOAD FUNCTIONS

    public void SaveForProfile<T>(SaveVariable variableName, T data)
    {
        profileData.data[variableName] = new SaveValue<T> { value = data };
    }

    // Load function, throws exception if variable not found or type mismatch
    //Expect caller to know type of data being loaded
    public T LoadForProfile<T>(SaveVariable variableName)
    {
        //Check if variable exists
        if (profileData.data.TryGetValue(variableName, out var val))
        {
            if (val is SaveValue<T> typed)
            {
                return typed.value;
            }
            //If variable exists but type mismatch, throw exception
            throw new InvalidCastException(
                $"Stored value for {variableName} is {val.GetType()}, not {typeof(T)}"
            );
        }

        //If variable doesn't exist, throw exception
        throw new KeyNotFoundException($"No data found for variable: {variableName}");
    }

    public void SaveToFile()
    {
        //Write profileData to json file based on fullPath
    }

    private void LoadFromFile()
    {
        //Load from json file to profileData based on fullPath
    }

    #endregion

    #region SETTERS AND GETTERS
    public void SetActiveProfile(int profileNumber)
    {
        activeProfile = profileNumber;
        folderPath = Application.persistentDataPath;
        string fileName = "profile_" + activeProfile + fileNameEnd;
        fullPath = Path.Combine(folderPath, fileName);

        //Once Active profile is set, load from file
        LoadFromFile();
    }
    #endregion
}

//Create enum for each variable to be saved
//Each enum value is the key in the hashmap
//Needs to be updated when new variables are added
public enum SaveVariable
{
    //Example:
    PlayerHealth,
    PlayerPosition,
    CurrentLevel,
}