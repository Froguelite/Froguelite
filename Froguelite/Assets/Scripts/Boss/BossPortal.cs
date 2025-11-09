using System.Collections;
using UnityEngine;

public class BossPortal : MonoBehaviour
{

    // BossPortal is a script for handling the boss portal functionality

    private bool triggered = false;
    [SerializeField] private Transform portalSuckPoint;


    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player") || collider.CompareTag("Tongue"))
        {
            PlayerAttack.Instance.StopTongueExtension();
            if (triggered) return;
            triggered = true;

            //LevelManager.Instance.LoadScene("BossScene");
            //Change to use enum instead of string
            LevelManager.Instance.LoadScene(LevelManager.Scenes.BossScene, true, false);
        }
    }
}
