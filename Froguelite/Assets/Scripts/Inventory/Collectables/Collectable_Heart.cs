using UnityEngine;

public class Collectable_Heart : GroundCollectable
{

    // Collectable_Heart is a collectable for the player to regain health.

    public override void OnCollect()
    {
        StatsManager.Instance.playerHealth.DamagePlayer(-1);
        Destroy(gameObject);
    }
}
