using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileCard : MonoBehaviour
{
    //Controls card behavior in profile selection menu

    #region VARIABLES

    //[SerializeField] private int profileNumber; //1, 2 or 3

    //private string sceneToLoad;

    private ProfileCardData profileData;

    //[SerializeField] private TextMeshProUGUI profileNameText;

    [SerializeField] private GameObject profileNameInputField;
    [SerializeField] private GameObject profileNameTMP;

    #endregion

    #region SETUP

    //public void Initialize(int profile, string profileScene)
    //{
    //    profileNumber = profile;
    //    sceneToLoad = profileScene;
    //}

    public void Initialize(ProfileCardData data)
    {
        profileData = data;

        //Check if existing or new profile
        if (profileData.name != null)
        {
            profileNameTMP.GetComponent<TextMeshProUGUI>().text = profileData.name;
            ShowNameTMP();
        } else
        {
            ShowNameInputField();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #endregion

    #region CARD BEHAVIOR

    public void OnStartProfile()
    {
        //LevelManager.Instance.LoadScene(sceneToLoad);
        UIManager.Instance.OnProfileStartClick(profileData.sceneToLoad);
    }

    public void OnDeleteProfile()
    {
        ProfileUIManager.Instance.DeleteProfile(profileData);
    }

    public void UpdateProfileName(string name)
    {
        //Update name in profile data and display it
        profileData.name = name;
        profileNameTMP.GetComponent<TextMeshProUGUI>().text = name;

        //Unsubscribe from event and hide input field
        profileNameInputField.GetComponent<InputFieldGrabber>().OnInputGrabbed -= UpdateProfileName;
        ShowNameTMP();
    }

    #endregion

    #region HELPER FUNCTIONS
    private void ShowNameInputField()
    {
        //Show input field and subscribe to event
        profileNameInputField.SetActive(true);
        profileNameInputField.GetComponent<InputFieldGrabber>().OnInputGrabbed += UpdateProfileName;
        profileNameTMP.SetActive(false);
    }

    private void ShowNameTMP()
    {
        //Hide input fild and show TMP
        profileNameInputField.SetActive(false);
        profileNameTMP.SetActive(true);
    }

    #endregion
}
