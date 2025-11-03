using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    #region VARAIBLES

    public static UIManager Instance;

    [SerializeField] private UIPanelObject[] uiPanels; //Array to hold references to different UI panels
    [SerializeField] private CanvasGroup deathScreenMainCanvasGroup, deathScreenTextCanvasGroup;
    [SerializeField] private CanvasGroup winScreenMainCanvasGroup, winScreenTextCanvasGroup;
    [SerializeField] private bool displayOnStart = true;

    [SerializeField] private InputActionReference pause;
    private bool isPaused;

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

        //Subscribe to pause input action
        pause.action.performed += OnPauseClick;
        pause.action.Enable();
    }

    void OnDestroy()
    {
        //Unsubscribe from pause input action
        pause.action.performed -= OnPauseClick;
        pause.action.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        previousPanel = UIPanels.None;

        if (!displayOnStart)
            return;
            
        // Initialize to the starting panel, e.g., GameStart
        int gameStartIndex = (int)UIPanels.GameStart;
        uiPanels[gameStartIndex].panelObject.SetActive(true);
        currentPanel = UIPanels.GameStart;

        //Check array index equals UIPanels enum count
        int panelCount = System.Enum.GetNames(typeof(UIPanels)).Length;

        if (uiPanels.Length != panelCount)
        {
            Debug.LogError("UIManager: UIPanels array length does not match UIPanels enum count.");
        }

        for (int i = 0; i < uiPanels.Length; i++)
        {
            if (i != ((int)uiPanels[i].panel))
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

    // Resets the game, returning to the menu
    public void ResetGame()
    {
        PanelSwitch(UIPanels.LoadingScreen);
        LevelManager.Instance.LoadScene(LevelManager.Scenes.MenuScene);
    }

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
        //Switch to start panel if current == profile and previous == pause
        //  Will happen when game is quit, cannot go back to game then
        if(currentPanel == UIPanels.ProfileMenu && previousPanel == UIPanels.PauseMenu)
        {
            PanelSwitch(UIPanels.GameStart);
            return;
        }

        //Switch to previous panel
        PanelSwitch(previousPanel);
    }

    public void OnProfilesClick()
    {
        //Switch to Profile Menu panel
        PanelSwitch(UIPanels.ProfileMenu);

        ////Create profile cards for existing profiles
        //ProfileUIManager.Instance.CreateExistingProfiles();
    }

    public void OnQuitClick()
    {
        //Temporarily Quit to Profile Menu
        OnProfilesClick();
    }

    public void OnSettingsClick()
    {
        //Switch to Settings Panel
        PanelSwitch(UIPanels.SettingsScreen);
    }

    //public void OnProfileStartClick(string sceneToLoad)
    //{
    //    //Switch to Loading Screen and call LevelManager to load scene
    //    PanelSwitch(UIPanels.LoadingScreen);
    //    LevelManager.Instance.LoadScene(sceneToLoad);
    //}

    //Use enum instead string for setting scene to load
    public void OnProfileStartClick(LevelManager.Scenes sceneToLoad)
    {
        //Switch to Loading Screen and call LevelManager to load scene
        PanelSwitch(UIPanels.LoadingScreen);
        LevelManager.Instance.LoadScene(sceneToLoad);
    }

    public void OnSceneLoadReturn(UIPanels panelToReturnTo)
    {
        //Scene loaded, switch to corresponding panel
        PanelSwitch(panelToReturnTo);
    }

    private void OnPauseClick(InputAction.CallbackContext obj)
    {
        //Check if clicked during game
        if(currentPanel != UIPanels.None)
        {
            return;
        }
        
        Time.timeScale = 0f;

        //Temporary: Switch to Main Menu panel
        PanelSwitch(UIPanels.PauseMenu);

        //TO DO: Save game state if needed
    }

    public void OnResumeClick()
    {
        Time.timeScale = 1f;
        //Switch back to previous panel
        PanelSwitch(UIPanels.None);

        //Time.timeScale = 1f;
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

    #region DEATH SCREEN AND WIN SCREEN

    public void ShowDeathScreen()
    {
        StartCoroutine(DeathScreenCo());
    }

    public void ShowWinScreen()
    {
        StartCoroutine(WinScreenCo());
    }

    private IEnumerator DeathScreenCo()
    {
        float deathTextBigScale = 1.3f;

        LeanTween.cancel(deathScreenMainCanvasGroup.gameObject);
        LeanTween.cancel(deathScreenTextCanvasGroup.gameObject);
        deathScreenMainCanvasGroup.alpha = 0f;
        deathScreenTextCanvasGroup.alpha = 0f;
        deathScreenTextCanvasGroup.transform.localScale = Vector3.one * deathTextBigScale;

        PanelSwitch(UIPanels.DeathScreen);

        deathScreenMainCanvasGroup.LeanAlpha(1f, 0.5f);
        yield return new WaitForSeconds(0.5f);

        deathScreenTextCanvasGroup.transform.LeanScale(Vector3.one, 0.5f).setEaseInCubic();
        deathScreenTextCanvasGroup.LeanAlpha(1f, 0.5f);

        yield return new WaitForSeconds(4f);

        deathScreenTextCanvasGroup.LeanAlpha(0f, 0.5f);
        deathScreenTextCanvasGroup.transform.LeanScale(Vector3.one * deathTextBigScale, 0.5f).setEaseOutCubic();

        yield return new WaitForSeconds(1.5f);

        ResetGame();
    }

    private IEnumerator WinScreenCo()
    {
        float winTextBigScale = 1.3f;

        LeanTween.cancel(winScreenMainCanvasGroup.gameObject);
        LeanTween.cancel(winScreenTextCanvasGroup.gameObject);
        winScreenMainCanvasGroup.alpha = 0f;
        winScreenTextCanvasGroup.alpha = 0f;
        winScreenTextCanvasGroup.transform.localScale = Vector3.one * winTextBigScale;

        PanelSwitch(UIPanels.WinScreen);

        winScreenMainCanvasGroup.LeanAlpha(1f, 0.5f);
        yield return new WaitForSeconds(0.5f);

        winScreenTextCanvasGroup.transform.LeanScale(Vector3.one, 0.5f).setEaseInCubic();
        winScreenTextCanvasGroup.LeanAlpha(1f, 0.5f);

        yield return new WaitForSeconds(4f);

        winScreenTextCanvasGroup.LeanAlpha(0f, 0.5f);
        winScreenTextCanvasGroup.transform.LeanScale(Vector3.one * winTextBigScale, 0.5f).setEaseOutCubic();

        yield return new WaitForSeconds(1.5f);

        ResetGame();
    }

    #endregion

    #region HELPER METHODS
    private void PanelSwitch(UIPanels next)
    {
        //Set current panel to inactive
        int currentIndex = (int)currentPanel;
        if (uiPanels[currentIndex].panelObject != null)
            uiPanels[currentIndex].panelObject.SetActive(false);

        //Set next panel to active
        int nextIndex = (int)next;
        if (uiPanels[nextIndex].panelObject != null)
            uiPanels[nextIndex].panelObject.SetActive(true);

        //Update previous and current panel reference
        if (currentPanel != UIPanels.LoadingScreen)
        {
            //Only update previous panel if current panel is not Loading Screen
            previousPanel = currentPanel;
        }
        currentPanel = next;
    }

    #endregion
}

public enum UIPanels
{
    None,
    GameStart,
    PauseMenu,
    //OptionsMenu,
    ProfileMenu,
    LoadingScreen,
    DeathScreen,
    WinScreen,
    SettingsScreen
    //InGameHUD,
    //PauseMenu
}

[System.Serializable]
public struct UIPanelObject
{
    public UIPanels panel;
    public GameObject panelObject;
}