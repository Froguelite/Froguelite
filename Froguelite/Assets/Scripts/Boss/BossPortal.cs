using UnityEngine;

public class BossPortal : MonoBehaviour
{

    // BossPortal is a script for handling the boss portal functionality

    private bool triggered = false;


    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player") || collider.CompareTag("Tongue"))
        {
            if (triggered) return;
            triggered = true;
            
            LevelManager.Instance.LoadScene("BossScene");
            
        }
    }
}
