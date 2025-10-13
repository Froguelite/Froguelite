using UnityEngine;

public class TongueHitbox : MonoBehaviour
{
    public int damage = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player hit with tongue, and did {damage} damage.");
            //other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }
}

