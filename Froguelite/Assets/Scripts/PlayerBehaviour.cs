using UnityEngine;

public class playerBehaviour : MonoBehaviour
{
    private bool isDead = false;
    void Start()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayerTakeDmg(20);
            Debug.Log(GameManager.gameManager._playerHealth.Health);
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            PlayerHeal(10);
            Debug.Log(GameManager.gameManager._playerHealth.Health);
        }
    }
    private void PlayerTakeDmg(int dmg)
    {
        if (isDead) return;

        GameManager.gameManager._playerHealth.DmgUnit(dmg);

        // Check for death after applying damage
        if (GameManager.gameManager._playerHealth.Health <= 0)
        {
            Die();
        }
    }
    private void PlayerHeal(int healing)
    {
        GameManager.gameManager._playerHealth.HealUnit(healing);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Player has died.");

        // Disable this behaviour to prevent further input handling
        this.enabled = false;
        if (GameManager.gameManager != null)
        {
            GameManager.gameManager.HandlePlayerDeath();
        }
    }
}
