using UnityEngine;

public class BossPortal : MonoBehaviour
{

    // BossPortal is a script for handling the boss portal functionality

    private bool triggered = false;


    void OnTriggerEnter2D(Collider2D collider)
    {
        if (triggered) return;
        triggered = true;
        
        if (collider.CompareTag("Player") || collider.CompareTag("Tongue"))
        {
            LevelManager.Instance.LoadScene("BossScene");
            
        }
    }
}
