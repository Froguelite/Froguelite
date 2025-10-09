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
        if (profileCards[profileNumber] != null)
        {
            Debug.LogWarning("Profile number " + profileNumber + " already exists.");
            return;
        }

        //Create Profile Card
        GameObject newCard = Instantiate(profileCardPrefab, this.transform);
        ProfileCard cardScript = newCard.GetComponent<ProfileCard>();
        cardScript.Initialize(profileNumber);
        profileCards[profileNumber] = newCard;

        //Set Transform
        newCard.transform.position = addSlots[profileNumber].transform.position;

        //Disable Add Slot for profile number
        addSlots[profileNumber].SetActive(false);
    }

    private void CreateExistingProfiles()
    {

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

}
