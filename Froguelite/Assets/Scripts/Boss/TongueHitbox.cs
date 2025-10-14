using UnityEngine;

public class TongueHitbox : MonoBehaviour
{
    public int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player hit with tongue, and did {1} damage.");
            StatsManager.Instance.playerHealth.DamagePlayer(1);
        }
    }
}

