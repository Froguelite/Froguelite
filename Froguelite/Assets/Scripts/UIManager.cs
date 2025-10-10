using UnityEngine;

public class UIManager : MonoBehaviour
{
    #region VARAIBLES

    public static UIManager Instance;

    [SerializeField] private UIPanelObject[] uiPanels; //Array to hold references to different UI panels

    private GameObject currentPanel;

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
        DontDestroyOnLoad(gameObject); // persist across scenes
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize to the starting panel, e.g., GameStart
        int gameStartIndex = (int) UIPanels.GameStart;
        uiPanels[gameStartIndex].panelObject.SetActive(true);
        currentPanel = uiPanels[gameStartIndex].panelObject;

        //Check array index equals UIPanels enum count
        int panelCount = System.Enum.GetNames(typeof(UIPanels)).Length;

        if (uiPanels.Length != panelCount)
        {
            Debug.LogError("UIManager: UIPanels array length does not match UIPanels enum count.");
        }

        for (int i = 0; i < uiPanels.Length; i++)
        {
            if(i != ( (int) uiPanels[i].panel))
            {
                Debug.LogError("UIManager: UIPanels array is not in the correct order. Please ensure the order matches the UIPanels enum.");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion

    #region ON BUTTON CLICK METHODS

    public void OnStartGameClick()
    {
        //Temporary: Switch to Profile Menu
        OnProfilesClick();
    }

    public void OnOptionsClick()
    {

    }

    public void OnProfilesClick()
    {
        //Switch to Profile Menu panel
        int profileMenuIndex = (int)UIPanels.ProfileMenu;
        PanelSwitch(currentPanel, uiPanels[profileMenuIndex].panelObject);

        ////Create profile cards for existing profiles
        //ProfileUIManager.Instance.CreateExistingProfiles();
    }

    public void OnExitClick()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
                Debug.Log("Application Quit");
    }

    #endregion

    #region HELPER METHODS
    private void PanelSwitch(GameObject current, GameObject next)
    {
        //Set current panel to inactive
        current.SetActive(false);

        //Set next panel to active
        next.SetActive(true);

        //Update current panel reference
        currentPanel = next;
    }

    #endregion
}

public enum UIPanels
{
    GameStart,
    //MainMenu,
    //OptionsMenu,
    ProfileMenu,
    InGameHUD,
    //PauseMenu
}

[System.Serializable]
public struct UIPanelObject
{
    public UIPanels panel;
    public GameObject panelObject;
}