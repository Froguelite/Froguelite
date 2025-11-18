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

    private int currentZone = 0;
    private int currentSubZone = -1;

    public enum Scenes
    {
        MainScene,
        MenuScene,
        BossScene,
        StumpScene,
    }

    public enum LoadEffect
    {
        None,
        LoadingScreen,
        Portal,
        Bubble,
        Leaves
    }

    private string[] sceneNames = { "MainScene", "MenuScene", "BossScene", "StumpScene" };

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
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion

    public async void LoadScene(Scenes sceneName, LoadEffect loadEffect)
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
        
        var scene = SceneManager.LoadSceneAsync(sceneNames[(int) sceneName]);
        scene.allowSceneActivation = false;

        //loadingPanel.SetActive(true);

        if (loadEffect == LoadEffect.Portal)
        {
            portalLoadingEffect.StartEffect();
            await Task.Delay(2000); // Wait for portal effect duration
        }

        if (loadEffect == LoadEffect.Bubble)
        {
            bubbleLoadingEffect.StartEffect(false);
            await Task.Delay(2000); // Wait for bubble effect duration
        }

        if (loadEffect == LoadEffect.Leaves)
        {
            bubbleLoadingEffect.StartEffect(true);
            await Task.Delay(2000); // Wait for bubble effect duration
        }

        do
        {
            await Task.Delay(100);
            if (loadEffect == LoadEffect.LoadingScreen)
                progressBar.fillAmount = scene.progress;
        } while (scene.progress < 0.9f);

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
                IncrementZoneProgression();
                await GenerateZoneAndSetup(currentZone, currentSubZone);
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
                UIManager.Instance.OnSceneLoadReturn(UIPanels.None);
                
                // Force show the golden fly HUD in the stump scene
                if (GoldenFlyHUD.Instance != null)
                {
                    GoldenFlyHUD.Instance.ForceShow();
                }
                break;
            default:
                FrogueliteCam.Instance.UnconfineCamera();
                UIManager.Instance.OnSceneLoadReturn(UIPanels.None);
                break;
        }

        // Hide any loading effects
        if (loadEffect == LoadEffect.Portal)
        {
            await Task.Delay(1000);
            portalLoadingEffect.StopEffect();
        }

        if (loadEffect == LoadEffect.Bubble || loadEffect == LoadEffect.Leaves)
        {
            bubbleLoadingEffect.StopEffect();
        }

        PlayerMovement.Instance.SetCanMove(true);
        PlayerAttack.Instance.SetCanAttack(true);
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

    private void IncrementZoneProgression()
    {
        currentSubZone++;
        if (currentSubZone > 2)
        {
            currentSubZone = 0;
            currentZone++;
        }
    }

    private void ResetZoneProgression()
    {
        currentZone = 0;
        currentSubZone = -1;
    }
}
