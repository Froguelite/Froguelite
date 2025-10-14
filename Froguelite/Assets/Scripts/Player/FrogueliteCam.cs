using UnityEngine;

public class FrogueliteCam : MonoBehaviour
{
    
    public static FrogueliteCam Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}
