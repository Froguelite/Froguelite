using UnityEngine;

public class GameManager : MonoBehaviour
{

    // GameManager is the central manager of the game


    #region VARIABLES


    public static GameManager Instance { get; private set; }


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Awake, setup singleton
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }


    #endregion
    
    
}
