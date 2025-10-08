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

        loadingPanel.SetActive(true);

        do
        {
            await Task.Delay(100); //For demo purposes
            progressBar.fillAmount = scene.progress;
           // await System.Threading.Tasks.Task.Yield();
        } while (scene.progress < 0.9f);

        await Task.Delay(500); //For demo purposes

        scene.allowSceneActivation = true;
        loadingPanel.SetActive(false);
    }
}
