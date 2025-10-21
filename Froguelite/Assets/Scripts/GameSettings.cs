using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NUnit.Framework;

public class GameSettings : MonoBehaviour
{
    #region ENUM
    public enum SettingType
    {
        Audio,
        Resolution,
        Fullscreen
    }

    #endregion

    #region VARIABLES

    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider audioSlider; //TODO: Implement audio settings

    Resolution[] AllResolutions;
    List<Resolution> uniqueResolutions = new List<Resolution>();

    private int audioVolume;
    private int selectedResolution;
    private bool isFullscreen;

    #endregion

    #region SETUP
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Set default values
        isFullscreen = true;
        Resolution currentResoltion = Screen.currentResolution;

        //Get all resoltions and populate dropdown with unique resoltions
        AllResolutions = Screen.resolutions;

        List<string> resolutionOptions = new List<string>();

        foreach (Resolution res in AllResolutions)
        {
            string option = res.width + " x " + res.height;

            //Check if option is already added
            if (!resolutionOptions.Contains(option))
            {
                resolutionOptions.Add(option);
                uniqueResolutions.Add(res);

                //Check if res is same as current resolution and get index
                if (res.width == currentResoltion.width && res.height == currentResoltion.height)
                {
                    selectedResolution = uniqueResolutions.Count - 1;
                }
            }
        }
        resolutionDropdown.AddOptions(resolutionOptions);

        //Get Player Prefs for resolution
        //If none saved, save current resolution as default
        string savedResString = PlayerPrefs.GetString(SettingType.Resolution.ToString());
        if(savedResString != "")
        {
            int savedRes = int.Parse(savedResString);

            //Apply saved resolution if different from current
            if (savedRes != selectedResolution)
            {
                selectedResolution = savedRes;
                resolutionDropdown.value = savedRes;
                ChangeResolution();
            }
        } else
        {
            PlayerPrefs.SetString(SettingType.Resolution.ToString(), selectedResolution.ToString());
        }

        //Get Player Prefs for fullscreen
        string savedFullscreenString = PlayerPrefs.GetString(SettingType.Fullscreen.ToString());
        if(savedFullscreenString != "")
        {
            bool savedFullscreen = bool.Parse(savedFullscreenString);
            //Apply saved fullscreen if different from current
            if (savedFullscreen != isFullscreen)
            {
                isFullscreen = savedFullscreen;
                fullscreenToggle.isOn = savedFullscreen;
                ToggleFullscreen();
            }
        }
        else
        {
            PlayerPrefs.SetString(SettingType.Fullscreen.ToString(), isFullscreen.ToString());
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion

    #region SETTINGS FUNCTIONS
    public void ChangeResolution()
    {
        selectedResolution = resolutionDropdown.value;
        Resolution res = uniqueResolutions[selectedResolution];
        Screen.SetResolution(res.width, res.height, isFullscreen);
    }

    public void ToggleFullscreen()
    {
        isFullscreen = fullscreenToggle.isOn;
        Resolution res = uniqueResolutions[selectedResolution];
        Screen.SetResolution(res.width, res.height, isFullscreen);
    }
    #endregion
}
