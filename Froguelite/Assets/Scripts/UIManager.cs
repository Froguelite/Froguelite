using UnityEngine;

public class UIManager : MonoBehaviour
{
    #region VARAIBLES

    public static UIManager Instance;

    [SerializeField] private UIPanelObject[] uiPanels; //Array to hold references to different UI panels

    private UIPanels currentPanel;

    private UIPanels previousPanel;

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
        currentPanel = UIPanels.GameStart;
        previousPanel = UIPanels.None;

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

    #region PANEL CONTROL METHODS

    public void OnStartGameClick()
    {
        //Temporary: Switch to Profile Menu
        OnProfilesClick();
    }

    public void OnMainMenuClick()
    {
        //Temporary: Switch to Profile Menu
        OnProfilesClick();
    }

    public void OnBackClick()
    {
        //Switch to previous panel
        PanelSwitch(currentPanel, previousPanel);
    }

    public void OnProfilesClick()
    {
        //Switch to Profile Menu panel
        PanelSwitch(currentPanel, UIPanels.ProfileMenu);

        ////Create profile cards for existing profiles
        //ProfileUIManager.Instance.CreateExistingProfiles();
    }

    public void OnProfileStartClick(string sceneToLoad)
    {
        //Switch to Loading Screen and call LevelManager to load scene
        PanelSwitch(currentPanel, UIPanels.LoadingScreen);
        LevelManager.Instance.LoadScene(sceneToLoad);
    }

    public void OnSceneLoadReturn()
    {
        //Scene loaded, switch to corresponding panel, currently set to Main Menu
        PanelSwitch(currentPanel, UIPanels.MainMenu);
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
    private void PanelSwitch(UIPanels current, UIPanels next)
    {
        //Set current panel to inactive
        int currentIndex = (int)current;
        uiPanels[currentIndex].panelObject?.SetActive(false);

        //Set next panel to active
        int nextIndex = (int)next;
        uiPanels[nextIndex].panelObject?.SetActive(true);

        //Update previous and current panel reference
        if(current != UIPanels.LoadingScreen)
        {
            //Only update previous panel if current panel is not Loading Screen
            previousPanel = current;
        }
        currentPanel = next;
    }

    #endregion
}

public enum UIPanels
{
    None,
    GameStart,
    MainMenu,
    //OptionsMenu,
    ProfileMenu,
    LoadingScreen,
    //InGameHUD,
    //PauseMenu
}

[System.Serializable]
public struct UIPanelObject
{
    public UIPanels panel;
    public GameObject panelObject;
}