using System.Collections.Generic;
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

    private string testSceneName = "TestScene-Load-AA"; //TO DO: Replace with actual scene name from saved data

    private const int maxProfiles = 3;

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
        CreateExistingProfiles();
    }

    // Update is called once per frame
    void Update()
    {
        
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

        //Create Profile Card
        GameObject newCard = Instantiate(profileCardPrefab, this.transform);
        ProfileCard cardScript = newCard.GetComponent<ProfileCard>();
        cardScript.Initialize(profileNumber, testSceneName);
        profileCards[profileNumber] = newCard;

        //Set Transform
        newCard.transform.position = addSlots[profileNumber].transform.position;

        //Disable Add Slot for profile number
        addSlots[profileNumber].SetActive(false);
    }

    private void CreateSavedProfile(int profileNumber)
    {
        //Temporary: Use same method as CreateNewProfile for now
        CreateNewProfile(profileNumber);
    }

    public void CreateExistingProfiles()
    {
        string SaveProfilePath = SaveManager.GetFileNameEnd();

        //TO DO: Check for existing save files and create profile cards accordingly
        string folderPath = Application.persistentDataPath;
        List<int> existingProfiles = GetSavedProfileNumbers(folderPath, SaveProfilePath);

        //Create profile cards for existing profiles
        foreach (int profileNum in existingProfiles)
        {
            CreateSavedProfile(profileNum);
        }
    }

    public void DeleteProfile(int profileNumber)
    {
        //Destroy Profile Card
        Destroy(profileCards[profileNumber]);
        profileCards[profileNumber] = null;

        //Enable Add Slot for profile number
        addSlots[profileNumber].SetActive(true);
        
        //TODO: Delete save file
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
