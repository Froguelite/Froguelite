using Unity.Cinemachine;
using UnityEngine;

public class FrogueliteCam : MonoBehaviour
{
    
    public static FrogueliteCam Instance { get; private set; }

    [SerializeField] private CinemachineConfiner2D cinemachineConfiner;

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

    public void ConfineCamToBounds(BoxCollider2D bounds)
    {
        cinemachineConfiner.BoundingShape2D = bounds;
        cinemachineConfiner.InvalidateBoundingShapeCache();
        cinemachineConfiner.enabled = true;
        Debug.Log("Camera confined to bounds");
    }

    public void UnconfineCamera()
    {
        cinemachineConfiner.enabled = false;
        cinemachineConfiner.BoundingShape2D = null;
        Debug.Log("Camera unconfined from bounds");
    }
}
