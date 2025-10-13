using UnityEngine;

public class BossTemp : MonoBehaviour
{

    // BossTemp is a temporary script for testing boss functionality


    void OnTriggerEnter2D(Collider2D collider)
    {
            GameManager.Instance.OnWin();
            Destroy(gameObject);
    }
}
