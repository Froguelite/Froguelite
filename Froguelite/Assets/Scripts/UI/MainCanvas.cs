using UnityEngine;

public class MainCanvas : MonoBehaviour
{
    
    public static MainCanvas Instance { get; private set; }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}
