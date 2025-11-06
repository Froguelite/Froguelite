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

    [SerializeField] private Image progressBar;

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

    public async void LoadScene(string sceneName, bool showPortalEffect = false, bool showLoadingScreen = true)
    {
        var scene = SceneManager.LoadSceneAsync(sceneName);
        scene.allowSceneActivation = false;

        //loadingPanel.SetActive(true);

        if (showPortalEffect)
        {
            portalLoadingEffect.StartEffect();
            await Task.Delay(2000); // Wait for portal effect duration
        }

        do
        {
            await Task.Delay(100);
            if (showLoadingScreen)
                progressBar.fillAmount = scene.progress;
        } while (scene.progress < 0.9f);

        if (showPortalEffect)
        {
            await Task.Delay(1000);
            portalLoadingEffect.StopEffect();
        }

        scene.allowSceneActivation = true;

        // Temporary, might need adjustment to be cleaner -
        // If we are loading the main scene, wait for ZoneGenerator to be ready then generate zone
        if (sceneName == "MainScene")
        {
            await GenerateZoneAndSetup();
            UIManager.Instance.OnSceneLoadReturn(UIPanels.None);
        }
        else if (sceneName == "MenuScene")
        {
            GameObject.Destroy(InputManager.Instance.gameObject);
            GameObject.Destroy(MainCanvas.Instance.gameObject);
            GameObject.Destroy(FrogueliteCam.Instance.gameObject);
            GameObject.Destroy(GameManager.Instance.gameObject);
            UIManager.Instance.OnSceneLoadReturn(UIPanels.GameStart);
        }
        else if (sceneName == "BossScene")
        {
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
        }
    }

    private async Task GenerateZoneAndSetup()
    {
        // Wait for ZoneGenerator to be ready
        while (ZoneGenerator.Instance == null)
        {
            await Task.Delay(100);
        }

        // Generate the zone
        ZoneGenerator.Instance.GenerateZone();

        // Wait for the zone to be generated
        while (!ZoneGenerator.Instance.zoneGenerated)
        {
            await Task.Delay(100);
        }
    }
}
