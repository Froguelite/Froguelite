using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    #region VARIABLES

    public static LevelManager Instance;

    [SerializeField] private GameObject loadingPanel;

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

    public async void LoadScene(string sceneName)
    {
        var scene = SceneManager.LoadSceneAsync(sceneName);
        scene.allowSceneActivation = false;

        //loadingPanel.SetActive(true);

        do
        {
            await Task.Delay(100);
            progressBar.fillAmount = scene.progress;
        } while (scene.progress < 0.9f);

        await Task.Delay(500); //For demo purposes

        scene.allowSceneActivation = true;

        // Temporary, might need adjustment to be cleaner -
        // If we are loading the main scene, wait for ZoneGenerator to be ready then generate zone
        if (sceneName == "MainScene")
        {
            await GenerateZoneAndSetup();
            UIManager.Instance.OnSceneLoadReturn(UIPanels.None);
        }
        else
        {
            UIManager.Instance.OnSceneLoadReturn(UIPanels.GameStart);
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
