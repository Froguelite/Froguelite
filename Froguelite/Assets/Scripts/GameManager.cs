using System;
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

    public static event Action ResetPlayerState;
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

    public void InvokeReset()
    {
        ResetPlayerState?.Invoke();
    }

    public void OnDeath()
    {
        SetPlayerState(PlayerState.Dead);
        UIManager.Instance.ShowDeathScreen();

        AudioManager.Instance.PlayOverrideMusic(MusicType.Defeat);

        PlayerMovement.Instance.SetCanMove(false);
        PlayerAttack.Instance.SetCanAttack(false);
        ResetPlayerState?.Invoke();
    }

    public void OnWin()
    {
        ResetPlayerState?.Invoke();
        StartCoroutine(WinRoutine());
    }
    
    private IEnumerator WinRoutine()
    {
        // TEMPORARY: Load stump scene after win
        yield return new WaitForSeconds(10f);
        //Supress await _=
        _ = LevelManager.Instance.LoadScene(LevelManager.Scenes.StumpScene, LevelManager.LoadEffect.Portal);
    }


    #endregion


}
