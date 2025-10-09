using UnityEngine;

public class ProfileCard : MonoBehaviour
{
    //Controls card behavior in profile selection menu

    #region VARIABLES

    [SerializeField] private int profileNumber; //1, 2 or 3

    [SerializeField] private string sceneToLoad; //TO DO: populate with saved data from profile

    #endregion

    #region SETUP

    public void Initialize(int profile)
    {
        profileNumber = profile;
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
        LevelManager.Instance.LoadScene(sceneToLoad);
    }

    public void OnDeleteProfile()
    {
        ProfileUIManager.Instance.DeleteProfile(profileNumber);
    }

    #endregion
}
