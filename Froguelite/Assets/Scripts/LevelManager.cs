using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Cinemachine;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    #region VARIABLES

    public static LevelManager Instance;

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private PortalLoadingEffect portalLoadingEffect;
    [SerializeField] private BubbleLoadingEffect bubbleLoadingEffect;

    [SerializeField] private Image progressBar;

    public int currentZone {get; private set;} = 1;
    public int currentSubZone {get; private set;} = -1;
    public bool useLoadedVal = true;

    public enum Scenes
    {
        MainScene,
        MenuScene,
        BossScene,
        StumpScene,
        MinibossRushScene
    }

    public enum LoadEffect
    {
        None,
        LoadingScreen,
        Portal,
        Bubble,
        Leaves
    }

    //private string[] sceneNames = { "MainScene", "MenuScene", "BossScene", "StumpScene", "MinibossRushScene" };
    private string[] sceneNames = { "TestMainScene-AA", "TestMenuScene-AA", "TestBossScene-AA", "TestStumpScene-AA", "MinibossRushScene" }; //For testing purposes

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

        //Subscribe to save and load
        SaveManager.SaveData += SaveZones;
        SaveManager.LoadZone += LoadZones;
    }

    private void OnDestroy()
    {
        //Unsubscribe to save and load
        SaveManager.SaveData -= SaveZones;
        SaveManager.LoadZone -= LoadZones;

        //Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ManuallySetCurrentZone(int zone)
    {
        currentZone = zone;
    }

    #endregion

    public async Task LoadScene(Scenes sceneName, LoadEffect loadEffect)
    {
        // Release force show on golden fly HUD when leaving any scene
        if (GoldenFlyHUD.Instance != null)
        {
            GoldenFlyHUD.Instance.ReleaseForceShow();
        }

        if (loadEffect == LoadEffect.LoadingScreen)
        {
            UIManager.Instance.PanelSwitch(UIPanels.LoadingScreen);
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        var scene = SceneManager.LoadSceneAsync(sceneNames[(int) sceneName]);
        scene.allowSceneActivation = false;

        //loadingPanel.SetActive(true);

        if (loadEffect == LoadEffect.Portal)
        {
            AudioManager.Instance.PlaySoundIndefinite(TravelSound.PortalTravel);
            portalLoadingEffect.StartEffect();
            await Task.Delay(2000); // Wait for portal effect duration
        }

        if (loadEffect == LoadEffect.Bubble)
        {
            AudioManager.Instance.PlaySoundIndefinite(TravelSound.BubbleTravel);
            bubbleLoadingEffect.StartEffect(false);
            await Task.Delay(2000); // Wait for bubble effect duration
        }

        if (loadEffect == LoadEffect.Leaves)
        {
            AudioManager.Instance.PlaySoundIndefinite(TravelSound.LeafTravel);
            bubbleLoadingEffect.StartEffect(true);
            await Task.Delay(2000); // Wait for bubble effect duration
        }

        if(loadEffect == LoadEffect.LoadingScreen)
        {
            do
            {
                await Task.Delay(100);
                progressBar.fillAmount = scene.progress;
            } while (scene.progress < 0.9f);
        }

        scene.allowSceneActivation = true;

        Time.timeScale = 1f;

        await Task.Delay(100); // Small delay to ensure scene has loaded

        // Temporary, might need adjustment to be cleaner -
        // Load specific setups per scene
        switch(sceneName)
        {
            case Scenes.MainScene:
                FrogueliteCam.Instance.UnconfineCamera();
                // TODO: Needs to load a value or something
                if (!useLoadedVal)
                {
                    IncrementZoneProgression();
                }
                //change start scene from stump to main if 1 sub zone completed
                if(currentZone == 1 && currentSubZone == 0)
                {
                    int profileNumber = SaveManager.activeProfile;
                    ProfileUIManager.Instance.UpdateSceneToLoadForProfile(profileNumber, Scenes.MainScene);
                    Debug.Log("Changed load scene to MainScene");
                }

                await GenerateZoneAndSetup(currentZone, currentSubZone);
                if (!useLoadedVal)
                {
                    Debug.Log("Not using loaded value, save data at this stage instead");
                    SaveManager.WriteToFile(); //Save when entering a game scene might conflict with loading
                } 
                //else
                //{
                //    //Generation with loaded zone complete, return to normal
                //    useLoadedVal = false;
                //}
                UIManager.Instance.OnSceneLoadReturn(UIPanels.None);
                break;
            case Scenes.MenuScene:
                ResetZoneProgression();
                FrogueliteCam.Instance.UnconfineCamera();
                GameObject.Destroy(InputManager.Instance.gameObject);
                GameObject.Destroy(MainCanvas.Instance.gameObject);
                GameObject.Destroy(FrogueliteCam.Instance.gameObject);
                GameObject.Destroy(GameManager.Instance.gameObject);
                UIManager.Instance.OnSceneLoadReturn(UIPanels.GameStart);
                break;
            case Scenes.BossScene:
                FrogueliteCam.Instance.UnconfineCamera();
                PlayerMovement.Instance.transform.position = new Vector3(0.46f, -7.16f, 0);
                MinimapManager.Instance.HideMinimap();

                // Find and update all active CinemachineCamera components
                CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
                foreach (CinemachineCamera cam in cameras)
                {
                    if (cam.isActiveAndEnabled)
                    {
                        // Force the camera to update its position immediately
                        cam.ForceCameraPosition(PlayerMovement.Instance.transform.position, Quaternion.identity);
                        
                        // Manually update the camera's internal state
                        cam.UpdateCameraState(Vector3.up, Time.deltaTime);
                    }
                }
                UIManager.Instance.OnSceneLoadReturn(UIPanels.None);
                break;
            case Scenes.StumpScene:
                ResetZoneProgression();
                FindAnyObjectByType<StumpManager>().LoadStump();
                GameManager.Instance.InvokeReset();
                if (!useLoadedVal)
                {
                    //Mainly used for reset and saving after winning the game
                    Debug.Log("Not using loaded value, allowed for saved data to be overwritten");
                    SaveManager.WriteToFile(); //Save when entering stump scene
                    //Debug.Log("Calling load power up");
                    //PowerUpManager.Instance.LoadPowerUps(); //Values are already loaded from file, safe to call
                }
                //SaveManager.WriteToFile(); //Save when entering stump scene
                UIManager.Instance.OnSceneLoadReturn(UIPanels.None);

                useLoadedVal = false; //if start of profile play

                // Force show the golden fly HUD in the stump scene
                if (GoldenFlyHUD.Instance != null)
                {
                    GoldenFlyHUD.Instance.ForceShow();
                }
                
                int profileNum = SaveManager.activeProfile;
                ProfileUIManager.Instance.UpdateSceneToLoadForProfile(profileNum, Scenes.StumpScene);
                Debug.Log("Changed load scene to StumpScene");

                StatsManager.Instance.playerHealth.SetPlayerAlive(); //For avoiding taking damage after player died
                break;
            case Scenes.MinibossRushScene:
                FrogueliteCam.Instance.UnconfineCamera();
                PlayerMovement.Instance.transform.position = new Vector3(14.48f, 11.66f, 0);
                MinimapManager.Instance.HideMinimap();

                // Find and update all active CinemachineCamera components
                CinemachineCamera[] camera = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
                foreach (CinemachineCamera cam in camera)
                {
                    if (cam.isActiveAndEnabled)
                    {
                        // Force the camera to update its position immediately
                        cam.ForceCameraPosition(PlayerMovement.Instance.transform.position, Quaternion.identity);
                        
                        // Manually update the camera's internal state
                        cam.UpdateCameraState(Vector3.up, Time.deltaTime);
                    }
                }
                FindAnyObjectByType<MinibossRushHandler>().StartMinibossRush();
                UIManager.Instance.OnSceneLoadReturn(UIPanels.None);
                break;
            default:
                FrogueliteCam.Instance.UnconfineCamera();
                UIManager.Instance.OnSceneLoadReturn(UIPanels.None);
                break;
        }

        // Restart ambiant particles, if pertinent
        AmbiantParticleHandler ambiantParticleHandler = FindAnyObjectByType<AmbiantParticleHandler>();
        if (ambiantParticleHandler != null)
        {
            ambiantParticleHandler.ResetAmbiantParticles(sceneName == Scenes.MainScene || sceneName == Scenes.MinibossRushScene, currentZone);
        }

        // Hide any loading effects
        if (loadEffect == LoadEffect.Portal)
        {
            await Task.Delay(1000);
            AudioManager.Instance.StopIndefiniteSound(TravelSound.PortalTravel);
            portalLoadingEffect.StopEffect();
        }

        if (loadEffect == LoadEffect.Bubble || loadEffect == LoadEffect.Leaves)
        {
            AudioManager.Instance.StopIndefiniteSound(TravelSound.BubbleTravel);
            AudioManager.Instance.StopIndefiniteSound(TravelSound.LeafTravel);
            bubbleLoadingEffect.StopEffect();
        }

        // Call to display the current level
        StartCoroutine(DisplayCurrentLevel(sceneName));

        // Play music
        AudioManager.Instance.ClearOverrideMusic();
        PlayMusicForScene(sceneName);

        //Generation with loaded zone complete, return to normal
        useLoadedVal = false;

        PlayerMovement.Instance.SetCanMove(true);
        PlayerAttack.Instance.SetCanAttack(true);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        if (scene.name == sceneNames[(int) Scenes.StumpScene])
        {
            PowerUpManager.Instance.LoadPowerUps();
            Debug.Log("Calling load power up on scene loaded");
        }
    }

    private async Task GenerateZoneAndSetup(int zone, int subZone)
    {
        // Wait for ZoneGenerator to be ready
        while (ZoneGeneratorAsync.Instance == null)
        {
            await Task.Delay(100);
        }

        // Generate the zone
        StartCoroutine(ZoneGeneratorAsync.Instance.GenerateZoneAsync(zone, subZone));

        // Wait for the zone to be generated
        while (!ZoneGeneratorAsync.Instance.zoneGenerated)
        {
            await Task.Delay(100);
        }

        PlayerMovement.Instance.EnableCollision();
        PlayerMovement.Instance.playerSpriteRenderer.maskInteraction = SpriteMaskInteraction.None;

        MinimapManager.Instance.ShowMinimap();
        GameManager.Instance.SetPlayerState(GameManager.PlayerState.Exploring);
    }

    private IEnumerator DisplayCurrentLevel(Scenes sceneName)
    {
        if (sceneName != Scenes.MainScene && sceneName != Scenes.MinibossRushScene && sceneName != Scenes.StumpScene && sceneName != Scenes.BossScene)
        {
            yield break;
        }

        yield return new WaitForSeconds(1.5f);

        switch (sceneName)
        {
            case Scenes.MainScene:
                if (currentZone == 1)
                {
                    CollectionOverlayHandler.Instance.ShowGenericText("The Breezy Brush", "Zone " + (currentSubZone + 1) + " / 3");
                }
                else
                {
                    CollectionOverlayHandler.Instance.ShowGenericText("The Soggy Swamp", "Zone " + (currentSubZone + 1) + " / 3");
                }
                break;
            case Scenes.MinibossRushScene:
                CollectionOverlayHandler.Instance.ShowGenericText("Ambushed!!!", "");
                break;
            case Scenes.StumpScene:
                CollectionOverlayHandler.Instance.ShowGenericText("The Stump", "");
                break;
            case Scenes.BossScene:
                CollectionOverlayHandler.Instance.ShowGenericText("The King of the Swamp", "");
                break;
        }
    }

    private void PlayMusicForScene(Scenes sceneName)
    {
        switch (sceneName)
        {
            case Scenes.MainScene:
                if (currentZone == 1)
                {
                    AudioManager.Instance.PlayMusic(MusicType.ForestZone);
                }
                else
                {
                    AudioManager.Instance.PlayMusic(MusicType.SwampZone);
                }
                break;
            case Scenes.MenuScene:
                AudioManager.Instance.PlayMusic(MusicType.Menu);
                break;
            case Scenes.BossScene:
                AudioManager.Instance.PlayMusic(MusicType.Boss);
                break;
            case Scenes.StumpScene:
                AudioManager.Instance.PlayMusic(MusicType.Stump);
                break;
            case Scenes.MinibossRushScene:
                AudioManager.Instance.PlayMusic(MusicType.Boss);
                break;
        }
    }

    private void IncrementZoneProgression()
    {
        currentSubZone++;
        if (currentSubZone > 2)
        {
            currentSubZone = 0;
            currentZone--;
        }
    }

    private void ResetZoneProgression()
    {
        currentZone = 1;
        currentSubZone = -1;
    }

    #region SAVE AND LOAD ZONES AND SUBZONES
    //Get values from playerHealth script assuming it will be the most up to date
    private void SaveZones()
    {
        SaveManager.SaveForProfile<int>(SaveVariable.CurrentZone, currentZone);
        SaveManager.SaveForProfile<int>(SaveVariable.CurrentSubZone, currentSubZone);
        Debug.Log($"Saved zone {currentZone}, subzone {currentSubZone}");
    }

    private void LoadZones()
    {
        //Load player's current zone
        try
        {
            currentZone = SaveManager.LoadForProfile<int>(SaveVariable.CurrentZone);
            Debug.Log($"[LevelManager] Loaded {currentZone} as player current zone from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // No saved data yet, use default value 0
            currentZone = 0;
            Debug.Log($"[LevelManager] No saved current zone found, defaulting to 0");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[LevelManager] Failed to load current zone: {ex.Message}");
            currentZone = 0;
        }

        //Load player's current subzone
        try
        {
            currentSubZone = SaveManager.LoadForProfile<int>(SaveVariable.CurrentSubZone);
            Debug.Log($"[LevelManager] Loaded {currentSubZone} as player current subzone from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // No saved data yet, use default value 0
            currentSubZone = 0;
            Debug.Log($"[LevelManager] No saved current subzone found, defaulting to 0");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[LevelManager] Failed to load current subzone: {ex.Message}");
            currentSubZone = 0;
        }
    }

    #endregion
}
