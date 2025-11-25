using UnityEngine;

[CreateAssetMenu(fileName = "New Shield Flies Effect", menuName = "Froguelite/Power Fly Effects/Shield Flies")]
public class PFE_ShieldFlies : PowerFlyEffect
{
    public override void ApplyEffect()
    {
        // Find or create ShieldManager
        ShieldManager manager = ShieldManager.Instance;

        if (manager == null)
        {
            // Attach manager to player if it doesn't exist
            if (PlayerMovement.Instance != null)
            {
                manager = PlayerMovement.Instance.gameObject.AddComponent<ShieldManager>();
            }
            else
            {
                Debug.LogError("PFE_ShieldFlies: Cannot find player to attach ShieldManager!");
                return;
            }
        }

        // Add one shield
        manager.AddShield();
    }
}

