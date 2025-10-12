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

    [SerializeField] private TextMeshProUGUI profileNameText;

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

        if(profileNameText != null)
        {
            profileNameText.text = profileData.name;
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

    #endregion
}
