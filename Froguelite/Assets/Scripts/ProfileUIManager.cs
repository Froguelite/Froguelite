using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ProfileUIManager : MonoBehaviour
{
    //Role: Checks the save files and instantiate card prefabs for each existing profile file. Sets parameters for each card as needed.
    //      Deletes profile files if profile card is still empty after a certain time

    #region VARIABLES

    public static ProfileUIManager Instance;

    [SerializeField] private GameObject profileCardPrefab;

    [SerializeField] private GameObject[] addSlots; //Holds references to the "Add Profile" buttons in the UI

    private GameObject[] profileCards;

    //private string testSceneName = "TestScene-Load-AA"; //TO DO: Replace with actual scene name from saved data

    private const int maxProfiles = 3;

    private ProfileCardDataList profileCardDataList = null;

    private readonly string profileCardsFileName = "profile_cards.json";

    #endregion

    #region SETUP

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject); // persist across scenes
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Add buttonclick listeners to addSlots buttons
        for (int i = 0; i < addSlots.Length; i++)
        {
            int profileIndex = i; //Capture index for the listener
            addSlots[i].GetComponent<Button>().onClick.AddListener(() => CreateNewProfile(profileIndex));
        }

        //Initialize profileCards array
        profileCards = new GameObject[maxProfiles];

        //Check if addslots length matches maxProfiles
        if (addSlots.Length != maxProfiles)
        {
            Debug.LogError("Add slots length does not match max profiles.");
        }

        //Create profile cards for existing profiles
        //CreateExistingProfiles();
        LoadProfileCardsData();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnApplicationQuit()
    {
        SaveProfileCardsData();
    }

    #endregion

    #region PROFILE CARDS DATA FILE METHODS

    private void LoadProfileCardsData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, profileCardsFileName);

        if (!File.Exists(filePath))
        {
            Debug.Log("Profile cards data file does not exist. Creating new data file.");
            return;
        }

        try
        {
            string jsonString = File.ReadAllText(filePath);
            profileCardDataList = JsonUtility.FromJson<ProfileCardDataList>(jsonString);
        } catch (Exception e)
        {
            Debug.LogError("Failed to load profile cards data: " + e.Message);
            profileCardDataList = null;
        }

        //Create profile cards for loaded profiles
        CreateExistingProfiles();
    }

    private void SaveProfileCardsData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, profileCardsFileName);

        try
        {
            string jsonString = JsonUtility.ToJson(profileCardDataList, true);
            File.WriteAllText(filePath, jsonString);
        } catch (Exception e)
        {
            Debug.LogError("Failed to save profile cards data: " + e.Message);
        }
    }

    #endregion

    #region PROFILE CARD INSTANTIATION AND DELETION

    public void CreateNewProfile(int profileNumber)
    {
        //Check if profileNumber is already exists
        Debug.Log("Profile number: " + profileNumber);
        if (profileCards[profileNumber] != null)
        {
            Debug.LogWarning("Profile number " + profileNumber + " already exists.");
            return;
        }

        //Create a new ProfileCardData in the List
        ProfileCardData newCardData = new ProfileCardData(profileNumber);

        //Create profileCardDataList if null
        if (profileCardDataList == null)
        {
            profileCardDataList = new ProfileCardDataList();
        }

        //Add new card data to list
        profileCardDataList.profiles.Add(newCardData);

        //Create Profile Card
        GameObject newCard = Instantiate(profileCardPrefab, this.transform);
        ProfileCard cardScript = newCard.GetComponent<ProfileCard>();
        //cardScript.Initialize(profileNumber, testSceneName);
        
        cardScript.Initialize(newCardData); //Passed by reference, so changes to cardScript.profileData will update the list
        profileCards[profileNumber] = newCard;

        //Set Transform
        newCard.transform.position = addSlots[profileNumber].transform.position;

        //Disable Add Slot for profile number
        //addSlots[profileNumber].SetActive(false);
        addSlots[profileNumber].GetComponent<Button>().interactable = false;
    }

    private void CreateSavedProfile(ProfileCardData cardData)
    {
        //Temporary: Use same method as CreateNewProfile for now
        //CreateNewProfile(profileNumber);

        //Create Profile Card
        GameObject newCard = Instantiate(profileCardPrefab, this.transform);
        ProfileCard cardScript = newCard.GetComponent<ProfileCard>();
        //cardScript.Initialize(profileNumber, testSceneName);

        cardScript.Initialize(cardData); //Passed by reference, so changes to cardScript.profileData will update the list
        profileCards[cardData.profileNumber] = newCard;

        //Set Transform according to profile number
        newCard.transform.position = addSlots[cardData.profileNumber].transform.position;

        //Disable Add Slot for profile number
        //addSlots[cardData.profileNumber].SetActive(false);
        addSlots[cardData.profileNumber].GetComponent<Button>().interactable = false;
    }

    public void CreateExistingProfiles()
    {
        //string SaveProfilePath = SaveManager.GetFileNameEnd();

        ////Check for existing save files and create profile cards accordingly
        //string folderPath = Application.persistentDataPath;
        //List<int> existingProfiles = GetSavedProfileNumbers(folderPath, SaveProfilePath);

        ////Create profile cards for existing profiles
        //foreach (int profileNum in existingProfiles)
        //{
        //    CreateSavedProfile(profileNum);
        //}

        //Return if saved profiles do not exist
        if(profileCardDataList == null)
        {
            return;
        }

        //Create profile cards for existing profiles
        foreach (ProfileCardData cardData in profileCardDataList.profiles)
        {
            CreateSavedProfile(cardData);
        }
    }

    public void DeleteProfile(ProfileCardData cardData)
    {
        //Destroy Profile Card
        Destroy(profileCards[cardData.profileNumber]);
        profileCards[cardData.profileNumber] = null;

        //Enable Add Slot for profile number
        //addSlots[cardData.profileNumber].SetActive(true);
        addSlots[cardData.profileNumber].GetComponent<Button>().interactable = true;

        //Remove from profileCardDataList
        profileCardDataList.profiles.Remove(cardData);

        //TO DO: Delete save file
    }

    #endregion

    #region HELPER METHODS
    private List<int> GetSavedProfileNumbers(string folderPath, string fileNameEnd)
    {
        // Get all files that match "profile_*_savefile.json"
        string[] files = Directory.GetFiles(folderPath, "profile_*" + fileNameEnd);

        List<int> profileNumbers = new List<int>();

        foreach (string file in files)
        {
            // Extract the filename only (no path)
            string fileName = Path.GetFileName(file);

            // Example: "profile_3_savefile.json"
            // Remove "profile_" and "_savefile.json"
            string numberPart = fileName
                .Replace("profile_", "")
                .Replace(fileNameEnd, "");

            // Try parse the number
            if (int.TryParse(numberPart, out int num))
            {
                profileNumbers.Add(num);
            }
        }

        // Optional: sort for convenience
        profileNumbers.Sort();

        return profileNumbers;
    }
    #endregion
}

[System.Serializable]
public class ProfileCardData
{
    public int profileNumber;
    public string name;
    public string sceneToLoad;

    public ProfileCardData(int number, string scene, string name)
    {
        profileNumber = number;
        sceneToLoad = scene;
        this.name = name;
    }

    public ProfileCardData(int number)
    {
        profileNumber = number;
        sceneToLoad = "TestScene-Load-AA"; //TO DO: Replace with actual default scene name
        name = "Profile Name";
    }
}

[System.Serializable]
public class ProfileCardDataList
{
    public List<ProfileCardData> profiles = new List<ProfileCardData>();
}