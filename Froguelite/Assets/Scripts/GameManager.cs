using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    // GameManager is the central manager of the game


    #region VARIABLES


    public static GameManager Instance { get; private set; }

    [SerializeField] private int seed;

    public enum PlayerState
    {
        Exploring,
        InCombat,
        InBubble,
        InMenu,
        Dead
    }

    public PlayerState currentPlayerState { get; private set; } = PlayerState.Exploring;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Awake, setup singleton
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }


    #endregion


    #region PLAYER STATE MANAGEMENT


    // Sets the current player state
    public void SetPlayerState(PlayerState newState)
    {
        currentPlayerState = newState;
    }

    public void OnDeath()
    {
        SetPlayerState(PlayerState.Dead);
        UIManager.Instance.ShowDeathScreen();
    }

    public void OnWin()
    {
        StartCoroutine(WinRoutine());
    }
    
    private IEnumerator WinRoutine()
    {
        yield return new WaitForSeconds(4f);
        UIManager.Instance.ShowWinScreen();
    }


    #endregion


}
