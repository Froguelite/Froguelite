using UnityEngine;

public class OverlapDamageTester : MonoBehaviour
{

    [SerializeField] private int damageAmount = 1;
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StatsManager.Instance.playerHealth.DamagePlayer(damageAmount);
        }
    }
}
