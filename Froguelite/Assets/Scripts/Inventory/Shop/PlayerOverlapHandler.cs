using UnityEngine;
using UnityEngine.Events;

public class OverlapEventHandler : MonoBehaviour
{

    // PlayerOverlapHandler sends out overlap events caused by the player


    #region VARIABLES


    public UnityEvent onPlayerOverlap;
    public bool onlyOnce = false;

    private bool alreadyTriggered = false;


    #endregion


    #region OVERLAP


    // On overlap, send the event if pertinent
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (onlyOnce && alreadyTriggered) return;

        if (collision.CompareTag("Player") || collision.CompareTag("Tongue"))
        {
            alreadyTriggered = true;
            onPlayerOverlap?.Invoke();
        }
    }


    #endregion
}
